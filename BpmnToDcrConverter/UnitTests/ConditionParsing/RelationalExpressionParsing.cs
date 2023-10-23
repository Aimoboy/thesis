using Sprache;
using BpmnToDcrConverter;

namespace UnitTests.ConditionParsing
{
    [TestClass]
    public class RelationalExpressionParsing
    {
        [TestMethod]
        public void Integers()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 < 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.LessThan, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void Decimals()
        {
            Expression exp = LogicParser.ConditionParser.Parse("0.0065 < 0.008");
            Expression trueExp = new RelationalOperation(new DecimalConstant(0.0065m), RelationalOperator.LessThan, new DecimalConstant(0.008m));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }
    }
}
