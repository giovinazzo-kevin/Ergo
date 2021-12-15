using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        public static readonly Operator BinaryConjunction = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 20, ",");
        public static readonly Operator BinaryDisjunction = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 30, ";");
        public static readonly Operator BinaryList = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 15, "|");
        public static readonly Operator BinaryMultiplication = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "*");
        public static readonly Operator BinaryDivision = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "/");
        public static readonly Operator BinaryIntegerDivision = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "//");
        public static readonly Operator BinarySum = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "+");
        public static readonly Operator BinarySubtraction = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "-");
        public static readonly Operator BinaryColon = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 50, ":");
        public static readonly Operator BinaryPower = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 700, "^");
        public static readonly Operator BinaryMod = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 300, "mod");
        public static readonly Operator BinaryUnification = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "=");
        public static readonly Operator BinaryAssignment = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, ":=");
        public static readonly Operator BinaryEquality = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "==");
        public static readonly Operator BinaryInequality = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "\\==");
        public static readonly Operator BinaryTermComparisonGt = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, "#>");
        public static readonly Operator BinaryTermComparisonGte = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, "#>=");
        public static readonly Operator BinaryTermComparisonLt = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "#<");
        public static readonly Operator BinaryTermComparisonLte = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "#<=");
        public static readonly Operator BinaryComparisonGt = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, ">");
        public static readonly Operator BinaryComparisonGte = new(Operator.AffixType.Infix, Operator.AssociativityType.Right, 51, ">=");
        public static readonly Operator BinaryComparisonLt = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "<");
        public static readonly Operator BinaryComparisonLte = new(Operator.AffixType.Infix, Operator.AssociativityType.Left, 49, "<=");
        public static readonly Operator BinaryUnprovability = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "\\=");
        public static readonly Operator BinarySynctacticEquality = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "?=");
        public static readonly Operator BinaryEvaluation = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "is");
        public static readonly Operator UnaryHorn = new(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 10, ":-", "-?");
        public static readonly Operator BinaryHorn = new(Operator.AffixType.Infix, Operator.AssociativityType.None, 10, ":-", "-?");
        public static readonly Operator UnaryUnprovability = new(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 30, "\\+");
        public static readonly Operator UnaryNegative = new(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 5, "-");
        public static readonly Operator UnaryPositive = new(Operator.AffixType.Prefix, Operator.AssociativityType.Right, 5, "+");

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
            , BinaryTermComparisonGt
            , BinaryTermComparisonGte
            , BinaryTermComparisonLt
            , BinaryTermComparisonLte
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
