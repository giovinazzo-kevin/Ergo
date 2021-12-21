using System;

namespace Ergo.Lang
{
    public readonly struct Parsed<T>
    {
        private static T Box(object value) => (T)value;
        private readonly Lazy<Maybe<T>> _value;
        public readonly Maybe<T> Value => _value.Value;

        public Parsed(string data, ExceptionHandler handler, Func<string, T> onParseFail, Operator[] userOperators)
        {
            var parser = new Parser(new Lexer(Utils.FileStreamUtils.MemoryStream(data), userOperators), userOperators);
            Func<Parser, T> parse = true switch {
                    _ when typeof(T) == typeof(Atom) => 
                    (Parser p) => p.TryParseAtom(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Variable) => 
                    (Parser p) => p.TryParseVariable(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Complex) => 
                    (Parser p) => p.TryParseComplex(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(ITerm) => 
                    (Parser p) => p.TryParseTerm(out var x, out _) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(CommaSequence) => 
                    (Parser p) => p.TryParseExpression(out var x) && CommaSequence.TryUnfold(x.Complex, out var expr) ? Box(expr) : onParseFail(data)
                , _ when typeof(T) == typeof(Expression) => 
                    (Parser p) => p.TryParseExpression(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(List) => 
                    (Parser p) => p.TryParseList(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Predicate) => 
                    (Parser p) => p.TryParsePredicate(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Directive) => 
                    (Parser p) => p.TryParseDirective(out var x) ? Box(x) : onParseFail(data)
                , _ when typeof(T) == typeof(Query) => 
                    (Parser p) => p.TryParseExpression(out var x) 
                        ? CommaSequence.TryUnfold(x.Complex, out var expr) 
                            ? Box(new Query(expr))
                            : Box(new Query(new(x.Complex)))
                        : p.TryParseTerm(out var t, out _)
                            ? Box(new Query(new(t)))
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
