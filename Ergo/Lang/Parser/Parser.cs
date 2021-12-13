using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{

    public partial class Parser : IDisposable
    {
        private readonly Lexer _lexer;
        private readonly Term.InstantiationContext _discardContext;
        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _discardContext = new(String.Empty);
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
            if (Expect(Lexer.TokenType.Term, out string term)) {
                if (!IsAtomIdentifier(term)) {
                    return Fail(pos);
                }
                atom = new Atom(term);
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
                if(term.Equals(Term.Explain(Literals.Discard))) {
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
            if (!Expect(Lexer.TokenType.Term, out string functor) && !Expect(Lexer.TokenType.Operator, out functor) && !Expect(Lexer.TokenType.String, out functor)) {
                return Fail(pos);
            }
            if (!TryParseSequence(
                  CommaExpression.Functor
                , CommaExpression.EmptyLiteral
                , () => TryParseTermOrExpression(out var t, out _) ? (true, t) : (false, default)
                , "(", ",", ")"
                , true
                , out var inner
            )) {
                return Fail(pos);
            }
            cplx = CommaExpression.IsCommaExpression(inner)
                ? new Complex(new Atom(functor), inner.Contents)
                : new Complex(new Atom(functor), inner.Root);
            return true;
        }
        public bool TryParseTermOrExpression(out Term term, out bool parenthesized)
        {
            var pos = _lexer.State;
            parenthesized = false;
            if (TryParseExpression(out var expr)) {
                term = expr.Complex;
                return true;
            }
            if (TryParseTerm(out term, out parenthesized)) {
                return true;
            }
            term = default;
            return Fail(pos);
        }
        public bool TryParseTerm(out Term term, out bool parenthesized)
        {
            term = default; parenthesized = false;
            var pos = _lexer.State;
            if(Parenthesized(() => TryParseExpression(out var expr) ? (true, expr) : (false, default), out var expr)) {
                term = expr.Complex;
                parenthesized = true;
                return true;
            }
            if (Parenthesized(() => TryParseTerm(out var eval, out _) ? (true, eval) : (false, default), out var eval)) {
                term = eval;
                parenthesized = true;
                return true;
            }
            if(TryParseTermInner(out var t)) {
                term = t;
                return true;
            }
            return Fail(pos);

            bool TryParseTermInner(out Term term)
            {
                if (TryParseList(out var list)) {
                    term = list.Root;
                    return true;
                }
                if (TryParseVariable(out var var)) {
                    term = var;
                    return true;
                }
                if (TryParseComplex(out var cplx)) {
                    term = cplx;
                    return true;
                }
                if (TryParseAtom(out var atom)) {
                    term = atom;
                    return true;
                }
                term = default;
                return false;
            }
        }

        public bool TryParseList(out List seq)
        {
            seq = default;
            var pos = _lexer.State;
            if (TryParseSequence(
                  List.Functor
                , List.EmptyLiteral
                , () => TryParseTermOrExpression(out var t, out _) ? (true, t) : (false, default)
                , "[", ",", "]"
                , true
                , out var full
            )) {
                if (full.Contents.Length == 1 && full.Contents[0].Type == TermType.Complex && ((Complex)full.Contents[0]) is var cplx
                    && Operators.BinaryList.Synonyms.Contains(cplx.Functor)) {
                    var arguments = new[] { cplx.Arguments[0] };
                    if(CommaExpression.TryUnfold(cplx.Arguments[0], out var comma)) {
                        arguments = comma.Sequence.Contents;
                    }
                    seq = new List(new Sequence(full.Functor, List.EmptyLiteral, arguments), cplx.Arguments[1]);
                    return true;
                }
                seq = new List(full, List.EmptyLiteral);
                return true;
            }
            return Fail(pos);
        }

        private bool ExpectOperator(Func<Operator, bool> match, out Operator op)
        {
            op = default;
            if(Expect(Lexer.TokenType.Operator, str => Operator.TryGetOperatorsFromFunctor(new Atom(str), out var _op) && _op.Any(match)
            , out string str)
                && Operator.TryGetOperatorsFromFunctor(new Atom(str), out var ops)) {
                op = ops.Single(match);
                return true;
            }
            return false;
        }

        public bool TryParsePrefixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (ExpectOperator(op => op.Affix == Operator.AffixType.Prefix, out var op)
            && TryParseTerm(out var arg, out _)) {
                expr = op.BuildExpression(arg);
                return true;
            }
            return Fail(pos);
        }

        public bool TryParsePostfixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (TryParseTerm(out var arg, out _) 
            && ExpectOperator(op => op.Affix == Operator.AffixType.Postfix, out var op)) {
                expr = op.BuildExpression(arg);
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
                if(lhs.Type != TermType.Complex || !((Complex)lhs is var cplx)
                    || cplx.Arguments.Length > 1
                    || !Operator.TryGetOperatorsFromFunctor(cplx.Functor, out var ops)) {
                    return Fail(pos);
                }
                var op = ops.Single(op => op.Affix != Operator.AffixType.Infix);
                expr = op.BuildExpression(cplx.Arguments[0], Maybe<Term>.None);
                return true;
            }
            return Fail(pos);

            bool WithMinPrecedence(Term lhs, bool lhsParenthesized, int minPrecedence, out Expression expr)
            {
                expr = default; var pos = _lexer.State;
                if (!TryPeekNextOperator(out var lookahead)) { 
                    return Fail(pos);
                }
                if(lookahead.Affix != Operator.AffixType.Infix || lookahead.Precedence < minPrecedence) {
                    return Fail(pos);
                }
                while (lookahead.Affix == Operator.AffixType.Infix && lookahead.Precedence >= minPrecedence) {
                    _lexer.TryReadNextToken(out _);
                    var op = lookahead;
                    if(!TryParsePrimary(out var rhs, out var rhsParenthesized)) {
                        return Fail(pos);
                    }
                    if (!TryPeekNextOperator(out lookahead)) {
                        expr = op.BuildExpression(lhs, Maybe.Some(rhs), lhsParenthesized);
                        break;
                    }
                    while(lookahead.Affix == Operator.AffixType.Infix && lookahead.Precedence > op.Precedence
                        || lookahead.Associativity == Operator.AssociativityType.Right && lookahead.Precedence == op.Precedence) {
                        if(!WithMinPrecedence(rhs, rhsParenthesized, op.Precedence + 1, out var newRhs)) {
                            break;
                        }
                        rhs = newRhs.Complex;
                        if (!TryPeekNextOperator(out lookahead)) {
                            break;
                        }
                    }
                    lhs = (expr = op.BuildExpression(lhs, Maybe.Some(rhs))).Complex;
                }
                return true;
            }

            bool TryParsePrimary(out Term term, out bool parenthesized)
            {
                term = default; var pos = _lexer.State;
                parenthesized = false;
                if (TryParsePrefixExpression(out var prefix)) {
                    term = prefix.Complex;
                    return true;
                }
                if (TryParsePostfixExpression(out var postfix)) {
                    term = postfix.Complex;
                    return true;
                }
                if (TryParseTerm(out term, out parenthesized)) {
                    return true;
                }
                return Fail(pos);
            }

            bool TryPeekNextOperator(out Operator op)
            {
                op = default;
                if(_lexer.TryPeekNextToken(out var lookahead)
                && Operator.TryGetOperatorsFromFunctor(new Atom(lookahead.Value), out var ops)) {
                    op = ops.Where(op => op.Affix == Operator.AffixType.Infix).Single();
                    return true;
                }
                return false;
            }
        }


        public bool TryParseDirective(out Directive directive)
        {
            directive = default;
            var pos = _lexer.State;
            if (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string desc))
            {
                desc = desc[1..].TrimStart();
                while (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string newDesc))
                {
                    if (!String.IsNullOrEmpty(newDesc))
                    {
                        desc += "\n" + newDesc[1..].TrimStart();
                    }
                }
            }
            desc ??= " ";
            if (_lexer.Eof)
                return Fail(pos);
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
            if (CommaExpression.TryUnfold(lhs, out var expr))
            {
                lhs = expr.Sequence.Root;
            }
            return MakeDirective(pos, desc, lhs, out directive);

            bool MakeDirective(Lexer.StreamState pos, string desc, Term body, out Directive d)
            {
                d = new Directive(desc, body);
                return true;
            }
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
                    return MakePredicate(pos, desc, head, CommaExpression.Build(new Atom(true)), out predicate);
                }
                Throw(pos, ErrorType.ExpectedClauseList);
            }
            if(!Operators.BinaryHorn.Equals(op.Operator)) {
                Throw(pos, ErrorType.ExpectedClauseList);
            }
            if (!Expect(Lexer.TokenType.Punctuation, p => p.Equals("."), out string _)) {
                Throw(pos, ErrorType.UnterminatedClauseList);
            }
            var rhs = op.Right.Reduce(s => s, () => throw new NotImplementedException());
            if (!CommaExpression.TryUnfold(rhs, out var expr)) {
                expr = new CommaExpression(CommaExpression.Build(rhs));
            }
            return MakePredicate(pos, desc, op.Left, expr.Sequence, out predicate);

            bool MakePredicate(Lexer.StreamState pos, string desc, Term head, Sequence body, out Predicate c)
            {
                var headVars = Term.Variables(head)
                    .Where(v => !v.Equals(Literals.Discard));
                var bodyVars = body.Contents.SelectMany(t => Term.Variables(t))
                    .Distinct();
                var singletons = headVars.Where(v => !v.Ignored && !bodyVars.Contains(v) && headVars.Count(x => x.Name == v.Name) == 1);
                if (singletons.Any()) {
                    Throw(pos, ErrorType.PredicateHasSingletonVariables, Predicate.Signature(head), String.Join(", ", singletons));
                }
                c = new Predicate(
                    desc
                    , head
                    , body
                );
                return true;
            }
        }

        public bool TryParseProgram(out Program program)
        {
            var directives = new List<Directive>();
            var predicates = new List<Predicate>();
            while (TryParseDirective(out var directive))
            {
                directives.Add(directive);
            }
            while (TryParsePredicate(out var predicate)) {
                predicates.Add(predicate);
            }
            program = new Program(directives.ToArray(), predicates.ToArray());
            return true;
        }

        public void Dispose()
        {
            _lexer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
