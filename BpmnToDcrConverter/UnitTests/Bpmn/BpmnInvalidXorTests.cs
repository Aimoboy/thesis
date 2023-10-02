using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Bpmn.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Bpmn
{
    [TestClass]
    public class BpmnInvalidXorTests
    {
        [TestMethod]
        [ExpectedException(typeof(BpmnInvalidArrowException))]
        public void TwoArrowsWithoutConditions()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnExclusiveGateway xorStart = new BpmnExclusiveGateway("3");
            BpmnExclusiveGateway xorEnd = new BpmnExclusiveGateway("4");

            BpmnActivity activity1 = new BpmnActivity("5", "Name");
            BpmnActivity activity2 = new BpmnActivity("6", "Name");

            BpmnGraph graph = new BpmnGraph("7", new BpmnFlowElement[] { start, end, xorStart, xorEnd, activity1, activity2 });

            graph.AddArrow(BpmnFlowArrowType.Sequence, start, xorStart);
            graph.AddArrow(BpmnFlowArrowType.Sequence, xorStart, activity1);
            graph.AddArrow(BpmnFlowArrowType.Sequence, xorStart, activity2);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, xorEnd);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity2, xorEnd);
            graph.AddArrow(BpmnFlowArrowType.Sequence, xorEnd, end);

            graph.TestGraphValidity();
        }
    }
}
