﻿using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(true) }")]
public readonly struct Predicate : IExplainable
{
    public readonly Atom DeclaringModule;
    public readonly ITerm Head;
    public readonly NTuple Body;
    public readonly string Documentation;
    public readonly bool IsDynamic;
    public readonly bool IsExported;

    public Predicate(string desc, Atom module, ITerm head, NTuple body, bool dynamic, bool exported)
    {
        Documentation = desc;
        DeclaringModule = module;
        Head = head;
        Body = body;
        IsDynamic = dynamic;
        IsExported = exported;
    }

    public Predicate Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new Dictionary<string, Variable>();
        return new Predicate(
            Documentation
            , DeclaringModule
            , Head.Instantiate(ctx, vars)
            , new NTuple(Body.Contents.Select(x => x.Instantiate(ctx, vars)))
            , IsDynamic
            , IsExported
        );
    }

    public static Predicate Substitute(Predicate k, IEnumerable<Substitution> s)
        => new(k.Documentation, k.DeclaringModule, k.Head.Substitute(s), (NTuple)((IAbstractTerm)k.Body).Substitute(s), k.IsDynamic, k.IsExported);

    public Predicate WithModuleName(Atom module) => new(Documentation, module, Head, Body, IsDynamic, IsExported);
    public Predicate Dynamic() => new(Documentation, DeclaringModule, Head, Body, true, IsExported);
    public Predicate Exported() => new(Documentation, DeclaringModule, Head, Body, IsDynamic, true);
    public Predicate Qualified()
    {
        if (Head.IsQualified || !Head.TryQualify(DeclaringModule, out var head))
            return this;
        return new(Documentation, DeclaringModule, head, Body, IsDynamic, IsExported);
    }

    // TODO: Conform to abstract term method FromCanonical
    public static bool FromCanonical(ITerm term, Atom defaultModule, out Predicate pred)
    {
        if (term is Complex c && WellKnown.Functors.Horn.Contains(c.Functor))
        {
            var head_ = c.Arguments[0];
            var body = c.Arguments[1].IsAbstract<NTuple>(out var tuple)
                ? tuple : new NTuple(new[] { c.Arguments[1] });

            if (!head_.TryGetQualification(out var module_, out head_))
            {
                module_ = defaultModule;
            }

            pred = new("(dynamic)", module_, head_, body, true, false);
            return true;
        }

        if (!term.TryGetQualification(out var module, out var head))
        {
            module = defaultModule;
        }

        pred = new("(dynamic)", module, head, new NTuple(ImmutableArray<ITerm>.Empty.Add(WellKnown.Literals.True)), true, false);
        return true;
    }

    public string Explain(bool canonical)
    {
        string expl;
        var doc = string.Join("\r\n", Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r));
        if (Body.IsEmpty || Body.Contents.SequenceEqual(new ITerm[] { WellKnown.Literals.True }))
        {
            expl = $"{Head.Explain()}.";
            if (!canonical && !string.IsNullOrWhiteSpace(Documentation))
                expl = $"{doc}\r\n{expl}";
        }
        else
        {
            expl = $"{Head.Explain()}{(canonical ? '←' : " ←\r\n\t")}{string.Join(canonical ? "," : ",\r\n\t", Body.Contents.Select(x => x.Explain(canonical)))}.";
            if (!canonical && !string.IsNullOrWhiteSpace(Documentation))
                expl = $"{doc}\r\n{expl}";
        }

        return expl;
    }
}