namespace Ergo.Lang
{
    public static class Directives
    {
        public static readonly Directive ChooseModule = new("Selects the current module.", Complex.OfArity(new("module"), 1));
        public static readonly Directive DefineModule = new("Defines a module and its export list.", Complex.OfArity(new("module"), 2));
        public static readonly Directive UseModule = new("Imports another module's predicates.", Complex.OfArity(new("use_module"), 1));
    }
}
