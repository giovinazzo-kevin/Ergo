using Ergo.Lang.Ast;
namespace Tests;

public static class MockWellKnown
{
    public static class Operators
    {
        public static readonly Operator Addition = new(WellKnown.Modules.Math, Fixity.Infix, OperatorAssociativity.None, 500, WellKnown.Functors.Addition);
        public static readonly Operator Subtraction = new(WellKnown.Modules.Math, Fixity.Infix, OperatorAssociativity.None, 500, WellKnown.Functors.Subtraction);
        public static readonly Operator DictAccess = new(WellKnown.Modules.Math, Fixity.Infix, OperatorAssociativity.Left, 900, WellKnown.Functors.DictAccess);
    }
}

[Collection("Default")]
public class ParserTests : ErgoTests<ErgoTestFixture>
{
    public ParserTests(ErgoTestFixture test) : base(test) { }
    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData(".5", .5)]
    public void ShouldParseDecimals(string query, object constructor)
        => ShouldParse(query, new Atom(constructor));
    [Theory]
    [InlineData("0  .5", 0.5)]
    [InlineData("0. 5", 0.5)]
    [InlineData("0 .  5", 0.5)]
    [InlineData(".   5", .5)]
    public void ShouldNotParseDecimals(string query, object constructor)
        => ShouldNotParse(query, new Atom(constructor));
    [Theory]
    [InlineData("+26", +26)]
    [InlineData("+06.4592", +06.4592)]
    [InlineData("-.19438", -.19438)]
    [InlineData("+.19438", +.19438)]
    [InlineData("-2", -2)]
    public void ShouldParseSignedNumbers(string query, decimal number)
    {
        ShouldParse(query, new Atom(number));
    }
    [Theory]
    [InlineData("+ 63", +63)]
    [InlineData("-. 19438", -.19438)]
    [InlineData("+. 19438", +.19438)]
    [InlineData("- 3", -3)]
    [InlineData("+ .  0", +.0)]
    [InlineData("+.  015", +.015)]
    public void ShouldNotParseSignedNumbers(string query, decimal number)
    {
        ShouldNotParse(query, new Atom(number));
    }
    [Fact]
    public void ShouldRespectOperatorPrecedence()
    {
        ShouldParse("1-1/2", new Expr(new Complex(new Atom("-"), new Atom(1), new Complex(new Atom("/"), new Atom(1), new Atom(2))), InterpreterScope));
        ShouldParse("1/1-2", new Expr(new Complex(new Atom("-"), new Complex(new Atom("/"), new Atom(1), new Atom(1)), new Atom(2)), InterpreterScope));
    }

    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs1()
        => ShouldParse("f((V,L,R))",
            new Complex(new Atom("f"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }, default)));
    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs2()
        => ShouldParse("f(N, n, (V,L,R))",
            new Complex(new Atom("f"), new Variable("N"), new Atom("n"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }, default, true)));
    [Fact]
    public void ShouldParsePathologicalCases_PeriodAsInfix()
        => ShouldParse("a.b",
            new Expr(new Complex(MockWellKnown.Operators.DictAccess.CanonicalFunctor, new Atom("a"), new Atom("b"))
                .AsOperator(MockWellKnown.Operators.DictAccess), InterpreterScope));
}
