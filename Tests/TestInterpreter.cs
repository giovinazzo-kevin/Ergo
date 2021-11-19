using Ergo.Lang;
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
        private readonly ExceptionHandler Thrower = new ExceptionHandler(ex => throw ex);
        private readonly ExceptionHandler Silent = new ExceptionHandler(ex => { });
        protected static Interpreter MakeInterpreter()
        {
            var i = new Interpreter();
            i.Load("Test", FileStreamUtils.MemoryStream(@"
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

                map([], []) :- @cut.
                map([X|[]], [Y|[]]) :- Y is (X + 1), @cut.
                map([X|XT], [Y|YT]) :- map([X], [Y]), map(XT, YT).
            "));
            return i;
        }

        [DataRow("data(X)", "X/1; X/2")]
        [DataRow("data(_)", "; ")]
        [DataRow("tuple(X, Y, Z)", "X/1, Y/1, Z/1; X/2, Y/2, Z/2; X/1, Y/2, Z/3")]
        [DataRow("indr_3(X)", "X/1; X/2")]
        [DataRow("dynamic(X, Y)", "X/1, Y/1; X/1, Y/2; X/2, Y/1; X/2, Y/2")]
        [DataRow("dynamic(X, Y, Z)", "X/1, Y/1, Z/1; X/1, Y/1, Z/2; X/1, Y/2, Z/1; X/1, Y/2, Z/2; X/2, Y/1, Z/1; X/2, Y/1, Z/2; X/2, Y/2, Z/1; X/2, Y/2, Z/2")]
        [DataRow("map([1], X)", "X/[2]")]
        [DataRow("map([1, 2], X)", "X/[2, 3]")]
        [DataTestMethod]
        public void SolveSimpleQuery(string query, string expected)
        {
            var interpreter = MakeInterpreter();
            var Predicates = new Parsed<Query>(query, Thrower, _ => throw new Exception("Parse fail."));
            var ans = interpreter.Solve(Predicates.Value.Reduce(some => some, () => default).Goals);
            Assert.IsNotNull(ans);
            Assert.AreEqual(expected, String.Join("; ", ans.Select(e => String.Join(", ", e.Simplify().Select(s => s.Explanation)))));
        }
    }
}
