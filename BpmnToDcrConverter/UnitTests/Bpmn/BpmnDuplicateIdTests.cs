using BpmnToDcrConverter.Bpmn;
using BpmnToDcrConverter.Bpmn.Exceptions;

namespace UnitTests.Bpmn
{
    [TestClass]
    public class BpmnDuplicateIdTests
    {
        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsActivities()
        {
            BpmnActivity activity1 = new BpmnActivity("123", "Name!");
            BpmnActivity activity2 = new BpmnActivity("123", "Another name!");

            new BpmnGraph(new[] { activity1, activity2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsOrGateways()
        {
            BpmnExclusiveGateway gateway1 = new BpmnExclusiveGateway("123");
            BpmnExclusiveGateway gateway2 = new BpmnExclusiveGateway("123");

            new BpmnGraph(new[] { gateway1, gateway2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsAndGateways()
        {
            BpmnParallelGateway gateway1 = new BpmnParallelGateway("123");
            BpmnParallelGateway gateway2 = new BpmnParallelGateway("123");

            new BpmnGraph(new[] { gateway1, gateway2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsMixedGateways()
        {
            BpmnExclusiveGateway gateway1 = new BpmnExclusiveGateway("123");
            BpmnParallelGateway gateway2 = new BpmnParallelGateway("123");

            new BpmnGraph(new BpmnFlowElement[] { gateway1, gateway2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsInSubProcess()
        {
            BpmnActivity activity1 = new BpmnActivity("123", "Name!");

            BpmnActivity activity2 = new BpmnActivity("123", "Another name!");
            BpmnSubProcess subProcess = new BpmnSubProcess("456", new[] { activity2 });

            new BpmnGraph(new BpmnFlowElement[] { activity1, subProcess });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsAddSubProcess()
        {
            BpmnActivity activity1 = new BpmnActivity("123", "Name!");

            BpmnActivity activity2 = new BpmnActivity("123", "Another name!");
            BpmnSubProcess subProcess = new BpmnSubProcess("456", new[] { activity2 });

            BpmnGraph graph = new BpmnGraph(new[] { activity1 });

            BpmnPool pool = new BpmnPool(new BpmnPoolLane(new[] { subProcess }));
            graph.AddPool(pool);
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsSubProcessInSubProcess()
        {
            BpmnStartEvent start = new BpmnStartEvent("1");
            BpmnEndEvent end = new BpmnEndEvent("2");

            BpmnGraph graph = new BpmnGraph(new BpmnFlowElement[] { start, end });

            BpmnActivity activity1 = new BpmnActivity("3", "Activity!");
            BpmnActivity activity2 = new BpmnActivity("1", "Activity!");

            BpmnSubProcess subProcess1 = new BpmnSubProcess("4", new[] { activity1, activity2 });

            BpmnActivity activity3 = new BpmnActivity("5", "Activity!");
            BpmnActivity activity4 = new BpmnActivity("6", "Activity!");

            BpmnSubProcess subProcess2 = new BpmnSubProcess("7", new BpmnFlowElement[] { subProcess1, activity3, activity4 });

            BpmnActivity activity5 = new BpmnActivity("8", "Activity!");
            BpmnActivity activity6 = new BpmnActivity("9", "Activity!");

            BpmnPool pool = new BpmnPool(new BpmnPoolLane(new BpmnFlowElement[] { subProcess2, activity5, activity6 }));
            graph.AddPool(pool);
        }
    }
}
