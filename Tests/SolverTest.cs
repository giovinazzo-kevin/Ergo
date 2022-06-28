using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests;

[TestFixture]
public sealed class SolverTests : ErgoTest
{
    public static ErgoInterpreter Interpreter { get; private set; }
    public static InterpreterScope InterpreterScope { get; private set; }

    [OneTimeSetUp]
    public static void Initialize(TestContext _)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\Ergo\ergo");
        Interpreter = ErgoFacade.Standard
            .BuildInterpreter(InterpreterFlags.Default);
        InterpreterScope = Interpreter.CreateScope(x => x
            .WithExceptionHandler(ThrowingExceptionHandler)
            .WithoutSearchDirectories()
            .WithSearchDirectory(basePath)
            .WithSearchDirectory($@"{basePath}\stdlib\")
            .WithSearchDirectory($@"{basePath}\user\"));
    }
    // "⊤" : "⊥"
    public async Task ShouldSolve(string query, int numSolutions, params string[] expected)
    {
        using var solver = Interpreter.Facade.BuildSolver(InterpreterScope.KnowledgeBase, SolverFlags.Default);
        var parsed = Interpreter.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        var expectedSolutions = numSolutions;
        await foreach (var (sol, i) in solver.Solve(parsed, solver.CreateScope(InterpreterScope))
            .Select((x, i) => (x, i)))
        {
            Assert.IsTrue(i < expected.Length);
            Assert.AreEqual(expected[i], sol.Simplify().Substitutions.Join(s => s.Explain(), ";"));
            Assert.IsTrue(--numSolutions >= 0);
        }

        Assert.AreEqual(expectedSolutions, expectedSolutions - numSolutions);
    }
    #region Rows
    [TestCase("⊥", 0)]
    [TestCase("⊤", 1, "")]
    [TestCase("⊤, ⊤", 1, "")]
    [TestCase("⊤; ⊤", 2, "", "")]
    [TestCase("⊤, ⊥", 0)]
    [TestCase("⊤; ⊥", 1, "")]
    [TestCase("⊥, ⊤", 0)]
    [TestCase("⊥; ⊤", 1, "")]
    [TestCase("(⊥; ⊤), ⊥", 0)]
    [TestCase("(⊥; ⊤); ⊥", 1, "")]
    [TestCase("(⊤; ⊤), ⊤", 2, "", "")]
    [TestCase("(⊤, ⊤); ⊤", 2, "", "")]
    [TestCase("(⊤, ⊤); (⊤, ⊤)", 2, "", "")]
    [TestCase("(⊤, ⊥); (⊥; ⊤)", 1, "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 5, "", "", "", "", "")]
    #endregion
    public Task ShouldSolveConjunctionsAndDisjunctions(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    #region Rows
    [TestCase("!, (⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 5, "", "", "", "", "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤, !))))", 5, "", "", "", "", "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (⊤, ! ; ⊤))))", 4, "", "", "", "")]
    [TestCase("(⊤ ; (⊤ ; (⊤, ! ; (⊤ ; ⊤))))", 3, "", "", "")]
    [TestCase("(⊤ ; (⊤, ! ; (⊤ ; (⊤ ; ⊤))))", 2, "", "")]
    [TestCase("(⊤, ! ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 1, "")]
    [TestCase("(!, ⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤))))", 1, "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (⊤ ; ⊤)))), !", 1, "")]
    [TestCase("(⊤ ; (!, ⊤ ; (⊤ ; (⊤ ; ⊤))))", 2, "", "")]
    [TestCase("(⊤ ; (⊤ ; (!, ⊤ ; (⊤ ; ⊤))))", 3, "", "", "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (!, ⊤ ; ⊤))))", 4, "", "", "", "")]
    [TestCase("(⊤ ; (⊤ ; (⊤ ; (⊤ ; !, ⊤))))", 5, "", "", "", "", "")]
    #endregion
    public Task ShouldSolveCuts(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    #region Rows
    [TestCase("assertz(t:-⊥)", "t", "retractall(t)", 0)]
    [TestCase("assertz(t:-⊤)", "t", "retractall(t)", 1, "")]
    [TestCase("assertz(t:-(⊤; ⊤))", "t", "retractall(t)", 2, "", "")]
    [TestCase("assertz(t), assertz(t)", "t", "retractall(t)", 2, "", "")]
    [TestCase("assertz(t(_))", "t(_X)", "retractall(t)", 1, "")]
    [TestCase("assertz(t(X):-(X=⊤))", "t(X), X", "retractall(t)", 1, "X/⊤")]
    #endregion
    public Task ShouldSolveSetups(string setup, string goal, string cleanup, int numSolutions, params string[] expected)
        => ShouldSolve($"setup_call_cleanup(({setup}), ({goal}), ({cleanup}))", numSolutions, expected);
    [TestCase("[a,2,C]", "'[|]'(a,'[|]'(2,'[|]'(C,[])))")]
    [TestCase("[1,2,3|Rest]", "'[|]'(1,'[|]'(2,'[|]'(3,Rest)))")]
    [TestCase("[1,2,3|[a,2,_C]]", "'[|]'(1,'[|]'(2,'[|]'(3,'[|]'(a,'[|]'(2,'[|]'(_C,[]))))))")]
    [TestCase("{1,1,2,2,3,4}", "'{|}'(1,'{|}'(2,'{|}'(3,4)))")]
    public Task ShouldUnifyCanonicals(string term, string canonical)
        => ShouldSolve($"{term}={canonical}", 1, "");

}