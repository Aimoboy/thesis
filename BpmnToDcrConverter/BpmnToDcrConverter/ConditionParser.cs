using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprache;

namespace BpmnToDcrConverter
{
    public abstract class Expression
    {
    }

    public class Variable : Expression
    {
        public string Name { get; }

        public Variable(string name)
        {
            Name = name;
        }
    }

    public class BinaryOperation : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public Operator Operator { get; }

        public BinaryOperation(Expression left, Operator op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public enum Operator
    {
        And,
        Or,
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    public class IntegerConstant : Expression
    {
        public int Value { get; }

        public IntegerConstant(int value)
        {
            Value = value;
        }
    }

    public class DecimalConstant : Expression
    {
        public decimal Value { get; }

        public DecimalConstant(decimal value)
        {
            Value = value;
        }
    }

    public static class LogicParser
    {
        private static readonly Parser<Operator> LogicalOperator =
            Parse.String("&&").Token().Return(Operator.And)
                 .Or(Parse.String("||").Token().Return(Operator.Or));

        private static readonly Parser<Operator> RelationalOperator =
            Parse.String(">=").Token().Return(Operator.GreaterThanOrEqual)
                 .Or(Parse.String("<=").Token().Return(Operator.LessThanOrEqual))
                 .Or(Parse.String("=").Token().Return(Operator.Equal))
                 .Or(Parse.String("<").Token().Return(Operator.LessThan))
                 .Or(Parse.String(">").Token().Return(Operator.GreaterThan))
                 .Or(Parse.String("!=").Token().Return(Operator.NotEqual));

        private static readonly Parser<Expression> Integer =
            from minus in Parse.String("-").Text().Optional()
            from number in Parse.Digit.AtLeastOnce().Text()
            select new IntegerConstant(int.Parse(minus.GetOrElse("") + number));

        public static readonly Parser<Expression> Decimal =
            from minus in Parse.String("-").Text().Optional()
            from front in Parse.Digit.Many().Text()
            from dot in Parse.Char('.')
            from back in Parse.Digit.AtLeastOnce().Text()
            select new DecimalConstant(decimal.Parse(minus.GetOrElse("") + front + dot + back, CultureInfo.InvariantCulture));

        private static readonly Parser<Expression> Constant =
            Decimal.Or(Integer).Token();

        private static readonly Parser<Expression> Variable =
            from letter in Parse.Letter
            from rest in Parse.LetterOrDigit.Many().Text()
            select new Variable(letter + rest);

        private static readonly Parser<Expression> Term =
            Constant.Or(Variable);

        private static readonly Parser<BinaryOperation> RelationalExpression =
            from firstTerm in Term
            from op in RelationalOperator
            from secondTerm in Term
            select new BinaryOperation(firstTerm, op, secondTerm);

        private static readonly Parser<BinaryOperation> LogicalExpression =
            from firstParens in Parse.Char('(').Token()
            from firstTerm in LogicalExpression.Or(RelationalExpression)
            from secondParens in Parse.Char(')').Token()
            from op in LogicalOperator
            from thirdParens in Parse.Char('(').Token()
            from secondTerm in LogicalExpression.Or(RelationalExpression)
            from fourthParens in Parse.Char(')').Token()
            select new BinaryOperation(firstTerm, op, secondTerm);

        public static readonly Parser<BinaryOperation> LogicalExpressionParser =
            RelationalExpression.Or(LogicalExpression).End();
    }

    public enum DataType
    {
        Unknown,
        Integer,
        Float
    }
}
