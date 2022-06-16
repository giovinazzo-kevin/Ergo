using Ergo.Interpreter;

namespace Ergo.Lang.Ast;

public static partial class WellKnown
{
    public static class Operators
    {
        // Unary prefix operators always associate right-to-left and unary postfix operators always associate left-to-right.
        // To prevent cases where operands would be associated with two operators, or no operator at all,
        //   operators with the same precedence must have the same associativity. 
        // As few operators as possible are defined here; most are defined in the standard library for consistency.

        // The Horn operator is fundamental to Prolog, so it must be defined a-priori. It's necessary for both directives and clauses.
        public static readonly Operator UnaryHorn = new(Modules.Stdlib, OperatorAffix.Prefix, OperatorAssociativity.Right, 10, Functors.Horn);
        public static readonly Operator BinaryHorn = new(Modules.Stdlib, OperatorAffix.Infix, OperatorAssociativity.None, 10, Functors.Horn);
        // The arity indicator is a bootstrapping operator that's used while parsing directives; the real definition is the division operator in the math module.
        public static readonly Operator ArityIndicator = new(Modules.Stdlib, OperatorAffix.Infix, OperatorAssociativity.Left, 500, Functors.Arity);
        public static readonly Operator Conjunction = new(Modules.Stdlib, OperatorAffix.Infix, OperatorAssociativity.Right, 20, Functors.Conjunction);

        public static readonly Operator[] DefinedOperators = new[] {
            UnaryHorn, BinaryHorn, ArityIndicator, Conjunction
        };
    }
}
