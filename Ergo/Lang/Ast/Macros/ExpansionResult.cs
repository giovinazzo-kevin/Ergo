namespace Ergo.Lang.Ast;

public readonly record struct ExpansionResult(ITerm Match, NTuple Expansion, Maybe<Variable> Binding);
