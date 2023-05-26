using Ergo.Lang.Ast.Terms.Interfaces;
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
    public readonly bool IsTailRecursive;
    //public readonly bool IsDeterminate;

    //public bool IsLastCallOptimizable => IsTailRecursive && IsDeterminate;

    private static bool GetIsTailRecursive(ITerm head, NTuple body)
    {
        if (head.Equals(WellKnown.Literals.TopLevel))
            return false;
        return IsLastCall(head, body);
    }

    public static bool IsLastCall(ITerm head, NTuple body)
    {
        if (head is Variable)
            return false;
        var calls = 0;
        var sign = head.GetSignature();
        var anon = sign.Functor.BuildAnonymousTerm(sign.Arity.GetOr(0));
        foreach (var (goal, i) in body.Contents.Select((g, i) => (g, i)))
        {
            if (goal is not Variable && anon.Unify(goal).TryGetValue(out _))
            {
                if (++calls > 1)
                    return false;
                if (i == body.Contents.Length - 1)
                    return true;
            }
        }
        return false;
    }

    public bool IsSameDeclarationAs(Predicate other)
    {
        if (other.DeclaringModule != DeclaringModule)
            return false;
        if (!other.Head.GetSignature().Equals(Head.GetSignature()))
            return false;
        return true;
    }

    public bool IsSameDefinitionAs(Predicate other)
    {
        if (!IsSameDeclarationAs(other))
            return false;
        if (!Head.NumberVars().Equals(other.Head.NumberVars()))
            return false;
        if (!Body.CanonicalForm.NumberVars().Equals(other.Body.CanonicalForm.NumberVars()))
            return false;
        return true;
    }

    public Predicate(string desc, Atom module, ITerm head, NTuple body, bool dynamic, bool exported, bool tailRecursive)
    {
        Documentation = desc;
        DeclaringModule = module;
        Head = head;
        Body = body;
        IsDynamic = dynamic;
        IsExported = exported;
        IsTailRecursive = tailRecursive;
    }

    public Predicate(string desc, Atom module, ITerm head, NTuple body, bool dynamic, bool exported)
        : this(desc, module, head, body, dynamic, exported, GetIsTailRecursive(head, body))
    {
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
            , IsTailRecursive
        );
    }

    public static Predicate Substitute(Predicate k, IEnumerable<Substitution> s)
        => new(k.Documentation, k.DeclaringModule, k.Head.Substitute(s), (NTuple)((IAbstractTerm)k.Body).Substitute(s), k.IsDynamic, k.IsExported, k.IsTailRecursive);

    public Predicate WithHead(ITerm newHead) => new(Documentation, DeclaringModule, newHead, Body, IsDynamic, IsExported, IsTailRecursive);
    public Predicate WithModuleName(Atom module) => new(Documentation, module, Head, Body, IsDynamic, IsExported, IsTailRecursive);
    public Predicate Dynamic() => new(Documentation, DeclaringModule, Head, Body, true, IsExported, IsTailRecursive);
    public Predicate Exported() => new(Documentation, DeclaringModule, Head, Body, IsDynamic, true, IsTailRecursive);
    public Predicate Qualified()
    {
        if (Head.IsQualified)
            return this;
        return new(Documentation, DeclaringModule, Head.Qualified(DeclaringModule), Body, IsDynamic, IsExported, IsTailRecursive);
    }

    // TODO: Conform to abstract term method FromCanonical
    public static bool FromCanonical(ITerm term, Atom defaultModule, out Predicate pred)
    {
        if (term is Complex c && WellKnown.Functors.Horn.Contains(c.Functor))
        {
            var head = c.Arguments[0];
            var body = c.Arguments[1].IsAbstract<NTuple>()
                .GetOr(new NTuple(new[] { c.Arguments[1] }));

            var mod = head.GetQualification(out head).GetOr(defaultModule);
            pred = new("(dynamic)", mod, head, body, true, false);
            return true;
        }

        var module = term.GetQualification(out term).GetOr(defaultModule);
        pred = new("(dynamic)", module, term, new NTuple(ImmutableArray<ITerm>.Empty.Add(WellKnown.Literals.True)), true, false);
        return true;
    }

    public string Explain(bool canonical)
    {
        string expl;
        var doc = Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r).Join("\r\n");
        if (Body.IsEmpty || Body.Contents.SequenceEqual(new ITerm[] { WellKnown.Literals.True }))
        {
            expl = $"{Head.Explain()}.";
            if (!canonical && !string.IsNullOrWhiteSpace(Documentation))
                expl = $"{doc}\r\n{expl}";
        }
        else
        {
            expl = $"{Head.Explain()}{(canonical ? '←' : " ←\r\n\t")}{Body.Contents.Join(x => x.Explain(canonical), canonical ? "," : ",\r\n\t")}.";
            if (!canonical && !string.IsNullOrWhiteSpace(Documentation))
                expl = $"{doc}\r\n{expl}";
        }

        return expl;
    }
}