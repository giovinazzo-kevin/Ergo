using Ergo.Interpreter;
using Ergo.Lang.Compiler;
using Ergo.Solver;

namespace Ergo.Lang.Utils;

public static class ExceptionUtils
{
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

    public static string GetParserError(ErgoParser.ErrorType error, params object[] args)
    {
        var msg = error switch
        {
            ErgoParser.ErrorType.ExpectedPredicateDelimiterOrTerminator => "Expected predicate delimter (',') or terminator ('.').",
            ErgoParser.ErrorType.PredicateHasSingletonVariables => "Predicate {0} has singleton variables: {1}. Use them, or replace them with a discard ('_').",
            ErgoParser.ErrorType.ComplexHasNoArguments => "Complex term has no arguments.",
            ErgoParser.ErrorType.ExpectedArgumentDelimiterOrClosedParens => "Expected argument delimiter ('{0}') or terminator ('{1}').",
            ErgoParser.ErrorType.ExpectedClauseList => "Expected clause list.",
            ErgoParser.ErrorType.UnterminatedClauseList => "Unterminated clause list.",
            ErgoParser.ErrorType.UnexpectedEndOfFile => "Unexpected end of file.",
            ErgoParser.ErrorType.TermHasIllegalName => "Term has illegal or reserved name: {0}",
            ErgoParser.ErrorType.KeyExpected => "Key expected; found: {0}",
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

    public static string GetInterpreterError(InterpreterError error, InterpreterScope scope, params object[] args)
    {
        var msg = error switch
        {
            InterpreterError.UndefinedDirective => string.Format("Undefined directive: {0}", args),
            InterpreterError.ModuleRedefinition => string.Format("Declaration of module {1} would shadow existing declaration: {0}", args),
            InterpreterError.ModuleNameClash => string.Format("Module {0} can't be declared because it would shadow a static module", args),
            InterpreterError.CyclicLiteralDefinition => string.Format("Literal {0} can't be declared as {1} because the definition would be cyclic", args),
            InterpreterError.OperatorClash => string.Format("Operator {0} can't be declared because it would shadow a built-in operator", args),
            InterpreterError.ExpectedTermOfTypeAt => string.Format("Expected term of type {0}, found: {1}", args),
            InterpreterError.ModuleAlreadyImported => string.Format("Module already imported: {0}", args),
            InterpreterError.ExpansionClashWithLiteral => string.Format("Literal {0} can't be declared because it would shadow a built-in literal", args),
            InterpreterError.ExpansionClash => string.Format("Literal {0} was already declared in this module", args),
            InterpreterError.ExpansionLambdaShouldHaveOneVariable => string.Format("Expansion lambda must only capture one variable: {0}", args),
            InterpreterError.ExpansionBodyMustReferenceHeadVariables => string.Format("Expansion body must reference all variables found in its head: {0}", args),
            InterpreterError.ExpansionBodyMustReferenceLambdaVariable => string.Format("Expansion body must reference captured lambda variable: {0}", args),
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

    public static string GetSolverError(SolverError error, SolverScope scope, params object[] args)
    {
        var msg = error switch
        {
            SolverError.CannotRetractImportedPredicate => string.Format("Can't retract {0} from module {1} because it was declared in module {2}", args),
            SolverError.CannotRetractStaticPredicate => string.Format("Can't retract {0} because it is not a dynamic predicate", args),
            SolverError.UndefinedPredicate => string.Format("Undefined predicate: {0}", args),
            SolverError.TermNotSufficientlyInstantiated => string.Format("Term not sufficiently instantiated: {0}", args),
            SolverError.KeyNotFound => string.Format("Key {1} not found in: {0}", args),
            SolverError.ExpectedTermOfTypeAt => string.Format("Expected term of type {0}, found: {1}", args),
            SolverError.StackOverflow => string.Format("Stack overflow", args),
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

        var expl = scope.Explain();
        if (!string.IsNullOrWhiteSpace(expl))
        {
            msg = $"{msg}\r\n\r\nIn:\r\n{expl}";
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
