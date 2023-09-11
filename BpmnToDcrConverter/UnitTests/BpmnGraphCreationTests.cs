using BpmnToDcrConverter.BPMN;
using BpmnToDcrConverter.BPMN.Exceptions;

namespace UnitTests
{
    [TestClass]
    public class BpmnGraphCreationTests
    {
        [TestMethod]
        [ExpectedException(typeof(BpmnDuplicateIdException))]
        public void DuplicateIds()
        {
            BpmnActivity activity1 = new BpmnActivity("123", "Name!");
            BpmnActivity activity2 = new BpmnActivity("123", "Another name!");

            new BpmnGraph(new[] { activity1, activity2 });
        }
    }
}