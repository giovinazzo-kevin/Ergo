using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace Tests
{

    [TestClass]
    public class TestLexer
    {
        private static void AssertNextToken(Lexer l, Lexer.TokenType expectedType, object expectedValue)
        {
            Assert.IsTrue(l.TryReadNextToken(out var token));
            Assert.AreEqual(expectedType, token.Type);
            Assert.AreEqual(expectedValue, token.Value);
        }

        [TestMethod]
        public void TestTokenizer_1()
        {
            using var stream = FileStreamUtils.MemoryStream("1234.56789.123");
            var lexer = new Lexer(stream, Array.Empty<Operator>());
            AssertNextToken(lexer, Lexer.TokenType.Number, 1234.56789d);
            AssertNextToken(lexer, Lexer.TokenType.Punctuation, ".");
            AssertNextToken(lexer, Lexer.TokenType.Number, 123d);
        }

        [TestMethod]
        public void TestTokenizer_2()
        {
            using var stream = FileStreamUtils.MemoryStream("hello = _");
            var lexer = new Lexer(stream, Array.Empty<Operator>());
            AssertNextToken(lexer, Lexer.TokenType.Term, "hello");
            AssertNextToken(lexer, Lexer.TokenType.Operator, "=");
            AssertNextToken(lexer, Lexer.TokenType.Term, "_");
        }


        [TestMethod]
        public void TestTokenizer_3()
        {
            using var stream = FileStreamUtils.MemoryStream("'\"string 1\"' \"'string 2'\" \"\\\"string 3\\\"\"");
            var lexer = new Lexer(stream, Array.Empty<Operator>());
            AssertNextToken(lexer, Lexer.TokenType.String, "\"string 1\"");
            AssertNextToken(lexer, Lexer.TokenType.String, "'string 2'");
            AssertNextToken(lexer, Lexer.TokenType.String, "\"string 3\"");
        }

        [TestMethod]
        public void TestTokenizer_4()
        {
            using var stream = FileStreamUtils.MemoryStream("Y = functor(arg_1, X)");
            var lexer = new Lexer(stream, Array.Empty<Operator>());
            AssertNextToken(lexer, Lexer.TokenType.Term, "Y");
            AssertNextToken(lexer, Lexer.TokenType.Operator, "=");
            AssertNextToken(lexer, Lexer.TokenType.Term, "functor");
            AssertNextToken(lexer, Lexer.TokenType.Punctuation, "(");
            AssertNextToken(lexer, Lexer.TokenType.Term, "arg_1");
            AssertNextToken(lexer, Lexer.TokenType.Operator, ",");
            AssertNextToken(lexer, Lexer.TokenType.Term, "X");
            AssertNextToken(lexer, Lexer.TokenType.Punctuation, ")");
        }
    }
}
