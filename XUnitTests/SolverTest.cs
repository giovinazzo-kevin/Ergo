using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;

namespace Tests;

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
    public async Task ShouldParse<T>(string query, T expected)
    {
        var parsed = Interpreter.Parse<T>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        Assert.Equal(parsed, expected);
    }

    // "⊤" : "⊥"
    public async Task ShouldSolve(string query, int expectedSolutions, bool checkParse, params string[] expected)
    {
        Assert.Equal(expectedSolutions, expected.Length);
        using var solver = Interpreter.Facade.BuildSolver(InterpreterScope.KnowledgeBase, SolverFlags.Default);
        await solver.InitializeAsync(InterpreterScope);
        var parsed = Interpreter.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        if (checkParse)
            Assert.Equal(query, parsed.Goals.Explain());
        var numSolutions = 0;
        await foreach (var sol in solver.Solve(parsed, solver.CreateScope(InterpreterScope)))
        {
            var check = sol.Simplify().Substitutions.Join(s => s.Explain(), ";");
            Assert.InRange(++numSolutions, 1, expected.Length);
            Assert.Equal(expected[numSolutions - 1], check);
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
        => ShouldSolve(query, numSolutions, false, expected);
    #region Rows
    [Theory]
    [InlineData("min(3,5,3)", 1, "")]
    [InlineData("max(3,5,5)", 1, "")]
    [InlineData("min(5,3,3)", 1, "")]
    [InlineData("max(5,3,5)", 1, "")]
    [InlineData("X = 1, X == 1 -> Y = a ; Y = b", 1, "X/1;Y/a")]
    [InlineData("X = 2, X == 1 -> Y = a ; Y = b", 1, "Y/b")]
    [InlineData("member(X,[1,2]) -> member(Y,[a,b]) ; member(Y,[c,d])", 2, "Y/a;X/1", "Y/b;X/1")]
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
    [InlineData("range(0 < X <= 3)", 3, "X/1", "X/2", "X/3")]
    [InlineData("range(0 < X <= 3), range(0 < Y <= 3)", 9, "X/1;Y/1", "X/1;Y/2", "X/1;Y/3", "X/2;Y/1", "X/2;Y/2", "X/2;Y/3", "X/3;Y/1", "X/3;Y/2", "X/3;Y/3")]
    [InlineData("range(0 < X <= 3), !", 1, "X/1")]
    [InlineData("range(0 < X <= 3), !, range(0 < Y <= 3)", 3, "X/1;Y/1", "X/1;Y/2", "X/1;Y/3")]
    [InlineData("range(0 < X <= 3), !, range(0 < Y <= 3), !", 1, "X/1;Y/1")]

    #endregion
    public Task ShouldSolveCuts(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
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
        => ShouldSolve($"setup_call_cleanup(({setup}), ({goal}), ({cleanup}))", numSolutions, false, expected);
    [Theory]
    [InlineData("[a,2,C]", "'[|]'(a,'[|]'(2,'[|]'(C,[])))")]
    [InlineData("[1,2,3|Rest]", "'[|]'(1,'[|]'(2,'[|]'(3,Rest)))")]
    [InlineData("[1,2,3|[a,2,_C]]", "'[|]'(1,'[|]'(2,'[|]'(3,'[|]'(a,'[|]'(2,'[|]'(_C,[]))))))")]
    [InlineData("{1,1,2,2,3,4}", "'{|}'(1,'{|}'(2,'{|}'(3,4)))")]
    public Task ShouldUnifyCanonicals(string term, string canonical)
        => ShouldSolve($"{term}={canonical}", 1, false, "");

    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData("0  .5", 0.5)]
    [InlineData("0. 5", 0.5)]
    [InlineData("0 .  5", 0.5)]
    [InlineData(".5", .5)]
    [InlineData(".   5", .5)]
    public Task ShouldParseDecimals(string query, object constructor)
        => ShouldParse(query, new Atom(constructor));
    [InlineData("+26", +26)]
    [InlineData("+ 63", +63)]
    [InlineData("+06.4592", +06.4592)]
    [InlineData("-.194381", -.194381)]
    [InlineData("-2", -2)]
    [InlineData("- 3", -3)]
    [InlineData("+ .  0", +.0)]
    public Task ShouldParseSignedNumbers(string query, object functor, params object[] args)
        => ShouldParse(query, new Complex(new Atom(functor), args.Select(a => (ITerm)new Atom(a)).ToArray()).AsOperator(OperatorAffix.Prefix));
}