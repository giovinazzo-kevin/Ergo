using Ergo.Lang.Ast.Terms.Abstract;
using static Ergo.Lang.ErgoParser;

namespace Ergo.Lang.Parser;

public sealed class DictParser : AbstractTermParser<Dict>
{
    public override Maybe<Dict> TryParse(ErgoParser parser)
    {
        var functor = (Either<Atom, Variable>)default;
        if (parser.TryParseAtom(out var atom))
            functor = atom;
        else if (parser.TryParseVariable(out var variable))
            functor = variable;
        else
            return default;

        // TODO: Use a bracy list instead
        if (!parser.TryParseSequence(
              CommaSequence.CanonicalFunctor
            , CommaSequence.EmptyLiteral
            , () => parser.TryParseTermOrExpression(out var t, out var p)
                ? (t is Complex cplx && (WellKnown.Functors.NamedArgument.Contains(cplx.Functor) || WellKnown.Functors.Conjunction.Contains(cplx.Functor)), t, p)
                : (false, default, p)
            , "{", ",", "}"
            , true
            , out var inner
        ))
        {
            return default;
        }

        foreach (var item in inner.Contents)
        {
            if (item is not Complex)
            {
                throw new ParserException(ErrorType.KeyExpected, parser.Lexer.State, item.Explain());
            }

            if (item is Complex cplx && cplx.Arguments.First() is not Atom)
            {
                throw new ParserException(ErrorType.KeyExpected, parser.Lexer.State, cplx.Arguments.First().Explain());
            }
        }

        var pairs = inner.Contents.Select(item => new KeyValuePair<Atom, ITerm>((Atom)((Complex)item).Arguments[0], ((Complex)item).Arguments[1]));
        return Maybe.Some<Dict>(new(functor, pairs));
    }
}