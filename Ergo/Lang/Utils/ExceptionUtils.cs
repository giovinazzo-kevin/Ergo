using Ergo.Interpreter;
using System;

namespace Ergo.Lang.Utils
{
    public static class ExceptionUtils
    {
        public static string GetParserError(Parser.ErrorType error, params object[] args)
        {
            var msg = error switch
            {
                Parser.ErrorType.ExpectedPredicateDelimiterOrTerminator => "Expected predicate delimter (',') or terminator ('.')."
                , Parser.ErrorType.PredicateHasSingletonVariables => "Predicate {0} has singleton variables: {1}. Use them, or replace them with a discard ('_')."
                , Parser.ErrorType.ComplexHasNoArguments => "Complex term has no arguments."
                , Parser.ErrorType.ExpectedArgumentDelimiterOrClosedParens => "Expected argument delimiter ('{0}') or terminator ('{1}')."
                , Parser.ErrorType.ExpectedClauseList => "Expected clause list."
                , Parser.ErrorType.UnterminatedClauseList => "Unterminated clause list."
                , Parser.ErrorType.UnexpectedEndOfFile => "Unexpected end of file."
                , Parser.ErrorType.TermHasIllegalName => "Term has illegal or reserved name: {0}"
                , _ => error.ToString()
            };

            if (args != null && args.Length > 0) {
                msg = String.Format(msg, args);
            }

            return msg;
        }

        public static string GetLexerError(Lexer.ErrorType error, params object[] args)
        {
            var msg = error switch
            {
                _ => error.ToString()
            };

            if (args != null && args.Length > 0) {
                msg = String.Format(msg, args);
            }

            return msg;
        }

        public static string GetInterpreterError(InterpreterError error, params object[] args)
        {
            var msg = error switch
            {
                InterpreterError.UnknownPredicate => String.Format("Predicate not found: {0}", args)
                , InterpreterError.UserPredicateConflictsWithBuiltIn => String.Format("User-defined predicate conflicts with built-in: {0}", args)
                , InterpreterError.ExpectedTermOfTypeAt => String.Format("Expected term of type {0}, found: {1}", args)
                , InterpreterError.UndefinedPredicate => String.Format("Undefined predicate: {0}", args)
                , InterpreterError.ExpectedTermWithArity => String.Format("Expected: {0}/{1}", args)
                , InterpreterError.ModuleRedefinition => String.Format("Declaration of module {1} would shadow existing declaration: {0}", args)
                , InterpreterError.ModuleNameClash => String.Format("Module {0} can't be declared because it would shadow a static module", args)
                , InterpreterError.LiteralClashWithBuiltIn => String.Format("Literal {0} can't be declared because it would shadow a built-in literal", args)
                , InterpreterError.LiteralClash => String.Format("Literal {0} was already declared in this module", args)
                , InterpreterError.OperatorClash => String.Format("Operator {0} can't be declared because it would shadow a built-in operator", args)
                , _ => error.ToString()
            };

            if (args != null && args.Length > 0) {
                msg = String.Format(msg, args);
            }

            return msg;
        }

        public static string GetMessage(Lexer.StreamState state, string error)
        {
            var ctx = state.Context.Replace("\t", "    ");
            var ctxIndicator = new string('~', ctx.Length) + "^";
            var ret = $"at line {state.Line}, col {state.Column}:\r\n\t{error}\r\n\r\n\t{ctx}\r\n\t{ctxIndicator} (here)\r\n";
            if (!String.IsNullOrEmpty(state.Filename)) {
                ret = $"In file '{state.Filename}', " + ret;
            }
            return ret;
        }
    }
}
