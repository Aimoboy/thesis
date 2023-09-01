using BpmnToDcrConverter.BPMN;
using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace BpmnToDcrConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new Exception("Need a single argument that is the location of the BPMN XML file.");
            }

            BpmnGraph res = BpmnXmlParser.Parse(args[0]);
            res.TestGraphValidity();
        }
    }
}
