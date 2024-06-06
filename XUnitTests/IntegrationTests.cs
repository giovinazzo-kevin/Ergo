

namespace Tests;

public class IntegrationTests(ErgoTestFixture fixture) : ErgoTests(fixture)
{
    #region Rows
    [Theory]
    [InlineData("tests : fail", 0)]
    [InlineData("tests : succeed", 1, "")]
    [InlineData("case01 : case01(X,N,L)", 3, "L/[1,2];N/2;X/1", "L/[3];N/1;X/2", "L/[3,1,2];N/3;X/3") /*Not caused by TCO*/]
    [InlineData("case02 : case02([1,2,3,4,5,6,7],[2,4,6,8,10,12,15])", 0)]
    [InlineData("case02 : case02([1,2,3,4,5,6,7],[2,4,6,8,10,12,14])", 1, "")]
    [InlineData("case02 : case02([1,2,3,4,5,6,7],L)", 1, "L/[2,4,6,8,10,12,14]")]
    [InlineData("case03 : case03", 1, "")]
    [InlineData("case04 : one_solution(X)", 1, "X/1")]
    [InlineData("case04 : two_solutions(X)", 2, "X/2.1", "X/2.2")]
    [InlineData("case04 : one_solution_two_vars(X,Y)", 1, "X/1;Y/1")]
    [InlineData("case04 : two_solutions_two_vars_1(X,Y)", 2, "X/1;Y/2.1", "X/1;Y/2.2")]
    [InlineData("case04 : two_solutions_two_vars_2(X,Y)", 2, "X/2.1;Y/1", "X/2.2;Y/1")]
    [InlineData("case04 : four_solutions_two_vars(X,Y)", 4, "X/2.1;Y/2.1", "X/2.1;Y/2.2", "X/2.2;Y/2.1", "X/2.2;Y/2.2")]
    [InlineData("case04 : eight_solutions_three_vars(X,Y,Z)", 8, "X/2.1;Y/2.1;Z/2.1", "X/2.1;Y/2.1;Z/2.2", "X/2.1;Y/2.2;Z/2.1", "X/2.1;Y/2.2;Z/2.2",
        "X/2.2;Y/2.1;Z/2.1", "X/2.2;Y/2.1;Z/2.2", "X/2.2;Y/2.2;Z/2.1", "X/2.2;Y/2.2;Z/2.2")]
    [InlineData("case04 : cyclic(X)", 2, "X/2", "X/2")]
    [InlineData("case04 : builtin_cyclic(X)", 2, "X/2", "X/2")]
    [InlineData("case05 : test_geometry(p(2,2),p(1,1) + p(1,1))", 1, "")]
    //[InlineData("list_element_rest([a,b],E,Rest)", 2, "E/a;Rest/b", "E/b;Rest/a")]
    //[InlineData("list_element_rest(Ls,c,[a,b])", 3, "Ls/[c,a,b]", "Ls/[a,c,b]", "Ls/[a,b,c]")]
    #endregion
    public void ShouldSolveFromKnowledgeBase(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
}
