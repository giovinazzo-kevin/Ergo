using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Linq;

namespace Ergo.Interpreter.Directives
{
    public class DefineExpansion : InterpreterDirective
    {
        public DefineExpansion()
            : base("", new("expand"), Maybe.Some(2), 20)
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            var module = scope.Modules[scope.Module];
            var allLiterals = scope.Modules.Values
                .Where(x => module.Imports.Contents.Contains(x.Name))
                .SelectMany(x => x.Expansions)
                .ToLookup(l => l.Key);
            if (WellKnown.Literals.DefinedLiterals.Any(l => l.IsGround && l.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.ExpansionClashWithLiteral, scope, args[0].Explain());
            }
            var signature = args[0].GetSignature();
            if (scope.Modules[scope.Module].Expansions.TryGetValue(signature, out var expansions) && expansions.Any(a => a.Head.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.ExpansionClash, scope, args[0].Explain());
            }
            if(CyclicDefinition(args[0], args[1]))
            {
                throw new InterpreterException(InterpreterError.LiteralCyclicDefinition, scope, args[0].Explain(), args[1].Explain());
            }
            scope = scope.WithModule(scope.Modules[scope.Module]
                .WithLiteral(args[0], args[1]));
            return true;

            bool CyclicDefinition(ITerm start, ITerm t)
            {
                if (start.IsGround && start.Equals(t)) return true;
                var sig = t.GetSignature();
                foreach (var l in allLiterals[sig].SelectMany(l => l.Value))
                    if (CyclicDefinition(start, l.Value)) return true;
                return false;
            }
        }
    }
}
