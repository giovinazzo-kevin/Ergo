﻿using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System;

namespace Ergo.Interpreter.Directives
{
    public class DefineOperator : InterpreterDirective
    {
        enum OperatorType
        {
            fx, xf, xfx, xfy, yfx
        }

        public DefineOperator()
            : base("", new("op"), Maybe.Some(3))
        {
        }

        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (!args[0].Matches<int>(out var precedence))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Integer, args[0].Explain());
            }
            if (!args[0].Matches<OperatorType>(out var type))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            if (!args[0].Matches<string>(out var name))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
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

            scope = scope.WithModule(scope.Modules[scope.CurrentModule]
                .WithOperator(new(affix, assoc, precedence, name)));
            return true;
        }
    }
}