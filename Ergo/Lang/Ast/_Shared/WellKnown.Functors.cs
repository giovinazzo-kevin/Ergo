namespace Ergo.Lang.Ast;

public static partial class WellKnown
{

    public static class Functors
    {
        public static readonly HashSet<Atom> Conjunction = new([new(","), new("∧")]);
        public static readonly HashSet<Atom> Disjunction = new([new(";"), new("∨")]);
        public static readonly HashSet<Atom> Lambda = new([new(">>")]);
        public static readonly HashSet<Atom> HeadTail = new([new("|")]);
        public static readonly HashSet<Atom> List = new([new("[|]")]);
        public static readonly HashSet<Atom> Set = new([new("{|}")]);
        public static readonly HashSet<Atom> Tuple = Conjunction;
        public static readonly HashSet<Atom> Multiplication = new([new("*")]);
        public static readonly HashSet<Atom> Division = new([new("/")]);
        public static readonly HashSet<Atom> IntDivision = new([new("//")]);
        public static readonly HashSet<Atom> Floor = new([new("floor")]);
        public static readonly HashSet<Atom> Round = new([new("round")]);
        public static readonly HashSet<Atom> Ceiling = new([new("ceil")]);
        public static readonly HashSet<Atom> Arity = Division;
        public static readonly HashSet<Atom> Module = new([new(":")]);
        public static readonly HashSet<Atom> Dict = new([new("dict")]);
        public static readonly HashSet<Atom> NamedArgument = new([new(":")]);
        public static readonly HashSet<Atom> DictAccess = new([new(".")]);
        public static readonly HashSet<Atom> Addition = new([new("+")]);
        public static readonly HashSet<Atom> Plus = Addition;
        public static readonly HashSet<Atom> Subtraction = new([new("-")]);
        public static readonly HashSet<Atom> SignatureTag = new([new("^")]);
        public static readonly HashSet<Atom> Minus = Subtraction;
        public static readonly HashSet<Atom> Power = new([new("^")]);
        public static readonly HashSet<Atom> SquareRoot = new([new("√"), new("sqrt")]);
        public static readonly HashSet<Atom> Modulo = new([new("mod")]);
        public static readonly HashSet<Atom> AbsoluteValue = new([new("abs")]);
        public static readonly HashSet<Atom> Unification = new([new("=")]);
        public static readonly HashSet<Atom> Gt = new([new(">")]);
        public static readonly HashSet<Atom> Gte = new([new("≥"), new(">=")]);
        public static readonly HashSet<Atom> Lt = new([new("<")]);
        public static readonly HashSet<Atom> Lte = new([new("≤"), new("<=")]);
        public static readonly HashSet<Atom> Horn = new([new("←"), new(":-")]);
        public static readonly HashSet<Atom> ExistentialQualifier = new([new("^")]);
        public static readonly HashSet<Atom> If = new([new("->")]);
        public static readonly HashSet<Atom> Not = new([new("¬"), new("\\+")]);
    }
}