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
                    AuthenticationHeaderValue authenticationHeader = DcrSolutionsPostRequestHandler.GetDcrSolutionsAuthenticationHeader();
                    DcrSolutionsPostRequestHandler.PostGraph(dcrGraph, authenticationHeader);
                    break;
            }
        }

        private static ArgumentParsingResults HandleArguments(string[] args)
        {
            OutputType outputType = OutputType.XML;

            if (args.Length == 0)
            {
                throw new ArgumentException("Too few arguments. Please specify the path to the file you want to convert.");
            }

            // Handle flags
            if (args.Length >= 2)
            {
                foreach (string arg in args[1..])
                {
                    if (arg == "--dcrsolutions")
                    {
                        outputType = OutputType.DcrSolutionsPost;
                    }
                    else if (arg == "--xml")
                    {
                        outputType = OutputType.XML;
                    }
                    else
                    {
                        throw new ArgumentException($"Argument \"{arg}\" not recognized.");
                    }
                }
            }

            string path = args[0];
            if (!File.Exists(path))
            {
                throw new ArgumentException($"The given path \"{path}\" is not valid");
            }

            return new ArgumentParsingResults
            {
                Folder = Path.GetDirectoryName(path),
                File = Path.GetFileName(path),
                OutputType = outputType
            };
        }
    }

    public class ArgumentParsingResults
    {
        public string Folder;
        public string File;
        public OutputType OutputType;
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
// - Parse extra parentheses
// - Parse NOT ex. !(x < 9)
