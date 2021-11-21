using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        public static readonly Operator BinaryConjunction = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 20, ",");
        public static readonly Operator BinaryDisjunction = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 30, ";");
        public static readonly Operator BinaryList = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 15, "|");
        public static readonly Operator BinaryMultiplication = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "*");
        public static readonly Operator BinaryDivision = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "/");
        public static readonly Operator BinaryIntegerDivision = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "//");
        public static readonly Operator BinarySum = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "+");
        public static readonly Operator BinarySubtraction = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "-");
        public static readonly Operator BinaryPower = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 700, "^");
        public static readonly Operator BinaryMod = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 300, "mod");
        public static readonly Operator BinaryUnification = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "=");
        public static readonly Operator BinaryAssignment = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, ":=");
        public static readonly Operator BinaryEquality = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "==");
        public static readonly Operator BinaryInequality = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "\\==");
        public static readonly Operator BinaryComparisonGt = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, ">");
        public static readonly Operator BinaryComparisonGte = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, ">=");
        public static readonly Operator BinaryComparisonLt = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "<");
        public static readonly Operator BinaryComparisonLte = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "<=");
        public static readonly Operator BinaryUnprovability = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "\\=");
        public static readonly Operator BinarySynctacticEquality = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "?=");
        public static readonly Operator BinaryEvaluation = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "is");
        public static readonly Operator BinaryHorn = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 10, ":-", "-?");
        public static readonly Operator UnaryUnprovability = new Operator(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 30, "\\+");
        public static readonly Operator UnaryNegative = new Operator(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 5, "-");
        public static readonly Operator UnaryPositive = new Operator(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 5, "+");

        public static readonly Operator[] DefinedOperators = new[] {
              BinaryConjunction
            , BinaryDisjunction
            , BinaryList
            , BinaryUnification
            , BinaryEvaluation
            , BinaryEquality
            , BinaryComparisonGt
            , BinaryComparisonGte
            , BinaryComparisonLt
            , BinaryComparisonLte
            , BinarySynctacticEquality
            , BinaryInequality
            , BinaryUnprovability
            , BinaryAssignment
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
