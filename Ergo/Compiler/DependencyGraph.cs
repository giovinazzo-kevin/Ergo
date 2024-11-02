using Ergo.Modules;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Compiler;

public abstract record ArgDefinition
{
    public abstract bool IsGround { get; }
}
public sealed record ConstArgDefinition(object Value) : ArgDefinition
{
    public override bool IsGround => true;
}
public sealed record VariableArgDefinition(int VariableIndex) : ArgDefinition
{
    public override bool IsGround => false;
}
public sealed record ComplexArgDefinition(object Functor, ArgDefinition[] Args) : ArgDefinition
{
    private readonly bool _isGround = Args.Any(x => x.IsGround);
    public override bool IsGround => _isGround;
}

public abstract record GoalDefinition;
public record StaticGoalDefinition(Signature Signature, ArgDefinition[] Args) : GoalDefinition
{
    public PredicateDefinition Callee { get; internal set; }
}

public record RuntimeGoalDefinition(ArgDefinition Goal) : GoalDefinition;

public record ClauseDefinition(
    Atom DeclaringModule,
    Maybe<Atom> DeclaredModule,
    Atom Functor,
    int Arity,
    ArgDefinition[] Args,
    GoalDefinition[] Goals,
    bool IsGround, 
    bool IsFactual, 
    bool IsTailRecursive, 
    Dictionary<Signature, PredicateDefinition> Dependencies
)
{
    public bool IsCyclical { get; internal set; }
    public int DependencyDepth { get; internal set; }
}
public record PredicateDefinition(
    Atom Module,
    Atom Functor,
    Maybe<int> Arity,
    Maybe<ErgoBuiltIn> BuiltIn,
    List<ClauseDefinition> Clauses,
    bool IsExported,
    bool IsDynamic
);

public record class ErgoDependencyGraph(ErgoModuleTree ModuleTree)
{
    protected static readonly ClauseDefinition TRUE = new(
        DeclaringModule: WellKnown.Modules.Stdlib,
        DeclaredModule: default,
        Functor: WellKnown.Literals.True,
        Arity: 0,
        Args: [],
        Goals: [],
        IsGround: true,
        IsFactual: true,
        IsTailRecursive: false,
        Dependencies: []
    );

    protected static readonly ClauseDefinition FALSE = new(
        DeclaringModule: WellKnown.Modules.Stdlib,
        DeclaredModule: default,
        Functor: WellKnown.Literals.False,
        Arity: 0,
        Args: [],
        Goals: [],
        IsGround: true,
        IsFactual: true,
        IsTailRecursive: false,
        Dependencies: []
    );

    public readonly Dictionary<Signature, PredicateDefinition> Predicates = new()
    {
        { WellKnown.Signatures.True, new(WellKnown.Modules.Stdlib, WellKnown.Literals.True, 0, default, [TRUE], IsExported: true, IsDynamic: false) },
        { WellKnown.Signatures.False, new(WellKnown.Modules.Stdlib, WellKnown.Literals.False, 0, default, [FALSE], IsExported: true, IsDynamic: false) },
    };
}
