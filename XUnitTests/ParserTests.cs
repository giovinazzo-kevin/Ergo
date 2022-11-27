using Ergo.Lang.Ast;


namespace Tests;

public class ParserTests : ErgoTests
{
    public ParserTests(ErgoTestFixture fixture) : base(fixture) { }
    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData("0  .5", 0.5)]
    [InlineData("0. 5", 0.5)]
    [InlineData("0 .  5", 0.5)]
    [InlineData(".5", .5)]
    [InlineData(".   5", .5)]
    public void ShouldParseDecimals(string query, object constructor)
        => ShouldParse(query, new Atom(constructor));
    [Theory]
    [InlineData("+26", +26)]
    [InlineData("+ 63", +63)]
    [InlineData("+06.4592", +06.4592)]
    [InlineData("-.19438", -.19438)]
    [InlineData("-. 19438", -.19438)]
    [InlineData("+.19438", +.19438)]
    [InlineData("+. 19438", +.19438)]
    [InlineData("-2", -2)]
    [InlineData("- 3", -3)]
    [InlineData("+ .  0", +.0)]
    [InlineData("+.  015", +.015)]
    public void ShouldParseSignedNumbers(string query, decimal number)
    {
        var f = number < 0 ? WellKnown.Functors.Subtraction.First() : WellKnown.Functors.Addition.First();
        ShouldParse(query, new Expression(new Complex(f, new Atom(Math.Abs(number))).AsOperator(OperatorAffix.Prefix), InterpreterScope));
    }

    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs1()
        => ShouldParse("f((V,L,R))",
            new Complex(new Atom("f"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }).CanonicalForm.AsParenthesized(true)));
    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs2()
        => ShouldParse("f(N, n, (V,L,R))",
            new Complex(new Atom("f"), new Variable("N"), new Atom("n"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }).CanonicalForm.AsParenthesized(true)));
}
