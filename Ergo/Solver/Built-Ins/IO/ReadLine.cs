using System.Text;

namespace Ergo.Solver.BuiltIns;

public sealed class ReadLine : SolverBuiltIn
{
    public ReadLine()
        : base("", new("read_line"), 1, WellKnown.Modules.IO)
    {
    }

    public override IEnumerable<Evaluation> Apply(SolverContext context, SolverScope scope, ITerm[] arguments)
    {
        var builder = new StringBuilder();
        int value;

        while ((value = context.Solver.In.Read()) != -1 && value != '\n')
        {
            builder.Append((char)value);
        }

        ITerm lineTerm = value != -1 ? new Atom(builder.ToString()) : new Atom("end_of_file");

        if (LanguageExtensions.Unify(arguments[0], lineTerm).TryGetValue(out var subs))
        {
            yield return True(subs);
            yield break;
        }
        yield return False();
    }
}