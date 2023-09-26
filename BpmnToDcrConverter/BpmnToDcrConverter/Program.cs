using BpmnToDcrConverter.Bpmn;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using BpmnToDcrConverter.Dcr;

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
                    DcrSolutionsPostRequestHandler.Post(dcrGraph).GetAwaiter().GetResult();
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
// - Handle exclusive gateway condition expression
// - Handle message arrows
// - Activities pointing to an end event should be pending
// - Exclusive gateway into end event should conditionally exclude all others
// - End event in parallel gateway path should exclude other paths
// - Start event directly into exclusive gateway
// - Set parents in JSON export
// - Handle either outputting as XML or JSON
// - JSON marking output
// - Add excludes betweeen XOR gateway paths in case their cases overlap
// - Add inverse of other path conditions in case XOR gateway has a path with no condition
// - Add pool parsing
// - Add pool handlilng in conversion
