using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Ergo.Interpreter.Directives
{
    public class DefineModule : InterpreterDirective
    {
        public DefineModule()
            : base("", new("module"), Maybe.Some(2), 0)
        {
        }
        public override bool Execute(ErgoInterpreter interpreter, ref InterpreterScope scope, params ITerm[] args)
        {
            if (args[0] is not Atom moduleName)
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.String, args[0].Explain());
            }
            if (!scope.Runtime && scope.CurrentModule != Modules.User)
            {
                throw new InterpreterException(InterpreterError.ModuleRedefinition, scope.CurrentModule.Explain(), moduleName.Explain());
            }
            if (!List.TryUnfold(args[1], out var exports))
            {
                throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.List, args[1].Explain());
            }
            if (scope.Modules.TryGetValue(moduleName, out var module))
            {
                if (!module.Runtime && !interpreter.Flags.HasFlag(InterpreterFlags.AllowStaticModuleRedefinition))
                {
                    throw new InterpreterException(InterpreterError.ModuleNameClash, moduleName.Explain());
                }
                module = module.WithExports(exports.Contents);
            }
            else
            {
                module = new Module(moduleName, runtime: scope.Runtime)
                    .WithImport(Modules.Prologue);
            }
            scope = scope
                .WithModule(module)
                .WithCurrentModule(module.Name);
            foreach (var item in exports.Contents)
            {
                // make sure that 'item' is in the form 'predicate/arity'
                if (!item.Matches(out var match, new { Predicate = default(string), Arity = default(int) }))
                {
                    throw new InterpreterException(InterpreterError.ExpectedTermOfTypeAt, Types.Signature, item.Explain());
                }
            }
            return true;
        }
    }
}
