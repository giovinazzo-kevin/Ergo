using Ergo.Interpreter.Directives;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain() }")]
public readonly struct ErgoProgram : IExplainable
{
    public readonly Directive[] Directives;
    public readonly KnowledgeBase KnowledgeBase;
    public readonly bool IsPartial;

    public string Explain(bool canonical)
    {
        return Directives.Select(d => d.Explain(canonical)).Concat(KnowledgeBase.Select(r => r.Explain(canonical)))
            .Join("\r\n\r\n");
    }

    public ErgoProgram(Directive[] directives, Predicate[] kb)
    {
        Directives = directives;
        KnowledgeBase = new KnowledgeBase(default);
        foreach (var k in kb)
        {
            KnowledgeBase.AssertZ(k);
        }

        IsPartial = false;
    }

    private ErgoProgram(Directive[] dirs, KnowledgeBase kb, bool partial)
    {
        Directives = dirs;
        KnowledgeBase = kb;
        IsPartial = partial;
    }

    public ErgoProgram Clone() => new(Directives, KnowledgeBase.Clone(), IsPartial);

    public ErgoProgram AsPartial(bool partial) => new(Directives, KnowledgeBase, partial);

    public static ErgoProgram Empty(Atom module) => new ErgoProgram(
        [new Directive(new Complex(new DeclareModule().Signature.Functor, module, WellKnown.Literals.EmptyList), string.Empty)],
        []
    );
}

