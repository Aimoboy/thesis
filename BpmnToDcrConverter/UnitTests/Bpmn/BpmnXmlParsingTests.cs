using BpmnToDcrConverter;
using BpmnToDcrConverter.Bpmn;

namespace UnitTests.Bpmn
{
    [TestClass]
    public class BpmnXmlParsingTests
    {
        [TestMethod]
        public void ParseStartToEnd()
        {
            string path = Path.Combine("BpmnXmlFiles", "BpmnStartToEnd.bpmn");
            BpmnGraph graph = BpmnXmlParser.Parse(path);
            graph.TestGraphValidity();

            List<BpmnFlowElement> graphFlowElements = graph.GetFlowElements();
            Assert.AreEqual(2, graphFlowElements.Count);
            Assert.IsNotNull(graph.GetFlowElementFromId("StartEvent_1"));
            Assert.IsNotNull(graph.GetFlowElementFromId("Event_07tbguc"));
        }

        [TestMethod]
        public void ParseStartToActivityToEnd()
        {
            string path = Path.Combine("BpmnXmlFiles", "BpmnStartToActivityToEnd.bpmn");
            BpmnGraph graph = BpmnXmlParser.Parse(path);
            graph.TestGraphValidity();

            Assert.AreEqual(3, graph.GetFlowElements().Count);

            BpmnFlowElement start = graph.GetFlowElementFromId("StartEvent_1");
            BpmnFlowElement end = graph.GetFlowElementFromId("Event_07tbguc");
            BpmnFlowElement activity = graph.GetFlowElementFromId("Activity_1b4273j");

            Assert.IsNotNull(start);
            Assert.IsNotNull(end);
            Assert.IsNotNull(activity);

            Assert.AreEqual(0, start.IncomingArrows.Count);
            Assert.AreEqual(1, start.OutgoingArrows.Count);
            Assert.AreEqual(1, activity.IncomingArrows.Count);
            Assert.AreEqual(1, activity.OutgoingArrows.Count);
            Assert.AreEqual(1, end.IncomingArrows.Count);
            Assert.AreEqual(0, end.OutgoingArrows.Count);


            Assert.AreEqual("Activity!", ((BpmnActivity)activity).Name);
        }

        [TestMethod]
        public void ParseExclusiveGateway()
        {
            string path = Path.Combine("BpmnXmlFiles", "BpmnExclusiveGateway.bpmn");
            BpmnGraph graph = BpmnXmlParser.Parse(path);
            graph.TestGraphValidity();

            Assert.AreEqual(7, graph.GetFlowElements().Count);

            BpmnFlowElement start = graph.GetFlowElementFromId("StartEvent_1");
            BpmnFlowElement end = graph.GetFlowElementFromId("Event_07tbguc");
            BpmnFlowElement gatewayStart = graph.GetFlowElementFromId("Gateway_1wujiar");
            BpmnFlowElement gatewayEnd = graph.GetFlowElementFromId("Gateway_0kqr9ss");
            BpmnFlowElement activity1 = graph.GetFlowElementFromId("Activity_0qxf49t");
            BpmnFlowElement activity2 = graph.GetFlowElementFromId("Activity_0yr28lh");
            BpmnFlowElement activity3 = graph.GetFlowElementFromId("Activity_0tv141c");

            Assert.IsNotNull(start);
            Assert.IsNotNull(end);
            Assert.IsNotNull(gatewayStart);
            Assert.IsNotNull(gatewayEnd);
            Assert.IsNotNull(activity1);
            Assert.IsNotNull(activity2);
            Assert.IsNotNull(activity3);

            Assert.AreEqual(0, start.IncomingArrows.Count);
            Assert.AreEqual(1, start.OutgoingArrows.Count);
            Assert.AreEqual(1, end.IncomingArrows.Count);
            Assert.AreEqual(0, end.OutgoingArrows.Count);
            Assert.AreEqual(1, activity1.IncomingArrows.Count);
            Assert.AreEqual(1, activity1.OutgoingArrows.Count);
            Assert.AreEqual(1, activity2.IncomingArrows.Count);
            Assert.AreEqual(1, activity2.OutgoingArrows.Count);
            Assert.AreEqual(1, activity3.IncomingArrows.Count);
            Assert.AreEqual(1, activity3.OutgoingArrows.Count);
            Assert.AreEqual(1, gatewayStart.IncomingArrows.Count);
            Assert.AreEqual(3, gatewayStart.OutgoingArrows.Count);
            Assert.AreEqual(3, gatewayEnd.IncomingArrows.Count);
            Assert.AreEqual(1, gatewayEnd.OutgoingArrows.Count);

            Assert.AreEqual("A1", ((BpmnActivity)activity1).Name);
            Assert.AreEqual("A2", ((BpmnActivity)activity2).Name);
            Assert.AreEqual("A3", ((BpmnActivity)activity3).Name);
        }

