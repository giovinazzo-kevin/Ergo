using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ergo.Shell.Commands
{
    public abstract class PredicatesShellCommand : ShellCommand
    {
        public readonly bool Explain;

        public PredicatesShellCommand(string[] names, string desc, bool explain)
            : base(names, desc, @"(?<term>[^\s].*)?", true, 90)
        {
            Explain = explain;
        }

        public override async Task<ShellScope> Callback(ErgoShell shell, ShellScope scope, Match m)
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
                    return scope;
                }
                if (!scope.ExceptionHandler.TryGet(scope, () =>
                {
                    var matches = shell.Interpreter.GetMatches(ref interpreterScope, parsed.GetOrDefault().Contents.First());
                    if (matches.Any())
                    {
                        predicates = matches.Select(m => m.Rhs);
                        shellScope = shellScope.WithInterpreterScope(interpreterScope);
                        return true;
                    }
                    return false;
                }, out var yes) || !yes)
                {
                    shell.No();
                    scope = shellScope;
                    return scope;
                }
                scope = shellScope;
            }

            if (!Explain)
            {
                predicates = predicates.DistinctBy(p => p.Head.GetSignature());
            }

            var explanations = predicates
                .Select(r => Explain
                    ? new[] { r.Head.GetSignature().Explain(), r.DeclaringModule.Explain(canonical: false), r.Explain(canonical: !Explain) }
                    : new[] { r.Head.GetSignature().Explain(), r.DeclaringModule.Explain(canonical: false), r.Documentation })
                .ToArray();
            if (explanations.Length == 0)
            {
                shell.No();
                return scope;
            }
            var cols = Explain
                ? new[] { "Predicate", "Module", "Explanation" }
                : new[] { "Predicate", "Module", "Documentation" }
                ;
            shell.WriteTable(cols, explanations, Explain ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan);
            return scope;
        }
    }
}
