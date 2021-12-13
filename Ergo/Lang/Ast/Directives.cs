namespace Ergo.Lang
{
    public static class Directives
    {
        public static readonly Directive Module = new("Defines a module and its export list.", Complex.OfArity(new("module"), 2));
        public static readonly Directive UseModule = new("Imports another module's predicates.", Complex.OfArity(new("use_module"), 1));
    }
}
