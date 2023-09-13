using BpmnToDcrConverter.Bpmn;

namespace UnitTests.Bpmn
{
    [TestClass]
    public class ValidGraphTests
    {
        [TestMethod]
        public void StartEventToEndEvent()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, end });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, end);
            graph.TestGraphValidity();
        }

        [TestMethod]
        public void StartToActivityToEnd()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");
            BpmnActivity activity = new BpmnActivity("3", "Activity");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, end, activity });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, activity);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity, end);
            graph.TestGraphValidity();
        }

        [TestMethod]
        public void ExclusiveGateway()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnExclusiveGateway startGateway = new BpmnExclusiveGateway("3");
            BpmnExclusiveGateway endGateway = new BpmnExclusiveGateway("4");

            BpmnActivity activity1 = new BpmnActivity("5", "Activity1");
            BpmnActivity activity2 = new BpmnActivity("6", "Activity2");
            BpmnActivity activity3 = new BpmnActivity("7", "Activity3");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, end, startGateway, endGateway, activity1, activity2, activity3 });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, startGateway);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateway, activity1);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateway, activity2);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateway, activity3);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity1, endGateway);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity2, endGateway);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activity3, endGateway);
            graph.AddArrow(BpmnFlowArrowType.Sequence, endGateway, end);
            graph.TestGraphValidity();
        }

        [TestMethod]
        public void StartToSubProcessToEnd()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnStartEvent startInner = new BpmnStartEvent("3");
            BpmnEndEvent endInner = new BpmnEndEvent("4");

            BpmnSubProcess subProcess = new BpmnSubProcess("5", new BpmnFlowElement[] { startInner, endInner });
            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, end, subProcess });
            graph.AddArrow(BpmnFlowArrowType.Sequence, start, subProcess);
            graph.AddArrow(BpmnFlowArrowType.Sequence, subProcess, end);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startInner, endInner);
            graph.TestGraphValidity();
        }

        [TestMethod]
        public void ExclusiveGatewayInSubProcess()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnStartEvent startInner = new BpmnStartEvent("3");
            BpmnEndEvent endInner = new BpmnEndEvent("4");

            BpmnExclusiveGateway startGateWayInner = new BpmnExclusiveGateway("5");
            BpmnExclusiveGateway endGateWayInner = new BpmnExclusiveGateway("6");

            BpmnActivity activityInner1 = new BpmnActivity("7", "Inner!");
            BpmnActivity activityInner2 = new BpmnActivity("8", "Inner!");
            BpmnActivity activityInner3 = new BpmnActivity("9", "Inner!");

            BpmnSubProcess subProcess = new BpmnSubProcess("10", new BpmnFlowElement[]
            {
                startInner,
                endInner,
                startGateWayInner,
                endGateWayInner,
                activityInner1,
                activityInner2,
                activityInner3
            });

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[]
            {
                start,
                end,
                subProcess
            });

            graph.AddArrow(BpmnFlowArrowType.Sequence, start, subProcess);
            graph.AddArrow(BpmnFlowArrowType.Sequence, subProcess, end);

            graph.AddArrow(BpmnFlowArrowType.Sequence, startInner, startGateWayInner);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateWayInner, activityInner1);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateWayInner, activityInner2);
            graph.AddArrow(BpmnFlowArrowType.Sequence, startGateWayInner, activityInner3);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activityInner1, endGateWayInner);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activityInner2, endGateWayInner);
            graph.AddArrow(BpmnFlowArrowType.Sequence, activityInner3, endGateWayInner);
            graph.AddArrow(BpmnFlowArrowType.Sequence, endGateWayInner, endInner);

            graph.TestGraphValidity();
        }
    }
}
