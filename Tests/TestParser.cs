using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace Tests
{

    [TestClass]
    public class TestParser
    {
        private readonly ExceptionHandler Thrower = new((scope, ex) => ExceptionDispatchInfo.Capture(ex).Throw());

        [DataRow("a_simple_atom", "a_simple_atom")]
        [DataRow("'a string'", "'a string'")]
        [DataRow("\"another string\"", "'another string'")]
        [DataRow("'a \"nested\" string'", "'a \"nested\" string'")]
        [DataRow("\"another 'nested' string\"", "'another \\'nested\\' string'")]
        [DataRow("42", "42")]
        [DataRow("42.30", "42.3")]
        [DataRow("0.25", "0.25")]
        [DataTestMethod]
        public void ParseAtom(string atom, string normalized)
        {
            var p = new Parsed<Atom>(atom, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain(canonical: true));
        }

        [DataRow("X", "X")]
        [DataRow("Variable", "Variable")]
        [DataRow("_X", "_X")]
        [DataRow("_variable", "_variable")]
        [DataRow("_", "_1")]
        [DataTestMethod]
        public void ParseVariable(string variable, string normalized)
        {
            var p = new Parsed<Variable>(variable, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain());
        }

        [DataRow("[1, 2, 3, 4]", "[1,2,3,4]")]
        [DataRow("[1, 2, [3, 4]]", "[1,2,[3,4]]")]
        [DataRow("[[1, 2], 3, 4]", "[[1,2],3,4]")]
        [DataRow("[1, 2, 3 | [4, 5, 6]]", "[1,2,3|[4,5,6]]")]
        [DataRow("[1, 2|[]]", "[1,2]")]
        [DataRow("[X|Rest]", "[X|Rest]")]
        [DataTestMethod]
        public void ParseList(string toParse, string expected)
        {
            var p = new Parsed<List>(toParse, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(expected, p.Value.GetOrDefault().Explain());
        }

        [DataRow("a(X)", "a(X)")]
        [DataRow("a((X))", "a(X)")]
        [DataRow("a(X, Y)", "a(X,Y)")]
        [DataRow("a((X, Y))", "a((X,Y))")]
        [DataRow("a([X|Rest])", "a([X|Rest])")]
        [DataRow("a([X, Y|Rest])", "a([X,Y|Rest])")]
        [DataRow("f(A, B, C)", "f(A,B,C)")]
        [DataRow("f(A, B, g(C, D))", "f(A,B,g(C,D))")]
        [DataRow("f(A, B, g(C, h(D, 'string', 32)))", "f(A,B,g(C,h(D,string,32)))")]
        [DataTestMethod]
        public void ParseComplex(string complex, string normalized)
        {
            var p = new Parsed<Complex>(complex, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain());
        }



        [DataRow("(((((a)))) + ((((b)))))", "+(a,b)")]
        [DataRow("((((a))))", "a")]
        [DataRow("((((a + b))))", "+(a,b)")]
        [DataRow("((((-1.25))))", "-(1.25)")]
        [DataTestMethod]
        public void ParseITerm(string exp, string normalized)
        {
            var p = new Parsed<ITerm>(exp, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain(canonical: true));
        }

        [DataRow("-a","-(a)")]
        [DataRow("a,b,c,d","a∧b∧c∧d")]
        [DataRow("a+b*c","+(a,*(b,c))")]
        [DataRow("(a+b)*c","*(+(a,b),c)")]
        [DataRow("(a+b)*π","*(+(a,b),π)")]
        [DataRow("a+b*c-d","-(+(a,*(b,c)),d)")]
        [DataRow("a+b*c*d-e","-(+(a,*(*(b,c),d)),e)")]
        [DataRow("X=a+b*c*d-e","=(X,-(+(a,*(*(b,c),d)),e))")]
        [DataRow("F=B*2^(1/12)^N","=(F,*(B,^(2,^(/(1,12),N))))")]
        [DataRow("F=B*(2^(1/12))^N","=(F,*(B,^(^(2,/(1,12)),N)))")]

        [DataTestMethod]
        public void ParseExpression(string exp, string canonical)
        {
            var p = new Parsed<Expression>(exp, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(canonical, p.Value.GetOrDefault().Complex.Explain(canonical: true));
            Assert.AreEqual(exp, p.Value.GetOrDefault().Complex.Explain(canonical: false));
        }

        [DataRow("fact.", "fact.")]
        [DataRow("fact :- true.", "fact.")]
        [DataRow("fact :- false.", "fact←⊥.")]
        [DataRow("pred :- fact.", "pred←fact.")]
        [DataRow("pred(X) :- fact(X).", "pred(X)←fact(X).")]
        [DataRow("pred([X|Rest]) :- fact(X), fact(Rest).", "pred([X|Rest])←fact(X),fact(Rest).")]
        [DataRow("pred(X) :- fact(X), test(X).", "pred(X)←fact(X),test(X).")]
        [DataTestMethod]
        public void ParsePredicate(string predicate, string normalized)
        {
            var p = new Parsed<Predicate>(predicate, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain(canonical: true).RemoveExtraWhitespace());
        }

        [DataRow(":- module(test, []).", "← module(test,[]).")]
        [DataTestMethod]
        public void ParseDirective(string directive, string normalized)
        {
            var p = new Parsed<Directive>(directive, _ => throw new Exception("Parse fail."), Array.Empty<Operator>());
            Assert.AreEqual(normalized, p.Value.GetOrDefault().Explain(canonical: true).RemoveExtraWhitespace());
        }
    }
}
