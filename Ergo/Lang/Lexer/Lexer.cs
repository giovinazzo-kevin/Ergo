using Ergo.Facade;
using Ergo.Lang.Utils;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Lang;

public partial class ErgoLexer : IDisposable
{
    public readonly ErgoStream Stream;
    protected int Line { get; private set; }
    protected int Column { get; private set; }
    protected long Position => Stream.Position;
    protected string Context { get; private set; }

    public readonly Operator[] AvailableOperators;
    public readonly ErgoFacade Facade;

    protected readonly Dictionary<long, (StreamState State, Token Token)> _memoizationTable = new();

    #region Regular Expressions
    protected static readonly Regex UnescapeRegex =
        new("\\\\[abfnrtv?\"'\\\\]|\\\\[0-3]?[0-7]{1,2}|\\\\u[0-9a-fA-F]{4}|\\\\U[0-9a-fA-F]{8}|.", RegexOptions.Compiled);
    #endregion

    private readonly DiagnosticProbe Probe = new();
    public StreamState State => new(Stream.FileName, Position, Line, Column, Context);

    private void Memoize(StreamState state, Token tok, [CallerMemberName] string caller = null)
    {
#if !ERGO_LEXER_DISABLE_MEMOIZATION
        if (_memoizationTable.ContainsKey(state.Position))
            throw new InvalidOperationException();
        _memoizationTable[state.Position] = (State, tok);
#endif
    }

    public Maybe<Token> Memoized(StreamState state, [CallerMemberName] string caller = null)
    {
#if !ERGO_LEXER_DISABLE_MEMOIZATION
        if (_memoizationTable.TryGetValue(state.Position, out var memo))
        {
            Seek(memo.State, SeekOrigin.Begin);
            return Maybe.Some(memo.Token);
        }
#endif
        return default;
    }

    public void Seek(StreamState state, SeekOrigin origin = SeekOrigin.Begin)
    {
        Stream.Seek(state.Position, origin);
        Line = state.Line;
        Column = state.Column;
        Context = state.Context;
    }

    public void Reset()
    {
        Seek(State with { Position = 0, Line = 0, Column = 0, Context = string.Empty }, SeekOrigin.Begin);
    }

    public bool Eof => Stream.Position >= Stream.Length;

    internal ErgoLexer(ErgoFacade facade, ErgoStream s, IEnumerable<Operator> userOperators)
    {
        Facade = facade;
        Stream = s;
        AvailableOperators = userOperators.ToArray();
        OperatorSymbols = AvailableOperators
            .SelectMany(op => op.Synonyms
                .Select(s => (string)s.Value))
            .ToArray();
    }

    public Maybe<Token> PeekNext()
    {
        var s = State;
        return ReadNext()
            .Do(_ => Seek(s));
    }

    public static string Unescape(string s)
    {
        var sb = new StringBuilder();
        var mc = UnescapeRegex.Matches(s, 0);

        foreach (Match m in mc)
        {
            if (m.Length == 1)
            {
                sb.Append(m.Value);
            }
            else
            {
                if (m.Value[1] is >= '0' and <= '7')
                {
                    var i = Convert.ToInt32(m.Value[1..], 8);
                    sb.Append((char)i);
                }
                else if (m.Value[1] == 'u')
                {
                    var i = Convert.ToInt32(m.Value[2..], 16);
                    sb.Append((char)i);
                }
                else if (m.Value[1] == 'U')
                {
                    var i = Convert.ToInt32(m.Value[2..], 16);
                    sb.Append(char.ConvertFromUtf32(i));
                }
                else
                {
                    switch (m.Value[1])
                    {
                        case 'a':
                            sb.Append('\a');
                            break;
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        default:
                            sb.Append(m.Value[1]);
                            break;
                    }
                }
            }
        }

        return sb.ToString();
    }

