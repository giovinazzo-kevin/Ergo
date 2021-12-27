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
            : base("", new("lit"), Maybe.Some(2))
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            var allLiterals = scope.Modules.Values.SelectMany(x => x.Literals)
                .ToLookup(l => l.Key);

            if (!args[0].Matches<string>(out var literalName))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            if (Literals.DefinedLiterals.Any(l => l.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.LiteralClashWithBuiltIn, args[0].Explain());
            }
            if (scope.Modules[scope.CurrentModule].Literals.Any(l => l.Key.Equals(args[0])))
            {
                throw new InterpreterException(InterpreterError.LiteralClash, args[0].Explain());
            }
            if(DefinedCircularly(args[0], args[1]))
            {
                throw new InterpreterException(InterpreterError.LiteralCircularDefinition, args[0].Explain());
            }
            scope = scope.WithModule(scope.Modules[scope.CurrentModule]
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
