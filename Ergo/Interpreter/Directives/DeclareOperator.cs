using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System;
using System.Linq;

namespace Ergo.Interpreter.Directives
{

    public class DeclareOperator : InterpreterDirective
    {
        enum OperatorType
        {
            fx, xf, xfx, xfy, yfx
        }

        public DeclareOperator()
            : base("", new("op"), Maybe.Some(3), 10)
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (!args[0].Matches<int>(out var precedence))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, Types.Integer, args[0].Explain());
            }
            if (!args[1].Matches<OperatorType>(out var type))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, "OperatorType", args[1].Explain());
            }
            if (!args[2].Matches<string[]>(out var synonyms))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, scope, Types.List, args[2].Explain());
            }
            var (affix, assoc) = type switch
            {
                OperatorType.fx => (OperatorAffix.Prefix, OperatorAssociativity.Right),
                OperatorType.xf => (OperatorAffix.Postfix, OperatorAssociativity.Left),
                OperatorType.xfx => (OperatorAffix.Infix, OperatorAssociativity.None),
                OperatorType.xfy => (OperatorAffix.Infix, OperatorAssociativity.Right),
                OperatorType.yfx => (OperatorAffix.Infix, OperatorAssociativity.Left),
                _ => throw new NotSupportedException()
            };
            var existingOperators = scope.Operators.Value;
            foreach (var op in existingOperators.Where(x => x.Affix == affix))
            {
                var intersectingSynonyms = op.Synonyms.Select(x => x.Explain()).Intersect(synonyms);
                // Operators can be re-defined, but only if the new definition covers all synonyms.
                if (intersectingSynonyms.Any())
                {
                    if(intersectingSynonyms.Count() != op.Synonyms.Length)
                    {
                        throw new InterpreterException(InterpreterError.OperatorClash, scope, args[2].Explain());
                    }
                }
            }
            var synonymAtoms = synonyms.Select(x => new Atom(x)).ToArray();
            scope = scope.WithModule(scope.Modules[scope.Module]
                .WithOperator(new(scope.Module, affix, assoc, precedence, synonymAtoms)));
            return true;
        }
    }
}
