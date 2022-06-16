namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Functors
    {
        public static readonly Atom[] Conjunction = new Atom[] { new(","), new("∧") };
        public static readonly Atom[] HeadTail = new Atom[] { new("|") };
        public static readonly Atom[] List = new Atom[] { new("[|]") };
        public static readonly Atom[] BracyList = new Atom[] { new("{|}") };
        public static readonly Atom[] CommaList = Conjunction;
        public static readonly Atom[] Multiplication = new Atom[] { new("*") };
        public static readonly Atom[] Division = new Atom[] { new("/") };
        public static readonly Atom[] IntDivision = new Atom[] { new("//") };
        public static readonly Atom[] Arity = Division;
        public static readonly Atom[] Module = new Atom[] { new("::") };
        public static readonly Atom[] Dict = new Atom[] { new("dict") };
        public static readonly Atom[] NamedArgument = new Atom[] { new(":") };
        public static readonly Atom[] DictAccess = new Atom[] { new(".") };
        public static readonly Atom[] Addition = new Atom[] { new("+") };
        public static readonly Atom[] Plus = Addition;
        public static readonly Atom[] Subtraction = new Atom[] { new("-") };
        public static readonly Atom[] SignatureTag = new Atom[] { new("^") };
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