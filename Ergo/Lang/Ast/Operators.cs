using System.Linq;
using System.Reflection;

namespace Ergo.Lang
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        public static readonly Operator BinaryComma = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Right, 20, ",");
        public static readonly Operator BinaryAsterisk = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "*");
        public static readonly Operator BinaryForwardSlash = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "/");
        public static readonly Operator BinaryDoubleForwardSlash = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 600, "//");
        public static readonly Operator BinaryPlus = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "+");
        public static readonly Operator BinaryMinus = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 500, "-");
        public static readonly Operator BinaryConjunction = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 400, "/\\");
        public static readonly Operator BinaryDisjunction = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 400, "\\/");
        public static readonly Operator BinaryMod = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 300, "mod");
        public static readonly Operator BinaryXor = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.Left, 200, "xor");
        public static readonly Operator BinaryEquals = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "=");
        public static readonly Operator BinaryEvaluation = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 50, "is");
        public static readonly Operator BinaryHorn = new Operator(Operator.AffixType.Infix, Operator.AssociativityType.None, 10, ":-", "-?");

        public static readonly Operator[] DefinedOperators = new[] {
              BinaryComma
            , BinaryEquals
            , BinaryEvaluation
            , BinaryHorn
            , BinaryPlus
            , BinaryMinus
            , BinaryConjunction
            , BinaryDisjunction
            , BinaryAsterisk
            , BinaryForwardSlash
            , BinaryDoubleForwardSlash
            , BinaryXor
            , BinaryMod
        };
    }

}
