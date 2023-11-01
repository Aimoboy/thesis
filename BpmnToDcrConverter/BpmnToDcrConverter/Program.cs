using BpmnToDcrConverter.Bpmn;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using BpmnToDcrConverter.Dcr;
using System.Net.Http.Headers;
using Sprache;

namespace BpmnToDcrConverter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ArgumentParsingResults argumentParsingResults = HandleArguments(args);

            if (argumentParsingResults.CompleteTestPath != null)
            {
                CompleteTest(argumentParsingResults);
                return;
            }

            BpmnGraph bpmnGraph = BpmnXmlParser.Parse(argumentParsingResults.BpmnPath);
            DcrGraph dcrGraph = BpmnToDcrConverter.ConvertBpmnToDcr(bpmnGraph);

            string fileName = Path.GetFileNameWithoutExtension(argumentParsingResults.BpmnPath);
            dcrGraph.Name = fileName;

            switch (argumentParsingResults.OutputType)
            {
                case OutputType.XML:
                    string xmlFileName = fileName + ".xml";
                    string outputPath = Path.Combine(Path.GetDirectoryName(argumentParsingResults.BpmnPath), xmlFileName);
                    dcrGraph.Export(outputPath);
                    break;

                case OutputType.DcrSolutionsPost:
                    try
                    {
                        DcrSolutionsPostCase(argumentParsingResults, dcrGraph);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    break;
            }
        }

        private static void CompleteTest(ArgumentParsingResults argumentParsingResults)
        {
            List<string> fileLines;
            using (StreamReader reader = new StreamReader(argumentParsingResults.CompleteTestPath))
            {
                fileLines = reader.ReadToEnd().Replace("\r", "").Split("\n").Where(x => x != "").ToList();
            }

            if (!fileLines.Any())
            {
                Console.WriteLine("The specified file does not contain any filenames.");
                return;
            }
            
            List<string> duplicateLines = fileLines.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateLines.Any())
            {
                Console.WriteLine("The following filenames not unique:");
                Console.WriteLine(string.Join("\n", duplicateLines.Select(x => "    - " + x)));
                return;
            }

            string dir = Path.GetDirectoryName(argumentParsingResults.CompleteTestPath);
            List<string> files = Directory.GetFiles(dir).Select(x => Path.GetFileName(x)).ToList();

            List<string> missingFiles = new List<string>();
            foreach (string line in fileLines)
            {
                string bpmnFile = line + ".bpmn";
                string traceFile = line + ".trace";

                if (!files.Contains(bpmnFile))
                {
                    missingFiles.Add(bpmnFile);
                }

                if (!files.Contains(traceFile))
                {
                    missingFiles.Add(traceFile);
                }
            }

            if (missingFiles.Any())
            {
                string missingFilesStr = string.Join("\n", missingFiles.Select(x => "    - " + x));

                Console.WriteLine("Missing files:");
                Console.WriteLine(missingFilesStr);
                return;
            }

            AuthenticationHeaderValue authenticationHeader = DcrSolutionsApiHandler.GetDcrSolutionsAuthenticationHeader();

            Dictionary<string, string> lineToGraphXmlDict = new Dictionary<string, string>();
            foreach (string line in fileLines)
            {
                string bpmnGraphXmlPath = Path.Combine(dir, line + ".bpmn");
                BpmnGraph bpmnGraph = BpmnXmlParser.Parse(bpmnGraphXmlPath);
                DcrGraph dcrGraph = BpmnToDcrConverter.ConvertBpmnToDcr(bpmnGraph);

                string graphId = DcrSolutionsApiHandler.CreateGraph(dcrGraph, authenticationHeader);
                string graphXml = DcrSolutionsApiHandler.GetGraphXml(graphId, authenticationHeader);
                lineToGraphXmlDict.Add(line, graphXml);

                DcrSolutionsApiHandler.DeleteGraph(graphId, authenticationHeader);
            }

            Dictionary<string, List<GraphTrace>> lineToTracesDict = new Dictionary<string, List<GraphTrace>>();
            foreach (string line in fileLines)
            {
                string tracePath = Path.Combine(dir, line + ".trace");

                TraceParseResult tracesParseResult;
                using (StreamReader reader = new StreamReader(tracePath))
                {
                    tracesParseResult = GraphTraceParser.TraceResultParser.Parse(reader.ReadToEnd());
                }

                lineToTracesDict[line] = tracesParseResult.ToGraphTraces();
            }

            Console.WriteLine();

            int errorCount = 0;
            foreach (string line in fileLines)
            {
                string graphXml = lineToGraphXmlDict[line];
                List<GraphTrace> traces = lineToTracesDict[line];

                Console.WriteLine(line + ":");

                foreach (GraphTrace trace in traces)
                {
                    string traceXml = trace.ToXml().OuterXml;
                    var (valid, explanation) = DcrSolutionsApiHandler.ValidateLog(graphXml, traceXml, authenticationHeader);

                    if (valid)
                    {
                        Console.WriteLine($"    - {trace.Title} (valid)");
                    }
                    else
                    {
                        Console.WriteLine($"    - {trace.Title} (invalid): {explanation}");
                        errorCount++;
                    }
                }

            }

            Console.WriteLine();
            Console.WriteLine($"Errors: {errorCount}");
        }

        private static void DcrSolutionsPostCase(ArgumentParsingResults argumentParsingResults, DcrGraph dcrGraph)
        {
            List<GraphTrace> graphTraces = new List<GraphTrace>();
            if (argumentParsingResults.TracesPath != null)
            {
                TraceParseResult tracesParseResult = null;
                using (StreamReader reader = new StreamReader(argumentParsingResults.TracesPath))
                {
                    tracesParseResult = GraphTraceParser.TraceResultParser.Parse(reader.ReadToEnd());
                }

                graphTraces = tracesParseResult.ToGraphTraces();

                bool invalidTraces = false;
                foreach (GraphTrace trace in graphTraces)
                {
                    var (isValid, errorMessages) = dcrGraph.ValidateTrace(trace);

                    if (!isValid)
                    {
                        Console.WriteLine($"Trace \"{trace.Title}\" is not valid:");
                        string combinedErrorMessages = string.Join("\n", errorMessages.Select(x => "    - " + x));
                        Console.WriteLine(combinedErrorMessages);

                        invalidTraces = true;
                    }
                }

                if (invalidTraces)
                {
                    throw new Exception("Traces file contains an invalid trace(s).");
                }
            }

            AuthenticationHeaderValue authenticationHeader = DcrSolutionsApiHandler.GetDcrSolutionsAuthenticationHeader();

            string graphId = DcrSolutionsApiHandler.CreateGraph(dcrGraph, authenticationHeader);
            Console.WriteLine($"Created new graph with ID \"{graphId}\".");

            foreach (GraphTrace trace in graphTraces)
            {
                DcrSolutionsApiHandler.PostTrace(graphId, trace, authenticationHeader);
                Console.WriteLine($"Created trace \"{trace.Title}\".");
            }

            if (argumentParsingResults.CleanUpAfter)
            {
                DcrSolutionsApiHandler.DeleteGraph(graphId, authenticationHeader);
                Console.WriteLine($"Deleted graph with ID \"{graphId}\".");
            }


        }

        private static ArgumentParsingResults HandleArguments(string[] args)
        {
            string path = null;
            OutputType outputType = OutputType.None;
            string tracesPath = null;
            bool clean = false;
            string completeTestPath = null;

            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i].ToLower();

                if (arg == "--path")
                {
                    path = args[i + 1];
                }
                else if (arg == "--output")
                {
                    string secondArg = args[i + 1].ToLower();

                    if (secondArg == "xml")
                    {
                        outputType = OutputType.XML;
                    }
                    else if (secondArg == "dcrsolutions")
                    {
                        outputType = OutputType.DcrSolutionsPost;
                    }
                    else
                    {
                        throw new Exception($"{args[i + 1]} is not a valid output type");
                    }
                }
                else if (arg == "--traces")
                {
                    tracesPath = args[i + 1];
                }
                else if (arg == "--clean")
                {
                    string secondArg = args[i + 1].ToLower();
                    if (secondArg == "true")
                    {
                        clean = true;
                    }
                    else if (secondArg == "false")
                    {
                        clean = false;
                    }
                    else
                    {
                        throw new Exception($"{args[i + 1]} is not a valid clean option");
                    }
                }
                else if (arg == "--completetest")
                {
                    completeTestPath = args[i + 1];
                }
                else
                {
                    throw new Exception($"{args[i]} is not a valid flag");
                }

                i += 2;
            }

            if (completeTestPath != null) {
                if (path != null)
                {
                    Console.WriteLine("Warning: Path argument does not do anything if CompleteTest is specified.");
                }

                if (outputType != OutputType.None)
                {
                    Console.WriteLine("Warning: Output argument does not do anything if CompleteTest is specified.");
                }

                if (tracesPath != null)
                {
                    Console.WriteLine("Warning: Traces argument does not do anything if CompleteTest is specified.");
                }

                if (clean)
                {
                    Console.WriteLine("Warning: Clean argument does not do anything if CompleteTest is specified.");
                }

                if (completeTestPath.ToLower() == "default")
                {
                    string currentDir = Directory.GetCurrentDirectory();
                    string targetDir = Path.Combine(currentDir, @"..\..\..\ConversionTests\complete-test");

                    completeTestPath = Path.GetFullPath(targetDir);
                }

                if (!File.Exists(completeTestPath))
                {
                    throw new Exception($"The given path \"{completeTestPath}\" is not valid.");
                }
            }

            if (completeTestPath == null && path == null)
            {
                throw new Exception("You need to give a path to a BPMN XML file like \"--path (path)\"");
            }

            if (outputType != OutputType.DcrSolutionsPost && tracesPath != null)
            {
                Console.WriteLine("Warning: Traces argument does not do anything if the output is not set to DCR Solutions.");
            }

            if (path != null && !File.Exists(path))
            {
                throw new ArgumentException($"The given path \"{path}\" is not valid");
            }

            if (outputType == OutputType.None)
            {
                outputType = OutputType.XML;
            }

            return new ArgumentParsingResults
            {
                BpmnPath = path,
                OutputType = outputType,
                TracesPath = tracesPath,
                CleanUpAfter = clean,
                CompleteTestPath = completeTestPath
            };
        }
    }

    public class ArgumentParsingResults
    {
        public string BpmnPath;
        public OutputType OutputType;
        public string TracesPath;
        public bool CleanUpAfter;
        public string CompleteTestPath;
    }


    public enum OutputType
    {
        None,
        XML,
        DcrSolutionsPost
    }
}

// TODO:
// - Handle message arrows
// - Add pool handling in conversion
// - Loops with BPMN sub processes dont work
// - Export DCR as XML to run trace validation
