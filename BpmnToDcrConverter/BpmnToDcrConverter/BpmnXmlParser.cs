using BpmnToDcrConverter.BPMN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;

// TODO:
// - Handle exclusive gateway condition expression
// - Handle message arrows
// - Refactor events

namespace BpmnToDcrConverter
{
    public static class BpmnXmlParser
    {
        public static BpmnGraph Parse(string userSpecifiedPath)
        {
            // Find specified file
            string currentDir = Directory.GetCurrentDirectory();
            string filePath;

            if (File.Exists(Path.Combine(currentDir, userSpecifiedPath)))
            {
                filePath = Path.Combine(currentDir, userSpecifiedPath);
            }
            else if (File.Exists(userSpecifiedPath))
            {
                filePath = userSpecifiedPath;
            }
            else
            {
                throw new Exception("Could not find the specified file.");
            }

            // XML name spaces
            XDocument doc = XDocument.Load(filePath);
            XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
            XElement process = doc.Element(bpmn + "definitions").Element(bpmn + "process");

            BpmnGraph graph = new BpmnGraph();

            // Find start events
            IEnumerable<XElement> startEvents = process.Elements(bpmn + "startEvent");
            foreach (XElement item in startEvents)
            {
                string id = item.Attribute("id").Value;
                graph.AddFlowElements(new List<BpmnFlowElement>() { new BpmnEvent(id, BpmnEventType.Start) });
            }

            // Find end events
            IEnumerable<XElement> endEvents = process.Elements(bpmn + "endEvent");
            foreach (XElement item in endEvents)
            {
                string id = item.Attribute("id").Value;
                graph.AddFlowElements(new List<BpmnFlowElement>() { new BpmnEvent(id, BpmnEventType.End) });
            }

            // Find activities
            IEnumerable<XElement> tasks = process.Elements(bpmn + "task");
            foreach (XElement item in tasks)
            {
                string id = item.Attribute("id").Value;
                string name = item.Attribute("name") != null ? item.Attribute("name").Value : "";
                graph.AddFlowElements(new List<BpmnFlowElement>() { new BpmnActivity(id, name) });
            }

            // Find exclusive gateways
            IEnumerable<XElement> exclusiveGateways = process.Elements(bpmn + "exclusiveGateway");
            foreach (XElement item in exclusiveGateways)
            {
                string id = item.Attribute("id").Value;
                graph.AddFlowElements(new List<BpmnFlowElement>() { new BpmnGateway(id, BpmnGatewayType.Or) });
            }

            // Find arrows
            IEnumerable<XElement> sequenceFlows = process.Elements(bpmn + "sequenceFlow");
            foreach (XElement item in sequenceFlows)
            {
                string fromId = item.Attribute("sourceRef").Value;
                string toId = item.Attribute("targetRef").Value;

                BpmnFlowElement from = graph.GetFlowElementFromId(fromId);
                BpmnFlowElement to = graph.GetFlowElementFromId(toId);
                graph.AddArrow(BpmnFlowArrowType.Sequence, from, to);
            }

            return graph;
        }
    }
}
