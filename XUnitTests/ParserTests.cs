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

public class ParserTests : ErgoTests
{
    public ParserTests(ErgoTestFixture test) : base(test) { }
    [Theory]
    [InlineData("0", 0)]
    [InlineData("0.5", 0.5)]
    [InlineData(".5", .5)]
    public void ShouldParseDecimals(string query, object constructor)
        => ShouldParse(query, (Atom)constructor);
    [Theory]
    [InlineData("0  .5", 0.5)]
    [InlineData("0. 5", 0.5)]
    [InlineData("0 .  5", 0.5)]
    [InlineData(".   5", .5)]
    public void ShouldNotParseDecimals(string query, object constructor)
        => ShouldNotParse(query, (Atom)constructor);
    [Theory]
    [InlineData("+26", +26)]
    [InlineData("+06.4592", +06.4592)]
    [InlineData("-.19438", -.19438)]
    [InlineData("+.19438", +.19438)]
    [InlineData("-2", -2)]
    public void ShouldParseSignedNumbers(string query, decimal number)
    {
        ShouldParse(query, (Atom)number);
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
        ShouldNotParse(query, (Atom)number);
    }
    [Fact]
    public void ShouldRespectOperatorPrecedence()
    {
        ShouldParse("1-1/2", new Expression(new Complex("-", (Atom)1, new Complex("/", (Atom)1, (Atom)2)), InterpreterScope));
        ShouldParse("1/1-2", new Expression(new Complex("-", new Complex("/", (Atom)1, (Atom)1), (Atom)2), InterpreterScope));
    }

    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs1()
        => ShouldParse("f((V,L,R))",
            new Complex("f",
                new NTuple(new ITerm[] { (Variable)"V", (Variable)"L", (Variable)"R" }, default)));
    [Fact]
    public void ShouldParsePathologicalCases_ParensInArgs2()
        => ShouldParse("f(N, n, (V,L,R))",
            new Complex("f", (Variable)"N", (Atom)"n",
                new NTuple(new ITerm[] { (Variable)"V", (Variable)"L", (Variable)"R" }, default, true)));
    [Fact]
    public void ShouldParsePathologicalCases_PeriodAsInfix()
        => ShouldParse("a.b",
            new Expression(new Complex(MockWellKnown.Operators.DictAccess.CanonicalFunctor, (Atom)"a", (Atom)"b")
                .AsOperator(MockWellKnown.Operators.DictAccess), InterpreterScope));
}
