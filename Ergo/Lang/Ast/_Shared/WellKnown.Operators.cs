namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        // As few operators as possible are defined here; most are defined in the Ergo standard library for consistency.

        // The Horn operator is fundamental to Prolog, so it must be defined a-priori. It's necessary for both directives and clauses.
        public static readonly Operator UnaryHorn = new(WellKnown.Modules.Stdlib, Fixity.Prefix, OperatorAssociativity.Right, 10, Functors.Horn);
        public static readonly Operator BinaryHorn = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.None, 10, Functors.Horn);
        // The arity indicator is a bootstrapping operator that's used while parsing directives; the real definition is the division operator in the math module.
        public static readonly Operator ArityIndicator = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Left, 600, Functors.Arity);
        public static readonly Operator Conjunction = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Right, 40, Functors.Conjunction);
        // These operators need to be defined here as the parser needs to reference them directly.
        public static readonly Operator Module = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Right, 5, Functors.Module);
        public static readonly Operator Unification = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.None, 50, Functors.Unification);
        // These are defined so that the built-in abstract terms can reference them for folding their contents.
        public static readonly Operator List = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Right, 41, Functors.List);
        public static readonly Operator Set = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Right, 42, Functors.Set);
        public static readonly Operator NamedArgument = new(WellKnown.Modules.Stdlib, Fixity.Infix, OperatorAssociativity.Right, 1000, Functors.NamedArgument);

        public static readonly Operator[] DeclaredOperators = new[] {
            UnaryHorn, BinaryHorn, ArityIndicator, Conjunction
        };
    }
}
