using Ergo.Lang;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tests
{

    [TestClass]
    public class TestParser
    {
        private readonly ExceptionHandler Thrower = new ExceptionHandler(ex => throw ex);

        [DataRow("a_simple_atom", "a_simple_atom")]
        [DataRow("'a string'", "'a string'")]
        [DataRow("\"another string\"", "'another string'")]
        [DataRow("'a \"nested\" string'", "'a \"nested\" string'")]
        [DataRow("\"another 'nested' string\"", "'another \\'nested\\' string'")]
        [DataRow("42", "42")]
        [DataRow("42.30", "42.3")]
        [DataRow("0.009423480", "0.00942348")]
        [DataTestMethod]
        public void ParseAtom(string atom, string normalized)
        {
            var p = new Parsed<Atom>(atom, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(normalized, Atom.Explain(p.Value.Reduce(some => some, () => default)));
        }

        [DataRow("X", "X")]
        [DataRow("Variable", "Variable")]
        [DataRow("_X", "_X")]
        [DataRow("_variable", "_variable")]
        [DataRow("_", "_")]
        [DataTestMethod]
        public void ParseVariable(string variable, string normalized)
        {
            var p = new Parsed<Variable>(variable, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(normalized, Variable.Explain(p.Value.Reduce(some => some, () => default)));
        }

        [DataRow("[1, 2, 3, 4]", "[1, 2, 3, 4]")]
        [DataRow("[1, 2, [3, 4]]", "[1, 2, [3, 4]]")]
        [DataRow("[[1, 2], 3, 4]", "[[1, 2], 3, 4]")]
        [DataTestMethod]
        public void ParseList(string toParse, string expected)
        {
            var p = new Parsed<List>(toParse, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(expected, List.Explain(p.Value.Reduce(some => some, () => default)));
        }

        [DataRow("a(_)", "a(_)")]
        [DataRow("a(X)", "a(X)")]
        [DataRow("f(A, B, C)", "f(A, B, C)")]
        [DataRow("f(A, B, g(C, D))", "f(A, B, g(C, D))")]
        [DataRow("f(A, B, g(C, h(D, 'string', 32)))", "f(A, B, g(C, h(D, string, 32)))")]
        [DataTestMethod]
        public void ParseComplex(string complex, string normalized)
        {
            var p = new Parsed<Complex>(complex, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(normalized, Complex.Explain(p.Value.Reduce(some => some, () => default)));
        }


        [DataRow("a, b, c, d", "(a, b, c, d)")]
        [DataRow("a + b * c", "+(a, *(b, c))")]
        [DataRow("(a + b) * c", "*(+(a, b), c)")]
        [DataRow("a + b * c - d", "-(+(a, *(b, c)), d)")]
        [DataRow("a + b * c * d - e", "-(+(a, *(*(b, c), d)), e)")]
        [DataRow("X = a + b * c * d - e", "=(X, -(+(a, *(*(b, c), d)), e))")]
        [DataTestMethod]
        public void ParseExpression(string exp, string normalized)
        {
            var p = new Parsed<Expression>(exp, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(normalized, Complex.Explain(p.Value.Reduce(some => some, () => default).Complex));
        }

        [DataRow("fact.", "fact.")]
        [DataRow("fact :- true.", "fact.")]
        [DataRow("fact :- false.", "fact :- false.")]
        [DataRow("pred :- fact.", "pred :- fact.")]
        [DataRow("pred(X) :- fact(X).", "pred(__G1) :- fact(__G1).")]
        [DataRow("pred(X) :- fact(X), test(X).", "pred(__G1) :- (fact(__G1), test(__G1)).")]
        [DataTestMethod]
        public void ParsePredicate(string predicate, string normalized)
        {
            var p = new Parsed<Predicate>(predicate, Thrower, _ => throw new Exception("Parse fail."));
            Assert.AreEqual(normalized, Predicate.Explain(p.Value.Reduce(some => some, () => default)).RemoveExtraWhitespace());
        }
    }
}