        [TestMethod]
        public void ParseSubProcesses()
        {
            string path = Path.Combine("BpmnXmlFiles", "BpmnSubProcesses.bpmn");
            BpmnGraph graph = BpmnXmlParser.Parse(path);
            graph.TestGraphValidity();

            Assert.AreEqual(3, graph.GetFlowElements().Count);
            Assert.AreEqual(8, graph.GetFlowElementsFlat().Count);

            BpmnFlowElement start = graph.GetFlowElementFromId("StartEvent_1");
            BpmnFlowElement end = graph.GetFlowElementFromId("Event_07tbguc");

            BpmnFlowElement innerStart = graph.GetFlowElementFromId("Event_1bqkohj");
            BpmnFlowElement innerEnd = graph.GetFlowElementFromId("Event_0jrv087");

            BpmnFlowElement innerInnerStart = graph.GetFlowElementFromId("Event_1fwjnq4");
            BpmnFlowElement innerInnerEnd = graph.GetFlowElementFromId("Event_1udz90n");

            BpmnFlowElement subProcess = graph.GetFlowElementFromId("Activity_16v2inh");
            BpmnFlowElement innerSubProcess = graph.GetFlowElementFromId("Activity_12l1z3h");

            Assert.IsNotNull(start);
            Assert.IsNotNull(end);
            Assert.IsNotNull(innerStart);
            Assert.IsNotNull(innerEnd);
            Assert.IsNotNull(innerInnerStart);
            Assert.IsNotNull(innerInnerEnd);
            Assert.IsNotNull(subProcess);
            Assert.IsNotNull(innerSubProcess);

            Assert.AreEqual(0, start.IncomingArrows.Count);
            Assert.AreEqual(1, start.OutgoingArrows.Count);
            Assert.AreEqual(1, end.IncomingArrows.Count);
            Assert.AreEqual(0, end.OutgoingArrows.Count);

            Assert.AreEqual(0, innerStart.IncomingArrows.Count);
            Assert.AreEqual(1, innerStart.OutgoingArrows.Count);
            Assert.AreEqual(1, innerEnd.IncomingArrows.Count);
            Assert.AreEqual(0, innerEnd.OutgoingArrows.Count);

            Assert.AreEqual(0, innerInnerStart.IncomingArrows.Count);
            Assert.AreEqual(1, innerInnerStart.OutgoingArrows.Count);
            Assert.AreEqual(1, innerInnerEnd.IncomingArrows.Count);
            Assert.AreEqual(0, innerInnerEnd.OutgoingArrows.Count);

            Assert.AreEqual(1, subProcess.IncomingArrows.Count);
            Assert.AreEqual(1, subProcess.OutgoingArrows.Count);

            Assert.AreEqual(1, innerSubProcess.IncomingArrows.Count);
            Assert.AreEqual(1, innerSubProcess.OutgoingArrows.Count);
        }
    }
}
