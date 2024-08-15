using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Runtime;

namespace Tests;

public class ErgoTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : ErgoTestFixture
{
    public readonly ErgoInterpreter Interpreter = fixture.Interpreter;
    public InterpreterScope InterpreterScope = fixture.InterpreterScope;
    public KnowledgeBase KnowledgeBase = fixture.KnowledgeBase;

    // "⊤" : "⊥"
    protected void ShouldParse<T>(string query, T expected)
    {
        var parsed = InterpreterScope.Parse<T>(query)
            .GetOrThrow(new InvalidOperationException());
        if (parsed is IExplainable expl && expected is IExplainable expExpl)
            Assert.Equal(expl.Explain(true), expExpl.Explain(true));
        else Assert.Equal(parsed, expected);
    }
    protected void ShouldParse<T>(string query, Func<T, bool> expected)
    {
        var parsed = InterpreterScope.Parse<T>(query)
            .GetOrThrow(new InvalidOperationException());
        if (parsed is IExplainable expl && expected is IExplainable expExpl)
            Assert.Equal(expl.Explain(true), expExpl.Explain(true));
        else Assert.True(expected(parsed));
    }
    // "⊤" : "⊥"
    protected void ShouldNotParse<T>(string query, T expected)
    {
        var parsed = InterpreterScope.Parse<T>(query, onParseFail: _ => default);
        if (parsed.TryGetValue(out var value))
        {
            Assert.NotEqual(value, expected);
        }
    }
    // "⊤" : "⊥"
    protected void ShouldSolve(string query, int expectedSolutions, bool checkParse, params string[] expected)
    {
        if (expected.Length != 0)
            Assert.Equal(expectedSolutions, expected.Length);
        var parsed = InterpreterScope.Parse<Query>(query)
            .GetOrThrow(new InvalidOperationException());
        if (checkParse)
            Assert.Equal(query, ((ITerm)parsed.Goals).StripTemporaryVariables().Explain(false));
        Optimized();
        void Optimized()
        {
            var vm = Interpreter.Facade.BuildVM(KnowledgeBase.Clone(), DecimalType.BigDecimal);
            Solve(vm, parsed);
        }

        void Solve(ErgoVM vm, Query parsed)
        {
            var numSolutions = 0;
            vm.Query = vm.CompileQuery(parsed, CompilerFlags.Default);
            vm.Run();
            foreach (var sol in vm.Solutions)
            {
                Assert.InRange(++numSolutions, 1, expectedSolutions);
                if (expected.Length != 0)
                {
                    var check = sol.Simplify().Substitutions.OrderBy(x => x.Lhs).Join(s => s.Explain(), ";");
                    Assert.Equal(expected[numSolutions - 1], check);
                }
            }

            Assert.Equal(expectedSolutions, numSolutions);
        }
    }
}