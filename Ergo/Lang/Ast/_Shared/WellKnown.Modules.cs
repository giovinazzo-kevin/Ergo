namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Modules
    {
        public static readonly Atom Prologue = "prologue";
        public static readonly Atom List = "list";
        public static readonly Atom Dict = "dict";
        public static readonly Atom Set = "set";
        public static readonly Atom Math = "math";
        public static readonly Atom Lambda = "lambda";
        public static readonly Atom Meta = "meta";
        public static readonly Atom IO = "io";
        public static readonly Atom Reflection = "reflection";
        public static readonly Atom String = "string";
        public static readonly Atom CSharp = "csharp";
        public static readonly Atom Tabling = "tabling";
        public static readonly Atom Expansions = "expansions";
        public static readonly Atom Async = "async";

        public static readonly Atom Stdlib = "stdlib";
        public static readonly Atom Compiler = "compiler";
        public static readonly Atom User = "user";
    }
}
