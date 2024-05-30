namespace Ergo.Lang.Ast;

public static partial class WellKnown
{

    public static class Functors
    {
        public static readonly HashSet<Atom> Conjunction = new([",", "∧"]);
        public static readonly HashSet<Atom> Disjunction = new([";", "∨"]);
        public static readonly HashSet<Atom> Lambda = new([">>"]);
        public static readonly HashSet<Atom> HeadTail = new(["|"]);
        public static readonly HashSet<Atom> List = new(["[|]"]);
        public static readonly HashSet<Atom> Set = new(["{|}"]);
        public static readonly HashSet<Atom> Tuple = Conjunction;
        public static readonly HashSet<Atom> Multiplication = new(["*"]);
        public static readonly HashSet<Atom> Division = new(["/"]);
        public static readonly HashSet<Atom> IntDivision = new(["//"]);
        public static readonly HashSet<Atom> Floor = new(["floor"]);
        public static readonly HashSet<Atom> Round = new(["round"]);
        public static readonly HashSet<Atom> Ceiling = new(["ceil"]);
        public static readonly HashSet<Atom> Arity = Division;
        public static readonly HashSet<Atom> Module = new([":"]);
        public static readonly HashSet<Atom> Dict = new(["dict"]);
        public static readonly HashSet<Atom> NamedArgument = new([":"]);
        public static readonly HashSet<Atom> DictAccess = new(["."]);
        public static readonly HashSet<Atom> Addition = new(["+"]);
        public static readonly HashSet<Atom> Plus = Addition;
        public static readonly HashSet<Atom> Subtraction = new(["-"]);
        public static readonly HashSet<Atom> SignatureTag = new(["^"]);
        public static readonly HashSet<Atom> Minus = Subtraction;
        public static readonly HashSet<Atom> Power = new(["^"]);
        public static readonly HashSet<Atom> SquareRoot = new(["√", "sqrt"]);
        public static readonly HashSet<Atom> Modulo = new(["mod"]);
        public static readonly HashSet<Atom> AbsoluteValue = new(["abs"]);
        public static readonly HashSet<Atom> Unification = new(["="]);
        public static readonly HashSet<Atom> Gt = new([">"]);
        public static readonly HashSet<Atom> Gte = new(["≥", ">="]);
        public static readonly HashSet<Atom> Lt = new(["<"]);
        public static readonly HashSet<Atom> Lte = new(["≤", "<="]);
        public static readonly HashSet<Atom> Horn = new(["←", ":-"]);
        public static readonly HashSet<Atom> ExistentialQualifier = new(["^"]);
        public static readonly HashSet<Atom> If = new(["->"]);
        public static readonly HashSet<Atom> Not = new(["¬", "\\+"]);
    }
}