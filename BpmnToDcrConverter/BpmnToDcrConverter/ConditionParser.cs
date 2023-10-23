using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sprache;

namespace BpmnToDcrConverter
{
    public abstract class Expression
    {
        public abstract bool Evaluate(Dictionary<string, decimal> variableToValueDict);

        public bool Evaluate()
        {
            return Evaluate(new Dictionary<string, decimal>());
        }

        public abstract List<string> GetVariableNames();

        public abstract string GetString();

        public abstract bool PurelyConstant();

        public abstract bool CanEvaluateWithoutVariables();

        public abstract List<Expression> GetAllConstantlyEvaluableSubExpressions();

        public abstract bool EqualToExpression(Expression other);
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

        public override List<string> GetVariableNames()
        {
            return Left.GetVariableNames().Concat(Right.GetVariableNames()).ToList();
        }

        public override string GetString()
        {
            string left = Left.GetString();
            string right = Right.GetString();
            string op = GetRelationalOperatorString(Operator);

            return $"{left} {op} {right}";
        }

        public string GetRelationalOperatorString(RelationalOperator op)
        {
            switch (op)
            {
                case RelationalOperator.LessThan:
                    return "<";
                case RelationalOperator.GreaterThan:
                    return ">";
                case RelationalOperator.Equal:
                    return "=";
                case RelationalOperator.NotEqual:
                    return "!=";
                case RelationalOperator.LessThanOrEqual:
                    return "<=";
                case RelationalOperator.GreaterThanOrEqual:
                    return ">=";
                default:
                    throw new Exception("Unhandled case.");
            }
        }

        public override bool PurelyConstant()
        {
            return Left.IsConstant() && Right.IsConstant();
        }

        public override bool CanEvaluateWithoutVariables()
        {
            return PurelyConstant();
        }

        public override List<Expression> GetAllConstantlyEvaluableSubExpressions()
        {
            if (PurelyConstant())
            {
                return new List<Expression> { this };
            }

            return new List<Expression>();
        }

        public override bool EqualToExpression(Expression other)
        {
            if (!(other is RelationalOperation))
            {
                return false;
            }

            RelationalOperation otherRelation = (RelationalOperation)other;
            return Operator == otherRelation.Operator && Left.EqualToTerm(otherRelation.Left) && Right.EqualToTerm(otherRelation.Right);
        }
    }

    public class BinaryLogicalOperation : Expression
    {
        public Expression Left { get; }
        public Expression Right { get; }
        public BinaryLogicalOperator Operator { get; }

        public BinaryLogicalOperation(Expression left, BinaryLogicalOperator op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override bool Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            bool leftRes;
            bool rightRes;

            // Short circuit
            switch (Operator)
            {
                case BinaryLogicalOperator.And:
                    leftRes = true;
                    rightRes = true;

                    if (Left.CanEvaluateWithoutVariables())
                    {
                        leftRes = Left.Evaluate();
                    }

                    if (Right.CanEvaluateWithoutVariables())
                    {
                        rightRes = Right.Evaluate();
                    }

                    if (!leftRes || !rightRes)
                    {
                        return false;
                    }
                    break;
                case BinaryLogicalOperator.Or:
                    leftRes = false;
                    rightRes = false;

                    if (Left.CanEvaluateWithoutVariables())
                    {
                        leftRes = Left.Evaluate();
                    }

                    if (Right.CanEvaluateWithoutVariables())
                    {
                        rightRes = Right.Evaluate();
                    }

                    if (leftRes || rightRes)
                    {
                        return true;
                    }
                    break;
            }


            leftRes = Left.Evaluate(variableToValueDict);
            rightRes = Right.Evaluate(variableToValueDict);
            Func<bool, bool, bool> func = GetBinaryLogicalOperatorFunction();

            return func(leftRes, rightRes);
        }

        public Func<bool, bool, bool> GetBinaryLogicalOperatorFunction()
        {
            switch (Operator)
            {
                case BinaryLogicalOperator.And:
                    return (a, b) => a && b;
                case BinaryLogicalOperator.Or:
                    return (a, b) => a || b;
                default:
                    throw new Exception("Unhandled case.");
            }
        }

