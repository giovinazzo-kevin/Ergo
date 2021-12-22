using Ergo.Lang.Ast;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public abstract class PredicatesShellCommand : ShellCommand
    {
        public readonly bool Explain;

        public PredicatesShellCommand(string[] names, string desc, bool explain)
            : base(names, desc, @"(?<term>[^\s].*)?", 100)
        {
            Explain = explain;
        }

        public override void Callback(Shell s, Match m)
        {
            var term = m.Groups["term"];
            var predicates = s.GetInterpreterPredicates(Maybe.Some(s.CurrentModule));
            if (term?.Success ?? false)
            {
                var parsed = s.Parse<CommaSequence>($"{term.Value}, true").Value;
                if (!parsed.HasValue)
                {
                    s.No();
                    return;
                }
                if (!s.ExceptionHandler.TryGet(() =>
                {
                    if (s.Interpreter.TryGetMatches(parsed.Reduce(some => some.Contents.First(), () => default), s.CurrentModule, out var matches))
                    {
                        predicates = matches.Select(m => m.Rhs);
                        return true;
                    }
                    return false;
                }, out var yes) || !yes)
                {
                    s.No();
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
                s.No();
                return;
            }
            var cols = Explain
                ? new[] { "Predicate", "Module", "Explanation" }
                : new[] { "Predicate", "Module", "Documentation" }
                ;
            s.WriteTable(cols, canonicals, Explain ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan);
        }
    }
}
