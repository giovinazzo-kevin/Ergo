namespace Ergo.Lang.Ast
{
    public static partial class WellKnown
    {
        public static class Functors
        {
            public static readonly Atom[] Conjunction = new Atom[] { new("∧"), new(",") };
            public static readonly Atom[] List = new Atom[] { new("|") };
            public static readonly Atom[] Multiplication = new Atom[] { new("*") };
            public static readonly Atom[] Division = new Atom[] { new("/") };
            public static readonly Atom[] IntDivision = new Atom[] { new("//") };
            public static readonly Atom[] Arity = Division;
            public static readonly Atom[] Module = new Atom[] { new("::") };
            public static readonly Atom[] Dict = new Atom[] { new("dict") };
            public static readonly Atom[] NamedArgument = new Atom[] { new(":") };
            public static readonly Atom[] Addition = new Atom[] { new("+") };
            public static readonly Atom[] Plus = Addition;
            public static readonly Atom[] Subtraction = new Atom[] { new("-") };
            public static readonly Atom[] Minus = Subtraction;
            public static readonly Atom[] Power = new Atom[] { new("^") };
            public static readonly Atom[] SquareRoot = new Atom[] { new("√"), new("sqrt") };
            public static readonly Atom[] Modulo = new Atom[] { new("mod") };
            public static readonly Atom[] Unification = new Atom[] { new("=") };
            public static readonly Atom[] Gt = new Atom[] { new(">") };
            public static readonly Atom[] Gte = new Atom[] { new("≥"), new(">=") };
            public static readonly Atom[] Lt = new Atom[] { new("<") };
            public static readonly Atom[] Lte = new Atom[] { new("≤"), new("<=") };
            public static readonly Atom[] Horn = new Atom[] { new("←"), new(":-") };
            public static readonly Atom[] ExistentialQualifier = new Atom[] { new("^") };
        }
    }

    //public static class Operators
    //{
    //    // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
    //    // To prevent cases where operands would be associated with two operators, or no operator at all,
    //    //   operators with the same precedence must have the same associativity. 
    //    public static readonly Operator BinaryConjunction = new(OperatorAffix.Infix, OperatorAssociativity.Right, 20, "∧", ",");
    //    public static readonly Operator BinaryList = new(OperatorAffix.Infix, OperatorAssociativity.Right, 15, "|");
    //    public static readonly Operator BinaryMultiplication = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "*");
    //    public static readonly Operator BinaryDivision = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "/");
    //    public static readonly Operator BinaryIntegerDivision = new(OperatorAffix.Infix, OperatorAssociativity.Left, 600, "//");
    //    public static readonly Operator BinarySum = new(OperatorAffix.Infix, OperatorAssociativity.Left, 500, "+");
    //    public static readonly Operator BinarySubtraction = new(OperatorAffix.Infix, OperatorAssociativity.Left, 500, "-");
    //    public static readonly Operator BinaryPower = new(OperatorAffix.Infix, OperatorAssociativity.Right, 700, "^");
    //    public static readonly Operator BinaryMod = new(OperatorAffix.Infix, OperatorAssociativity.Left, 300, "mod");
    //    public static readonly Operator BinaryUnification = new(OperatorAffix.Infix, OperatorAssociativity.None, 50, "=");
    //    public static readonly Operator BinaryComparisonGt = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, ">");
    //    public static readonly Operator BinaryComparisonGte = new(OperatorAffix.Infix, OperatorAssociativity.Right, 51, "≥", ">=");
    //    public static readonly Operator BinaryComparisonLt = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "<");
    //    public static readonly Operator BinaryComparisonLte = new(OperatorAffix.Infix, OperatorAssociativity.Left, 49, "≤", "<=");
    //    public static readonly Operator UnaryHorn = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 10, "←", ":-");
    //    public static readonly Operator BinaryHorn = new(OperatorAffix.Infix, OperatorAssociativity.None, 10, "←", ":-");
    //    public static readonly Operator UnaryNegative = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 5, "-");
    //    public static readonly Operator UnaryPositive = new(OperatorAffix.Prefix, OperatorAssociativity.Right, 5, "+");

    //    public static readonly Operator[] DefinedOperators = new[] {
    //          BinaryConjunction
    //        , BinaryList
    //        , BinaryUnification
    //        , BinaryComparisonGt
    //        , BinaryComparisonGte
    //        , BinaryComparisonLt
    //        , BinaryComparisonLte
    //        , UnaryHorn
    //        , BinaryHorn
    //        , BinarySum
    //        , BinarySubtraction
    //        , BinaryMultiplication
    //        , BinaryDivision
    //        , BinaryIntegerDivision
    //        , BinaryPower
    //        , BinaryMod
    //        , UnaryPositive
    //        , UnaryNegative
    //    };
    //}

}
