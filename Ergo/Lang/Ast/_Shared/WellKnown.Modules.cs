namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Modules
    {
        public static readonly Atom Prologue = new("prologue");
        public static readonly Atom List = new("list");
        public static readonly Atom Dict = new("dict");
        public static readonly Atom Set = new("set");
        public static readonly Atom Math = new("math");
        public static readonly Atom Lambda = new("lambda");
        public static readonly Atom Meta = new("meta");
        public static readonly Atom IO = new("io");
        public static readonly Atom Reflection = new("reflection");
        public static readonly Atom String = new("string");
        public static readonly Atom CSharp = new("csharp");
        public static readonly Atom Tabling = new("tabling");
        public static readonly Atom Expansions = new("expansions");
        public static readonly Atom Async = new("async");

        public static readonly Atom Stdlib = new("stdlib");
        public static readonly Atom Compiler = new("compiler");
        public static readonly Atom User = new("user");
    }
}
