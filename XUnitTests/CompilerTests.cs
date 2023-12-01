

using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Runtime;

namespace Tests;

public class CompilerTests : ErgoTests
{
    public CompilerTests(ErgoTestFixture fixture) : base(fixture)
    {
        var inlining = new Atom("inlining");
        Interpreter.Load(ref InterpreterScope, inlining);
        InterpreterScope = InterpreterScope.WithModule(InterpreterScope.EntryModule
            .WithImport(inlining));
        KnowledgeBase = InterpreterScope.BuildKnowledgeBase(VMFlags.Default); // Triggers Compiler library
    }
    [Theory]
    [InlineData("inline_b", "inline_b.")] // instead of: inline_b :- inline_a.
    [InlineData("inline_c", "inline_c.")] // instead of: inline_c :- inline_b.
    [InlineData("inline_d", "inline_d.")] // instead of: inline_c :- inline_b.
    [InlineData("inline_f(X)", "inline_f(X) ←\r\n\tnoinline_a, X = 1 ; noinline_b, X = 2.")]
    [InlineData("inline_g", "inline_g.")] // instead of: inline_a ; inline_b
    [InlineData("inline_j(X)", "inline_j(X) ←\r\n\tX = 1 ; unify(X,2).")] // instead of: inline_h(X)
    public void ShouldInlineCorrectly(string head, string expectedExpl)
    {
        var maybeHead = Interpreter.Facade.Parse<ITerm>(InterpreterScope, head);
        if (!maybeHead.TryGetValue(out var headTerm))
            Assert.True(false);
        if (!KnowledgeBase.Get(headTerm.GetSignature()).TryGetValue(out var matches))
            Assert.True(false);
        Assert.Single(matches);
        var expl = matches.Single().Explain(false);
        Assert.Equal(expectedExpl, expl);
    }
}
