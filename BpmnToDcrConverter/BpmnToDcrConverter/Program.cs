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
            if (args.Length != 2)
            {
                throw new Exception("Need two arguments. The first should be the path to the BPMN XML file, and the second should be the output path.");
            }

            BpmnGraph bpmnGraph = BpmnXmlParser.Parse(args[0]);
            DcrGraph dcrGraph = Converter.ConvertBpmnToDcr(bpmnGraph);
            dcrGraph.Export(args[1]);
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
