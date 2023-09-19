using BpmnToDcrConverter.Dcr;
using BpmnToDcrConverter.Dcr.Exceptions;

namespace UnitTests.Dcr
{
    [TestClass]
    public class DcrDuplicateIdTests
    {
        [TestMethod]
        [ExpectedException(typeof(DcrDuplicateIdException))]
        public void InitializeGraphNoNesting()
        {
            DcrActivity activity1 = new DcrActivity("1", "Activity 1!");
            DcrActivity activity2 = new DcrActivity("1", "Activity 2!");

            new DcrGraph(new[] { activity1, activity2 });
        }

        [TestMethod]
        [ExpectedException(typeof(DcrDuplicateIdException))]
        public void InitializeGraphNesting()
        {
            DcrActivity activity1 = new DcrActivity("1", "Activity 1!");
            DcrActivity activity2 = new DcrActivity("2", "Activity 2!");
            DcrActivity activity3 = new DcrActivity("3", "Activity 3!");
            DcrActivity activity4 = new DcrActivity("1", "Activity 4!");

            DcrNesting nesting1 = new DcrNesting("4", new[] { activity3, activity4 });
            DcrNesting nesting2 = new DcrNesting("5", new[] { nesting1 });

            new DcrGraph(new DcrFlowElement[] { activity1, activity2, nesting2 });
        }
    }
}
