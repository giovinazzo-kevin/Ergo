using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ergo.Lang
{
    public partial class Parser
    {
        protected static bool IsPunctuation(Lexer.Token token, [NotNull] string p) => token.Type == Lexer.TokenType.Punctuation && p.Equals(token.Value);
        protected static bool IsVariableIdentifier(string s) => s[0] == '_' || (Char.IsLetter(s[0]) && Char.IsUpper(s[0]));
        protected static bool IsAtomIdentifier(string s) => !(IsVariableIdentifier(s));
        protected bool Fail(Lexer.StreamState s)
        {
            _lexer.Seek(s);
            return false;
        }
        protected bool Expect<T>(Lexer.TokenType type, Func<T, bool> pred, out T value)
        {
            var pos = _lexer.State;
            value = default;
            if (!_lexer.TryReadNextToken(out var token) || token.Type != type || token.Value is not T t || !pred(t)) {
                return Fail(pos);
            }
            value = t;
            return true;
        }
        protected bool Expect<T>(Lexer.TokenType type, out T value) => Expect(type, _ => true, out value);
        protected bool Parenthesized<T>(Func<(bool Parsed, T Result)> tryParse, out T value)
        {
            var pos = _lexer.State; value = default;
            if (Expect(Lexer.TokenType.Punctuation, str => str.Equals("("), out string _)
            && tryParse() is (true, var res)
            && Expect(Lexer.TokenType.Punctuation, str => str.Equals(")"), out string _)) {
                value = res;
                return true;
            }
            return Fail(pos);
        }
        protected void Throw(Lexer.StreamState s, ErrorType error, params object[] args)
        {
            var old = _lexer.State;
            _lexer.Seek(s);
            throw new ParserException(error, old, args);
        }
        protected bool TryParseSequence(
            Atom functor, 
            ITerm emptyElement, 
            Func<(bool, ITerm, bool parens)> tryParseElement, 
            string openingDelim, 
            string separator, 
            string closingDelim, 
            bool @throw,
            out UntypedSequence seq)
        {
            seq = default;
            var pos = _lexer.State;
            var args = new List<(ITerm Term, bool Parens)>();
            var isSeparatorComma = WellKnown.Functors.Conjunction.Contains(new Atom(separator));
            if (openingDelim != null)
            {
                if (!ExpectDelimiter(p => p == openingDelim, out string _))
                {
                    return Fail(pos);
                }
                if (closingDelim != null && ExpectDelimiter(p => p == closingDelim, out string _))
                {
                    // Empty list
                    seq = new UntypedSequence(functor, emptyElement, ImmutableArray<ITerm>.Empty, false);
                    return true;
                }
            }

            while (tryParseElement() is (true, var term, var parens))
            {
                args.Add((term, parens));
                if (!ExpectDelimiter(p => true, out string q) || q != separator && q != closingDelim)
                {
                    if (@throw)
                        Throw(pos, ErrorType.ExpectedArgumentDelimiterOrClosedParens, separator, closingDelim);
                    return Fail(pos);
                }
                if (closingDelim != null && q == closingDelim)
                {
                    break;
                }
            }
            // Special case: when the delimiter is a comma, and the expression is not parenthesized, we need to unfold the underlying expression
            if (isSeparatorComma && args.Count == 1 && args.Single() is { } arg && !arg.Parens && CommaSequence.TryUnfold(arg.Term, out var comma))
            {
                seq = new UntypedSequence(functor, emptyElement, comma.Contents, false);
            }
            else
            {
                seq = new UntypedSequence(functor, emptyElement, ImmutableArray.CreateRange(args.Select(a => a.Term)), false);
            }
            return true;

            bool ExpectDelimiter(Func<string, bool> condition, out string d)
            {
                var pos = _lexer.State;
                if (Expect(Lexer.TokenType.Punctuation, condition, out d))
                {
                    return true;
                }
                if (Expect(Lexer.TokenType.Operator, condition, out d))
                {
                    return true;
                }
                return Fail(pos);
            }
        }
    }
}
