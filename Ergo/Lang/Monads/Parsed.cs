using Ergo.Facade;
using Ergo.Lang.Ast.Terms.Interfaces;
using Ergo.Lang.Utils;

namespace Ergo.Lang;

public static class Parsed
{
    public static Maybe<AbstractTerm> Abstract(ErgoFacade facade, string data, IEnumerable<Operator> userOperators, Type type, Func<string, Maybe<AbstractTerm>> onParseFail = null)
    {
        onParseFail ??= _ => default;
        var parser = facade.BuildParser(FileStreamUtils.MemoryStream(data), userOperators);
        return parser.Abstract(type);
    }
}

/// <summary>
/// Typed wrapper for common parser operations.
/// </summary>
public readonly struct Parsed<T>
{
    private static Maybe<U> Cast<V, U>(Maybe<V> value) => value.Select(u => (U)(object)u);
    public static Func<LegacyErgoParser, Maybe<T>> GetParser(string data, Func<string, Maybe<T>> onParseFail) => 0 switch
    {
        _ when typeof(T) == typeof(Atom) => p => Cast<Atom, T>(p.Atom()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Variable) => p => Cast<Variable, T>(p.Variable()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Complex) => p => Cast<Complex, T>(p.Complex()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Expr) => p => Cast<Expr, T>(p.Expression()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Clause) => p => Cast<Clause, T>(p.Clause()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Directive) => p => Cast<Directive, T>(p.Directive()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(ErgoProgram) => p => Cast<ErgoProgram, T>(p.Program()).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(ITerm) => p => Cast<ITerm, T>(p.Term()).Or(() => onParseFail(data)),
        _ when typeof(T).IsAssignableTo(typeof(AbstractTerm)) => p => Cast<AbstractTerm, T>(p.Abstract(typeof(T))).Or(() => onParseFail(data)),
        _ when typeof(T) == typeof(Query) =>
            (LegacyErgoParser p) => p.ExpressionOrTerm().Map(t => Cast<Query, T>(new Query(t))
                .Or(() => onParseFail(data))),
        _ => throw new ArgumentException($"Parsed<T> can't handle type: {typeof(T).Name}")
    };

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

    public Parsed(ErgoFacade facade, string data, IEnumerable<Operator> userOperators, Func<string, Maybe<T>> onParseFail = null)
    {
        onParseFail ??= _ => default;
        var parser = facade.BuildParser(FileStreamUtils.MemoryStream(data), userOperators);
        _value = new Lazy<Maybe<T>>(() =>
        {
            try
            {
                return GetParser(data, onParseFail)(parser);
            }
            catch (Exception e) when (e is ParserException or LexerException)
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
                return GetParser(data, onParseFail)(parser);
            }
            finally
            {
                parser.Dispose();
            }
        });
    }
}
