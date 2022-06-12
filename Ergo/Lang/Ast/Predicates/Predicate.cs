using System.Collections.Immutable;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(true) }")]
public readonly struct Predicate : IExplainable
{
    public readonly Atom DeclaringModule;
    public readonly ITerm Head;
    public readonly CommaSequence Body;
    public readonly string Documentation;
    public readonly bool IsDynamic;

    public string Explain(bool canonical)
    {
        if (Body.IsEmpty || Body.Contents.SequenceEqual(new ITerm[] { WellKnown.Literals.True }))
        {
            return $"{Head.Explain()}.";
        }

        var expl = $"{Head.Explain()}{(canonical ? '←' : " ←\r\n\t")}{string.Join(canonical ? "," : ",\r\n\t", Body.Contents.Select(x => x.Explain(canonical)))}.";
        if (!canonical && !String.IsNullOrWhiteSpace(Documentation))
        {
            expl = $"{String.Join("\r\n", Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r))}\r\n" + expl;
        }
        return expl;
    }

    public static int Arity(ITerm head) => head.Reduce(a => 0, v => 0, c => c.Arity, d => 2);

    public Predicate(string desc, Atom module, ITerm head, CommaSequence body, bool dynamic = false)
    {
        Documentation = desc;
        DeclaringModule = module;
        Head = head;
        Body = body;
        IsDynamic = dynamic;
    }

    public Predicate Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new Dictionary<string, Variable>();
        return new Predicate(
            Documentation
            , DeclaringModule
            , Head.Instantiate(ctx, vars)
            , (CommaSequence)Body.Instantiate(ctx, vars)
        );
    }

    public static Predicate Substitute(Predicate k, IEnumerable<Substitution> s)
    {
        return new Predicate(k.Documentation, k.DeclaringModule, k.Head.Substitute(s), (CommaSequence)k.Body.Substitute(s));
    }

    public Predicate WithModuleName(Atom module) => new(Documentation, module, Head, Body);

    public Predicate Qualified()
    {
        if (Head.IsQualified || !Head.TryQualify(DeclaringModule, out var head))
            return this;
        return new(Documentation, DeclaringModule, head, Body);
    }

    public static bool TryUnify(ITerm head, Predicate predicate, out IEnumerable<Substitution> substitutions)
    {
        var S = new List<Substitution>();
        predicate.Head.TryGetQualification(out _, out var qv);
        head.TryGetQualification(out _, out var hv);
        if (hv.Unify(qv).TryGetValue(out var subs))
        {
            S.AddRange(subs);
            substitutions = S;
            return true;
        }
        substitutions = default;
        return false;
    }

    public static bool TryUnfold(ITerm term, Atom defaultModule, out Predicate pred)
    {
        if (term is Complex c && WellKnown.Functors.Horn.Contains(c.Functor))
        {
            var head_ = c.Arguments[0];
            if (!CommaSequence.TryUnfold(c.Arguments[1], out var body))
            {
                body = new CommaSequence(ImmutableArray<ITerm>.Empty.Add(c.Arguments[1]));
            }
            if (!head_.TryGetQualification(out var module_, out head_))
            {
                module_ = defaultModule;
            }
            pred = new("(dynamic)", module_, head_, body);
            return true;
        }
        if (!term.TryGetQualification(out var module, out var head))
        {
            module = defaultModule;
        }
        pred = new("(dynamic)", module, head, new CommaSequence(ImmutableArray<ITerm>.Empty.Add(WellKnown.Literals.True)));
        return true;
    }

    public Predicate AsDynamic() => new(Documentation, DeclaringModule, Head, Body, true);
}