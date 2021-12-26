using Ergo.Interpreter;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions;
using Ergo.Lang.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{

    public partial class Parser : IDisposable
    {
        private readonly Lexer _lexer;
        private readonly InstantiationContext _discardContext;
        private readonly IEnumerable<Operator> _userDefinedOperators;

        public Parser(Lexer lexer, IEnumerable<Operator> userOperators)
        {
            _lexer = lexer;
            _discardContext = new(String.Empty);
            _userDefinedOperators = userOperators;
        }

        public bool TryGetOperatorsFromFunctor(Atom functor, out IEnumerable<Operator> ops)
        {
            ops = default;
            var match = Operators.DefinedOperators.Concat(_userDefinedOperators)
                .Where(op => op.Synonyms.Any(s => functor.Equals(s)));
            if (!match.Any())
            {
                return false;
            }
            ops = match;
            return true;
        }

        public bool TryParseAtom(out Atom atom)
        {
            atom = default;
            var pos = _lexer.State;
            if (Expect(Lexer.TokenType.String, out string str)) {
                if ("true".Equals(str)) {
                    atom = new Atom(true);
                }
                else if ("false".Equals(str)) {
                    atom = new Atom(false);
                }
                else {
                    atom = new Atom(str);
                }
                return true;
            }
            else if (Expect(Lexer.TokenType.Number, out double dec)) {
                atom = new Atom(dec);
                return true;
            }
            else if (Expect(Lexer.TokenType.Keyword, kw => kw == "true" || kw == "false", out string kw)) {
                atom = new Atom(kw == "true");
                return true;
            }
            if (Expect(Lexer.TokenType.Term, out string ITerm)) {
                if (!IsAtomIdentifier(ITerm)) {
                    return Fail(pos);
                }
                atom = new Atom(ITerm);
                return true;
            }
            return Fail(pos);

        }
        public bool TryParseVariable(out Variable var)
        {
            var = default;
            var pos = _lexer.State;
            if (Expect(Lexer.TokenType.Term, out string term)) {
                if (!IsVariableIdentifier(term)) {
                    return Fail(pos);
                }
                if (term.StartsWith("__K")) {
                    Throw(pos, ErrorType.TermHasIllegalName, var.Name);
                }
                if(term.Equals(Literals.Discard.Explain())) {
                    term = $"_{_discardContext.VarPrefix}{_discardContext.GetFreeVariableId()}";
                }
                var = new Variable(term);
                return true;
            }
            return Fail(pos);
        }
        public bool TryParseComplex(out Complex cplx)
        {
            cplx = default;
            var pos = _lexer.State;
            if (!Expect(Lexer.TokenType.Term, out string functor)
                && !Expect(Lexer.TokenType.Operator, out functor) 
                && !Expect(Lexer.TokenType.String, out functor)) {
                return Fail(pos);
            }
            if (!TryParseSequence(
                  CommaSequence.CanonicalFunctor
                , CommaSequence.EmptyLiteral
                , () => TryParseTermOrExpression(out var t, out _) ? (true, t) : (false, default)
                , "(", ",", ")"
                , true
                , out var inner
            )) {
                return Fail(pos);
            }
            cplx = CommaSequence.TryUnfold(inner.Root, out _)
                ? new Complex(new Atom(functor), inner.Contents.ToArray())
                : new Complex(new Atom(functor), inner.Root);
            return true;
        }
        public bool TryParseTermOrExpression(out ITerm ITerm, out bool parenthesized)
        {
            var pos = _lexer.State;
            parenthesized = false;
            if (TryParseExpression(out var expr)) {
                ITerm = expr.Complex;
                return true;
            }
            if (TryParseTerm(out ITerm, out parenthesized)) {
                return true;
            }
            ITerm = default;
            return Fail(pos);
        }
        public bool TryParseTerm(out ITerm ITerm, out bool parenthesized)
        {
            ITerm = default; parenthesized = false;
            var pos = _lexer.State;
            if(Parenthesized(() => TryParseExpression(out var expr) ? (true, expr) : (false, default), out var expr)) {
                ITerm = expr.Complex;
                parenthesized = true;
                return true;
            }
            if (Parenthesized(() => TryParseTerm(out var eval, out _) ? (true, eval) : (false, default), out var eval)) {
                ITerm = eval;
                parenthesized = true;
                return true;
            }
            if(TryParseTermInner(out var t)) {
                ITerm = t;
                return true;
            }
            return Fail(pos);

            bool TryParseTermInner(out ITerm ITerm)
            {
                if (TryParseList(out var list)) {
                    ITerm = list.Root;
                    return true;
                }
                if (TryParseVariable(out var var)) {
                    ITerm = var;
                    return true;
                }
                if (TryParseComplex(out var cplx)) {
                    ITerm = cplx;
                    return true;
                }
                if (TryParseAtom(out var atom)) {
                    ITerm = atom;
                    return true;
                }
                ITerm = default;
                return false;
            }
        }

        public bool TryParseList(out List seq)
        {
            seq = default;
            var pos = _lexer.State;
            if (TryParseSequence(
                  List.CanonicalFunctor
                , List.EmptyLiteral
                , () => TryParseTermOrExpression(out var t, out _) ? (true, t) : (false, default)
                , "[", ",", "]"
                , true
                , out var full
            )) {
                if (full.Contents.Length == 1 && full.Contents[0] is Complex cplx
                    && Operators.BinaryList.Synonyms.Contains(cplx.Functor)) {
                    var arguments = ImmutableArray.Create<ITerm>().Add(cplx.Arguments[0]);
                    if(CommaSequence.TryUnfold(cplx.Arguments[0], out var comma)) {
                        arguments = comma.Contents;
                    }
                    seq = new List(arguments, Maybe.Some(cplx.Arguments[1]));
                    return true;
                }
                seq = new List(full.Contents);
                return true;
            }
            return Fail(pos);
        }

        private bool ExpectOperator(Func<Operator, bool> match, out Operator op)
        {
            op = default;
            if(Expect(Lexer.TokenType.Operator, str => TryGetOperatorsFromFunctor(new Atom(str), out var _op) && _op.Any(match)
            , out string str)
                && TryGetOperatorsFromFunctor(new Atom(str), out var ops)) {
                op = ops.Single(match);
                return true;
            }
            return false;
        }

        public Expression BuildExpression(Operator op, ITerm lhs, Maybe<ITerm> maybeRhs = default, bool lhsParenthesized = false)
        {
            return maybeRhs.Reduce(
                rhs => Associate(lhs, rhs),
                () => new Expression(op, lhs, Maybe<ITerm>.None)
            );

            Expression Associate(ITerm lhs, ITerm rhs)
            {
                // When the lhs represents an expression with the same precedence as this (and thus associativity, by design)
                // and right associativity, we have to swap the arguments around until they look right.
                if (!lhsParenthesized
                && TryConvertExpression(lhs, out var lhsExpr)
                && lhsExpr.Operator.Affix == OperatorAffix.Infix
                && lhsExpr.Operator.Associativity == OperatorAssociativity.Right
                && lhsExpr.Operator.Precedence == op.Precedence)
                {
                    // a, b, c -> ','(','(','(a, b), c)) -> ','(a, ','(b, ','(c))
                    var lhsRhs = lhsExpr.Right.Reduce(x => x, () => throw new InvalidOperationException());
                    var newRhs = BuildExpression(lhsExpr.Operator, lhsRhs, Maybe.Some(rhs));
                    return BuildExpression(op, lhsExpr.Left, Maybe.Some<ITerm>(newRhs.Complex));
                }
                return new Expression(op, lhs, Maybe.Some(rhs));
            }

            bool TryConvertExpression(ITerm t, out Expression expr)
            {
                expr = default;
                if (t is not Complex cplx)
                    return false;
                if (!TryGetOperatorsFromFunctor(cplx.Functor, out var ops))
                    return false;
                var op = ops.Single(op => cplx.Arity switch {
                    1 => op.Affix != OperatorAffix.Infix
                    ,
                    _ => op.Affix == OperatorAffix.Infix
                });
                if (cplx.Arguments.Length == 1)
                {
                    expr = BuildExpression(op, cplx.Arguments[0]);
                    return true;
                }
                if (cplx.Arguments.Length == 2)
                {
                    expr = BuildExpression(op, cplx.Arguments[0], Maybe.Some(cplx.Arguments[1]));
                    return true;
                }
                return false;
            }
        }

        public bool TryParsePrefixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (ExpectOperator(op => op.Affix == OperatorAffix.Prefix, out var op)
            && TryParseTerm(out var arg, out _)) {
                expr = BuildExpression(op, arg);
                return true;
            }
            return Fail(pos);
        }

        public bool TryParsePostfixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (TryParseTerm(out var arg, out _) 
            && ExpectOperator(op => op.Affix == OperatorAffix.Postfix, out var op)) {
                expr = BuildExpression(op, arg);
                return true;
            }
            return Fail(pos);
        }

        public bool TryParseExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (TryParsePrimary(out var lhs, out var lhsParenthesized)) {
                if(WithMinPrecedence(lhs, lhsParenthesized, 0, out expr)) {
                    return true;
                }
                // Special case for unary expressions
                if(lhs is not Complex cplx
                    || cplx.Arguments.Length > 1
                    || !TryGetOperatorsFromFunctor(cplx.Functor, out var ops)) {
                    return Fail(pos);
                }
                var op = ops.Single(op => op.Affix != OperatorAffix.Infix);
                expr = BuildExpression(op, cplx.Arguments[0], Maybe<ITerm>.None);
                return true;
            }
            return Fail(pos);

            bool WithMinPrecedence(ITerm lhs, bool lhsParenthesized, int minPrecedence, out Expression expr)
            {
                expr = default; var pos = _lexer.State;
                if (!TryPeekNextOperator(out var lookahead)) { 
                    return Fail(pos);
                }
                if(lookahead.Affix != OperatorAffix.Infix || lookahead.Precedence < minPrecedence) {
                    return Fail(pos);
                }
                while (lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence >= minPrecedence) {
                    _lexer.TryReadNextToken(out _);
                    var op = lookahead;
                    if(!TryParsePrimary(out var rhs, out var rhsParenthesized)) {
                        return Fail(pos);
                    }
                    if (!TryPeekNextOperator(out lookahead)) {
                        expr = BuildExpression(op, lhs, Maybe.Some(rhs), lhsParenthesized);
                        break;
                    }
                    while(lookahead.Affix == OperatorAffix.Infix && lookahead.Precedence > op.Precedence
                        || lookahead.Associativity == OperatorAssociativity.Right && lookahead.Precedence == op.Precedence) {
                        if(!WithMinPrecedence(rhs, rhsParenthesized, op.Precedence + 1, out var newRhs)) {
                            break;
                        }
                        rhs = newRhs.Complex;
                        if (!TryPeekNextOperator(out lookahead)) {
                            break;
                        }
                    }
                    lhs = (expr = BuildExpression(op, lhs, Maybe.Some(rhs))).Complex;
                }
                return true;
            }

            bool TryParsePrimary(out ITerm ITerm, out bool parenthesized)
            {
                ITerm = default; var pos = _lexer.State;
                parenthesized = false;
                if (TryParsePrefixExpression(out var prefix)) {
                    ITerm = prefix.Complex;
                    return true;
                }
                if (TryParsePostfixExpression(out var postfix)) {
                    ITerm = postfix.Complex;
                    return true;
                }
                if (TryParseTerm(out ITerm, out parenthesized)) {
                    return true;
                }
                return Fail(pos);
            }

            bool TryPeekNextOperator(out Operator op)
            {
                op = default;
                if(_lexer.TryPeekNextToken(out var lookahead)
                && TryGetOperatorsFromFunctor(new Atom(lookahead.Value), out var ops)) {
                    op = ops.Where(op => op.Affix == OperatorAffix.Infix).Single();
                    return true;
                }
                return false;
            }
        }


        public bool TryParseDirective(out Directive directive)
        {
            directive = default;
            var pos = _lexer.State;
            if (!TryParseExpression(out var op))
            {
                return Fail(pos);
            }
            if (!Operators.UnaryHorn.Equals(op.Operator))
            {
                return Fail(pos);
            }
            if (!Expect(Lexer.TokenType.Punctuation, p => p.Equals("."), out string _))
            {
                Throw(pos, ErrorType.UnterminatedClauseList);
            }
            var lhs = op.Left;
            if (CommaSequence.TryUnfold(lhs, out var expr))
            {
                lhs = expr.Root;
            }
            directive = new(lhs);
            return true;
        }

        public bool TryParsePredicate(out Predicate predicate)
        {
            predicate = default;
            var pos = _lexer.State;
            if (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string desc)) {
                desc = desc[1..].TrimStart();
                while (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string newDesc)) {
                    if (!String.IsNullOrEmpty(newDesc)) {
                        desc += "\n" + newDesc[1..].TrimStart();
                    }
                }
            }
            desc ??= " ";
            if (_lexer.Eof)
                return Fail(pos);
            if (!TryParseExpression(out var op)) {
                if (TryParseTerm(out var head, out _) && Expect(Lexer.TokenType.Punctuation, p => p.Equals("."), out string _)) {
                    return MakePredicate(pos, desc, head, new(ImmutableArray.Create<ITerm>().Add(new Atom(true))), out predicate);
                }
                Throw(pos, ErrorType.ExpectedClauseList);
            }
            if(!Operators.BinaryHorn.Equals(op.Operator)) {
                op = new Expression(Operators.BinaryHorn, op.Complex, Maybe.Some(Literals.True));
            }
            if (!Expect(Lexer.TokenType.Punctuation, p => p.Equals("."), out string _)) {
                Throw(pos, ErrorType.UnterminatedClauseList);
            }
            var rhs = op.Right.Reduce(s => s, () => throw new NotImplementedException());
            if (!CommaSequence.TryUnfold(rhs, out var expr)) {
                expr = new(ImmutableArray.Create<ITerm>().Add(rhs));
            }
            return MakePredicate(pos, desc, op.Left, expr, out predicate);

            bool MakePredicate(Lexer.StreamState pos, string desc, ITerm head, CommaSequence body, out Predicate c)
            {
                var headVars = head.Variables
                    .Where(v => !v.Equals(Literals.Discard));
                var bodyVars = body.Contents.SelectMany(t => t.Variables)
                    .Distinct();
                var singletons = headVars.Where(v => !v.Ignored && !bodyVars.Contains(v) && headVars.Count(x => x.Name == v.Name) == 1)
                    .Select(v => v.Explain());
                if (singletons.Any()) {
                    Throw(pos, ErrorType.PredicateHasSingletonVariables, Predicate.Signature(head), String.Join(", ", singletons));
                }
                c = new Predicate(
                    desc
                    , Modules.User
                    , head
                    , body
                );
                return true;
            }
        }

        public bool TryParseProgram(out ErgoProgram program)
        {
            var directives = new List<Directive>();
            var predicates = new List<Predicate>();
            while (TryParseDirective(out var directive))
            {
                directives.Add(directive);
            }
            while (TryParsePredicate(out var predicate))
            {
                predicates.Add(predicate);
            }
            program = new ErgoProgram(directives.ToArray(), predicates.ToArray());
            return true;
        }

        public bool TryParseProgramDirectives(out ErgoProgram program)
        {
            var directives = new List<Directive>();
            while (TryParseDirective(out var directive))
            {
                directives.Add(directive);
            }
            program = new ErgoProgram(directives.ToArray(), Array.Empty<Predicate>());
            return true;
        }

        public void Dispose()
        {
            _lexer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
