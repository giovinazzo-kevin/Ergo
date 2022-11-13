namespace Ergo.Lang.Ast;
public readonly record struct Expansion(Atom DeclaringModule, Variable OutVariable, Predicate Predicate);