using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class TestInterpreter
    {
        private readonly ExceptionHandler Thrower = new(ex => throw ex);
        protected static (ErgoInterpreter, InterpreterScope) MakeInterpreter()
        {
            var i = new ErgoInterpreter();
            var s = i.CreateScope();
            i.Load(ref s, "Test", FileStreamUtils.MemoryStream(@"
                fact.
                data(1).
                data(2).
                tuple(1, 1, 1).
                tuple(2, 2, 2).
                tuple(1, 2, 3).

                indr_1(X) :- 
                    data(X).
                indr_2(X) :- 
                    indr_1(X).
                indr_3(X) :- 
                    indr_2(X).
                
                dynamic(A, B) :-
                    data(A),
                    data(B).
                
                dynamic(A, B, C) :-
                    data(A),
                    data(B),
                    data(C).

                yinAndYang([], []).
                yinAndYang([yin|Tail1], [yang|Tail2]) :- yinAndYang(Tail1, Tail2).

                map([], []) :- @cut.
                map([X|[]], [Y|[]]) :- Y is (X + 1), @cut.
                map([X|XT], [Y|YT]) :- XT \= [], map([X], [Y]), map(XT, YT).
            "));
            return (i, s);
        }

        [DataRow("data(X)", "X/1; X/2")]
        [DataRow("data(_)", "; ")]
        [DataRow("tuple(X, Y, Z)", "X/1, Y/1, Z/1; X/2, Y/2, Z/2; X/1, Y/2, Z/3")]
        [DataRow("indr_3(X)", "X/1; X/2")]
        [DataRow("dynamic(X, Y)", "X/1, Y/1; X/1, Y/2; X/2, Y/1; X/2, Y/2")]
        [DataRow("dynamic(X, Y, Z)", "X/1, Y/1, Z/1; X/1, Y/1, Z/2; X/1, Y/2, Z/1; X/1, Y/2, Z/2; X/2, Y/1, Z/1; X/2, Y/1, Z/2; X/2, Y/2, Z/1; X/2, Y/2, Z/2")]
        [DataRow("yinAndYang([yin], Y)", "Y/[yang]")]
        [DataRow("yinAndYang([yin, yin], Y)", "Y/[yang, yang]")]
        [DataRow("yinAndYang([yin, yin, yin], Y)", "Y/[yang, yang, yang]")]
        [DataRow("map([1,2,3,4,5,6,7,8], X)", "X/[2, 3, 4, 5, 6, 7, 8, 9]")]
        [DataRow("map([1,2,3], X), map(X, Y)", "X/[2, 3, 4], Y/[3, 4, 5]")]
        [DataTestMethod]
        public void SolveSimpleQuery(string query, string expected)
        {
            var (interpreter, scope) = MakeInterpreter();
            var Predicates = new Parsed<Query>(query, Thrower, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            var ans = interpreter.Solve(ref scope, Predicates.Value.Reduce(some => some, () => default));
            Assert.IsNotNull(ans);
            Assert.AreEqual(expected, String.Join("; ", ans.Select(e => String.Join(", ", e.Simplify().Select(s => s.Explain())))));
        }
    }
}
