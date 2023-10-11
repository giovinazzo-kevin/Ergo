using static Ergo.Lang.ErgoParser;

namespace Ergo.Lang.Parser;

public sealed class DictParser : IAbstractTermParser<Dict>
{
    public int ParsePriority => 0;
    public IEnumerable<Atom> FunctorsToIndex { get; }
        = new[] { new Atom("dict") };

    public Maybe<Dict> Parse(ErgoParser parser)
    {
        var functor = parser
            .Atom().Select(a => (Either<Atom, Variable>)a)
            .Or(() => parser.Variable().Select(v => (Either<Atom, Variable>)v));

        return functor
            .Map(f => new SetParser().Parse(parser)
                .Where(args => args.Contents.All(a => WellKnown.Functors.NamedArgument.Contains(a.GetFunctor().GetOr(default))))
                .Select(args => GetPairs(parser, args))
                .Select(pairs => new Dict(f, pairs)));

        static IEnumerable<KeyValuePair<Atom, ITerm>> GetPairs(ErgoParser parser, Set args)
        {
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

            return args.Contents.Select(item => new KeyValuePair<Atom, ITerm>((Atom)((Complex)item).Arguments[0], ((Complex)item).Arguments[1]));
        }
    }
}