using Ergo.Lang.Compiler;
using Ergo.Modules;

namespace Ergo.Lang.Utils;

public static class ExceptionUtils
{
    public static string GetVMError(ErgoVM.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            ErgoVM.ErrorType.Custom => string.Format("{0}", args),
            ErgoVM.ErrorType.MatchFailed => string.Format("Could not resolve predicate: {0}, and it is not marked as dynamic.", args),
            ErgoVM.ErrorType.CannotRetractImportedPredicate => string.Format("Can't retract {0} from module {1} because it was declared in module {2}", args),
            ErgoVM.ErrorType.CannotRetractStaticPredicate => string.Format("Can't retract {0} because it is not a dynamic predicate", args),
            ErgoVM.ErrorType.UndefinedPredicate => string.Format("Undefined predicate: {0}", args),
            ErgoVM.ErrorType.TermNotSufficientlyInstantiated => string.Format("Term not sufficiently instantiated: {0}", args),
            ErgoVM.ErrorType.KeyNotFound => string.Format("Key {1} not found in: {0}", args),
            ErgoVM.ErrorType.ExpectedTermOfTypeAt => string.Format("Expected term of type {0}, found: {1}", args),
            ErgoVM.ErrorType.StackOverflow => string.Format("Stack overflow", args),
            _ => error.ToString()
        };
        return msg;
    }
    public static string GetCompilerError(ErgoCompiler.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            ErgoCompiler.ErrorType.UnresolvedPredicate => "Could not resolve predicate: {0}, and it is not marked as dynamic.",
            ErgoCompiler.ErrorType.NotEnoughMemoryToEmitNextInstruction => "The compiler attempted to allocate {1} bytes, but only {0} were available.",
            _ => error.ToString()
        };

        if (args != null && args.Length > 0)
        {
            msg = string.Format(msg, args);
        }

        return msg;
    }

    public static string GetParserError(LegacyErgoParser.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            LegacyErgoParser.ErrorType.ExpectedPredicateDelimiterOrTerminator => "Expected predicate delimter (',') or terminator ('.').",
            LegacyErgoParser.ErrorType.PredicateHasSingletonVariables => "Predicate {0} has singleton variables: {1}. Use them, or replace them with a discard ('_').",
            LegacyErgoParser.ErrorType.ComplexHasNoArguments => "Complex term has no arguments.",
            LegacyErgoParser.ErrorType.ExpectedArgumentDelimiterOrClosedParens => "Expected argument delimiter ('{0}') or terminator ('{1}').",
            LegacyErgoParser.ErrorType.ExpectedClauseList => "Expected clause list.",
            LegacyErgoParser.ErrorType.UnterminatedClauseList => "Unterminated clause list.",
            LegacyErgoParser.ErrorType.UnexpectedEndOfFile => "Unexpected end of file.",
            LegacyErgoParser.ErrorType.TermHasIllegalName => "Term has illegal or reserved name: {0}",
            LegacyErgoParser.ErrorType.KeyExpected => "Key expected; found: {0}",
            _ => error.ToString()
        };

        if (args != null && args.Length > 0)
        {
            msg = string.Format(msg, args);
        }

        return msg;
    }

    public static string GetLexerError(ErgoLexer.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            _ => error.ToString()
        };

        if (args != null && args.Length > 0)
        {
            msg = string.Format(msg, args);
        }

        return msg;
    }

    public static string GetInterpreterError(ErgoInterpreter.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            ErgoInterpreter.ErrorType.UndefinedDirective => string.Format("Undefined directive: {0}", args),
            ErgoInterpreter.ErrorType.ModuleRedefinition => string.Format("Declaration of module {1} would shadow existing declaration: {0}", args),
            ErgoInterpreter.ErrorType.ModuleNameClash => string.Format("Module {0} can't be declared because it would shadow a static module", args),
            ErgoInterpreter.ErrorType.CyclicLiteralDefinition => string.Format("Literal {0} can't be declared as {1} because the definition would be cyclic", args),
            ErgoInterpreter.ErrorType.OperatorClash => string.Format("Operator {0} can't be declared because it would shadow a built-in operator", args),
            ErgoInterpreter.ErrorType.ExpectedTermOfTypeAt => string.Format("Expected term of type {0}, found: {1}", args),
            ErgoInterpreter.ErrorType.ModuleAlreadyImported => string.Format("Module already imported: {0}", args),
            ErgoInterpreter.ErrorType.ExpansionClashWithLiteral => string.Format("Literal {0} can't be declared because it would shadow a built-in literal", args),
            ErgoInterpreter.ErrorType.ExpansionClash => string.Format("Literal {0} was already declared in this module", args),
            ErgoInterpreter.ErrorType.ExpansionLambdaShouldHaveOneVariable => string.Format("Expansion lambda must only capture one variable: {0}", args),
            ErgoInterpreter.ErrorType.ExpansionBodyMustReferenceHeadVariables => string.Format("Expansion body must reference all variables found in its head: {0}", args),
            ErgoInterpreter.ErrorType.ExpansionBodyMustReferenceLambdaVariable => string.Format("Expansion body must reference captured lambda variable: {0}", args),
            _ => error.ToString()
        };

        if (args != null && args.Length > 0)
        {
            try
            {
                msg = string.Format(msg, args);
            }
            catch { }
        }

        return msg;
    }

    public static string GetMessage(ErgoLexer.StreamState state, string error)
    {
        var ctx = state.Context?.Replace("\t", "    ") ?? String.Empty;
        var ctxIndicator = new string('~', ctx.Length) + "^";
        var ret = $"at line {state.Line}, col {state.Column}:\r\n\t{error}\r\n\r\n\t{ctx}\r\n\t{ctxIndicator} (here)\r\n";
        if (!string.IsNullOrEmpty(state.Filename))
        {
            ret = $"In file '{state.Filename}', " + ret;
        }

        return ret;
    }
}
