using BpmnToDcrConverter.BPMN;
using BpmnToDcrConverter.BPMN.Exceptions;

namespace UnitTests.Bpmn
{
    [TestClass]
    public class BpmnInvalidArrowTests
    {
        [TestMethod]
        [ExpectedException(typeof(BpmnInvalidArrowException))]
        public void InvalidArrowActivityTooFewOutgoing()
        {
            BpmnEvent start = new BpmnEvent("1", BpmnEventType.Start);
            BpmnActivity activity = new BpmnActivity("2", "Activity1");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, activity });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, activity);
            activity.TestArrowCountValidity();
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnInvalidArrowException))]
        public void InvalidArrowActivityTooManyOutgoing()
        {
            BpmnEvent start = new BpmnEvent("1", BpmnEventType.Start);

            BpmnActivity activity1 = new BpmnActivity("2", "Activity1");
            BpmnActivity activity2 = new BpmnActivity("3", "Activity2");
            BpmnActivity activity3 = new BpmnActivity("4", "Activity3");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, activity1, activity2, activity3 });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, activity1);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, activity2);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, activity3);

            activity1.TestArrowCountValidity();
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnInvalidArrowException))]
        public void InvalidArrowActivityTooFewIncoming()
        {
            BpmnEvent end = new BpmnEvent("1", BpmnEventType.End);
            BpmnActivity activity = new BpmnActivity("2", "Activity!");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { end, activity });
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity, end);
            activity.TestArrowCountValidity();
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnInvalidArrowException))]
        public void InvalidArrowActivityTooManyIncoming()
        {
            BpmnEvent end = new BpmnEvent("1", BpmnEventType.End);

            BpmnActivity activity1 = new BpmnActivity("2", "Activity1");
            BpmnActivity activity2 = new BpmnActivity("3", "Activity2");
            BpmnActivity activity3 = new BpmnActivity("4", "Activity3");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { end, activity1, activity2, activity3 });

            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, activity3);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity2, activity3);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity3, end);

            activity3.TestArrowCountValidity();
        }
    }
}
