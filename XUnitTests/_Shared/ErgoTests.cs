using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;

namespace Tests;

public class ErgoTests : IClassFixture<ErgoTestFixture>
{
    public readonly ErgoInterpreter Interpreter;
    public InterpreterScope InterpreterScope;
    public KnowledgeBase KnowledgeBase;

    public ErgoTests(ErgoTestFixture fixture)
    {
        Interpreter = fixture.Interpreter;
        InterpreterScope = fixture.InterpreterScope;
        KnowledgeBase = fixture.KnowledgeBase;
        using var solver = Interpreter.Facade.BuildSolver(KnowledgeBase, SolverFlags.Default, DecimalType.BigDecimal);
    }
    // "⊤" : "⊥"
    protected void ShouldParse<T>(string query, T expected)
    {
        var parsed = Interpreter.Facade.Parse<T>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        if (parsed is IExplainable expl && expected is IExplainable expExpl)
            Assert.Equal(expl.Explain(true), expExpl.Explain(true));
        else Assert.Equal(parsed, expected);
    }
    protected void ShouldParse<T>(string query, Func<T, bool> expected)
    {
        var parsed = Interpreter.Facade.Parse<T>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        if (parsed is IExplainable expl && expected is IExplainable expExpl)
            Assert.Equal(expl.Explain(true), expExpl.Explain(true));
        else Assert.True(expected(parsed));
    }
    // "⊤" : "⊥"
    protected void ShouldNotParse<T>(string query, T expected)
    {
        var parsed = Interpreter.Facade.Parse<T>(InterpreterScope, query, onParseFail: _ => default);
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
        var parsed = Interpreter.Facade.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        if (checkParse)
            Assert.Equal(query, ((ITerm)parsed.Goals).StripTemporaryVariables().Explain(false));
        //Interpreted();
        //Compiled();
        Optimized();

        void Interpreted()
        {
            using var solver = Interpreter.Facade.BuildSolver(KnowledgeBase, (SolverFlags.Default & ~SolverFlags.EnableCompiler), DecimalType.BigDecimal);
            Solve(solver, parsed);
        }

        void Compiled()
        {
            using var solver = Interpreter.Facade.BuildSolver(KnowledgeBase, (SolverFlags.Default & ~SolverFlags.EnableCompilerOptimizations), DecimalType.BigDecimal);
            Solve(solver, parsed);
        }

        void Optimized()
        {
            using var solver = Interpreter.Facade.BuildSolver(KnowledgeBase, (SolverFlags.Default), DecimalType.BigDecimal);
            Solve(solver, parsed);
        }

        void Solve(ErgoSolver solver, Query parsed)
        {
            var numSolutions = 0;
            var timeoutToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(10000)).Token;
            foreach (var sol in solver.Solve(parsed, solver.CreateScope(InterpreterScope), timeoutToken))
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