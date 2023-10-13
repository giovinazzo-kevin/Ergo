﻿using System.Diagnostics.CodeAnalysis;

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
    public Maybe<T> Expect<T>(IEnumerable<ErgoLexer.TokenType> types, Func<T, bool> pred)
    {
        var pos = Lexer.State;
        var watch = Probe.Enter();
        return Lexer.ReadNext()
            .Where(token => types.Contains(token.Type) && token.Value is T t && pred(t))
            .Select(token => (T)token.Value)
            .Or(() => Fail<T>(pos))
            .Do(() => Probe.Leave(watch))
            ;
    }
    public Maybe<string> ExpectDelimiter(Func<string, bool> condition)
    {
        var pos = Lexer.State;
        return Expect(ErgoLexer.TokenType.Punctuation, condition)
            .Or(() => Fail<string>(pos));
    }
    public Maybe<T> Expect<T>(ErgoLexer.TokenType type, Func<T, bool> cond) => Expect<T>(new[] { type }, cond);
    public Maybe<T> Expect<T>(ErgoLexer.TokenType type) => Expect<T>(type, _ => true);
    public Maybe<T> Expect<T>(IEnumerable<ErgoLexer.TokenType> types) => Expect<T>(types, _ => true);
    public Maybe<T> Parenthesized<T>(string opening, string closing, Func<Maybe<T>> tryParse)
    {
        var key = $"Parenthesized{opening}{typeof(T).Name}{closing}";
        var watch = Probe.Enter(key);
        var pos = Lexer.State;
        if (IsFailureMemoized(pos, key))
        {
            Probe.Leave(watch, key);
            return default;
        }
        return Inner()
            .Do(() => Probe.Leave(watch, key));
        Maybe<T> Inner()
        {
            var pos = Lexer.State;
            if (!Expect<string>(ErgoLexer.TokenType.Punctuation, str => str.Equals(opening)).TryGetValue(out _))
                return MemoizeFailureAndFail<T>(pos, key);
            if (!tryParse().TryGetValue(out var ret))
                return MemoizeFailureAndFail<T>(pos, key);
            if (!Expect<string>(ErgoLexer.TokenType.Punctuation, str => str.Equals(closing)).TryGetValue(out _))
                return MemoizeFailureAndFail<T>(pos, key);
            return ret;
        }
    }
    protected void Throw(ErgoLexer.StreamState s, ErrorType error, params object[] args)
    {
        var old = Lexer.State;
        Lexer.Seek(s);
        throw new ParserException(error, old, args);
    }

    public Maybe<IEnumerable<T>> Fold<T, U>(Func<Maybe<IEnumerable<T>>> parse, Func<Maybe<U>> parseJoin)
    {
        var list = new List<T>();
        var pos = Lexer.State;
        while (parse().TryGetValue(out var items))
        {
            list.AddRange(items);
            if (!parseJoin().TryGetValue(out _))
                break;
        }

        return !list.Any()
            ? Fail<IEnumerable<T>>(pos)
            : Maybe.Some(list.AsEnumerable());
    }

    public Maybe<UntypedSequence> Sequence(
        Operator op,
        Atom emptyElement,
        string openingDelim,
        Operator separator,
        string closingDelim)
    {
        var scope = GetScope();
        return
            Parenthesized(openingDelim, closingDelim, () =>
                Unfold(ExpressionOrTerm())
                .Select(t => new UntypedSequence(op, emptyElement, (openingDelim, closingDelim), ImmutableArray.CreateRange(t), scope))
                .Or(() => new UntypedSequence(op, emptyElement, (openingDelim, closingDelim), ImmutableArray<ITerm>.Empty, scope)))
            .Or(() => Fail<UntypedSequence>(scope.LexerState))
        ;

        Maybe<IEnumerable<ITerm>> Unfold(Maybe<ITerm> term)
        {
            return term
                .Where(t => t is Complex { IsParenthesized: false })
                .Select(t => (Complex)t)
                .Where(c => separator.Synonyms.Contains(c.Functor) && c.Arguments.Length == 2)
                .Map(c => Unfold(Maybe.Some(c.Arguments[1])).Select(u => u.Prepend(c.Arguments[0])))
                .Or(() => term.Select(t => new[] { t }.AsEnumerable()));
        }
    }
}
