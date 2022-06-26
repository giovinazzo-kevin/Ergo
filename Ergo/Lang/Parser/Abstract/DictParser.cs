using static Ergo.Lang.ErgoParser;

namespace Ergo.Lang.Parser;

public sealed class DictParser : IAbstractTermParser<Dict>
{
    public Maybe<Dict> TryParse(ErgoParser parser)
    {
        var functor = (Either<Atom, Variable>)default;
        if (parser.TryParseAtom(out var atom))
            functor = atom;
        else if (parser.TryParseVariable(out var variable))
            functor = variable;
        else
            return default;

        var argParse = new SetParser()
            .TryParse(parser);
        if (!argParse.TryGetValue(out var args))
            return default;
        if (!args.Contents.All(a => WellKnown.Functors.NamedArgument.Contains(a.GetFunctor().GetOr(default))))
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