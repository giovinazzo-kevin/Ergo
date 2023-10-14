using static Ergo.Lang.ErgoParser;

namespace Ergo.Lang.Parser;

public sealed class DictParser : IAbstractTermParser<Dict>
{
    public int ParsePriority => 0;
    public Maybe<Dict> Parse(ErgoParser parser)
    {
        var scope = parser.GetScope();
        return ParseCanonical()
            .Or(ParseSugared);
        Maybe<Dict> ParseCanonical()
        {
            return parser.Complex()
                .Where(c => WellKnown.Functors.Dict.Contains(c.Functor) && c.Arity == 2
                    && (c.Arguments[0] is Variable || c.Arguments[0] is Atom)
                    && (c.Arguments[1] is Set || c.Arguments[1] is Variable))
                .Select(c => c.Arguments[1] is Set
                    ? new Dict(Label(c.Arguments[0]), GetPairs((Set)c.Arguments[1], scope))
                    : new Dict(Label(c.Arguments[0]), (Variable)c.Arguments[1]));
            Either<Atom, Variable> Label(ITerm t)
            {
                if (t is Atom a)
                    return a;
                if (t is Variable v)
                    return v;
                return default;
            }
        }
        Maybe<Dict> ParseSugared()
        {
            var scope = parser.GetScope();
            var functor = parser
                .Atom().Select(a => (Either<Atom, Variable>)a)
                .Or(() => parser.Variable().Select(v => (Either<Atom, Variable>)v));

            return functor
                .Map(f => new SetParser().Parse(parser)
                    .Where(args => args.Contents.All(a => WellKnown.Functors.NamedArgument.Contains(a.GetFunctor().GetOr(default))))
                    .Select(args => GetPairs(args, scope))
                    .Select(pairs => new Dict(f, pairs, scope, false)));

        }
    }
    public static IEnumerable<KeyValuePair<Atom, ITerm>> GetPairs(Set args, ParserScope scope)
    {
        foreach (var item in args.Contents)
        {
            if (item is not Complex)
            {
                throw new ParserException(ErrorType.KeyExpected, scope.LexerState, item.Explain());
            }

            if (item is Complex cplx && cplx.Arguments.First() is not Atom)
            {
                throw new ParserException(ErrorType.KeyExpected, scope.LexerState, cplx.Arguments.First().Explain());
            }
        }
        return args.Contents.Select(item => new KeyValuePair<Atom, ITerm>((Atom)((Complex)item).Arguments[0], ((Complex)item).Arguments[1]));
    }
}