        public override List<string> GetVariableNames()
        {
            return Left.GetVariableNames().Concat(Right.GetVariableNames()).ToList();
        }

        public override string GetString()
        {
            string left = Left.GetString();
            string right = Right.GetString();
            string op = GetBinaryLogicalOperatorString();

            return $"({left}) {op} ({right})";
        }

        public string GetBinaryLogicalOperatorString()
        {
            switch (Operator)
            {
                case BinaryLogicalOperator.And:
                    return "&&";
                case BinaryLogicalOperator.Or:
                    return "||";
                default:
                    throw new Exception("Unhandled case.");
            }
        }

        public override bool PurelyConstant()
        {
            return Left.PurelyConstant() && Right.PurelyConstant();
        }

        public override bool CanEvaluateWithoutVariables()
        {
            bool leftEval;
            bool rightEval;

            switch (Operator)
            {
                case BinaryLogicalOperator.And:
                    leftEval = true;
                    rightEval = true;

                    if (Left.CanEvaluateWithoutVariables())
                    {
                        leftEval = Left.Evaluate();
                    }

                    if (Right.CanEvaluateWithoutVariables())
                    {
                        rightEval = Right.Evaluate();
                    }

                    return !leftEval || !rightEval;
                case BinaryLogicalOperator.Or:
                    leftEval = false;
                    rightEval = false;

                    if (Left.CanEvaluateWithoutVariables())
                    {
                        leftEval = Left.Evaluate();
                    }

                    if (Right.CanEvaluateWithoutVariables())
                    {
                        rightEval = Right.Evaluate();
                    }

                    return leftEval || rightEval;
            }

            throw new Exception("Unhandled case");
        }

        public override List<Expression> GetAllConstantlyEvaluableSubExpressions()
        {
            if (CanEvaluateWithoutVariables())
            {
                return new List<Expression> { this };
            }

            return Left.GetAllConstantlyEvaluableSubExpressions().Concat(Right.GetAllConstantlyEvaluableSubExpressions()).ToList();
        }

        public override bool EqualToExpression(Expression other)
        {
            if (!(other is BinaryLogicalOperation))
            {
                return false;
            }

            BinaryLogicalOperation otherlogical = (BinaryLogicalOperation)other;

            if (Operator != otherlogical.Operator)
            {
                return false;
            }

            // The logical opeators are commutative
            return Left.EqualToExpression(otherlogical.Left) && Right.EqualToExpression(otherlogical.Right) || Left.EqualToExpression(otherlogical.Right) && Right.EqualToExpression(otherlogical.Left);
        }
    }

    public class UnaryLogicalOperation : Expression
    {
        public Expression Expression { get; set; }
        public UnaryLogicalOperator Operator { get; }

        public UnaryLogicalOperation(Expression expression, UnaryLogicalOperator op)
        {
            Expression = expression;
            Operator = op;
        }
        
        public override bool CanEvaluateWithoutVariables()
        {
            return Expression.CanEvaluateWithoutVariables();
        }

        public override bool EqualToExpression(Expression other)
        {
            if (!(other is UnaryLogicalOperation))
            {
                return false;
            }

            UnaryLogicalOperation otherlogical = (UnaryLogicalOperation)other;

            if (Operator != otherlogical.Operator)
            {
                return false;
            }

            return Expression.EqualToExpression(otherlogical.Expression);
        }

        public override bool Evaluate(Dictionary<string, decimal> variableToValueDict)
        {
            bool eval = Expression.Evaluate(variableToValueDict);
            Func<bool, bool> func = GetUnaryLogicalOperatorFunction();
            return func(eval);
        }

        public override List<Expression> GetAllConstantlyEvaluableSubExpressions()
        {
            if (CanEvaluateWithoutVariables())
            {
                return new List<Expression> { this };
            }

            return Expression.GetAllConstantlyEvaluableSubExpressions().ToList();
        }

        public override string GetString()
        {
            string exprStr = Expression.GetString();
            string opStr = GetUnaryLogicalOperatorString();
            return $"{opStr}({exprStr})";
        }

        public override List<string> GetVariableNames()
        {
            return Expression.GetVariableNames();
        }

