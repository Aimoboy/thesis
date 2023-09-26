using BpmnToDcrConverter.Dcr.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BpmnToDcrConverter.Dcr
{
    public class DcrGraph
    {
        public string Name = "DcrGraph";

        private List<DcrFlowElement> _flowElements;

        public DcrGraph()
        {
            _flowElements = new List<DcrFlowElement>();
        }

        public DcrGraph(IEnumerable<DcrFlowElement> flowElements)
        {
            IEnumerable<string> allIds = flowElements.SelectMany(x => x.GetIds());
            List<string> duplicateIds = allIds.GroupBy(x => x).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

            if (duplicateIds.Any())
            {
                string exceptionString = string.Join(", ", duplicateIds);
                throw new DcrDuplicateIdException($"Multiple flow elements with the ids \"{exceptionString}\" are given.");
            }

            _flowElements = flowElements.ToList();
        }

        public List<DcrFlowElement> GetFlowElements()
        {
            return _flowElements;
        }

        public List<DcrFlowElement> GetFlowElementsFlat()
        {
            return _flowElements.SelectMany(x => x.GetFlowElementsFlat()).ToList();
        }

        public void AddFlowElements(IEnumerable<DcrFlowElement> newFlowElements)
        {
            HashSet<string> currentIds = GetFlowElementsFlat().Select(x => x.Id).ToHashSet();
            IEnumerable<string> newFlowElementIds = newFlowElements.SelectMany(x => x.GetIds());

            foreach (string id in newFlowElementIds)
            {
                if (currentIds.Contains(id))
                {
                    throw new DcrDuplicateIdException($"A flow element with id \"{id}\" already exists.");
                }
            }

            _flowElements = _flowElements.Concat(newFlowElements).ToList();
        }

        public void AddArrow(DcrFlowArrowType type, DcrFlowElement from, DcrFlowElement to)
        {
            from.OutgoingArrows.Add(new DcrFlowArrow(type, to));
            to.IncomingArrows.Add(new DcrFlowArrow(type, from));
        }

        public void Export(string path)
        {
            XNamespace dcr = "http://tk/schema/dcr";
            XNamespace dcrDi = "http://tk/schema/dcrDi";
            XNamespace dc = "http://www.omg.org/spec/DD/20100524/DC";

            XmlDocument doc = new XmlDocument();

            // Create XML Declaration
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(xmlDeclaration);

            // Create root element
            XmlElement root = doc.CreateElement("dcr:definitions", dcr.NamespaceName);
            root.SetAttribute("xmlns:dcrDi", "http://tk/schema/dcrDi");
            root.SetAttribute("xmlns:dc", "http://www.omg.org/spec/DD/20100524/DC");
            doc.AppendChild(root);

            // Add dcrGraph
            XmlElement dcrGraph = doc.CreateElement("dcr:dcrGraph", dcr.NamespaceName);
            dcrGraph.SetAttribute("id", "Graph");
            root.AppendChild(dcrGraph);

            // Add dcrRootBoard and dcrPlane
            XmlElement dcrRootBoard = doc.CreateElement("dcrDi:dcrRootBoard", dcrDi.NamespaceName);
            dcrRootBoard.SetAttribute("id", "RootBoard");
            root.AppendChild(dcrRootBoard);

            XmlElement dcrPlane = doc.CreateElement("dcrDi:dcrPlane", dcrDi.NamespaceName);
            dcrPlane.SetAttribute("id", "Plane");
            dcrPlane.SetAttribute("boardElement", "Graph");
            dcrRootBoard.AppendChild(dcrPlane);

            // Activities
            foreach (DcrActivity flowElement in _flowElements.Where(x => x is DcrActivity).Select(x => (DcrActivity)x))
            {
                XmlElement activity = doc.CreateElement("dcr:event", dcr.NamespaceName);
                activity.SetAttribute("id", flowElement.Id);
                activity.SetAttribute("description", flowElement.Name);
                activity.SetAttribute("included", flowElement.Included.ToString().ToLower());
                activity.SetAttribute("executed", flowElement.Executed.ToString().ToLower());
                activity.SetAttribute("pending", flowElement.Pending.ToString().ToLower());
                dcrGraph.AppendChild(activity);

                XmlElement activityPosition = doc.CreateElement("dcrDi:dcrShape", dcrDi.NamespaceName);
                activityPosition.SetAttribute("id", flowElement.Id + "_di");
                activityPosition.SetAttribute("boardElement", flowElement.Id);
                dcrPlane.AppendChild(activityPosition);

                XmlElement bounds = doc.CreateElement("dc:Bounds", dc.NamespaceName);
                bounds.SetAttribute("x", flowElement.X.ToString());
                bounds.SetAttribute("y", flowElement.Y.ToString());
                bounds.SetAttribute("width", flowElement.Width.ToString());
                bounds.SetAttribute("height", flowElement.Height.ToString());
                activityPosition.AppendChild(bounds);
            }

            // Arrows
            foreach (DcrActivity flowElement in _flowElements)
            {
                foreach (DcrFlowArrow arrow in flowElement.OutgoingArrows)
                {
                    XmlElement dcrRelation = doc.CreateElement("dcr:relation", dcr.NamespaceName);
                    dcrRelation.SetAttribute("id", arrow.Id);
                    dcrRelation.SetAttribute("type", Utilities.DcrArrowTypeToString(arrow.Type));
                    dcrRelation.SetAttribute("sourceRef", flowElement.Id);
                    dcrRelation.SetAttribute("targetRef", arrow.Element.Id);
                    dcrGraph.AppendChild(dcrRelation);

                    XmlElement dcrDiRelation = doc.CreateElement("dcrDi:relation", dcrDi.NamespaceName);
                    dcrDiRelation.SetAttribute("id", arrow.Id + "_di");
                    dcrDiRelation.SetAttribute("boardElement", arrow.Id);
                    dcrPlane.AppendChild(dcrDiRelation);

                    // Self referencing arrow
                    if (flowElement == arrow.Element)
                    {
                        XmlElement waypoint1 = doc.CreateElement("dcrDi:waypoint", dcrDi.NamespaceName);
                        int waypoint1X = flowElement.X + flowElement.Width - 10;
                        int waypoint1Y = flowElement.Y;
                        waypoint1.SetAttribute("x", waypoint1X.ToString());
                        waypoint1.SetAttribute("y", waypoint1Y.ToString());
                        dcrDiRelation.AppendChild(waypoint1);

                        XmlElement waypoint2 = doc.CreateElement("dcrDi:waypoint", dcrDi.NamespaceName);
                        int waypoint2X = waypoint1X;
                        int waypoint2Y = waypoint1Y - 20;
                        waypoint2.SetAttribute("x", waypoint2X.ToString());
                        waypoint2.SetAttribute("y", waypoint2Y.ToString());
                        dcrDiRelation.AppendChild(waypoint2);

                        XmlElement waypoint3 = doc.CreateElement("dcrDi:waypoint", dcrDi.NamespaceName);
                        int waypoint3X = waypoint2X + 30;
                        int waypoint3Y = waypoint2Y;
                        waypoint3.SetAttribute("x", waypoint3X.ToString());
                        waypoint3.SetAttribute("y", waypoint3Y.ToString());
                        dcrDiRelation.AppendChild(waypoint3);

                        XmlElement waypoint4 = doc.CreateElement("dcrDi:waypoint", dcrDi.NamespaceName);
                        int waypoint4X = waypoint3X;
                        int waypoint4Y = waypoint3Y + 30;
                        waypoint4.SetAttribute("x", waypoint4X.ToString());
                        waypoint4.SetAttribute("y", waypoint4Y.ToString());
                        dcrDiRelation.AppendChild(waypoint4);

                        XmlElement waypoint5 = doc.CreateElement("dcrDi:waypoint", dcrDi.NamespaceName);
                        int waypoint5X = waypoint4X - 20;
                        int waypoint5Y = waypoint4Y;
                        waypoint5.SetAttribute("x", waypoint5X.ToString());
                        waypoint5.SetAttribute("y", waypoint5Y.ToString());
                        dcrDiRelation.AppendChild(waypoint5);
                    }
                }
            }
            
            doc.Save(path);
        }
    }
}
