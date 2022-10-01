﻿using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Lang.Extensions;
using Ergo.Solver;

namespace Tests;
public sealed class SolverTestFixture : IDisposable
{
    public readonly ExceptionHandler NullExceptionHandler = default;
    public readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;

    public SolverTestFixture()
    {
        // Run at start
        var basePath = Directory.GetCurrentDirectory();
        var stdlibPath = Path.Combine(basePath, @"..\..\..\..\Ergo\ergo");
        var testsPath = Path.Combine(basePath, @"..\..\..\ergo");

        Interpreter = ErgoFacade.Standard
            .BuildInterpreter(InterpreterFlags.Default);
        InterpreterScope = Interpreter.CreateScope(x => x
            .WithExceptionHandler(ThrowingExceptionHandler)
            .WithoutSearchDirectories()
            .WithSearchDirectory(testsPath)
            .WithSearchDirectory(stdlibPath)
            .WithModule(x.EntryModule
                .WithImport(new("tests")))
        );
    }

    ~SolverTestFixture()
    {
        Dispose();
    }

    public void Dispose() => GC.SuppressFinalize(this);// Run at end

}

public sealed class SolverTests : IClassFixture<SolverTestFixture>
{
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;

    public SolverTests(SolverTestFixture fixture)
    {
        Interpreter = fixture.Interpreter;
        InterpreterScope = fixture.InterpreterScope;
    }

    // "⊤" : "⊥"
    public async Task ShouldSolve(string query, int expectedSolutions, params string[] expected)
    {
        Assert.Equal(expectedSolutions, expected.Length);
        using var solver = Interpreter.Facade.BuildSolver(InterpreterScope.KnowledgeBase, SolverFlags.Default);
        var parsed = Interpreter.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        var numSolutions = 0;
        await foreach (var sol in solver.Solve(parsed, solver.CreateScope(InterpreterScope)))
        {
            Assert.InRange(++numSolutions, 1, expected.Length);
            Assert.Equal(expected[numSolutions - 1], sol.Simplify().Substitutions.Join(s => s.Explain(), ";"));
        }

        Assert.Equal(expectedSolutions, numSolutions);
    }
    #region Rows
    [Theory]
    [InlineData("⊥", 0)]
    [InlineData("⊤", 1, "")]
    [InlineData("⊤, ⊤", 1, "")]
    [InlineData("⊤; ⊤", 2, "", "")]
    [InlineData("⊤, ⊥", 0)]
    [InlineData("⊤; ⊥", 1, "")]
    [InlineData("⊥, ⊤", 0)]
    [InlineData("⊥; ⊤", 1, "")]
    [InlineData("(⊥; ⊤), ⊥", 0)]
    [InlineData("(⊥; ⊤); ⊥", 1, "")]
    [InlineData("(⊤; ⊤), ⊤", 2, "", "")]
    [InlineData("(⊤, ⊤); ⊤", 2, "", "")]
    [InlineData("(⊤, ⊤); (⊤, ⊤)", 2, "", "")]
    [InlineData("(⊤, ⊥); (⊥; ⊤)", 1, "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 5, "", "", "", "", "")]
    #endregion
    public Task ShouldSolveConjunctionsAndDisjunctions(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    #region Rows
    [Theory]
    [InlineData("!, (⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 5, "", "", "", "", "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤, !))))", 5, "", "", "", "", "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (⊤, ! ; ⊤))))", 4, "", "", "", "")]
    [InlineData("(⊤ ; (⊤ ; (⊤, ! ; (⊤ ; ⊤))))", 3, "", "", "")]
    [InlineData("(⊤ ; (⊤, ! ; (⊤ ; (⊤ ; ⊤))))", 2, "", "")]
    [InlineData("(⊤, ! ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 1, "")]
    [InlineData("(!, ⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 1, "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤)))), !", 1, "")]
    [InlineData("(⊤ ; (!, ⊤ ; (⊤ ; (⊤ ; ⊤))))", 2, "", "")]
    [InlineData("(⊤ ; (⊤ ; (!, ⊤ ; (⊤ ; ⊤))))", 3, "", "", "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (!, ⊤ ; ⊤))))", 4, "", "", "", "")]
    [InlineData("(⊤ ; (⊤ ; (⊤ ; (⊤ ; !, ⊤))))", 5, "", "", "", "", "")]

    [InlineData("(⊤ ; !, ⊥ ; ⊤)", 1, "")]
    [InlineData("(⊤ ; ⊥, ! ; ⊤)", 2, "", "")]

    #endregion
    public Task ShouldSolveCuts(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    #region Rows
    [Theory]
    [InlineData("assertz(t:-⊥)", "t", "retractall(t)", 0)]
    [InlineData("assertz(t:-⊤)", "t", "retractall(t)", 1, "")]
    [InlineData("assertz(t:-(⊤; ⊤))", "t", "retractall(t)", 2, "", "")]
    [InlineData("assertz(t), assertz(t)", "t", "retractall(t)", 2, "", "")]
    [InlineData("assertz(t(_))", "t(_X)", "retractall(t)", 1, "")]
    [InlineData("assertz(t(X):-(X=⊤))", "t(X), X", "retractall(t)", 1, "X/⊤")]
    #endregion
    public Task ShouldSolveSetups(string setup, string goal, string cleanup, int numSolutions, params string[] expected)
        => ShouldSolve($"setup_call_cleanup(({setup}), ({goal}), ({cleanup}))", numSolutions, expected);
    [Theory]
    [InlineData("[a,2,C]", "'[|]'(a,'[|]'(2,'[|]'(C,[])))")]
    [InlineData("[1,2,3|Rest]", "'[|]'(1,'[|]'(2,'[|]'(3,Rest)))")]
    [InlineData("[1,2,3|[a,2,_C]]", "'[|]'(1,'[|]'(2,'[|]'(3,'[|]'(a,'[|]'(2,'[|]'(_C,[]))))))")]
    [InlineData("{1,1,2,2,3,4}", "'{|}'(1,'{|}'(2,'{|}'(3,4)))")]
    public Task ShouldUnifyCanonicals(string term, string canonical)
        => ShouldSolve($"{term}={canonical}", 1, "");
}