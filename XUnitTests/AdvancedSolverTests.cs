namespace Tests;

public class AdvancedSolverTests(ErgoTestFixture fixture) : ErgoTests(fixture)
{
    #region Rows
    [Theory]
    [InlineData("min(3,5,3)", 1, "")]
    [InlineData("max(3,5,5)", 1, "")]
    [InlineData("min(5,3,3)", 1, "")]
    [InlineData("max(5,3,5)", 1, "")]
    [InlineData("X = 1, X == 1 -> Y = a ; Y = b", 1, "X/1;Y/a")]
    [InlineData("X = 2, X == 1 -> Y = a ; Y = b", 1, "Y/b")]
    [InlineData("member(X,[1,2]) -> member(Y,[a,b]) ; member(Y,[c,d])", 2, "X/1;Y/a", "X/1;Y/b")]
    [InlineData("⊤, !, ⊤, !, ⊤", 1, "")]
    [InlineData("⊤, ⊤ ; ⊤, !", 2, "", "")]
    [InlineData("(⊤ ; ⊤), !, (⊤ ; ⊤ ; ⊤)", 3, "", "", "")]
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
    [InlineData("range(0 < _ <= 10)", 10, "", "", "", "", "", "", "", "", "", "")]
    [InlineData("range(0 < X <= 1)", 1, "X/1")]
    [InlineData("range(0 < X <= 1), range(0 < Y <= 1)", 1, "X/1;Y/1")]
    [InlineData("range(0 < X <= 3)", 3, "X/1", "X/2", "X/3")]
    [InlineData("range(0 < X <= 3), range(0 < Y <= 3)", 9, "X/1;Y/1", "X/1;Y/2", "X/1;Y/3", "X/2;Y/1", "X/2;Y/2", "X/2;Y/3", "X/3;Y/1", "X/3;Y/2", "X/3;Y/3")]
    [InlineData("range(0 < X <= 2), range(0 < Y <= 2), range(0 < Z <= 2)", 8, "X/1;Y/1;Z/1", "X/1;Y/1;Z/2", "X/1;Y/2;Z/1", "X/1;Y/2;Z/2", "X/2;Y/1;Z/1", "X/2;Y/1;Z/2", "X/2;Y/2;Z/1", "X/2;Y/2;Z/2")]
    [InlineData("range(0 < X <= 3), !", 1, "X/1")]
    [InlineData("range(0 < X <= 3), !, range(0 < Y <= 3)", 3, "X/1;Y/1", "X/1;Y/2", "X/1;Y/3")]
    [InlineData("range(0 < X <= 3), !, range(0 < Y <= 3), !", 1, "X/1;Y/1")]

    #endregion
    public void ShouldSolveCuts(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    #region Rows
    [Theory]
    [InlineData("length('[]',L)", 1, "L/0")]
    [InlineData("length([_],L)", 1, "L/1")]
    [InlineData("length([_,_],L)", 1, "L/2")]
    [InlineData("length([_,_,_,_,_,_],L)", 1, "L/6")]

    #endregion
    public void ShouldSolveRecursive(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    [Theory]
    [InlineData("repeat(10,⊤)", 10, "", "", "", "", "", "", "", "", "", "")]
    [InlineData("map([X,Y] >> (Y := X * 2),[1,2,3],[A,B,C])", 1, "A/2;B/4;C/6")]
    public void ShouldSolveHigherOrderPredicates(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    [Theory]
    [InlineData("P := p(1,1) * 2", 1, "P/p(2,2)")]
    [InlineData("_X = 2, P := p(1,1) * _X", 1, "P/p(2,2)")]
    [InlineData("_X = 2, _Y = p(2,2), P := _X * _Y", 1, "P/p(4,4)")]
    public void ShouldSolveProblematicQueries(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    #region Rows
    [Theory]
    [InlineData("assertz(t:-⊥)", "tests:t", "retractall(t)", 0)]
    [InlineData("assertz(t:-⊤)", "t", "retractall(t)", 1, "")]
    [InlineData("assertz(t:-(⊤; ⊤))", "t", "retractall(t)", 2, "", "")]
    [InlineData("assertz(t), assertz(t)", "t", "retractall(t)", 2, "", "")]
    [InlineData("assertz(t(_))", "tests:t(_X)", "retractall(t(_))", 1, "")]
    [InlineData("assertz(t(X):-(X=⊤))", "t(X), X", "retractall(t(_))", 1, "X/⊤")]
    #endregion
    public void ShouldSolveSetups(string setup, string goal, string cleanup, int numSolutions, params string[] expected)
        => ShouldSolve($"setup_call_cleanup(({setup}), ({goal}), ({cleanup}))", numSolutions, false, expected);

}