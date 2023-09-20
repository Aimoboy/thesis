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
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || args.Length > 2)
            {
                throw new ArgumentException("Need one or two arguments. The first should be the path to the BPMN XML file, and the second should be the output path. If only one is given it will save the output to the same location as the input file.");
            }

            if (args.Length == 1)
            {
                string arg = args[0];
                args = new string[2];
                args[0] = arg;

                if (arg.Contains('\\'))
                {
                    string[] split = arg.Split('\\');
                    string path = Path.Combine(split.Take(split.Count() - 1).ToArray());
                    string fileName = split.Last();
                    string fileNameWithoutExtension = fileName.Split('.')[0];
                    args[1] = Path.Combine(path, fileNameWithoutExtension + ".json");
                }
                else
                {
                    string fileNameWithoutExtension = arg.Split('.')[0];
                    args[1] = fileNameWithoutExtension + ".xml";
                }
            }

            BpmnGraph bpmnGraph = BpmnXmlParser.Parse(args[0]);
            DcrGraph dcrGraph = Converter.ConvertBpmnToDcr(bpmnGraph);
            //dcrGraph.Export(args[1]);
            JsonExporter.ExportToFile(dcrGraph, args[1]);
        }
    }
}

// TODO:
// - Handle exclusive gateway condition expression
// - Handle message arrows
// - Activities pointing to an end event should be pending
// - Exclusive gateway into end event should conditionally exclude all others
// - End event in parallel gateway path should exclude other paths
// - Start event directly into exclusive gateway
