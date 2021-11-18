using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Lang
{
    public partial class Parser
    {
        protected bool IsPunctuation(Lexer.Token token, [NotNull] string p) => token.Type == Lexer.TokenType.Punctuation && p.Equals(token.Value);
        protected bool IsAtomIdentifier(string s) => s[0] == '@' || Char.IsLetter(s[0]) && Char.IsLower(s[0]);
        protected bool IsVariableIdentifier(string s) => s[0] == '_' || (Char.IsLetter(s[0]) && Char.IsUpper(s[0]));
        protected bool Fail(Lexer.StreamState s)
        {
            _lexer.Seek(s);
            return false;
        }
        protected bool Expect<T>(Lexer.TokenType type, Func<T, bool> pred, out T value)
        {
            var s = _lexer.State;
            value = default;
            if (!_lexer.TryReadNextToken(out var token) || token.Type != type || !(token.Value is T t) || !pred(t)) {
                _lexer.Seek(s);
                return false;
            }
            value = t;
            return true;
        }
        protected bool Expect<T>(Lexer.TokenType type, out T value) => Expect(type, _ => true, out value);
        protected void Throw(Lexer.StreamState s, ErrorType error, params object[] args)
        {
            var old = _lexer.State;
            _lexer.Seek(s);
            throw new ParserException(error, old, args);
        }
        protected bool TryParseSequence(Atom functor, Term emptyElement, Func<(bool, Term)> tryParseElement, string openingDelim, string separator, string closingDelim, bool @throw, out Sequence seq)
        {
            seq = default;
            var pos = _lexer.State;
            var args = new List<Term>();

            if (openingDelim != null) {
                if (!ExpectDelimiter(p => p == openingDelim, out string _)) {
                    return Fail(pos);
                }
                if (closingDelim != null && ExpectDelimiter(p => p == closingDelim, out string _)) {
                    // Empty list
                    seq = new Sequence(functor, emptyElement);
                    return true;
                }
            }

            while (tryParseElement() is (true, var term)) {
                args.Add(term);
                if (!ExpectDelimiter(p => true, out string q) || q != separator && q != closingDelim) {
                    if(@throw)
                        Throw(pos, ErrorType.ExpectedArgumentDelimiterOrClosedParens, separator, closingDelim);
                    return Fail(pos);
                }
                if (closingDelim != null && q == closingDelim) {
                    break;
                }
            }
            seq = new Sequence(functor, emptyElement, args.ToArray());
            return true;

            bool ExpectDelimiter(Func<string,bool> condition, out string d)
            {
                var pos = _lexer.State;
                if (Expect(Lexer.TokenType.Punctuation, condition, out d)) {
                    return true;
                }
                if (Expect(Lexer.TokenType.Operator, condition, out d)) {
                    return true;
                }
                return Fail(pos);
            }
        }
    }
}
