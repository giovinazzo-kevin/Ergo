using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public partial class Lexer : IDisposable
    {
        private readonly Stream _reader;
        protected int Line { get; private set; }
        protected int Column { get; private set; }
        protected long Position => _reader.Position;
        protected string Context { get; private set; }
        protected string Filename { get; private set; }

        public StreamState State => new(Filename, Position, Line, Column, Context);

        public void Seek(StreamState state, SeekOrigin origin = SeekOrigin.Begin)
        {
            _reader.Seek(state.Position, origin);
            Line = state.Line;
            Column = state.Column;
            Context = state.Context;
            Filename = state.Filename;
        }

        public bool Eof => _reader.Position >= _reader.Length;

        public Lexer(Stream s, IEnumerable<Operator> userOperators, string fn = "")
        {
            Filename = fn;
            _reader = s;
            OperatorSymbols = Operators.DefinedOperators
                .Concat(userOperators)
                .SelectMany(op => op.Synonyms
                    .Select(s => (string)s.Value))
                .ToArray();
        }

        public bool TryPeekNextToken(out Token next)
        {
            var s = State;
            if (TryReadNextToken(out next)) {
                Seek(s);
                return true;
            }
            Seek(s);
            return false;
        }

        public bool TryReadNextToken(out Token next)
        {
            next = default;
            SkipWhitespace();
            SkipComments();
            if (Eof) return false;

            var ch = Peek();
            if (IsStringDelimiter(ch)) {
                next = ReadString(ch);
                return true;
            }
            if (IsNumberStart(ch)) {
                next = ReadNumber();
                return true;
            }
            if (IsIdentifierStart(ch)) {
                next = ReadIdentifier();
                return true;
            }
            if (IsOperatorPiece(ch)) {
                next = ReadOperator();
                return true;
            }
            if (IsPunctuationPiece(ch)) {
                next = ReadPunctuation();
                return true;
            }
            if (IsSingleLineCommentStart(ch)) {
                next = ReadSingleLineComment();
                return true;
            }
            return false;

            // ------------------- Helpers -------------------
            static char ReadUTF8Char(Stream s)
            {
                if (s.Position >= s.Length)
                    throw new Exception("Error: Read beyond EOF");

                using (BinaryReader reader = new BinaryReader(s, Encoding.Unicode, true))
                {
                    int numRead = Math.Min(4, (int)(s.Length - s.Position));
                    byte[] bytes = reader.ReadBytes(numRead);
                    char[] chars = Encoding.UTF8.GetChars(bytes);

                    if (chars.Length == 0)
                        throw new Exception("Error: Invalid UTF8 char");

                    int charLen = Encoding.UTF8.GetByteCount(new char[] { chars[0] });

                    s.Position += (charLen - numRead);

                    return chars[0];
                }
            }

            char Peek()
            {
                var p = State;
                var ret = ReadUTF8Char(_reader);
                Seek(p);
                return ret;
            }

            char Read()
            {
                var c = ReadUTF8Char(_reader);
                Context += c;
                if (IsCarriageReturn(c)) {
                    Column = 0;
                    Context = "";
                }
                else if (IsNewline(c)) {
                    Line++;
                    Column = 0;
                    Context = "";
                }
                else {
                    Column++;
                }
                return c;
            }

            bool IsSingleLineCommentStart(char c) => c == '%';
            bool IsStringDelimiter(char c) => c == '"' || c == '\'';
            bool IsCarriageReturn(char c) => c == '\r';
            bool IsNewline(char c) => c == '\n';
            bool IsDigit(char c) => Char.IsDigit(c);
            bool IsDecimalDelimiter(char c) => c == '.';
            bool IsDocumentationCommentStart(char c) => c == ':';
            bool IsNumberStart(char c) => IsDigit(c);
            bool IsNumberPiece(char c) => IsNumberStart(c) || IsDecimalDelimiter(c) || c == '-' || c == '+';
            bool IsIdentifierStart(char c) => Char.IsLetter(c) || c == '@' || c == '_';
            bool IsIdentifierPiece(char c) => IsIdentifierStart(c) || IsDigit(c);
            bool IsKeyword(string s) => KeywordSymbols.Contains(s);
            bool IsPunctuationPiece(char c) => PunctuationSymbols.SelectMany(p => p).Contains(c);
            bool IsOperatorPiece(char c) => OperatorSymbols.SelectMany(o => o).Contains(c);


            void SkipWhitespace()
            {
                while(!Eof && Char.IsWhiteSpace(Peek())) {
                    Read();
                }
            }

            void SkipComments()
            {
                while (!Eof && IsSingleLineCommentStart(Peek())) {
                    var p = State;
                    Read();
                    if (Eof) break;
                    var c = Read();
                    Seek(p);
                    if (IsDocumentationCommentStart(c)) {
                        break;
                    }
                    ReadSingleLineComment();
                    SkipWhitespace();
                }
            }

            
            Token ReadString(char delim)
            {
                var sb = new StringBuilder();
                Read(); // Skip opening quotes
                while (!Eof) {
                    var escaping = false;
                    if (Peek() == '\\') {
                        escaping = true;
                        Read();
                    }
                    if (Eof) break;
                    if (Peek() != delim || escaping) {
                        var escaped = $"{Read()}";
                        if(escaping) {
                            escaped = escaped == "\\" ? escaped : Regex.Unescape($"\\{escaped}");
                        }
                        sb.Append(escaped);
                    }
                    else {
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
                for (int i = 0; !Eof && IsNumberPiece(Peek()); ++i) {
                    if (IsDigit(Peek())) {
                        var digit = Int32.Parse(Read().ToString());
                        if (integralPlaces == -1) {
                            number = number * 10 + digit;
                        }
                        else {
                            number += digit / Math.Pow(10, i - integralPlaces);
                        }
                    }
                    else if (IsDecimalDelimiter(Peek())) {
                        if (integralPlaces != -1) break;
                        var s = State;
                        Read();
                        if(!Eof && !IsNumberPiece(Peek())) {
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
                while (!Eof && IsIdentifierPiece(Peek())) {
                    sb.Append(Read());
                }
                var str = sb.ToString();
                if (IsKeyword(str)) {
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
                while (!Eof && !IsNewline(Peek())) {
                    sb.Append(Read());
                }
                return Token.FromComment(sb.ToString().Trim());
            }

            Token ReadPunctuation()
            {
                var set = PunctuationSymbols.ToList();
                int i = 0;
                var p = State;
                while (!Eof && IsPunctuationPiece(Peek())) {
                    var ch = Read();
                    for (int o = set.Count - 1; o >= 0; o--) {
                        if (set[o].Length <= i || set[o][i] != ch)
                            set.RemoveAt(o);
                    }
                    if (set.Count >= 1) {
                        i++;
                        if (set.Count == 1) {
                            while (!Eof && i++ < set[0].Length) Read();
                            break;
                        }
                    }
                    else {
                        Seek(p);
                        throw new LexerException(ErrorType.UnrecognizedPunctuation, State);
                    }
                }
                SkipWhitespace();
                return Token.FromPunctuation(set.OrderBy(s => s.Length).First());
            }

            Token ReadOperator()
            {
                var set = OperatorSymbols.ToList();
                int i = 0;
                var p = State;
                while (!Eof && IsOperatorPiece(Peek())) {
                    var ch = Read();
                    for (int o = set.Count - 1; o >= 0; o--) {
                        if (set[o].Length <= i || set[o][i] != ch)
                            set.RemoveAt(o);
                    }
                    if (set.Count >= 1) {
                        i++;
                        if (set.Count == 1) {
                            while (!Eof && i++ < set[0].Length) {
                                ch = Read();
                                if(ch != set[0][i - 1]) {
                                    Seek(p);
                                    throw new LexerException(ErrorType.UnrecognizedOperator, State);
                                }
                            }
                            break;
                        }
                    }
                    else {
                        Seek(p);
                        throw new LexerException(ErrorType.UnrecognizedOperator, State);
                    }
                }
                SkipWhitespace();
                return Token.FromOperator(set.OrderBy(s => s.Length).First());
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
