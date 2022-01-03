using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class TestUnification
    {
        [TestMethod]
        public void TestUnification_1()
        {
            var eq = new Substitution(
                new Complex(
                    new Atom("functor")
                    , new Atom("yo")
                    , new Variable("X")
                ), new Complex(
                    new Atom("functor")
                    , new Atom("yo")
                    , new Atom("hey")
                )
            );
            Assert.IsTrue(eq.TryUnify(out var substitutions));
            Assert.IsTrue(substitutions.Single().Explain() == "X/hey");
        }

        [TestMethod]
        public void TestUnification_2()
        {
            using var fs = FileStreamUtils.MemoryStream("a(X) ← b(X).");
            var lexer = new Lexer(fs, string.Empty, TestOperators.DefinedOperators);
            var parser = new Parser(lexer);
            Assert.IsTrue(parser.TryParsePredicate(out var Predicate));
            Assert.IsTrue(Predicate.TryUnify(new Complex(new Atom("a"), new Atom("bob")), Predicate, out var substitutions));
            Assert.AreEqual("X/bob", String.Join(", ", substitutions.Select(s => s.Explain())));
            Assert.AreEqual("a(bob)←b(bob).", Predicate.Substitute(Predicate, substitutions).Explain(canonical: true).RemoveExtraWhitespace());
        }

        [TestMethod]
        public void TestUnification_3()
        {
            using var fs = FileStreamUtils.MemoryStream("a(X, Y) ← b(X, Y), c(Y).");
            var lexer = new Lexer(fs, string.Empty, TestOperators.DefinedOperators);
            var parser = new Parser(lexer);
            Assert.IsTrue(parser.TryParsePredicate(out var Predicate));
            Assert.IsTrue(Predicate.TryUnify(new Complex(new Atom("a"), new Atom("bob"), new Atom("complex(john)")), Predicate, out var substitutions));
            Assert.AreEqual("X/bob, Y/complex(john)", String.Join(", ", substitutions.Select(s => s.Explain())));
            Assert.AreEqual("a(bob,complex(john))←b(bob,complex(john)),c(complex(john)).",
                Predicate.Substitute(Predicate, substitutions).Explain(canonical: true).RemoveExtraWhitespace());
        }

        [TestMethod]
        public void TestUnification_4()
        {
            using var fs = FileStreamUtils.MemoryStream("a(X, Y) ← '='(X, Y), c(Y).");
            var lexer = new Lexer(fs, string.Empty, TestOperators.DefinedOperators);
            var parser = new Parser(lexer);
            Assert.IsTrue(parser.TryParsePredicate(out var Predicate));
            Assert.IsTrue(Predicate.TryUnify(new Complex(new Atom("a"), new Atom("bob"), new Variable("Y")), Predicate, out var substitutions));
            Assert.AreEqual("X/bob", String.Join(", ", substitutions.Select(s => s.Explain())));
            Assert.AreEqual("a(bob,Y)←=(bob,Y),c(Y).", Predicate.Substitute(Predicate, substitutions).Explain(canonical: true).RemoveExtraWhitespace());
        }
    }
}
