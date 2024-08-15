using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;

namespace Tests;

using A = Atom;
using C = Complex;
using V = Variable;

public class BasicSolverTests : ErgoTests<ErgoTestFixture>
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
    [InlineData("⊤, (⊤, ⊤ ; ⊤, ⊤), ⊤ ; (⊤ ; (⊤, ⊤), ⊤)", 4, "", "", "", "")]
    [InlineData("⊥; ⊤", 1, "")]
    [InlineData("(⊥; ⊤), ⊥", 0)]
    [InlineData("(⊥; ⊤); ⊥", 1, "")]
    [InlineData("(⊤; ⊤), ⊤", 2, "", "")]
    [InlineData("(⊤, ⊤); ⊤", 2, "", "")]
    [InlineData("(⊤; ⊤); ⊤", 3, "", "", "")]
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
    [InlineData("{1,1,2,2,3,4}", "'{|}'(1,'{|}'(2,'{|}'(3,'{|}'(4,'{}'))))")]
    [InlineData("test{x:1, y : cool}", "dict(test, {x:1, y:cool})")]
    [InlineData("test{x:1, y : cool}", "test{x:1, y:cool}")]
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

    [Fact]
    public void ShouldHashSignatures()
    {
        var s1 = new C(new A(":="), new V("A"), new V("B"))
            .GetSignature();
        var s2 = new C(new A(":="), new V("A"))
            .GetSignature();
        var s3 = new C(new A(":="), new V("X"), new V("Y"))
            .GetSignature();
        Assert.NotEqual(s1, s2);
        Assert.NotEqual(s2, s3);
        Assert.Equal(s1, s3);
        Assert.NotEqual(s1.GetHashCode(), s2.GetHashCode());
        Assert.NotEqual(s2.GetHashCode(), s3.GetHashCode());
        Assert.Equal(s1.GetHashCode(), s3.GetHashCode());
    }
}
