using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Shell.Commands
{
    public abstract class PredicatesShellCommand : ShellCommand
    {
        public readonly bool Explain;

        public PredicatesShellCommand(string[] names, string desc, bool explain)
            : base(names, desc, @"(?<term>[^\s].*)?", 90)
        {
            Explain = explain;
        }

        public override void Callback(ErgoShell shell, ref ShellScope scope, Match m)
        {
            var term = m.Groups["term"];
            var predicates = shell.GetInterpreterPredicates(scope);
            if (term?.Success ?? false)
            {
                var parsed = shell.Parse<CommaSequence>(scope, $"{term.Value}, true").Value;
                if (!parsed.HasValue)
                {
                    shell.No();
                    return;
                }
                var interpreterScope = scope.InterpreterScope;
                if (!scope.ExceptionHandler.TryGet(scope, () =>
                {
                    if (shell.Interpreter.TryGetMatches(interpreterScope, parsed.Reduce(some => some.Contents.First(), () => default), out var matches))
                    {
                        predicates = matches.Select(m => m.Rhs);
                        return true;
                    }
                    return false;
                }, out var yes) || !yes)
                {
                    shell.No();
                    return;
                }
            }

            if (!Explain)
            {
                predicates = predicates.DistinctBy(p => Predicate.Signature(p.Head));
            }

            var canonicals = predicates
                .Select(r => Explain
                    ? new[] { Predicate.Signature(r.Head), r.DeclaringModule.Explain(), r.Explain() }
                    : new[] { Predicate.Signature(r.Head), r.DeclaringModule.Explain(), r.Documentation })
                .ToArray();
            if (canonicals.Length == 0)
            {
                shell.No();
                return;
            }
            var cols = Explain
                ? new[] { "Predicate", "Module", "Explanation" }
                : new[] { "Predicate", "Module", "Documentation" }
                ;
            shell.WriteTable(cols, canonicals, Explain ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan);
        }
    }
}
