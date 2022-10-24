using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct Module
{
    public readonly Atom Name;
    public readonly List Exports;
    public readonly List Imports;
    public readonly ImmutableArray<Operator> Operators;
    public readonly ImmutableDictionary<Signature, ImmutableArray<Expansion>> Expansions;
    public readonly ImmutableHashSet<Signature> DynamicPredicates;
    public readonly ImmutableHashSet<Signature> TabledPredicates;
    public readonly ErgoProgram Program;
    public readonly bool IsRuntime;

    public Module(Atom name, bool runtime)
        : this(
              name,
              List.Empty,
              List.Empty,
              ImmutableArray<Operator>.Empty,
              ImmutableDictionary<Signature,
              ImmutableArray<Expansion>>.Empty,
              ImmutableHashSet<Signature>.Empty,
              ImmutableHashSet<Signature>.Empty,
              ErgoProgram.Empty(name),
              runtime
            )
    {

    }

    public Module(
        Atom name,
        List import,
        List export,
        ImmutableArray<Operator> operators,
        ImmutableDictionary<Signature, ImmutableArray<Expansion>> literals,
        ImmutableHashSet<Signature> dynamicPredicates,
        ImmutableHashSet<Signature> tabledPredicates,
        ErgoProgram program,
        bool runtime = false
    )
    {
        Name = name;
        Imports = import;
        Exports = export;
        Operators = operators;
        Expansions = literals;
        Program = program;
        IsRuntime = runtime;
        DynamicPredicates = dynamicPredicates;
        TabledPredicates = tabledPredicates;
    }

    public string Explain()
    {
        var expl = $"← module({Name.Explain()}, {Exports.Explain()}).";
        return expl;
    }

    public Module WithImport(Atom import) => new(Name, new(Imports.Contents.Add(import)), Exports, Operators, Expansions, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithExports(ImmutableArray<ITerm> exports) => new(Name, Imports, new(exports), Operators, Expansions, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithOperators(ImmutableArray<Operator> operators) => new(Name, Imports, Exports, operators, Expansions, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithoutOperator(OperatorAffix affix, Atom[] synonyms) => new(Name, Imports, Exports, Operators.RemoveAll(op => op.Affix == affix && op.Synonyms.SequenceEqual(synonyms)), Expansions, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithOperator(Operator op) => new(Name, Imports, Exports, Operators.Add(op), Expansions, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithExpansions(ImmutableDictionary<Signature, ImmutableArray<Expansion>> literals) => new(Name, Imports, Exports, Operators, literals, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    public Module WithExpansion(Variable outVar, Predicate pred)
    {
        var signature = pred.Head.GetSignature();
        if (!Expansions.TryGetValue(signature, out var arr))
        {
            arr = ImmutableArray<Expansion>.Empty;
        }

        var newLiterals = Expansions.SetItem(signature, arr.Add(new Expansion(outVar, pred)));
        return new(Name, Imports, Exports, Operators, newLiterals, DynamicPredicates, TabledPredicates, Program, IsRuntime);
    }
    public Module WithDynamicPredicates(ImmutableHashSet<Signature> predicates) => new(Name, Imports, Exports, Operators, Expansions, predicates, TabledPredicates, Program, IsRuntime);
    public Module WithDynamicPredicate(Signature predicate) => new(Name, Imports, Exports, Operators, Expansions, DynamicPredicates.Add(predicate.WithModule(Name)), TabledPredicates, Program, IsRuntime);
    public Module WithTabledPredicates(ImmutableHashSet<Signature> predicates) => new(Name, Imports, Exports, Operators, Expansions, DynamicPredicates, predicates, Program, IsRuntime);
    public Module WithTabledPredicate(Signature predicate) => new(Name, Imports, Exports, Operators, Expansions, DynamicPredicates, TabledPredicates.Add(predicate.WithModule(Name)), Program, IsRuntime);
    public Module WithProgram(ErgoProgram p) => new(Name, Imports, Exports, Operators, Expansions, DynamicPredicates, TabledPredicates, p, IsRuntime);

    public bool ContainsExport(Signature sign)
    {
        var form = new Complex(WellKnown.Functors.Arity.First(), sign.Functor, new Atom((decimal)sign.Arity.GetOr(default)))
            .AsOperator(OperatorAffix.Infix);
        return Exports.Contents.Any(t => t.Equals(form));
    }
}
