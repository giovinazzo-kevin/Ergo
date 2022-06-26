using System.Diagnostics.CodeAnalysis;

namespace Ergo.Lang;

public partial class ErgoParser
{
    protected static bool IsPunctuation(ErgoLexer.Token token, [NotNull] string p) => token.Type == ErgoLexer.TokenType.Punctuation && p.Equals(token.Value);
    protected static bool IsVariableIdentifier(string s) => s[0] == '_' || char.IsLetter(s[0]) && char.IsUpper(s[0]);
    protected static bool IsAtomIdentifier(string s) => !IsVariableIdentifier(s);
    protected Maybe<T> Fail<T>(ErgoLexer.StreamState s, T _ = default)
    {
        Lexer.Seek(s);
        return default;
    }
    public Maybe<T> Expect<T>(ErgoLexer.TokenType type, Func<T, bool> pred)
    {
        var pos = Lexer.State;
        if (!Lexer.TryReadNextToken(out var token) || token.Type != type || token.Value is not T t || !pred(t))
        {
            return Fail<T>(pos);
        }

        return t;
    }
    public Maybe<string> ExpectDelimiter(Func<string, bool> condition)
    {
        var pos = Lexer.State;
        return Expect(ErgoLexer.TokenType.Punctuation, condition)
            .Or(() => Expect(ErgoLexer.TokenType.Operator, condition))
            .Or(() => Fail<string>(pos));
    }
    public Maybe<T> Expect<T>(ErgoLexer.TokenType type) => Expect<T>(type, _ => true);
    protected Maybe<T> Parenthesized<T>(Func<Maybe<T>> tryParse)
    {
        var pos = Lexer.State;
        return Expect<T>(ErgoLexer.TokenType.Punctuation, str => str.Equals("("))
            .Map(_ => tryParse()
                .Map(res => Expect<T>(ErgoLexer.TokenType.Punctuation, str => str.Equals(")"))
                    .Select(_ => res)))
            .Or(() => Fail<T>(pos));
    }
    protected void Throw(ErgoLexer.StreamState s, ErrorType error, params object[] args)
    {
        var old = Lexer.State;
        Lexer.Seek(s);
        throw new ParserException(error, old, args);
    }
    public Maybe<UntypedSequence> Sequence(
        Atom functor,
        Atom emptyElement,
        Func<Maybe<ITerm>> tryParseElement,
        string openingDelim,
        Operator separator,
        string closingDelim,
        bool @throw)
    {
        var pos = Lexer.State;
        var args = new List<(ITerm Term, bool Parens)>();
        if (openingDelim != null)
        {
            if (!ExpectDelimiter(p => p == openingDelim).TryGetValue(out _))
            {
                return Fail<UntypedSequence>(pos);
            }

            if (closingDelim != null && ExpectDelimiter(p => p == closingDelim).TryGetValue(out var _))
            {
                // Empty list
                return new UntypedSequence(functor, emptyElement, (openingDelim, closingDelim), ImmutableArray<ITerm>.Empty);
            }
        }

        while (tryParseElement().TryGetValue(out var term))
        {
            args.Add((term, term.IsParenthesized));
            if (!ExpectDelimiter(p => true).TryGetValue(out var q) || !separator.Synonyms.Any(s => q.Equals(s.Value)) && q != closingDelim)
            {
                if (@throw)
                    Throw(pos, ErrorType.ExpectedArgumentDelimiterOrClosedParens, separator, closingDelim);
                return Fail<UntypedSequence>(pos);
            }

            if (closingDelim != null && q == closingDelim)
            {
                break;
            }
        }

        return new UntypedSequence(functor, emptyElement, (openingDelim, closingDelim), ImmutableArray.CreateRange(args.Select(a => a.Term)));
    }
}
