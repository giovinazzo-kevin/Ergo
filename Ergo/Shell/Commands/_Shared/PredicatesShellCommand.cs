using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands;

public abstract class PredicatesShellCommand : ShellCommand
{
    public readonly bool Explain;

    public PredicatesShellCommand(string[] names, string desc, bool explain)
        : base(names, desc, @"(?<term>[^\s].*)?", true, 90) => Explain = explain;

    public override async IAsyncEnumerable<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
    {
        var term = m.Groups["term"];
        var shellScope = scope;
        var interpreterScope = scope.InterpreterScope;
        var predicates = interpreterScope.KnowledgeBase.AsEnumerable();
        if (term?.Success ?? false)
        {
            var parsed = shell.Interpreter.Parse<NTuple>(interpreterScope, $"{term.Value}, true");
            if (!parsed.TryGetValue(out var tuple))
            {
                shell.No();
                yield return scope;
                yield break;
            }

            var yes = interpreterScope.ExceptionHandler.TryGet(() =>
            {
                var matches = interpreterScope.KnowledgeBase.GetMatches(new("S"), tuple.Contents.First(), desugar: true);
                if (matches.Any())
                {
                    predicates = predicates.Where(p =>
                        matches.Select(m => m.Rhs).Any(m => new Substitution(m.Head, p.Head).Unify().TryGetValue(out _)));
                    shellScope = shellScope.WithInterpreterScope(interpreterScope);
                    return true;
                }

                return false;
            });

            if (!yes.GetOr(false))
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
            .OrderBy(x => x switch
            {
                { IsDynamic: true } => 1,
                { IsExported: true } => 10,
                _ => 100
            })
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
