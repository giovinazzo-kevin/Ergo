using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using Ergo.Solver;

namespace Tests;

public sealed class SolverTests : IClassFixture<SolverTestFixture>
{
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;

    public SolverTests(SolverTestFixture fixture)
    {
        Interpreter = fixture.Interpreter;
        InterpreterScope = fixture.InterpreterScope;
        KnowledgeBase = fixture.KnowledgeBase;
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
        if (expected.Length != 0)
            Assert.Equal(expectedSolutions, expected.Length);
        using var solver = Interpreter.Facade.BuildSolver(KnowledgeBase, SolverFlags.Default);
        var parsed = Interpreter.Parse<Query>(InterpreterScope, query)
            .GetOrThrow(new InvalidOperationException());
        if (checkParse)
            Assert.Equal(query, parsed.Goals.Explain());
        var numSolutions = 0;
        foreach (var sol in solver.Solve(parsed, solver.CreateScope(InterpreterScope)))
        {
            Assert.InRange(++numSolutions, 1, expectedSolutions);
            if (expected.Length != 0)
            {
                var check = sol.Simplify().Substitutions.Join(s => s.Explain(), ";");
                Assert.Equal(expected[numSolutions - 1], check);
            }
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
    [Theory]
    [InlineData("a =@= A", 0)]
    [InlineData("A =@= B", 1, "")]
    [InlineData("x(A,A) =@= x(B,C)", 0)]
    [InlineData("x(A,A) =@= x(B,B)", 1, "")]
    [InlineData("x(A,A) =@= x(A,B)", 0)]
    [InlineData("x(A,B) =@= x(C,D)", 1, "")]
    [InlineData("x(A,B) =@= x(B,A)", 1, "")]
    [InlineData("x(A,B) =@= x(C,A)", 1, "")]
    public Task ShouldSolveVariants(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, false, expected);
    #region Rows
    [Theory]
    [InlineData("min(3,5,3)", 1, "")]
    [InlineData("max(3,5,5)", 1, "")]
    [InlineData("min(5,3,3)", 1, "")]
    [InlineData("max(5,3,5)", 1, "")]
    [InlineData("X = 1, X == 1 -> Y = a ; Y = b", 1, "X/1;Y/a")]
    [InlineData("X = 2, X == 1 -> Y = a ; Y = b", 1, "Y/b")]
    [InlineData("member(X,[1,2]) -> member(Y,[a,b]) ; member(Y,[c,d])", 2, "X/1;Y/a", "X/1;Y/b")]
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
    [InlineData("range(0 < X <= 1)", 1, "X/1")]
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
    [InlineData("fail", 0)]
    [InlineData("succeed", 1, "")]
    [InlineData("case01(X,N,L)", 3, "X/1;L/[1,2];N/2", "X/2;L/[3];N/1", "X/3;L/[3,1,2];N/3") /*Not caused by TCO*/]
    #endregion
    public Task ShouldSolveFromKnowledgeBase(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    [Theory]
    [InlineData("select([1,2,3],[A,B,C],[X,Y] >> (Y := X * 2))", 1, "A/2;B/4;C/6")]
    public Task ShouldSolveHigherOrderPredicates(string query, int numSolutions, params string[] expected)
        => ShouldSolve(query, numSolutions, true, expected);
    #region Rows
    [Theory]
    [InlineData("fibonacci(1000,X)", 1, "X/43466557686937456435688527675040625802564660517371780402481729089536555417949051890403879840079255169295922593080322634775209689623239873322471161642996440906533187938298969649928516003704476137795166849228875")]
    [InlineData("factorial(1000,X)", 1, "X/402387260077093773543702433923003985719374864210714632543799910429938512398629020592044208486969404800479988610197196058631666872994808558901323829669944590997424504087073759918823627727188732519779505950995276120874975462497043601418278094646496291056393887437886487337119181045825783647849977012476632889835955735432513185323958463075557409114262417474349347553428646576611667797396668820291207379143853719588249808126867838374559731746136085379534524221586593201928090878297308431392844403281231558611036976801357304216168747609675871348312025478589320767169132448426236131412508780208000261683151027341827977704784635868170164365024153691398281264810213092761244896359928705114964975419909342221566832572080821333186116811553615836546984046708975602900950537616475847728421889679646244945160765353408198901385442487984959953319101723355556602139450399736280750137837615307127761926849034352625200015888535147331611702103968175921510907788019393178114194545257223865541461062892187960223838971476088506276862967146674697562911234082439208160153780889893964518263243671616762179168909779911903754031274622289988005195444414282012187361745992642956581746628302955570299024324153181617210465832036786906117260158783520751516284225540265170483304226143974286933061690897968482590125458327168226458066526769958652682272807075781391858178889652208164348344825993266043367660176999612831860788386150279465955131156552036093988180612138558600301435694527224206344631797460594682573103790084024432438465657245014402821885252470935190620929023136493273497565513958720559654228749774011413346962715422845862377387538230483865688976461927383814900140767310446640259899490222221765904339901886018566526485061799702356193897017860040811889729918311021171229845901641921068884387121855646124960798722908519296819372388642614839657382291123125024186649353143970137428531926649875337218940694281434118520158014123344828015051399694290153483077644569099073152433278288269864602789864321139083506217095002597389863554277196742822248757586765752344220207573630569498825087968928162753848863396909959826280956121450994871701244516461260379029309120889086942028510640182154399457156805941872748998094254742173582401063677404595741785160829230135358081840096996372524230560855903700624271243416909004153690105933983835777939410970027753472000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("for(I,1,1000)", 1000)]
    [InlineData("for(I,1,10), for(J,1,10)", 100)]

    #endregion
    public Task ShouldSolveTailRecursivePredicatesWithoutOverflowingTheStack(string query, int numSolutions, params string[] expected)
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
    public Task ShouldParseSignedNumbers(string query, decimal number)
    {
        var f = number < 0 ? WellKnown.Functors.Subtraction.First() : WellKnown.Functors.Addition.First();
        return ShouldParse(query, new Expression(new Complex(f, new Atom(Math.Abs(number))).AsOperator(OperatorAffix.Prefix), InterpreterScope));
    }

    [Fact]
    public Task ShouldParsePathologicalCases_ParensInArgs1()
        => ShouldParse("f((V,L,R))",
            new Complex(new Atom("f"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }).CanonicalForm.AsParenthesized(true)));
    [Fact]
    public Task ShouldParsePathologicalCases_ParensInArgs2()
        => ShouldParse("f(N, n, (V,L,R))",
            new Complex(new Atom("f"), new Variable("N"), new Atom("n"),
                new NTuple(new ITerm[] { new Variable("V"), new Variable("L"), new Variable("R") }).CanonicalForm.AsParenthesized(true)));
}