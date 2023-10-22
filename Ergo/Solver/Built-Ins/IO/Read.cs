using System.Text;

namespace Ergo.Solver.BuiltIns;

public sealed class Read : SolverBuiltIn
{
    public Read()
        : base("", new("read"), 1, WellKnown.Modules.IO)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ImmutableArray<ITerm> arguments)
    {
        var sb = new StringBuilder();

        int ch;
        Maybe<ITerm> maybeTerm = default;
        while ((ch = context.Solver.In.Read()) != -1)
        {
            sb.Append((char)ch);
            if (ch == '\n')
            {
                maybeTerm = context.Solver.Facade.Parse<ITerm>(context.Scope, sb.ToString());
                if (maybeTerm.TryGetValue(out _))
                    break;
            }
        }

        if (!maybeTerm.TryGetValue(out ITerm term))
        {
            yield return False();
            yield break;
        }

        while ((ch = context.Solver.In.Peek()) != -1 && ch != '\n')
            context.Solver.In.Read();

        if (LanguageExtensions.Unify(arguments[0], term).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}