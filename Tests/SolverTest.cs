using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Tests;

[TestClass]
public class SolverTest : ErgoTest
{
    public static ErgoInterpreter Interpreter { get; private set; }
    public static InterpreterScope InterpreterScope { get; private set; }

    [ClassInitialize]
    public static void Initialize(TestContext ctx)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\Ergo\ergo");
        var facade = ErgoFacade.Standard;
        Interpreter = facade.BuildInterpreter(InterpreterFlags.Default);
        InterpreterScope = Interpreter.CreateScope(x => x
            .WithoutSearchDirectories()
            .WithSearchDirectory(basePath)
            .WithSearchDirectory($@"{basePath}\stdlib\")
            .WithSearchDirectory($@"{basePath}\user\"));
    }
    // "⊤" : "⊥"
    public async Task ShouldSolve(string query, int numSolutions, params string[] expected)
    {
        var testScope = InterpreterScope
            .WithExceptionHandler(ThrowingExceptionHandler);
        using var solver = Interpreter.Facade.BuildSolver(testScope.KnowledgeBase, SolverFlags.Default);
        var parsed = Interpreter.Parse<Query>(testScope, query)
            .GetOrThrow(new InvalidOperationException());
        await foreach (var (sol, i) in solver.Solve(parsed, solver.CreateScope(testScope))
            .Select((x, i) => (x, i)))
        {
            Assert.IsTrue(i < expected.Length);
            Assert.AreEqual(expected[i], sol.Simplify().Substitutions.Join(";"));
            Assert.IsTrue(--numSolutions >= 0);
        }

        Assert.AreEqual(0, numSolutions);
    }
    [DataTestMethod]
    [DataRow("⊥", 0)]
    [DataRow("⊤", 1, "")]
    [DataRow("⊤, ⊤", 1, "")]
    [DataRow("⊤; ⊤", 2, "", "")]
    [DataRow("⊤, ⊥", 0)]
    [DataRow("⊤; ⊥", 1, "")]
    [DataRow("⊥, ⊤", 0)]
    [DataRow("⊥; ⊤", 1, "")]
    [DataRow("(⊥; ⊤), ⊥", 0)]
    [DataRow("(⊥; ⊤); ⊥", 1, "")]
    [DataRow("(⊤; ⊤), ⊤", 2, "", "")]
    [DataRow("(⊤, ⊤); ⊤", 2, "", "")]
    [DataRow("(⊤, ⊤); (⊤, ⊤)", 2, "", "")]
    [DataRow("(⊤, ⊥); (⊥; ⊤)", 1, "")]
    public Task ShouldSolveConjunctionsAndDisjunctions(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    [DataTestMethod]
    [DataRow("!, ⊤", 1, "")]
    [DataRow("⊤, !", 1, "")]
    [DataRow("!, ⊤ ; ⊤", 1, "")]
    [DataRow("⊤ ; !, ⊤", 2, "", "")]
    [DataRow("(⊤, !; ⊤); ⊤", 1, "")]
    [DataRow("(⊤; ⊤, !); ⊤", 2, "", "")]
    [DataRow("⊤; (⊤, !; ⊤)", 2, "", "")]
    [DataRow("⊤; (⊤; ⊤, !)", 3, "", "", "")]
    [DataRow("(⊤, !; ⊤), !; (!, ⊤; ⊤)", 1, "")]
    public Task ShouldSolveCuts(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);
    [DataTestMethod]
    [DataRow("setup_call_cleanup(assertz(t:-(⊤)), (t), retractall(t))", 1, "")]
    [DataRow("setup_call_cleanup((assertz(t), assertz(t)), (t), retractall(t))", 2, "", "")]
    public Task ShouldSolveSetups(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, expected);

}