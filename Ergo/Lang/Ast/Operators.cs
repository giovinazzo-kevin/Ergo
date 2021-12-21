using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        public static readonly Operator BinaryConjunction = new(OperatorAffix.Infix, OperatorAssociativity.Right, 20, ",");
        public static readonly Operator BinaryDisjunction = new(OperatorAffix.Infix, OperatorAssociativity.Right, 30, ";");
        public static readonly Operator BinaryList = new(OperatorAffix.Infix, OperatorAssociativity.Right, 15, "|");
        public static readonly Operator BinaryMultiplication = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "*");
        public static readonly Operator BinaryDivision = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "/");
        public static readonly Operator BinaryIntegerDivision = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "//");
        public static readonly Operator BinarySum = new(OperatorAffix.Infix, OperatorAssociativity.Left, 500, "+");
        public static readonly Operator BinarySubtraction = new(OperatorAffix.Infix, OperatorAssociativity.Left, 500, "-");
        public static readonly Operator BinaryColon = new(OperatorAffix.Infix, OperatorAssociativity.Left, 50, ":");
        public static readonly Operator BinaryPower = new(OperatorAffix.Infix, OperatorAssociativity.Right, 700, "^");
        public static readonly Operator BinaryMod = new(OperatorAffix.Infix, OperatorAssociativity.Left, 300, "mod");
        public static readonly Operator BinaryUnification = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "=");
        public static readonly Operator BinaryAssignment = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, ":=");
        public static readonly Operator BinaryEquality = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "==");
        public static readonly Operator BinaryInequality = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "\\==");
        public static readonly Operator BinaryITermComparisonGt = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, "#>");
        public static readonly Operator BinaryITermComparisonGte = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, "#>=");
        public static readonly Operator BinaryITermComparisonLt = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "#<");
        public static readonly Operator BinaryITermComparisonLte = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "#<=");
        public static readonly Operator BinaryComparisonGt = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, ">");
        public static readonly Operator BinaryComparisonGte = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, ">=");
        public static readonly Operator BinaryComparisonLt = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "<");
        public static readonly Operator BinaryComparisonLte = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "<=");
        public static readonly Operator BinaryUnprovability = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "\\=");
        public static readonly Operator BinarySynctacticEquality = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "?=");
        public static readonly Operator BinaryEvaluation = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "is");
        public static readonly Operator UnaryHorn = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 10, ":-", "-?");
        public static readonly Operator BinaryHorn = new(OperatorAffix.Infix, OperatorAssociativity.None, 10, ":-", "-?");
        public static readonly Operator UnaryUnprovability = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 30, "\\+");
        public static readonly Operator UnaryNegative = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 5, "-");
        public static readonly Operator UnaryPositive = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 5, "+");

        public static readonly Operator[] DefinedOperators = new[] {
              BinaryConjunction
            , BinaryDisjunction
            , BinaryColon
            , BinaryList
            , BinaryUnification
            , BinaryEvaluation
            , BinaryEquality
            , BinaryComparisonGt
            , BinaryComparisonGte
            , BinaryComparisonLt
            , BinaryComparisonLte
            , BinaryITermComparisonGt
            , BinaryITermComparisonGte
            , BinaryITermComparisonLt
            , BinaryITermComparisonLte
            , BinarySynctacticEquality
            , BinaryInequality
            , BinaryUnprovability
            , BinaryAssignment
            , UnaryHorn
            , BinaryHorn
            , BinarySum
            , BinarySubtraction
            , BinaryMultiplication
            , BinaryDivision
            , BinaryIntegerDivision
            , BinaryPower
            , BinaryMod
            , UnaryUnprovability
            , UnaryPositive
            , UnaryNegative
        };
    }

}
