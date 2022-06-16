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

        var argParse = new ListParser<BracyList>((h, t) => new(h, t))
            .TryParse(parser);
        if (!argParse.HasValue)
            return default;
        var args = argParse.GetOrThrow();
        if (!args.Contents.All(a => WellKnown.Functors.NamedArgument.Contains(a.GetFunctor().GetOrDefault())))
            return default;

        foreach (var item in args.Contents)
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

        var pairs = args.Contents.Select(item => new KeyValuePair<Atom, ITerm>((Atom)((Complex)item).Arguments[0], ((Complex)item).Arguments[1]));
        return Maybe.Some<Dict>(new(functor, pairs));
    }
}