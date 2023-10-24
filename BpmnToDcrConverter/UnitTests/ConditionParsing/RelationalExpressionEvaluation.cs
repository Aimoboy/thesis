

using BpmnToDcrConverter;
using Sprache;

namespace UnitTests.ConditionParsing
{
    [TestClass]
    public class RelationalExpressionEvaluation
    {
        [TestMethod]
        public void EvaluateLessThan()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 < 2").Evaluate();
            Assert.IsTrue(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 < 2").Evaluate();
            Assert.IsFalse(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 < 2").Evaluate();
            Assert.IsFalse(res3);
        }

        [TestMethod]
        public void EvaluateLessThanOrEqual()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 <= 2").Evaluate();
            Assert.IsTrue(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 <= 2").Evaluate();
            Assert.IsTrue(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 <= 2").Evaluate();
            Assert.IsFalse(res3);
        }

        [TestMethod]
        public void EvaluateGreaterThan()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 > 2").Evaluate();
            Assert.IsFalse(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 > 2").Evaluate();
            Assert.IsFalse(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 > 2").Evaluate();
            Assert.IsTrue(res3);
        }

        [TestMethod]
        public void EvaluateGreaterThanOrEqual()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 >= 2").Evaluate();
            Assert.IsFalse(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 >= 2").Evaluate();
            Assert.IsTrue(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 >= 2").Evaluate();
            Assert.IsTrue(res3);
        }

        [TestMethod]
        public void EvaluateEqual()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 = 2").Evaluate();
            Assert.IsFalse(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 = 2").Evaluate();
            Assert.IsTrue(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 = 2").Evaluate();
            Assert.IsFalse(res3);
        }

        [TestMethod]
        public void EvaluateNotEqual()
        {
            bool res1 = LogicParser.ConditionParser.Parse("1 != 2").Evaluate();
            Assert.IsTrue(res1);

            bool res2 = LogicParser.ConditionParser.Parse("2 != 2").Evaluate();
            Assert.IsFalse(res2);

            bool res3 = LogicParser.ConditionParser.Parse("3 != 2").Evaluate();
            Assert.IsTrue(res3);
        }

        [TestMethod]
        public void EvaluateWithVariable()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 < x");

            Assert.IsTrue(exp.Evaluate(new Dictionary<string, decimal> { { "x", 2 } }));
            Assert.IsFalse(exp.Evaluate(new Dictionary<string, decimal> { { "x", 1 } }));
            Assert.IsFalse(exp.Evaluate(new Dictionary<string, decimal> { { "x", 0 } }));
        }
    }
}
