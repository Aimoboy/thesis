using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprache;

namespace BpmnToDcrConverter
{
    public abstract class Expression
    {
        public abstract bool Evaluate(Dictionary<string, decimal> variableToValueDict);
    }

    public class RelationalOperation : Expression
    {
        public Term Left { get; }
        public Term Right { get; }
        public RelationalOperator Operator { get; }

        public RelationalOperation(Term left, RelationalOperator op, Term right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public bool ContainsConstant()
        {
            return Left is Constant || Right is Constant;
        }

        public bool PurelyConstant()
        {
            return Left is Constant && Right is Constant;
        }

        public bool ContainsKnownAndUnknownVariables(Dictionary<string, DataType> variableToDataTypeDict)
        {
            if (ContainsConstant())
            {
                return false;
            }

            bool containsUnknown = Left.GetDataType(variableToDataTypeDict) == DataType.Unknown || Right.GetDataType(variableToDataTypeDict) == DataType.Unknown;
            bool containsKnown = Left.GetDataType(variableToDataTypeDict) != DataType.Unknown || Right.GetDataType(variableToDataTypeDict) != DataType.Unknown;

            return containsUnknown && containsKnown;
        }

        public override bool Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            decimal leftRes = Left.Evaluate(variableToValueDict);
            decimal rightRes = Right.Evaluate(variableToValueDict);
            Func<decimal, decimal, bool> func = GetRelationalOperatorFunction(Operator);

            return func(leftRes, rightRes);
        }

        public Func<decimal, decimal, bool> GetRelationalOperatorFunction(RelationalOperator op)
        {
            switch (op)
            {
                case RelationalOperator.LessThan:
                    return (a, b) => a < b;
                case RelationalOperator.GreaterThan:
                    return (a, b) => a > b;
                case RelationalOperator.Equal:
                    return (a, b) => a == b;
                case RelationalOperator.NotEqual:
                    return (a, b) => a != b;
                case RelationalOperator.LessThanOrEqual:
                    return (a, b) => a <= b;
                case RelationalOperator.GreaterThanOrEqual:
                    return (a, b) => a >= b;
                default:
                    throw new Exception("Unhandled case.");
            }
        }
    }

    public class LogicalOperation : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public LogicalOperator Operator { get; }

        public LogicalOperation(Expression left, LogicalOperator op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override bool Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            bool leftRes = Left.Evaluate(variableToValueDict);
            bool rightRes = Right.Evaluate(variableToValueDict);
            Func<bool, bool, bool> func = GetLogicalOperatorFunction(Operator);

            return func(leftRes, rightRes);
        }

        public Func<bool, bool, bool> GetLogicalOperatorFunction(LogicalOperator op)
        {
            switch (op)
            {
                case LogicalOperator.And:
                    return (a, b) => a && b;
                case LogicalOperator.Or:
                    return (a, b) => a || b;
                default:
                    throw new Exception("Unhandled case.");
            }
        }
    }

    public enum RelationalOperator
    {
        LessThan,
        GreaterThan,
        Equal,
        NotEqual,
        LessThanOrEqual,
        GreaterThanOrEqual
    }

    public enum LogicalOperator
    {
        And,
        Or
    }

    public abstract class Term
    {
        public abstract bool IsConstant();

        public abstract DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict);

        public abstract string GetString();

        public abstract decimal Evaluate(Dictionary<string, decimal> variableToValueDict);
    }

    public class Variable : Term
    {
        public string Name { get; }

        public Variable(string name)
        {
            Name = name;
        }

        public override bool IsConstant()
        {
            return false;
        }

        public override DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict)
        {
            return variableToDataTypeDict[Name];
        }

        public override string GetString()
        {
            return Name;
        }

        public override decimal Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            if (variableToValueDict.ContainsKey(Name))
            {
                return variableToValueDict[Name];
            }

            throw new Exception($"A value for the variable {Name} is not defined.");
        }
    }

    public abstract class Constant : Term
    {
        public override bool IsConstant()
        {
            return true;
        }
    }

    public class IntegerConstant : Constant
    {
        public int Value { get; }

        public IntegerConstant(int value)
        {
            Value = value;
        }

        public override DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict)
        {
            return DataType.Integer;
        }

        public override string GetString()
        {
            return Value.ToString();
        }

        public override decimal Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            return Value;
        }
    }

    public class DecimalConstant : Constant
    {
        public decimal Value { get; }

        public DecimalConstant(decimal value)
        {
            Value = value;
        }

        public override DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict)
        {
            return DataType.Float;
        }

        public override string GetString()
        {
            return Value.ToString();
        }

        public override decimal Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            return Value;
        }
    }

    public static class LogicParser
    {
        private static readonly Parser<LogicalOperator> LogicalOperatorParser =
            Parse.String("&&").Token().Return(LogicalOperator.And)
                 .Or(Parse.String("||").Token().Return(LogicalOperator.Or));

        private static readonly Parser<RelationalOperator> RelationalOperatorParser =
            Parse.String(">=").Token().Return(RelationalOperator.GreaterThanOrEqual)
                 .Or(Parse.String("<=").Token().Return(RelationalOperator.LessThanOrEqual))
                 .Or(Parse.String("=").Token().Return(RelationalOperator.Equal))
                 .Or(Parse.String("<").Token().Return(RelationalOperator.LessThan))
                 .Or(Parse.String(">").Token().Return(RelationalOperator.GreaterThan))
                 .Or(Parse.String("!=").Token().Return(RelationalOperator.NotEqual));

        private static readonly Parser<Constant> Integer =
            from minus in Parse.String("-").Text().Optional()
            from number in Parse.Digit.AtLeastOnce().Text()
            select new IntegerConstant(int.Parse(minus.GetOrElse("") + number));

        public static readonly Parser<Constant> Decimal =
            from minus in Parse.String("-").Text().Optional()
            from front in Parse.Digit.Many().Text()
            from dot in Parse.Char('.')
            from back in Parse.Digit.AtLeastOnce().Text()
            select new DecimalConstant(decimal.Parse(minus.GetOrElse("") + front + dot + back, CultureInfo.InvariantCulture));

        private static readonly Parser<Term> Constant =
            Decimal.Or(Integer);

        private static readonly Parser<Term> Variable =
            from letter in Parse.Letter
            from rest in Parse.LetterOrDigit.Many().Text()
            select new Variable(letter + rest);

        private static readonly Parser<Term> TermParser =
            Constant.Or(Variable).Token();

        private static readonly Parser<Expression> RelationalExpression =
            from firstTerm in TermParser
            from op in RelationalOperatorParser
            from secondTerm in TermParser
            select new RelationalOperation(firstTerm, op, secondTerm);

        private static readonly Parser<Expression> LogicalExpression =
            from firstParens in Parse.Char('(').Token()
            from firstTerm in LogicalExpression.Or(RelationalExpression)
            from secondParens in Parse.Char(')').Token()
            from op in LogicalOperatorParser
            from thirdParens in Parse.Char('(').Token()
            from secondTerm in LogicalExpression.Or(RelationalExpression)
            from fourthParens in Parse.Char(')').Token()
            select new LogicalOperation(firstTerm, op, secondTerm);

        public static readonly Parser<Expression> LogicalExpressionParser =
            RelationalExpression.Or(LogicalExpression).End();
    }

    public enum DataType
    {
        Unknown,
        Integer,
        Float
    }
}
