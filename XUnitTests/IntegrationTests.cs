

namespace Tests;

public class IntegrationTests : ErgoTests
{
    public IntegrationTests(ErgoTestFixture fixture) : base(fixture) { }
    #region Rows
    [Theory]
    [InlineData("tests : fail", 0)]
    [InlineData("tests : succeed", 1, "")]
    [InlineData("case01 : case01(X,N,L)", 3, "L/[1,2];N/2;X/1", "L/[3];N/1;X/2", "L/[3,1,2];N/3;X/3") /*Not caused by TCO*/]
    [InlineData("case02 : case02([1,2,3],[2,4,6])", 1, "")]
    [InlineData("case02 : case02([1,2,3],L)", 1, "L/[2,4,6]")]
    [InlineData("case03 : case03", 1, "")]
    //[InlineData("list_element_rest([a,b],E,Rest)", 2, "E/a;Rest/b", "E/b;Rest/a")]
    //[InlineData("list_element_rest(Ls,c,[a,b])", 3, "Ls/[c,a,b]", "Ls/[a,c,b]", "Ls/[a,b,c]")]
    #endregion
    public void ShouldSolveFromKnowledgeBase(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
}
