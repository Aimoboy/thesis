using BpmnToDcrConverter.BPMN;
using BpmnToDcrConverter.BPMN.Exceptions;

namespace UnitTests
{
    [TestClass]
    public class BpmnGraphCreationTests
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
    }
}