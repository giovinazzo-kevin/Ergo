using static Ergo.Lang.ErgoLexer;

namespace Ergo.Lang.Ast;

public readonly record struct ParserScope(StreamState LexerState);
