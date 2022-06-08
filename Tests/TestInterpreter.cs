using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Utils;
using Ergo.Solver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class TestInterpreter
    {
        protected static (ErgoInterpreter, InterpreterScope) MakeInterpreter()
        {
            var i = new ErgoInterpreter();
            var s = i.CreateScope();
            i.Load(ref s, FileStreamUtils.MemoryStream(@"
                :- module(test, []).

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

                map([], []) :- !.
                map([X|[]], [Y|[]]) :- Y is (X + 1), !.
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
        [DataRow("yinAndYang([yin, yin], Y)", "Y/[yang,yang]")]
        [DataRow("yinAndYang([yin, yin, yin], Y)", "Y/[yang,yang,yang]")]
        [DataRow("map([1,2,3,4,5,6,7,8], X)", "X/[2,3,4,5,6,7,8,9]")]
        [DataRow("map([1,2,3], X), map(X, Y)", "X/[2,3,4], Y/[3,4,5]")]
        [DataTestMethod]
        public void SolveSimpleQuery(string query, string expected)
        {
            var (interpreter, scope) = MakeInterpreter();
            var predicates = new Parsed<Query>(query, _ => throw new Exception("Parse fail."), scope.Operators.Value);
            var ans = SolverBuilder.Build(interpreter, ref scope).Solve(predicates.Value.GetOrDefault()).CollectAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(ans);
            Assert.AreEqual(expected, String.Join("; ", ans.Select(e => String.Join(", ", e.Simplify().Substitutions.Select(s => s.Explain())))));
        }
    }
}
