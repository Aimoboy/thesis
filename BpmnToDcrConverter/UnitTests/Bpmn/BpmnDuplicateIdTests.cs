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
            BpmnGateway gateway1 = new BpmnGateway("123", BpmnGatewayType.Or);
            BpmnGateway gateway2 = new BpmnGateway("123", BpmnGatewayType.Or);

            new BpmnGraph(new[] { gateway1, gateway2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsAndGateways()
        {
            BpmnGateway gateway1 = new BpmnGateway("123", BpmnGatewayType.And);
            BpmnGateway gateway2 = new BpmnGateway("123", BpmnGatewayType.And);

            new BpmnGraph(new[] { gateway1, gateway2 });
        }

        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIdsMixedGateways()
        {
            BpmnGateway gateway1 = new BpmnGateway("123", BpmnGatewayType.Or);
            BpmnGateway gateway2 = new BpmnGateway("123", BpmnGatewayType.And);

            new BpmnGraph(new[] { gateway1, gateway2 });
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
            graph.AddFlowElements(new[] { subProcess });
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

            graph.AddFlowElements(new BpmnFlowElement[] { subProcess2, activity5, activity6 });
        }
    }
}