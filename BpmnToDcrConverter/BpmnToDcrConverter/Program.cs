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
            args = new string[1];
            args[0] = @"C:\Users\dn\Downloads\new-bpmn-diagram4.bpmn";

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
