using System;

namespace Ergo.Lang
{
    public readonly struct Parsed<T>
    {
        private static T Box(object value) => (T)value;
        private readonly Lazy<Maybe<T>> _value;
        public readonly Maybe<T> Value => _value.Value;

        public Parsed(string data, ExceptionHandler handler, Func<string, T> onParseFail)
        {
            var parser = new Parser(new Lexer(Utils.FileStreamUtils.MemoryStream(data)));
            Func<Parser, T> parse = true switch {
                    _ when typeof(T) == typeof(Atom) => 
                    (Parser p) => p.TryParseAtom(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Variable) => 
                    (Parser p) => p.TryParseVariable(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Complex) => 
                    (Parser p) => p.TryParseComplex(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Term) => 
                    (Parser p) => p.TryParseTerm(out var x, out _) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(CommaExpression) => 
                    (Parser p) => p.TryParseExpression(out var x) && CommaExpression.TryUnfold(x.Complex, out var expr) ? Box(expr) : onParseFail(data)
                , _ when typeof(T) == typeof(Expression) => 
                    (Parser p) => p.TryParseExpression(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(List) => 
                    (Parser p) => p.TryParseList(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Predicate) => 
                    (Parser p) => p.TryParsePredicate(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Query) => 
                    (Parser p) => p.TryParseExpression(out var x) 
                        ? CommaExpression.TryUnfold(x.Complex, out var expr) 
                            ? Box(new Query(expr.Sequence))
                            : Box(new Query(CommaExpression.Build(x.Complex)))
                        : p.TryParseTerm(out var t, out _)
                            ? Box(new Query(CommaExpression.Build(t)))
                            : onParseFail(data)
                , _ when typeof(T) == typeof(Program) => 
                    (Parser p) => p.TryParseProgram(out var x) ? Box(x) : onParseFail(data)
                , _ => 
                    throw new ArgumentException($"Parsed<T> can't handle type: {typeof(T).Name}")
            };
            _value = new Lazy<Maybe<T>>(() => {
                var ret = handler.TryGet(() => parse(parser), out var parsed) ? Maybe<T>.Some(parsed) : Maybe<T>.None;
                parser.Dispose();
                return ret;
            });
        }
    }
}
