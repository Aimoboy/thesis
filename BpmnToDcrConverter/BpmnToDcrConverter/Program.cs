using BpmnToDcrConverter.Bpmn;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using BpmnToDcrConverter.Dcr;
using System.Net.Http.Headers;

namespace BpmnToDcrConverter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ArgumentParsingResults argumentParsingResults = HandleArguments(args);

            string inputPath = Path.Combine(argumentParsingResults.Folder, argumentParsingResults.File);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(argumentParsingResults.File);

            BpmnGraph bpmnGraph = BpmnXmlParser.Parse(inputPath);
            DcrGraph dcrGraph = BpmnToDcrConverter.ConvertBpmnToDcr(bpmnGraph);
            dcrGraph.Name = fileNameWithoutExtension;

            switch (argumentParsingResults.OutputType)
            {
                case OutputType.XML:
                    string xmlFileName = fileNameWithoutExtension + ".xml";
                    string outputPath = Path.Combine(argumentParsingResults.Folder, xmlFileName);
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

                bool validTraces = false;
                foreach (GraphTrace trace in graphTraces)
                {
                    bool valid = dcrGraph.ValidateTrace(trace);

                    if (!valid)
                    {
                        Console.WriteLine($"Trace \"{trace.Title}\" is not valid.");
                        validTraces = true;
                    }
                }

                if (validTraces)
                {
                    throw new Exception("Traces file contains invalid trace(s).");
                }
            }

            AuthenticationHeaderValue authenticationHeader = DcrSolutionsPostRequestHandler.GetDcrSolutionsAuthenticationHeader();

            string graphId = DcrSolutionsPostRequestHandler.PostGraph(dcrGraph, authenticationHeader);
            Console.WriteLine($"Created new graph with id \"{graphId}\".");

            foreach (GraphTrace trace in graphTraces)
            {
                DcrSolutionsPostRequestHandler.PostTrace(graphId, trace, authenticationHeader);
                Console.WriteLine($"Created trace \"{trace.Title}\".");
            }
        }

        private static ArgumentParsingResults HandleArguments(string[] args)
        {
            string path = null;
            OutputType outputType = OutputType.XML;
            string tracesPath = null;

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
                else
                {
                    throw new Exception($"{args[i]} is not a valid flag");
                }

                i += 2;
            }

            if (path == null)
            {
                throw new Exception("You need to give a path to a BPMN XML file like \"--path (path)\"");
            }

            if (outputType == OutputType.XML && tracesPath != null)
            {
                Console.WriteLine("Warning: Traces does not do anything if the output is set to XML.");
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The given path \"{path}\" is not valid");
            }

            return new ArgumentParsingResults
            {
                Folder = Path.GetDirectoryName(path),
                File = Path.GetFileName(path),
                OutputType = outputType,
                TracesPath = tracesPath
            };
        }
    }

    public class ArgumentParsingResults
    {
        public string Folder;
        public string File;
        public OutputType OutputType;
        public string TracesPath;
    }


    public enum OutputType
    {
        XML,
        DcrSolutionsPost
    }
}

// TODO:
// - Handle message arrows
// - Add excludes betweeen XOR gateway paths in case their conditions overlap
// - Add pool handling in conversion
// - Loops with BPMN sub processes dont work
// - Loops with a single activity dont work
// - Should I simplify BPMN sub processes, by "extracting its contents"
// - Check that the given role can do the activity/transaction
