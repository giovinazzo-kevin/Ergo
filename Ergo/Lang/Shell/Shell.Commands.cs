using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ergo.Lang
{
    public partial class Shell
    {
        protected virtual void InitializeCommandDispatcher()
        {
            Dispatcher.Add(
                @"^\s*(?:\?|help)\s+(?<cmd>[^\s].*)\s*$"
                , m => Cmd_PrintHelp(m.Groups["cmd"])
                , "? <cmd>"
                , "(alias: help) Displays help about all commands that start with the given string."
            );

            Dispatcher.Add(
                @"^\s*(?:\?|help)\s*$"
                , m => Cmd_PrintHelp(null)
                , "?"
                , "(alias: help) Displays help about all available commands."
            );

            Dispatcher.Add(
                @"^\s*(?::\?|expl)\s+(?<ITerm>[^\s].*)\s*$"
                , m => CmdPrintPredicates(m.Groups["ITerm"], explain: true)
                , ":? <ITerm>"
                , "(alias: expl) Explains all the predicates that match the given ITerm."
            );

            Dispatcher.Add(
                @"^\s*(?::\?|expl)\s*$"
                , m => CmdPrintPredicates(null, explain: true)
                , ":?"
                , "(alias: expl) Explains all the predicates that are available to the interpreter."
            );

            Dispatcher.Add(
                @"^\s*(?::#|builtin)\s+(?<ITerm>[^\s].*)\s*$"
                , m => CmdPrintBuiltIns(m.Groups["ITerm"])
                , ":# <ITerm>"
                , "(alias: builtin) Describes all the built-ins that match the given ITerm."
            );

            Dispatcher.Add(
                @"^\s*(?::#|builtin)\s*$"
                , m => CmdPrintBuiltIns(null)
                , ":#"
                , "(alias: builtin) Describes all the built-ins that are available to the interpeter."
            );


            Dispatcher.Add(
                @"^\s*(?::|desc)\s+(?<ITerm>[^\s].*)\s*$"
                , m => CmdPrintPredicates(m.Groups["ITerm"], explain: false)
                , ": <ITerm>"
                , "(alias: desc) Describes all the predicates that match the given ITerm."
            );

            Dispatcher.Add(
                @"^\s*(?::|desc)\s*$"
                , matches => CmdPrintPredicates(null, explain: false)
                , ":"
                , "(alias: desc) Describes all the predicates that are available to the interpreter."
            );


            Dispatcher.Add(
                @"^\s*(?:!-|asserta)\s+(?<predicate>.*)\s*$"
                , m => Cmd_Assert(m.Groups["predicate"], start: true)
                , "!- <predicate>"
                , "(alias: asserta) Asserts a predicate at the beginning of the knowledge bank."
            );

            Dispatcher.Add(
                @"^\s*(?:-!|assertz)\s+(?<predicate>.*)\s*$"
                , m => Cmd_Assert(m.Groups["predicate"], start: false)
                , "-! <predicate>"
                , "(alias: assertz) Asserts a predicate at the end of the knowledge bank."
            );

            Dispatcher.Add(
                @"^\s*(?:\*\*|retractall)\s+(?<ITerm>[^\s].*)\s*$"
                , m => Cmd_Retract(m.Groups["ITerm"], all: true)
                , "** <ITerm>"
                , "(alias: retractall) Retracts all predicates that match the given ITerm from the knowledge bank."
            );

            Dispatcher.Add(
                @"^\s*(?:\*|retract)\s+(?<ITerm>[^\s].*)\s*$"
                , m => Cmd_Retract(m.Groups["ITerm"], all: false)
                , "* <ITerm> "
                , "(alias: retract) Retracts the first predicate that matches the given ITerm from the knowledge bank."
            );

            Dispatcher.Add(
                @"^\s*load\s+(?<path>.*)\s*$"
                , m => Cmd_Load(m.Groups["path"])
                , "load <path>"
                , "Loads a knowledge bank from disk."
            );

            Dispatcher.Add(
                @"^\s*save\s+(?<path>.*)\s*$"
                , m => Cmd_Save(m.Groups["path"])
                , "save <path>"
                , "Saves the current knowledge bank to disk."
            );

            Dispatcher.Add(
                @"^\s*:-\s+(?<dir>.*)\s*$"
                , m => Cmd_Directive(m.Groups["dir"])
                , ":- <directive>"
                , "Executes an interpreter directive."
            );

            Dispatcher.Add(
                @"^\s*cls\s*$"
                , m => Clear()
                , "cls"
                , "Clears the screen."
            );

            Dispatcher.Add(
                @"^\s*trace\s*$"
                , m => Cmd_ToggleTrace()
                , "trace"
                , "Toggles trace mode."
            );

#if DEBUG
            Dispatcher.Add(
                @"^\s*throw\s*$"
                , m => Cmd_ToggleThrow()
                , "throw"
                , "Toggles throw mode, wherein unhandled .NET exceptions are thrown (for debugging purposes)."
            );
#endif
            var colorMap = Enum.GetNames(typeof(ConsoleColor))
                .Select(v => v.ToString())
                .ToList();
            var colorAlternatives = String.Join('|', colorMap);
            Dispatcher.Add(
                @$"^\s*\$(?<color>{colorAlternatives})?\s*(?<query>(?:[^\s].*\s*=)?\s*[^\s].*)\s*$"
                , m => Cmd_Solve(m.Groups["query"], interactive: false, 
                    accentColor: m.Groups["color"] is { Success: true, Value: var v } ? Enum.Parse<ConsoleColor>(colorMap.Single(x => x.Equals(v, StringComparison.OrdinalIgnoreCase))) : ConsoleColor.Black)
                , "$ <query>"
                , "Solves a clause or group of clauses in tabular mode, aggregating and returning all solutions at once."
            );

            Dispatcher.Add(
                @"^\s*(?<query>(?:[^\s].*\s*=)?\s*[^\s].*)\s*$"
                , m => Cmd_Solve(m.Groups["query"], interactive: true)
                , "<query>"
                , "Solves a clause or group of clauses in interactive mode, solution by solution."
            );

        }

        protected void Cmd_ToggleTrace()
        {
            TraceMode = !TraceMode;
            WriteLine($"Trace mode {(TraceMode ? "enabled" : "disabled")}.", LogLevel.Inf);
        }

        protected void Cmd_ToggleThrow()
        {
            ThrowUnhandledExceptions = !ThrowUnhandledExceptions;
            WriteLine($"Throw mode {(ThrowUnhandledExceptions ? "enabled" : "disabled")}.", LogLevel.Inf);
        }

        protected void Cmd_Solve(Group q, bool interactive, ConsoleColor accentColor = ConsoleColor.Black)
        {
            var userQuery = q.Value;
            if(!userQuery.EndsWith('.')) {
                // Syntactic sugar
                userQuery += '.';
            }
            var parsed = new Parsed<Query>(userQuery, Handler, str => throw new ShellException($"'{str}' does not resolve to a query."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
            if (!parsed.HasValue) {
                return;
            }

            var query = parsed.Reduce(some => some, () => default);
            WriteLine(query.Goals.Explain(), LogLevel.Dbg);

            var solutions = Interpreter.Solve(query, Maybe.Some(CurrentModule)); // Solution graph is walked lazily
            if (query.Goals.Contents.Length == 1 && query.Goals.Contents.Single() is Variable) {
                // SWI-Prolog goes with The Ultimate Question, we'll go with The Last Question instead.
                WriteLine("THERE IS AS YET INSUFFICIENT DATA FOR A MEANINGFUL ANSWER.", LogLevel.Cmt);
                No();
                return;
            }

            Handler.Try(() => {
                if (interactive) {
                    WriteLine("Press space to yield more solutions:", LogLevel.Inf);
                    var any = false;
                    foreach (var s in solutions) {
                        any = true;
                        if(s.Substitutions.Any()) {
                            var join = String.Join(", ", s.Simplify().Select(s => s.Explain()));
                            WriteLine($"\t| {join}");
                            if (ReadChar(true) != ' ') {
                                break;
                            }
                        }
                    }
                    if (any) Yes(); else No();
                }
                else {
                    var cols = query.Goals
                        .Contents
                        .SelectMany(t => t.Variables)
                        .Where(v => !v.Ignored)
                        .Select(v => v.Name)
                        .Distinct()
                        .ToArray();
                    var rows = solutions
                        .Select(s => s.Simplify()
                            .Select(r => r.Rhs.Explain())
                            .ToArray())
                        .ToArray();
                    if (rows.Length > 0 && rows[0].Length == cols.Length) {
                        WriteTable(cols, rows, accentColor);
                        Yes();
                    }
                    else {
                        No();
                    }
                }
            });
        }

        protected void Cmd_Load(Group path)
        {
            Load(path.Value);
        }

        protected void Cmd_Save(Group path)
        {
            Save(path.Value);
        }

        protected void Cmd_Directive(Group dir)
        {
            var currentModule = Interpreter.Modules[Interpreter.UserModule];
            var parsed = new Parsed<Directive>($":- {(dir.Value.EndsWith('.') ? dir.Value : dir.Value + '.')}", Handler, str => throw new ShellException($"'{str}' does not resolve to a directive."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
            var directive = parsed.Reduce(some => some, () => default);
            if (Interpreter.RunDirective(directive, ref currentModule, fromCli: true))
            {
                CurrentModule = currentModule.Name;
            }
            else throw new ShellException($"'{dir.Value}' does not resolve to a directive.");
        }

        protected void Cmd_Assert(Group predicate, bool start)
        {
            var parsed = new Parsed<Predicate>(predicate.Value, Handler, str => throw new ShellException($"'{str}' does not resolve to a predicate."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
            if (!parsed.HasValue) {
                return;
            }
            var pred = parsed.Reduce(some => some, () => default);
            Handler.Try(() => {
                if (start) {
                    Interpreter.AssertA(CurrentModule, pred);
                }
                else {
                    Interpreter.AssertZ(CurrentModule, pred);
                }
            });
            WriteLine($"Asserted {Predicate.Signature(pred.Head)} at the {(start ? "beginning" : "end")} of the predicate list.", LogLevel.Inf);
        }

        protected void Cmd_Retract(Group ITerm, bool all)
        {
            var parsed = new Parsed<ITerm>(ITerm.Value, Handler, str => throw new ShellException($"'{str}' does not resolve to a ITerm."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
            if (!parsed.HasValue) {
                return;
            }
            var t = parsed.Reduce(some => some, () => default);
            Handler.Try(() => {
                if (all) {
                    if (Interpreter.RetractAll(Interpreter.UserModule, t) is { } delta && delta > 0) {
                        WriteLine($"Retracted {delta} predicates that matched with {t}.", LogLevel.Inf);
                    }
                    else {
                        No();
                    }
                }
                else {
                    if (Interpreter.RetractOne(Interpreter.UserModule, t)) {
                        Yes();
                    }
                    else {
                        No();
                    }
                }
            });
        }

        protected virtual void Cmd_PrintHelp(Group cmd)
        {
            var dispatchersQuery = Dispatcher.RegisteredDispatchers;
            if (cmd?.Success ?? false) {
                var alias = $"alias: {cmd.Value}";
                dispatchersQuery = dispatchersQuery.Where(d => 
                    d.Name.StartsWith(cmd.Value, StringComparison.OrdinalIgnoreCase)
                    || d.Name.Contains(alias, StringComparison.OrdinalIgnoreCase)
                );
            }
            var dispatchers = dispatchersQuery
                .Select(d => new[] { d.Name, d.Description })
                .ToArray();
            if(dispatchers.Length == 0) {
                Dispatcher.Dispatch(cmd.Value); // Dispatches UnknownCommand
                return;
            }
            WriteTable(new[] { "Command", "Description" }, dispatchers, ConsoleColor.DarkGreen);
        }

        protected virtual void CmdPrintPredicates(Group ITerm, bool explain)
        {
            var predicates = GetInterpreterPredicates(Maybe.Some(CurrentModule));
            if (ITerm?.Success ?? false) {
                var parsed = new Parsed<CommaSequence>($"{ITerm.Value}, true", Handler, str => throw new ShellException($"'{str}' does not resolve to a ITerm."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
                if (!parsed.HasValue) {
                    No();
                    return;
                }
                if(!Handler.TryGet(() =>
                {
                    if (Interpreter.TryGetMatches(parsed.Reduce(some => some.Contents.First(), () => default), CurrentModule, out var matches))
                    {
                        predicates = matches.Select(m => m.Rhs);
                        return true;
                    }
                    return false;
                }, out var yes) || !yes)
                {
                    No();
                    return;
                }
            }

            if(!explain) {
                predicates = predicates.DistinctBy(p => Predicate.Signature(p.Head));
            }

            var canonicals = predicates
                .Select(r => explain 
                    ? new[] { Predicate.Signature(r.Head), r.DeclaringModule.Explain(), r.Explain() }
                    : new[] { Predicate.Signature(r.Head), r.DeclaringModule.Explain(), r.Documentation })
                .ToArray();
            if (canonicals.Length == 0) {
                No();
                return;
            }
            var cols = explain
                ? new[] { "Predicate", "Module", "Explanation" }
                : new[] { "Predicate", "Module", "Documentation" }
                ;
            WriteTable(cols, canonicals, explain ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan);
        }


        protected virtual void CmdPrintBuiltIns(Group _ITerm)
        {
            var builtins = new List<BuiltIn>();
            if (_ITerm?.Success ?? false) {
                var parsed = new Parsed<ITerm>(_ITerm.Value, Handler, str => throw new ShellException($"'{str}' does not resolve to a term."), Interpreter.GetUserDefinedOperators(CurrentModule).ToArray()).Value;
                if (!parsed.HasValue) {
                    No();
                    return;
                }
                var ITerm = parsed.Reduce(some => some, () => default);
                if (Interpreter.TryGetBuiltIn(ITerm, out var builtin)) {
                    builtins.Add(builtin);
                }
                else {
                    No();
                    return;
                }
            }
            else {
                builtins.AddRange(Interpreter.BuiltIns);
            }

            var canonicals = builtins
                .Select(r => new[] { r.Signature.Explain(), r.Documentation })
                .ToArray();

            if (canonicals.Length == 0) {
                No();
                return;
            }

            WriteTable(new[] { "Built-In", "Documentation" }, canonicals, ConsoleColor.DarkRed);
        }
    }
}
