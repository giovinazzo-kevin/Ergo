namespace Ergo.Lang;

public readonly struct Parsed<T>
{
    private static Maybe<T> Box(object value) => Maybe.Some((T)value);
    private readonly Lazy<Maybe<T>> _value;
    private readonly Lazy<Maybe<T>> _valueUnsafe;
    public readonly Maybe<T> Value => _value.Value;
    public readonly Maybe<T> ValueUnsafe => _valueUnsafe.Value;

    public Parsed(string data, Action<ErgoParser> configureParser, Func<string, Maybe<T>> onParseFail, Operator[] userOperators)
    {
        var lexer = new Lexer(Utils.FileStreamUtils.MemoryStream(data), string.Empty, userOperators);
        var parser = new ErgoParser(lexer);
        configureParser?.Invoke(parser);
        Func<ErgoParser, Maybe<T>> parse = true switch
        {
            _ when typeof(T) == typeof(Atom) =>
            (ErgoParser p) => p.TryParseAtom(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Variable) =>
                (ErgoParser p) => p.TryParseVariable(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Complex) =>
                (ErgoParser p) => p.TryParseComplex(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(ITerm) =>
                (ErgoParser p) => p.TryParseTerm(out var x, out _) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(CommaSequence) =>
                (ErgoParser p) => p.TryParseExpression(out var x) && CommaSequence.TryUnfold(x.Complex, out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Expression) =>
                (ErgoParser p) => p.TryParseExpression(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(List) =>
                (ErgoParser p) => p.TryParseList(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Predicate) =>
                (ErgoParser p) => p.TryParsePredicate(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Directive) =>
                (ErgoParser p) => p.TryParseDirective(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Query) =>
                (ErgoParser p) => p.TryParseExpression(out var x)
                    ? CommaSequence.TryUnfold(x.Complex, out var expr)
                        ? Box(new Query(expr))
                        : Box(new Query(x.Complex))
                    : p.TryParseTerm(out var t, out _)
                        ? Box(new Query(t))
                        : onParseFail(data)
            ,
            _ when typeof(T) == typeof(ErgoProgram) =>
                (ErgoParser p) => p.TryParseProgram(out var x) ? Box(x) : onParseFail(data)
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
