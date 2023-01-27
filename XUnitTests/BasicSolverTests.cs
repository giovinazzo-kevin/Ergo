

namespace Tests;

public class BasicSolverTests : ErgoTests
{
    public BasicSolverTests(ErgoTestFixture fixture) : base(fixture) { }
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
    public void ShouldSolveConjunctionsAndDisjunctions(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, false, expected);
    #region Rows
    [Theory]
    [InlineData("[a,2,C]", "'[|]'(a,'[|]'(2,'[|]'(C,[])))")]
    [InlineData("[1,2,3|Rest]", "'[|]'(1,'[|]'(2,'[|]'(3,Rest)))")]
    [InlineData("[1,2,3|[a,2,_C]]", "'[|]'(1,'[|]'(2,'[|]'(3,'[|]'(a,'[|]'(2,'[|]'(_C,[]))))))")]
    [InlineData("{1,1,2,2,3,4}", "'{|}'(1,'{|}'(2,'{|}'(3, 4)))")]
    #endregion
    public void ShouldUnifyCanonicals(string term, string canonical)
        => ShouldSolve($"{term}={canonical}", 1, false, "");
    #region Rows
    [Theory]
    [InlineData("a =@= A", 0)]
    [InlineData("A =@= B", 1, "")]
    [InlineData("x(A,A) =@= x(B,C)", 0)]
    [InlineData("x(A,A) =@= x(B,B)", 1, "")]
    [InlineData("x(A,A) =@= x(A,B)", 0)]
    [InlineData("x(A,B) =@= x(C,D)", 1, "")]
    [InlineData("x(A,B) =@= x(B,A)", 1, "")]
    [InlineData("x(A,B) =@= x(C,A)", 1, "")]
    #endregion
    public void ShouldSolveVariants(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, false, expected);
}