        public override bool PurelyConstant()
        {
            return Expression.PurelyConstant();
        }

        public Func<bool, bool> GetUnaryLogicalOperatorFunction()
        {
            switch (Operator)
            {
                case UnaryLogicalOperator.Not:
                    return a => !a;
                default:
                    throw new Exception("Unhandled case.");
            }
        }

        public string GetUnaryLogicalOperatorString()
        {
            switch (Operator)
            {
                case UnaryLogicalOperator.Not:
                    return "!";
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

    public enum BinaryLogicalOperator
    {
        And,
        Or
    }

    public enum UnaryLogicalOperator
    {
        Not
    }

    public abstract class Term
    {
        public abstract bool IsConstant();

        public abstract DataType GetDataType(Dictionary<string, DataType> variableToDataTypeDict);

        public abstract string GetString();

        public abstract decimal Evaluate(Dictionary<string, decimal> variableToValueDict);

        public abstract List<string> GetVariableNames();

        public abstract bool EqualToTerm(Term other);
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

        public override List<string> GetVariableNames()
        {
            return new List<string>() { Name };
        }

        public override bool EqualToTerm(Term other)
        {
            if (!(other is Variable))
            {
                return false;
            }

            Variable otherVariable = (Variable)other;
            return Name == otherVariable.Name;
        }
    }

    public abstract class Constant : Term
    {
        public override bool IsConstant()
        {
            return true;
        }

        public override List<string> GetVariableNames()
        {
            return new List<string>();
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

        public override bool EqualToTerm(Term other)
        {
            if (!(other is IntegerConstant))
            {
                return false;
            }

            IntegerConstant otherConstant = (IntegerConstant)other;
            return Value == otherConstant.Value;
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

        public override bool EqualToTerm(Term other)
        {
            if (!(other is DecimalConstant))
            {
                return false;
            }

            DecimalConstant otherConstant = (DecimalConstant)other;
            return Value == otherConstant.Value;
        }
    }

    public static class LogicParser
    {
        private static readonly Parser<BinaryLogicalOperator> BinaryLogicalOperatorParser =
            Parse.String("&&").Return(BinaryLogicalOperator.And)
                 .Or(Parse.IgnoreCase("and").Return(BinaryLogicalOperator.And))
                 .Or(Parse.String("||").Return(BinaryLogicalOperator.Or))
                 .Or(Parse.IgnoreCase("or").Return(BinaryLogicalOperator.Or)).Token();

        private static readonly Parser<UnaryLogicalOperator> UnaryLogicalOperatorParser =
            Parse.String("!").Return(UnaryLogicalOperator.Not);

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
            from firstExp in TermParser
            from op in RelationalOperatorParser
            from secondExp in TermParser
            select new RelationalOperation(firstExp, op, secondExp);

        private static readonly Parser<Expression> BinaryLogicalExpression =
            from firstParens in Parse.Char('(').Token()
            from firstExp in ExpressionParser
            from secondParens in Parse.Char(')').Token()
            from op in BinaryLogicalOperatorParser
            from thirdParens in Parse.Char('(').Token()
            from secondExp in ExpressionParser
            from fourthParens in Parse.Char(')').Token()
            select new BinaryLogicalOperation(firstExp, op, secondExp);

        private static readonly Parser<Expression> UnaryLogicalExpression =
            from op in UnaryLogicalOperatorParser
            from firstParens in Parse.Char('(').Token()
            from exp in ExpressionParser
            from secondParens in Parse.Char(')').Token()
            select new UnaryLogicalOperation(exp, op);

        private static readonly Parser<Expression> ExpressionInParentheses =
            from firstParens in Parse.Char('(').Token()
            from exp in ExpressionParser
            from secondParens in Parse.Char(')').Token()
            select exp;

        private static readonly Parser<Expression> ExpressionParser =
            ExpressionInParentheses.Or(RelationalExpression).Or(BinaryLogicalExpression).Or(UnaryLogicalExpression);

        public static readonly Parser<Expression> ConditionParser =
            ExpressionParser.End();
    }

    public enum DataType
    {
        Unknown,
        Integer,
        Float
    }
}
