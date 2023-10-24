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

        [TestMethod]
        public void Variables()
        {
            Expression exp = LogicParser.ConditionParser.Parse("x < y");
            Expression trueExp = new RelationalOperation(new Variable("x"), RelationalOperator.LessThan, new Variable("y"));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void LessThanOrEqual()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 <= 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.LessThanOrEqual, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void GreaterThan()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 > 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.GreaterThan, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void GreaterThanOrEqual()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 >= 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.GreaterThanOrEqual, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void Equals()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 = 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.Equal, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }

        [TestMethod]
        public void NotEquals()
        {
            Expression exp = LogicParser.ConditionParser.Parse("1 != 2");
            Expression trueExp = new RelationalOperation(new IntegerConstant(1), RelationalOperator.NotEqual, new IntegerConstant(2));

            Assert.IsTrue(exp.EqualToExpression(trueExp));
        }
    }
}
