using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public abstract class PredicatesShellCommand : ShellCommand
{
    public readonly bool Explain;

    public PredicatesShellCommand(string[] names, string desc, bool explain)
        : base(names, desc, @"(?<term>[^\s].*)?", true, 90)
    {
        Explain = explain;
    }

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var term = m.Groups["term"];
        var shellScope = scope;
        var interpreterScope = scope.InterpreterScope;
        var predicates = shell.GetInterpreterPredicates(scope)
            .Where(p => !p.Head.IsQualified);
        if (term?.Success ?? false)
        {
            var parsed = shell.Parse<CommaSequence>(scope, $"{term.Value}, true").Value;
            if (!parsed.HasValue)
            {
                shell.No();
                yield return scope;
                yield break;
            }
            if (!scope.ExceptionHandler.TryGet(scope, () =>
            {
                var matches = shell.Interpreter.GetMatches(ref interpreterScope, parsed.GetOrDefault().Contents.First());
                if (matches.Any())
                {
                    predicates = predicates.Where(p =>
                        matches.Select(m => m.Rhs).Any(m => new Substitution(m.Head, p.Head).Unify().HasValue));
                    shellScope = shellScope.WithInterpreterScope(interpreterScope);
                    return true;
                }
                return false;
            }, out var yes) || !yes)
            {
                shell.No();
                scope = shellScope;
                yield return scope;
                yield break;
            }
            scope = shellScope;
        }

        if (!Explain)
        {
            predicates = predicates.DistinctBy(p => p.Head.GetSignature());
        }

        var explanations = predicates
            .Select(p => Explain
                ? new[] { p.Head.GetSignature().Explain(), p.DeclaringModule.Explain(canonical: false), p.Explain(canonical: !Explain) }
                : new[] { p.Head.GetSignature().Explain(), p.DeclaringModule.Explain(canonical: false), p.Documentation })
            .ToArray();
        if (explanations.Length == 0)
        {
            shell.No();
            yield return scope;
            yield break;
        }
        var cols = Explain
            ? new[] { "Predicate", "Module", "Explanation" }
            : new[] { "Predicate", "Module", "Documentation" }
            ;
        shell.WriteTable(cols, explanations, Explain ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan);
        yield return scope;
    }
}
