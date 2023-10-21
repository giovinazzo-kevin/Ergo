namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Functors
    {
        public static readonly HashSet<Atom> Conjunction = new(new Atom[] { new(","), new("∧") });
        public static readonly HashSet<Atom> Disjunction = new(new Atom[] { new(";"), new("∨") });
        public static readonly HashSet<Atom> Lambda = new(new Atom[] { new(">>") });
        public static readonly HashSet<Atom> HeadTail = new(new Atom[] { new("|") });
        public static readonly HashSet<Atom> List = new(new Atom[] { new("[|]") });
        public static readonly HashSet<Atom> Set = new(new Atom[] { new("{|}") });
        public static readonly HashSet<Atom> Tuple = Conjunction;
        public static readonly HashSet<Atom> Multiplication = new(new Atom[] { new("*") });
        public static readonly HashSet<Atom> Division = new(new Atom[] { new("/") });
        public static readonly HashSet<Atom> IntDivision = new(new Atom[] { new("//") });
        public static readonly HashSet<Atom> Floor = new(new Atom[] { new("floor") });
        public static readonly HashSet<Atom> Round = new(new Atom[] { new("round") });
        public static readonly HashSet<Atom> Ceiling = new(new Atom[] { new("ceil") });
        public static readonly HashSet<Atom> Arity = Division;
        public static readonly HashSet<Atom> Module = new(new Atom[] { new(":") });
        public static readonly HashSet<Atom> Dict = new(new Atom[] { new("dict") });
        public static readonly HashSet<Atom> NamedArgument = new(new Atom[] { new(":") });
        public static readonly HashSet<Atom> DictAccess = new(new Atom[] { new(".") });
        public static readonly HashSet<Atom> Addition = new(new Atom[] { new("+") });
        public static readonly HashSet<Atom> Plus = Addition;
        public static readonly HashSet<Atom> Subtraction = new(new Atom[] { new("-") });
        public static readonly HashSet<Atom> SignatureTag = new(new Atom[] { new("^") });
        public static readonly HashSet<Atom> Minus = Subtraction;
        public static readonly HashSet<Atom> Power = new(new Atom[] { new("^") });
        public static readonly HashSet<Atom> SquareRoot = new(new Atom[] { new("√"), new("sqrt") });
        public static readonly HashSet<Atom> Modulo = new(new Atom[] { new("mod") });
        public static readonly HashSet<Atom> AbsoluteValue = new(new Atom[] { new("abs") });
        public static readonly HashSet<Atom> Unification = new(new Atom[] { new("=") });
        public static readonly HashSet<Atom> Gt = new(new Atom[] { new(">") });
        public static readonly HashSet<Atom> Gte = new(new Atom[] { new("≥"), new(">=") });
        public static readonly HashSet<Atom> Lt = new(new Atom[] { new("<") });
        public static readonly HashSet<Atom> Lte = new(new Atom[] { new("≤"), new("<=") });
        public static readonly HashSet<Atom> Horn = new(new Atom[] { new("←"), new(":-") });
        public static readonly HashSet<Atom> ExistentialQualifier = new(new Atom[] { new("^") });
        public static readonly HashSet<Atom> If = new(new Atom[] { new("->") });
        public static readonly HashSet<Atom> Not = new(new Atom[] { new("¬"), new("\\+") });
    }
}