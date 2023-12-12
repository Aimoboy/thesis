using BpmnToDcrConverter.Bpmn;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

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
            XNamespace bpmndi = "http://www.omg.org/spec/BPMN/20100524/DI";
            XNamespace dc = "http://www.omg.org/spec/DD/20100524/DC";
            XElement definitions = doc.Element(bpmn + "definitions");
            XElement collaboration = definitions.Element(bpmn + "collaboration");
            List<XElement> processes = definitions.Elements(bpmn + "process").ToList();
            XElement diagram = definitions.Element(bpmndi + "BPMNDiagram");
            XElement plane = diagram.Element(bpmndi + "BPMNPlane");

            string graphId = definitions.Attribute("id").Value;

            List<Tuple<BpmnPool, string>> emptyPools;
            if (collaboration == null)
            {
                emptyPools = processes.ConvertAll(x =>
                {
                    string processId = x.Attribute("id").Value;

                    return new Tuple<BpmnPool, string>(new BpmnPool(), processId);
                });
            }
            else
            {
                emptyPools = collaboration.Elements(bpmn + "participant").Select(x =>
                {
                    string id = x.Attribute("id").Value;
                    string name = x.Attribute("name").Value;
                    string processId = x.Attribute("processRef").Value;

                    return new Tuple<BpmnPool, string>(new BpmnPool(id, name), processId);
                }).ToList();
            }

            List<BpmnPool> pools = new List<BpmnPool>();
            foreach (var emptyPool in emptyPools)
            {
                BpmnPool pool = emptyPool.Item1;
                string processId = emptyPool.Item2;
                XElement process = processes.Where(x => x.Attribute("id").Value == processId).FirstOrDefault();

                List<BpmnFlowElement> flowElements = GetFlowElements(process, bpmn);

                XElement laneSet = process.Element(bpmn + "laneSet");
                List<Tuple<BpmnPoolLane, List<string>>> lanes;
                if (laneSet == null)
                {
                    lanes = new List<Tuple<BpmnPoolLane, List<string>>>();

                    BpmnPoolLane lane = new BpmnPoolLane();
                    List<string> elementIds = new List<string>();
                    Tuple<BpmnPoolLane, List<string>> tuple = new Tuple<BpmnPoolLane, List<string>>(lane, elementIds);

                    lanes.Add(tuple);
                }
                else
                {
                    lanes = laneSet.Elements(bpmn + "lane").Select(x =>
                    {
                        string id = x.Attribute("id").Value;
                        string name = x.Attribute("name").Value;
                        List<string> elementIds = x.Elements(bpmn + "flowNodeRef").Select(x => x.Value).ToList();

                        return new Tuple<BpmnPoolLane, List<string>>(new BpmnPoolLane(id, name), elementIds);
                    }).ToList();
                }

                foreach (var tuple in lanes)
                {
                    BpmnPoolLane lane = tuple.Item1;
                    List<string> elementIds = tuple.Item2;

                    if (elementIds.Count == 0)
                    {
                        lane.Elements = flowElements;
                    }
                    else
                    {
                        lane.Elements = flowElements.Where(x => elementIds.Contains(x.Id)).ToList();
                    }
                }


                pool.Lanes = lanes.ConvertAll(x => x.Item1);

                pools.Add(pool);
            }

            BpmnGraph graph = new BpmnGraph(graphId, pools);

            // Get flow element positions and size
            IEnumerable<XElement> shapes = plane.Elements(bpmndi + "BPMNShape");
            foreach (XElement element in shapes)
            {
                string bpmnElementId = element.Attribute("bpmnElement").Value;

                if (bpmnElementId.Contains("Participant") || bpmnElementId.Contains("Lane"))
                {
                    continue;
                }

                BpmnFlowElement bpmnElement = graph.GetFlowElementFromId(bpmnElementId);

                XElement bounds = element.Element(dc + "Bounds");
                string x = bounds.Attribute("x").Value;
                string y = bounds.Attribute("y").Value;
                string width = bounds.Attribute("width").Value;
                string height = bounds.Attribute("height").Value;

                bpmnElement.SetSize(int.Parse(x), int.Parse(y), int.Parse(width), int.Parse(height));
            }

            // Get flow arrows
            List<Tuple<BpmnFlowArrowType, string, string, string, string>> arrows = processes.SelectMany(x => GetFlowArrows(x, bpmn)).ToList();
            foreach (var arrow in arrows)
            {
                BpmnFlowArrowType type = arrow.Item1;
                string arrowId = arrow.Item2;
                BpmnFlowElement from = graph.GetFlowElementFromId(arrow.Item3);
                BpmnFlowElement to = graph.GetFlowElementFromId(arrow.Item4);
                string condition = arrow.Item5;

                graph.AddArrow(arrowId, type, from, to, condition);
            }

            graph.TestGraphValidity();
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
                string defaultPath = item.Attribute("default")?.Value ?? "";

                flowElements.Add(new BpmnExclusiveGateway(id, defaultPath));
            }

            // Find parallel gateways
            IEnumerable<XElement> parallelGateways = xmlElement.Elements(bpmn + "parallelGateway");
            foreach (XElement item in parallelGateways)
            {
                string id = item.Attribute("id").Value;
                flowElements.Add(new BpmnParallelGateway(id));
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

        private static List<Tuple<BpmnFlowArrowType, string, string, string, string>> GetFlowArrows(XElement xmlElement, XNamespace bpmn)
        {
            List<Tuple<BpmnFlowArrowType, string, string, string, string>> arrows = new List<Tuple<BpmnFlowArrowType, string, string, string, string>>();

            // Sequence flows
            IEnumerable<XElement> sequenceFlows = xmlElement.Elements(bpmn + "sequenceFlow");
            foreach (XElement item in sequenceFlows)
            {
                string arrowId = item.Attribute("id").Value;
                string fromId = item.Attribute("sourceRef").Value;
                string toId = item.Attribute("targetRef").Value;
                string condition = "";

                XElement conditionExpression = item.Element(bpmn + "conditionExpression");
                if (conditionExpression != null)
                {
                    condition = conditionExpression.Value[1..].Trim();
                }

                arrows.Add(new Tuple<BpmnFlowArrowType, string, string, string, string>(BpmnFlowArrowType.Sequence, arrowId, fromId, toId, condition));
            }

            // Add sub process arrows
            IEnumerable<XElement> subProcesses = xmlElement.Elements(bpmn + "subProcess");
            foreach (XElement item in subProcesses)
            {
                List<Tuple<BpmnFlowArrowType, string, string, string, string>> subProcessArrows = GetFlowArrows(item, bpmn);
                arrows = arrows.Concat(subProcessArrows).ToList();
            }

            return arrows;
        }
    }
}