    public Maybe<Token> ReadNext()
    {
        var pos = State;
        return Memoized(pos).Or(() => Maybe.None<Token>().Or(() =>
        {
            SkipWhitespace();
            SkipComments();
            if (Eof) return default;

            var ch = Peek();
            if (IsStringDelimiter(ch))
            {
                return ReadString(ch);
            }

            if (IsNumberStart(ch))
            {
                return ReadNumber();
            }

            if (IsIdentifierStart(ch))
            {
                return ReadIdentifier();
            }

            if (IsOperatorPiece(ch, 0))
            {
                return ReadOperator();
            }

            if (IsPunctuationPiece(ch))
            {
                return ReadPunctuation();
            }

            if (IsSingleLineCommentStart(ch))
            {
                return ReadSingleLineComment();
            }

            return default;
        })
        .Do(some => Memoize(pos, some)));


        // ------------------- Helpers -------------------
        static char ReadUTF8Char(ErgoStream s)
        {
            if (s.Position >= s.Length)
                throw new Exception("Error: Read beyond EOF");

            using var reader = new BinaryReader(s, Encoding.Unicode, true);
            var numRead = Math.Min(4, (int)(s.Length - s.Position));
            var bytes = reader.ReadBytes(numRead);
            var chars = Encoding.UTF8.GetChars(bytes);

            if (chars.Length == 0)
                throw new Exception("Error: Invalid UTF8 char");

            var charLen = Encoding.UTF8.GetByteCount(new char[] { chars[0] });

            s.Position += charLen - numRead;

            return chars[0];
        }

        char Peek()
        {
            var p = State;
            var ret = ReadUTF8Char(Stream);
            Seek(p);
            return ret;
        }

        bool TryPeekAhead(int n, out char c, bool skipWhitespace = true)
        {
            var p = State;
            c = (char)0;
            if (!Eof)
                c = Read();
            if (skipWhitespace)
            {
                while (!Eof && char.IsWhiteSpace(c = Peek()) && !IsNewline(c))
                    Read();
            }
            var ret = !Eof;
            for (int i = 0; i < n; i++)
            {
                if (ret = !Eof) c = Read();
            }
            Seek(p);
            return ret;
        }

        char Read()
        {
            var c = ReadUTF8Char(Stream);
            Context += c;
            if (IsCarriageReturn(c))
            {
                Column = 0;
                Context = "";
            }
            else if (IsNewline(c))
            {
                Line++;
                Column = 0;
                Context = "";
            }
            else
            {
                Column++;
            }

            return c;
        }

        bool IsSingleLineCommentStart(char c) => c == '%';
        bool IsStringDelimiter(char c) => c is '"' or '\'';
        bool IsCarriageReturn(char c) => c == '\r';
        bool IsNewline(char c) => c == '\n';
        bool IsDigit(char c) => char.IsDigit(c);
        bool IsDocumentationCommentStart(char c) => c == ':';
        bool IsNumberStart(char c) => IsDecimalDelimiter(c) || IsDigit(c);
        bool IsNumberPiece(char c) => IsDecimalDelimiter(c) || IsDigit(c);
        bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_' || c == '!' || c == '⊤' || c == '⊥';
        bool IsIdentifierPiece(char c) => IsIdentifierStart(c) || IsDigit(c);
        bool IsKeyword(string s) => KeywordSymbols.Contains(s);
        bool IsPunctuationPiece(char c) => PunctuationSymbols.SelectMany(p => p).Contains(c);
        bool IsDecimalDelimiter(char c) => c == '.' && TryPeekAhead(1, out var d, skipWhitespace: true) && IsDigit(d);
        bool IsOperatorPiece(char c, int index)
        {
            if (c == '\\') return true;
            var symbols = OperatorSymbols.Select(o => o.ElementAtOrDefault(index)).Where(x => x != 0)
                .ToHashSet();
            if (symbols.Contains(c))
            {
                if (c == '.' && index == 0)
                {
                    // Disambiguate between . as an operator for dict dereferencing, and . as a clause terminator or decimal separator
                    if (!TryPeekAhead(1, out var next, skipWhitespace: false))
                        // - EOF, which means that this is not an operator
                        return false;
                    if (IsPunctuationPiece(next))
                        return false;
                    if (char.IsWhiteSpace(next) || IsSingleLineCommentStart(next) || IsDocumentationCommentStart(next))
                        // - End of clause, which means that this is not an operator
                        return false;
                    if (IsDigit(c))
                        // Decimal separator
                        return false;
                    return true;
                }
                return true;
            }
            return false;
        }

        void SkipWhitespace()
        {
            while (!Eof && char.IsWhiteSpace(Peek()))
            {
                Read();
            }
        }

        void SkipComments()
        {
            while (!Eof && IsSingleLineCommentStart(Peek()))
            {
                var p = State;
                Read();
                if (Eof) break;
                var c = Read();
                Seek(p);
                if (IsDocumentationCommentStart(c))
                {
                    break;
                }

                ReadSingleLineComment();
                SkipWhitespace();
            }
        }

        Token ReadString(char delim)
        {
            var sb = new StringBuilder();
            var escapeSb = new StringBuilder();
            Read(); // Skip opening quotes
            while (!Eof)
            {
                var escaping = false;
                if (Peek() == '\\')
                {
                    escaping = true;
                    escapeSb.Append('\\');
                    Read();
                }

                if (Eof) break;
                if (Peek() != delim || escaping)
                {
                    escapeSb.Append(Read());
                    sb.Append(Unescape(escapeSb.ToString()));
                    escapeSb.Clear();
                }
                else
                {
                    Read();
                    break;
                }
            }

            return Token.FromString(sb.ToString());
        }

        Token ReadNumber()
        {
            var number = 0d;
            var integralPlaces = -1;
            for (var i = 0; !Eof && IsNumberPiece(Peek()); ++i)
            {
                if (IsDigit(Peek()))
                {
                    var digit = int.Parse(Read().ToString());
                    SkipWhitespace();
                    if (integralPlaces == -1)
                    {
                        number = number * 10 + digit;
                    }
                    else
                    {
                        number += digit / Math.Pow(10, i - integralPlaces);
                    }
                }
                else if (IsDecimalDelimiter(Peek()))
                {
                    if (integralPlaces != -1) break;
                    var s = State;
                    Read();
                    SkipWhitespace();
                    if (!Eof && !IsNumberPiece(Peek()))
                    {
                        Seek(s);
                        break;
                    }

                    integralPlaces = i;
                }
            }

            return Token.FromNumber(number);
        }

        Token ReadIdentifier()
        {
            var sb = new StringBuilder();
            while (!Eof && IsIdentifierPiece(Peek()))
            {
                sb.Append(Read());
            }

            var str = sb.ToString();
            if (IsKeyword(str))
            {
                return Token.FromKeyword(str);
            }

            return Token.FromITerm(str);
        }

        Token ReadSingleLineComment()
        {
            var sb = new StringBuilder();
            var p = State;
            Read(); // Discard comment marker
            SkipWhitespace();
            while (!Eof && !IsNewline(Peek()))
            {
                sb.Append(Read());
            }

            return Token.FromComment(sb.ToString().Trim());
        }

        Token ReadPunctuation()
        {
            var set = PunctuationSymbols.ToList();
            var i = 0;
            var p = State;
            while (!Eof && IsPunctuationPiece(Peek()))
            {
                var ch = Read();
                for (var o = set.Count - 1; o >= 0; o--)
                {
                    if (set[o].Length <= i || set[o][i] != ch)
                        set.RemoveAt(o);
                }

                if (set.Count >= 1)
                {
                    i++;
                    if (set.Count == 1)
                    {
                        while (!Eof && i++ < set[0].Length) Read();
                        break;
                    }
                }
                else
                {
                    Seek(p);
                    throw new LexerException(ErrorType.UnrecognizedPunctuation, State);
                }
            }

            SkipWhitespace();
            return Token.FromPunctuation(set.OrderBy(s => s.Length).First());
        }

        Token ReadOperator()
        {
            var set = OperatorSymbols.Distinct().ToList();
            var i = 0;
            var p = State;
            var peek = Peek();
            while (!Eof && IsOperatorPiece(peek, i) && set.SelectMany(x => x).Contains(peek))
            {
                var ch = Read();
                for (var o = set.Count - 1; o >= 0; o--)
                {
                    if (set[o].Length <= i || set[o][i] != ch)
                    {
                        set.RemoveAt(o);
                    }
                }

                if (set.Count >= 1)
                {
                    i++;
                    if (set.Count == 1)
                    {
                        while (!Eof && i++ < set[0].Length)
                        {
                            ch = Read();
                            if (ch != set[0][i - 1])
                            {
                                Seek(p);
                                throw new LexerException(ErrorType.UnrecognizedOperator, State);
                            }
                        }

                        break;
                    }
                }
                else
                {
                    Seek(p);
                    throw new LexerException(ErrorType.UnrecognizedOperator, State);
                }

                peek = Peek();
            }
            var op = set.OrderBy(s => s.Length).First();
            var token = Token.FromOperator(op);
            return token;
        }
    }

    public void Dispose()
    {
        Stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
