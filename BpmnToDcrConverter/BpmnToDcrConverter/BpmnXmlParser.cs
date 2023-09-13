using BpmnToDcrConverter.Bpmn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                throw new FileNotFoundException("Could not find the specified file.");
            }

            // XML parsing setup
            XDocument doc = XDocument.Load(filePath);
            XNamespace bpmn = "http://www.omg.org/spec/BPMN/20100524/MODEL";
            XElement process = doc.Element(bpmn + "definitions").Element(bpmn + "process");

            // Get flow elements
            List<BpmnFlowElement> flowElements = GetFlowElements(process, bpmn);
            BpmnGraph graph = new BpmnGraph(flowElements);

            // Get flow arrows
            List<Tuple<BpmnFlowArrowType, string, string>> arrows = GetFlowArrows(process, bpmn);
            foreach (var arrow in arrows)
            {
                BpmnFlowArrowType type = arrow.Item1;
                BpmnFlowElement from = graph.GetFlowElementFromId(arrow.Item2);
                BpmnFlowElement to = graph.GetFlowElementFromId(arrow.Item3);

                graph.AddArrow(type, from, to);
            }

            return graph;
        }

        private static List<BpmnFlowElement> GetFlowElements(XElement xmlElement, XNamespace bpmn)
        {
            List<BpmnFlowElement> flowElements = new List<BpmnFlowElement>();

            // Find start events
            IEnumerable<XElement> startEvents = xmlElement.Elements(bpmn + "startEvent");
            foreach (XElement item in startEvents)
            {
                string id = item.Attribute("id").Value;
                flowElements.Add(new BpmnStartEvent(id));
            }

            // Find end events
            IEnumerable<XElement> endEvents = xmlElement.Elements(bpmn + "endEvent");
            foreach (XElement item in endEvents)
            {
                string id = item.Attribute("id").Value;
                flowElements.Add(new BpmnEndEvent(id));
            }

            // Find activities
            IEnumerable<XElement> tasks = xmlElement.Elements(bpmn + "task");
            foreach (XElement item in tasks)
            {
                string id = item.Attribute("id").Value;
                string name = item.Attribute("name") != null ? item.Attribute("name").Value : "";
                flowElements.Add(new BpmnActivity(id, name));
            }

            // Find exclusive gateways
            IEnumerable<XElement> exclusiveGateways = xmlElement.Elements(bpmn + "exclusiveGateway");
            foreach (XElement item in exclusiveGateways)
            {
                string id = item.Attribute("id").Value;
                flowElements.Add(new BpmnGateway(id, BpmnGatewayType.Or));
            }

            // Find sub processes
            IEnumerable<XElement> subProcesses = xmlElement.Elements(bpmn + "subProcess");
            foreach (XElement item in subProcesses)
            {
                string id = item.Attribute("id").Value;
                List<BpmnFlowElement> nestedElements = GetFlowElements(item, bpmn);
                flowElements.Add(new BpmnSubProcess(id, nestedElements));
            }

            return flowElements;
        }

        private static List<Tuple<BpmnFlowArrowType, string, string>> GetFlowArrows(XElement xmlElement, XNamespace bpmn)
        {
            List<Tuple<BpmnFlowArrowType, string, string>> arrows = new List<Tuple<BpmnFlowArrowType, string, string>>();

            // Sequence flows
            IEnumerable<XElement> sequenceFlows = xmlElement.Elements(bpmn + "sequenceFlow");
            foreach (XElement item in sequenceFlows)
            {
                string fromId = item.Attribute("sourceRef").Value;
                string toId = item.Attribute("targetRef").Value;

                arrows.Add(new Tuple<BpmnFlowArrowType, string, string>(BpmnFlowArrowType.Sequence, fromId, toId));
            }

            // Add sub process arrows
            IEnumerable<XElement> subProcesses = xmlElement.Elements(bpmn + "subProcess");
            foreach (XElement item in subProcesses)
            {
                List<Tuple<BpmnFlowArrowType, string, string>> subProcessArrows = GetFlowArrows(item, bpmn);
                arrows = arrows.Concat(subProcessArrows).ToList();
            }

            return arrows;
        }
    }
}
