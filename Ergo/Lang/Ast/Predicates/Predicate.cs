using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ergo.Lang.Extensions;

namespace Ergo.Lang.Ast
{

    [DebuggerDisplay("{ Explain() }")]
    public readonly struct Predicate
    {
        public readonly Atom DeclaringModule;
        public readonly ITerm Head;
        public readonly CommaSequence Body;
        public readonly string Documentation;

        public string Explain()
        {
            if(Body.IsEmpty || Body.Contents.SequenceEqual(new ITerm[] { Literals.True })) {
                return $"{Head.Explain()}.";
            }

            var expl = $"{Head.Explain()} ← {string.Join(", ", Body.Contents.Select(x => x.Explain()))}.";
            if (!String.IsNullOrWhiteSpace(Documentation)) {
                expl = $"{String.Join("\r\n", Documentation.Replace("\r", "").Split('\n').AsEnumerable().Select(r => "%: " + r))}\r\n" + expl;
            }

            return expl;
        }

        public static int Arity(ITerm head) => head.Reduce(a => 0, v => 0, c => c.Arity);

        public Predicate(string desc, Atom module, ITerm head, CommaSequence body)
        {
            Documentation = desc;
            DeclaringModule = module;
            Head = head;
            Body = body;
        }

        public Predicate Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
        {
            vars ??= new Dictionary<string, Variable>();
            return new Predicate(
                Documentation
                , DeclaringModule
                , Head.Instantiate(ctx, vars)
                , (CommaSequence)Body.Instantiate(ctx, vars)
            );
        }

        public static Predicate Substitute(Predicate k, IEnumerable<Substitution> s)
        {
            return new Predicate(k.Documentation, k.DeclaringModule, k.Head.Substitute(s), (CommaSequence)k.Body.Substitute(s));
        }

        public Predicate WithModuleName(Atom module) => new(Documentation, module, Head, Body);

        public Predicate Qualified()
        {
            if (Head.IsQualified || !Head.TryQualify(DeclaringModule, out var head))
                return this;
            return new(Documentation, DeclaringModule, head, Body);
        }

        public static bool TryUnify(ITerm head, Predicate predicate, out IEnumerable<Substitution> substitutions)
        {
            var S = new List<Substitution>();
            if(predicate.Head.TryGetQualification(out _, out var qv)
            && new Substitution(head, qv).TryUnify(out var subs))
            {
                S.AddRange(subs);
                substitutions = S;
                return true;
            }
            if (new Substitution(head, predicate.Head).TryUnify(out subs)) {
                S.AddRange(subs);
                substitutions = S;
                return true;
            }
            substitutions = default;
            return false;
        }
    }
}