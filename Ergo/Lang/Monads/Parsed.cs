using Ergo.Facade;
using Ergo.Lang.Utils;

namespace Ergo.Lang;

/// <summary>
/// Typed wrapper for common parser operations.
/// </summary>
public readonly struct Parsed<T>
{
    private static Maybe<T> Box(object value) => (T)value;
    private readonly Lazy<Maybe<T>> _value;
    private readonly Lazy<Maybe<T>> _valueUnsafe;

    /// <summary>
    /// The parsed value, or None if the string could not be parsed or if a parser exception was thrown during parsing.
    /// </summary>
    public readonly Maybe<T> Value => _value.Value;
    /// <summary>
    /// The parsed value, or None if the string could not be parsed. Parser exceptions are not caught and will bubble up the stack.
    /// </summary>
    public readonly Maybe<T> ValueUnsafe => _valueUnsafe.Value;

    public Parsed(ErgoFacade facade, string data, Func<string, Maybe<T>> onParseFail, Operator[] userOperators)
    {
        var parser = facade.BuildParser(FileStreamUtils.MemoryStream(data), userOperators);
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
            // TODO: Generalize abstract terms
            _ when typeof(T) == typeof(List) =>
                (ErgoParser p) => p.TryParseTerm(out var x, out _) && x.IsAbstract<List>(out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(NTuple) =>
                (ErgoParser p) => p.TryParseTerm(out var x, out _) && x.IsAbstract<NTuple>(out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Set) =>
                (ErgoParser p) => p.TryParseTerm(out var x, out _) && x.IsAbstract<Set>(out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Dict) =>
                (ErgoParser p) => p.TryParseTerm(out var x, out _) && x.IsAbstract<Dict>(out var expr) ? Box(expr) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Expression) =>
                (ErgoParser p) => p.TryParseExpression(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Predicate) =>
                (ErgoParser p) => p.TryParsePredicate(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Directive) =>
                (ErgoParser p) => p.TryParseDirective(out var x) ? Box(x) : onParseFail(data)
            ,
            _ when typeof(T) == typeof(Query) =>
                (ErgoParser p) => p.TryParseExpression(out var x)
                    ? x.Complex.IsAbstract<NTuple>(out var expr)
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
