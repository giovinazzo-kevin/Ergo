using Ergo.Interpreter.Libraries;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct Module
{
    public readonly Atom Name;
    public readonly List Exports;
    public readonly List Imports;
    public readonly ImmutableArray<Operator> Operators;
    public readonly ImmutableHashSet<Signature> DynamicPredicates;
    public readonly Maybe<Library> LinkedLibrary;
    public readonly ErgoProgram Program;
    public readonly bool IsRuntime;
    public readonly int LoadOrder;

    public Module(Atom name, bool runtime)
        : this(
              name,
              List.Empty,
              List.Empty,
              ImmutableArray<Operator>.Empty,
              ImmutableHashSet<Signature>.Empty,
              ErgoProgram.Empty(name),
              default,
              runtime
            )
    {

    }

    public Module(
        Atom name,
        List import,
        List export,
        ImmutableArray<Operator> operators,
        ImmutableHashSet<Signature> dynamicPredicates,
        ErgoProgram program,
        Maybe<Library> linkedLibrary,
        bool runtime = false,
        int loadOrder = 0
    )
    {
        Name = name;
        Imports = import;
        Exports = export;
        Operators = operators;
        Program = program;
        IsRuntime = runtime;
        DynamicPredicates = dynamicPredicates;
        LinkedLibrary = linkedLibrary;
        LoadOrder = loadOrder;
    }

    public string Explain()
    {
        var expl = $"← module({Name.Explain()}, {Exports.Explain()}).";
        return expl;
    }

    public Module WithImport(Atom import) => new(Name, new(Imports.Contents.Add(import)), Exports, Operators, DynamicPredicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithExports(ImmutableArray<ITerm> exports) => new(Name, Imports, new(exports), Operators, DynamicPredicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithOperators(ImmutableArray<Operator> operators) => new(Name, Imports, Exports, operators, DynamicPredicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithoutOperator(Fixity affix, Atom[] synonyms) => new(Name, Imports, Exports, Operators.RemoveAll(op => op.Fixity == affix && op.Synonyms.SequenceEqual(synonyms)), DynamicPredicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithOperator(Operator op) => new(Name, Imports, Exports, Operators.Add(op), DynamicPredicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithDynamicPredicates(ImmutableHashSet<Signature> predicates) => new(Name, Imports, Exports, Operators, predicates, Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithDynamicPredicate(Signature predicate) => new(Name, Imports, Exports, Operators, DynamicPredicates.Add(predicate.WithModule(Name)), Program, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithProgram(ErgoProgram p) => new(Name, Imports, Exports, Operators, DynamicPredicates, p, LinkedLibrary, IsRuntime, LoadOrder);
    public Module WithLinkedLibrary(Maybe<Library> lib) => new(Name, Imports, Exports, Operators, DynamicPredicates, Program, lib, IsRuntime, LoadOrder);
    public Module AsRuntime(bool runtime) => new(Name, Imports, Exports, Operators, DynamicPredicates, Program, LinkedLibrary, runtime, LoadOrder);
    public Module WithLoadOrder(int loadOrder) => new(Name, Imports, Exports, Operators, DynamicPredicates, Program, LinkedLibrary, IsRuntime, loadOrder);

    public bool ContainsExport(Signature sign)
    {
        var form = new Complex(WellKnown.Operators.ArityIndicator.CanonicalFunctor, sign.Functor, new Atom((decimal)sign.Arity.GetOr(default)))
            .AsOperator(WellKnown.Operators.ArityIndicator);
        return Exports.Contents.Any(t => t.Equals(form));
    }
}
