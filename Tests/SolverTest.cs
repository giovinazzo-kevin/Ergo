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
            .WithExceptionHandler(NullExceptionHandler)
            .WithoutSearchDirectories()
            .WithSearchDirectory(basePath)
            .WithSearchDirectory($@"{basePath}\stdlib\")
            .WithSearchDirectory($@"{basePath}\user\"));
    }
    // "⊤" : "⊥"
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
    [DataRow("!, ⊤", 1, "")]
    [DataRow("⊤, !", 1, "")]
    [DataRow("!, ⊤ ; ⊤", 1, "")]
    [DataRow("⊤ ; !, ⊤", 2, "", "")]
    [DataRow("(⊤, !; ⊤); ⊤", 1, "")]
    [DataRow("(⊤; ⊤, !); ⊤", 2, "", "")]
    [DataRow("⊤; (⊤, !; ⊤)", 2, "", "")]
    [DataRow("⊤; (⊤; ⊤, !)", 3, "", "", "")]
    [DataRow("(⊤, !; ⊤), !; (!, ⊤; ⊤)", 1, "")]
    [DataRow("setup_call_cleanup(assertz(t:-(⊤)), (t), retractall(t))", 1, "")]
    [DataRow("setup_call_cleanup(assertz(t:-(⊤; ⊤)), (t), retractall(t))", 2, "", "")]
    public async Task ShouldSolve(string query, int numSolutions, params string[] expected)
    {
        using var solver = Interpreter.Facade.BuildSolver(InterpreterScope.KnowledgeBase, SolverFlags.Default);
        var parsed = Interpreter.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        await foreach (var (sol, i) in solver.Solve(parsed, solver.CreateScope(InterpreterScope))
            .Select((x, i) => (x, i)))
        {
            Assert.IsTrue(i < expected.Length);
            Assert.AreEqual(expected[i], sol.Simplify().Substitutions.Join(";"));
            Assert.IsTrue(--numSolutions >= 0);
        }

        Assert.AreEqual(0, numSolutions);
    }

    //[DataRow("data(X)", "X/1; X/2")]
    //[DataRow("data(_)", "; ")]
    //[DataRow("tuple(X, Y, Z)", "X/1, Y/1, Z/1; X/2, Y/2, Z/2; X/1, Y/2, Z/3")]
    //[DataRow("indr_3(X)", "X/1; X/2")]
    //[DataRow("dynamic(X, Y)", "X/1, Y/1; X/1, Y/2; X/2, Y/1; X/2, Y/2")]
    //[DataRow("dynamic(X, Y, Z)", "X/1, Y/1, Z/1; X/1, Y/1, Z/2; X/1, Y/2, Z/1; X/1, Y/2, Z/2; X/2, Y/1, Z/1; X/2, Y/1, Z/2; X/2, Y/2, Z/1; X/2, Y/2, Z/2")]
    //[DataRow("yinAndYang([yin], Y)", "Y/[yang]")]
    //[DataRow("yinAndYang([yin, yin], Y)", "Y/[yang,yang]")]
    //[DataRow("yinAndYang([yin, yin, yin], Y)", "Y/[yang,yang,yang]")]
    //[DataRow("map([1,2,3,4,5,6,7,8], X)", "X/[2,3,4,5,6,7,8,9]")]
    //[DataRow("map([1,2,3], X), map(X, Y)", "X/[2,3,4], Y/[3,4,5]")]
    //[DataTestMethod]
    //public void SolveSimpleQuery(string query, string expected)
    //{
    //    var (interpreter, scope) = MakeInterpreter();
    //    var predicates = new Parsed<Query>(query, _ => throw new Exception("Parse fail."), scope.Operators.Value);
    //    var ans = SolverBuilder.Build(interpreter, ref scope).Solve(predicates.Value.GetOrDefault()).CollectAsync().GetAwaiter().GetResult();
    //    Assert.IsNotNull(ans);
    //    Assert.AreEqual(expected, String.Join("; ", ans.Select(e => String.Join(", ", e.Simplify().Substitutions.Select(s => s.Explain())))));
    //}
}