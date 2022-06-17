namespace Ergo.Lang.Parser;

public sealed class LambdaParser : AbstractTermParser<Lambda>
{
    public override Maybe<Lambda> TryParse(ErgoParser parser)
    {
        if (parser.TryParseAbstract<Set>(out var set))
        {
            if (!parser.ExpectDelimiter(s => WellKnown.Functors.Division.Contains(new Atom(s)), out _))
                return default;
        }
        else
        {
            set = Set.Empty;
        }

        if (!parser.TryParseAbstract<List>(out var list))
            return default;

        if (!parser.ExpectDelimiter(s => WellKnown.Functors.Lambda.Contains(new Atom(s)), out _))
            return default;

        if (!parser.TryParseTermOrExpression(out var term, out var parens))
            return default;

        return Maybe.Some(new Lambda(set, list, term));
    }
}
