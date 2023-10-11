﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprache;

namespace BpmnToDcrConverter
{
    public abstract class Expression
    {
    }

    public abstract class BinaryOperation : Expression
    {
    }

    public class RelationalOperation : BinaryOperation
    {
        public Unit Left { get; }
        public Unit Right { get; }
        public RelationalOperator Operator { get; }

        public RelationalOperation(Unit left, RelationalOperator op, Unit right)
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
    }

    public class LogicalOperation : BinaryOperation
    {
        public BinaryOperation Left { get; }
        public BinaryOperation Right { get; }
        public LogicalOperator Operator { get; }

        public LogicalOperation(BinaryOperation left, LogicalOperator op, BinaryOperation right)
        {
            Left = left;
            Operator = op;
            Right = right;
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

    public abstract class Unit : Expression
    {
        public abstract bool IsConstant();

        public abstract DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict);

        public abstract string GetUnitString();
    }

    public class Variable : Unit
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

        public override string GetUnitString()
        {
            return Name;
        }
    }

    public abstract class Constant : Unit
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

        public override string GetUnitString()
        {
            return Value.ToString();
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

        public override string GetUnitString()
        {
            return Value.ToString();
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

        private static readonly Parser<Unit> Constant =
            Decimal.Or(Integer);

        private static readonly Parser<Unit> Variable =
            from letter in Parse.Letter
            from rest in Parse.LetterOrDigit.Many().Text()
            select new Variable(letter + rest);

        private static readonly Parser<Unit> Term =
            Constant.Or(Variable).Token();

        private static readonly Parser<BinaryOperation> RelationalExpression =
            from firstTerm in Term
            from op in RelationalOperatorParser
            from secondTerm in Term
            select new RelationalOperation(firstTerm, op, secondTerm);

        private static readonly Parser<BinaryOperation> LogicalExpression =
            from firstParens in Parse.Char('(').Token()
            from firstTerm in LogicalExpression.Or(RelationalExpression)
            from secondParens in Parse.Char(')').Token()
            from op in LogicalOperatorParser
            from thirdParens in Parse.Char('(').Token()
            from secondTerm in LogicalExpression.Or(RelationalExpression)
            from fourthParens in Parse.Char(')').Token()
            select new LogicalOperation(firstTerm, op, secondTerm);

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
