using Ergo.Lang.Extensions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang.Ast
{
    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Module
    {
        public readonly Atom Name;
        public readonly List Exports;
        public readonly List Imports;
        public readonly ImmutableArray<Operator> Operators;
        public readonly ImmutableDictionary<Atom, Literal> Literals;
        public readonly ImmutableHashSet<Signature> DynamicPredicates;
        public readonly ErgoProgram Program;
        public readonly bool Runtime;

        public Module(Atom name, bool runtime)
            : this(name, List.Empty, List.Empty, ImmutableArray<Operator>.Empty, ImmutableDictionary<Atom, Literal>.Empty, ImmutableHashSet<Signature>.Empty, ErgoProgram.Empty(name), runtime)
        {

        }

        public Module(
            Atom name, 
            List import, 
            List export, 
            ImmutableArray<Operator> operators, 
            ImmutableDictionary<Atom, Literal> literals,
            ImmutableHashSet<Signature> dynamicPredicates,
            ErgoProgram program, 
            bool runtime = false
        )
        {
            Name = name;
            Imports = import;
            Exports = export;
            Operators = operators;
            Literals = literals;
            Program = program;
            Runtime = runtime;
            DynamicPredicates = dynamicPredicates;
        }

        public string Explain()
        {
            var expl = $"← module({Name.Explain()}, {Exports.Explain()}).";
            return expl;
        }

        public Module WithImport(Atom import) => new(Name, new(Imports.Contents.Add(import)), Exports, Operators, Literals, DynamicPredicates, Program, Runtime);
        public Module WithExports(ImmutableArray<ITerm> exports) => new(Name, Imports, new(exports), Operators, Literals, DynamicPredicates, Program, Runtime);
        public Module WithOperators(ImmutableArray<Operator> operators) => new(Name, Imports, Exports, operators, Literals, DynamicPredicates, Program, Runtime);
        public Module WithoutOperator(OperatorAffix affix, Atom[] synonyms) => new(Name, Imports, Exports, Operators.RemoveAll(op => op.Affix == affix && op.Synonyms.SequenceEqual(synonyms)), Literals, DynamicPredicates, Program, Runtime);
        public Module WithOperator(Operator op) => new(Name, Imports, Exports, Operators.Add(op), Literals, DynamicPredicates, Program, Runtime);
        public Module WithLiterals(ImmutableDictionary<Atom, Literal> literals) => new(Name, Imports, Exports, Operators, literals, DynamicPredicates, Program, Runtime);
        public Module WithLiteral(Literal literal) => new(Name, Imports, Exports, Operators, Literals.Add(literal.Key, literal), DynamicPredicates, Program, Runtime);
        public Module WithDynamicPredicates(ImmutableHashSet<Signature> predicates) => new(Name, Imports, Exports, Operators, Literals, predicates, Program, Runtime);
        public Module WithDynamicPredicate(Signature predicate) => new(Name, Imports, Exports, Operators, Literals, DynamicPredicates.Add(predicate.WithModule(Maybe.Some(Name))), Program, Runtime);
        public Module WithProgram(ErgoProgram p) => new(Name, Imports, Exports, Operators, Literals, DynamicPredicates, p, Runtime);

        public bool ContainsExport(Signature sig)
        {
            return Exports.Contents.Any(t => t.Matches(out var m, new { P = default(string), A = default(int) })
                && m.P == sig.Functor.Explain()
                && (!sig.Arity.HasValue || m.A == sig.Arity.Reduce(x => x, () => 0)));
        }
    }
}
