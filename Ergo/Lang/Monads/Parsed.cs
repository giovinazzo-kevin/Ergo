using Ergo.Lang.Exceptions;
using System.Collections.Immutable;

namespace Ergo.Lang;

public readonly struct Parsed<T>
{
    private static Maybe<T> Box(object value) => Maybe.Some((T)value);
    private readonly Lazy<Maybe<T>> _value;
    private readonly Lazy<Maybe<T>> _valueUnsafe;
    public readonly Maybe<T> Value => _value.Value;
    public readonly Maybe<T> ValueUnsafe => _valueUnsafe.Value;

    public Parsed(string data, Func<string, Maybe<T>> onParseFail, Operator[] userOperators)
    {
        var parser = new Parser(new Lexer(Utils.FileStreamUtils.MemoryStream(data), string.Empty, userOperators));
        Func<Parser, Maybe<T>> parse = true switch
        {
            _ when typeof(T) == typeof(Atom) =>
            (Parser p) => p.TryParseAtom(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Variable) =>
                (Parser p) => p.TryParseVariable(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Complex) =>
                (Parser p) => p.TryParseComplex(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(ITerm) =>
                (Parser p) => p.TryParseTerm(out var x, out _) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(CommaSequence) =>
                (Parser p) => p.TryParseExpression(out var x) && CommaSequence.TryUnfold(x.Complex, out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Expression) =>
                (Parser p) => p.TryParseExpression(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(List) =>
                (Parser p) => p.TryParseList(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Predicate) =>
                (Parser p) => p.TryParsePredicate(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Directive) =>
                (Parser p) => p.TryParseDirective(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Query) =>
                (Parser p) => p.TryParseExpression(out var x)
                    ? CommaSequence.TryUnfold(x.Complex, out var expr)
                        ? Box(new Query(expr))
                        : Box(new Query(new(ImmutableArray<ITerm>.Empty.Add(x.Complex))))
                    : p.TryParseTerm(out var t, out _)
                        ? Box(new Query(new(ImmutableArray<ITerm>.Empty.Add(t))))
                        : onParseFail(data)
            ,
            _ when typeof(T) == typeof(ErgoProgram) =>
                (Parser p) => p.TryParseProgram(out var x) ? Box(x) : onParseFail(data)
            ,
            _ =>
                throw new ArgumentException($"Parsed<T> can't handle type: {typeof(T).Name}")
        };
        _value = new Lazy<Maybe<T>>(() =>
        {
            try
            {
                return parse(parser);
            }
            catch (ParserException)
            {
                return Maybe<T>.None;
            }
            finally
            {
                parser.Dispose();
            }
        });
        _valueUnsafe = new Lazy<Maybe<T>>(() =>
        {
            try
            {
                return parse(parser);
            }
            finally
            {
                parser.Dispose();
            }
        });
    }
}
