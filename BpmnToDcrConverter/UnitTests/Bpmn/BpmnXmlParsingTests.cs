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
    }
}
