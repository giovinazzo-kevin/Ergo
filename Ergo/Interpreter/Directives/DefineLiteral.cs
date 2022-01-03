using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System.Linq;

namespace Ergo.Interpreter.Directives
{
    public class DefineLiteral : InterpreterDirective
    {
        public DefineLiteral()
            : base("", new("lit"), Maybe.Some(2), 20)
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            var module = scope.Modules[scope.Module];
            var allLiterals = scope.Modules.Values
                .Where(x => module.Imports.Contents.Contains(x.Name))
                .SelectMany(x => x.Literals)
                .ToLookup(l => l.Key);

            if (!args[0].Matches<string>(out var literalName))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, Types.String, args[0].Explain());
            }
            if (WellKnown.Literals.DefinedLiterals.Any(l => l.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.LiteralClashWithBuiltIn, scope, args[0].Explain());
            }
            if (scope.Modules[scope.Module].Literals.Any(l => l.Key.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.LiteralClash, scope, args[0].Explain());
            }
            if(DefinedCircularly(args[0], args[1]))
            {
                throw new InterpreterException(InterpreterError.LiteralCircularDefinition, scope, args[0].Explain(), args[1].Explain());
            }
            scope = scope.WithModule(scope.Modules[scope.Module]
                .WithLiteral(new(new(literalName), args[1])));
            return true;

            bool DefinedCircularly(ITerm start, ITerm t)
            {
                if (start.Equals(t)) return true;
                if (t is not Atom a) return false;
                foreach (var l in allLiterals[a])
                    if (DefinedCircularly(start, l.Value.Value)) return true;
                return false;
            }
        }
    }
}
