using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ergo.Lang
{

    public partial class Parser : IDisposable
    {
        private readonly Lexer _lexer;
        private readonly Term.InstantiationContext _context;

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            _context = new Term.InstantiationContext("G");
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
            else if (Expect(Lexer.TokenType.Number, out decimal dec)) {
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
                if (term.StartsWith("__G")) {
                    Throw(pos, ErrorType.TermHasIllegalName, var.Name);
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
                , () => TryParseTermOrExpression(out var t) ? (true, t) : (false, default)
                , "(", ",", ")"
                , true
                , out var inner
            )) {
                return Fail(pos);
            }
            if (CommaExpression.IsCommaExpression(inner)) {
                cplx = new Complex(new Atom(functor), inner.GetContents().ToArray());
            }
            else {
                cplx = new Complex(new Atom(functor), inner.Root);
            }
            return true;
        }
        public bool TryParseTermOrExpression(out Term term)
        {
            if (TryParseExpression(out var expr)) {
                term = expr.Complex;
                return true;
            }
            if (TryParseTerm(out term))
                return true;
            term = default;
            return false;
        }
        public bool TryParseTerm(out Term term)
        {
            term = default;
            var pos = _lexer.State;
            if(Expect(Lexer.TokenType.Punctuation, str => str.Equals("("), out string _)
            && TryParseExpression(out var expr)
            && Expect(Lexer.TokenType.Punctuation, str => str.Equals(")"), out string _)) {
                term = expr.Complex;
                return true;
            }
            if (TryParseList(out var list)) {
                term = list.Root;
                return true;
            }
            if (TryParseVariable(out var var)) {
                term = var;
                // Iff var = _ then var = __G{N}
                if (var.Equals(Literals.Discard)) {
                    term = Term.Instantiate(_context, var, discardsOnly: true, vars: new Dictionary<string, Variable>());
                }
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
            return Fail(pos);
        }

        public bool TryParseList(out List seq)
        {
            seq = default;
            var pos = _lexer.State;
            if (TryParseSequence(
                  List.Functor
                , List.EmptyLiteral
                , () => TryParseTermOrExpression(out var t) ? (true, t) : (false, default)
                , "[", ",", "]"
                , true
                , out var full
            )) {
                var contents = full.GetContents().ToArray();
                if (contents.Length == 1 && contents[0].Type == TermType.Complex && ((Complex)contents[0]) is var cplx
                    && Operators.BinaryList.Synonyms.Contains(cplx.Functor)) {
                    var arguments = new[] { cplx.Arguments[0] };
                    if(CommaExpression.TryUnfold(cplx.Arguments[0], out var comma)) {
                        arguments = comma.Sequence.GetContents().ToArray();
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
            return Expect(Lexer.TokenType.Operator, str => Operator.TryGetOperatorFromFunctor(new Atom(str), out var _op) && match(_op), out string str)
                && Operator.TryGetOperatorFromFunctor(new Atom(str), out op);
        }

        public bool TryParsePrefixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (ExpectOperator(op => op.Affix == Operator.AffixType.Prefix, out var op)
            && TryParseTerm(out var arg)) {
                expr = op.BuildExpression(arg);
                return true;
            }
            return Fail(pos);
        }

        public bool TryParsePostfixExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (TryParseTerm(out var arg) 
            && ExpectOperator(op => op.Affix == Operator.AffixType.Postfix, out var op)) {
                expr = op.BuildExpression(arg);
                return true;
            }
            return Fail(pos);
        }

        public bool TryParseExpression(out Expression expr)
        {
            expr = default; var pos = _lexer.State;
            if (TryParsePrimary(out var lhs)) {
                if(WithMinPrecedence(lhs, 0, out expr)) {
                    return true;
                }
                // Special case for unary expressions
                if(lhs.Type != TermType.Complex || !((Complex)lhs is var cplx)
                    || cplx.Arguments.Length > 1
                    || !Operator.TryGetOperatorFromFunctor(cplx.Functor, out var op)) {
                    return Fail(pos);
                }
                expr = op.BuildExpression(cplx.Arguments[0], Maybe<Term>.None);
                return true;
            }
            return Fail(pos);

            bool WithMinPrecedence(Term lhs, int minPrecedence, out Expression expr)
            {
                expr = default; var pos = _lexer.State;
                if (!TryPeekNextOperator(out var lookahead)) { 
                    return Fail(pos);
                }
                if(lookahead.Affix != Operator.AffixType.Infix || lookahead.Precedence < minPrecedence) {
                    return Fail(pos);
                }
                while (lookahead.Affix == Operator.AffixType.Infix && lookahead.Precedence >= minPrecedence) {
                    Debug.Assert(_lexer.TryReadNextToken(out _));
                    var op = lookahead;
                    if(!TryParsePrimary(out var rhs)) {
                        expr = op.BuildExpression(lhs, Maybe<Term>.None);
                        break;
                    }
                    if(!TryPeekNextOperator(out lookahead)) {
                        expr = op.BuildExpression(lhs, Maybe.Some(rhs));
                        break;
                    }
                    while(lookahead.Affix == Operator.AffixType.Infix && lookahead.Precedence > op.Precedence
                        || lookahead.Associativity == Operator.AssociativityType.Right && lookahead.Precedence == op.Precedence) {
                        if(!WithMinPrecedence(rhs, op.Precedence + 1, out var newRhs)) {
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

            bool TryParsePrimary(out Term term)
            {
                term = default; var pos = _lexer.State;
                if (TryParsePrefixExpression(out var prefix)) {
                    term = prefix.Complex;
                    return true;
                }
                if (TryParsePostfixExpression(out var postfix)) {
                    term = postfix.Complex;
                    return true;
                }
                if (TryParseTerm(out term)) {
                    return true;
                }
                return Fail(pos);
            }

            bool TryPeekNextOperator(out Operator op)
            {
                op = default;
                return _lexer.TryPeekNextToken(out var lookahead)
                && Operator.TryGetOperatorFromFunctor(new Atom(lookahead.Value), out op);
            }
        }

        public bool TryParsePredicate(out Predicate predicate)
        {
            predicate = default;
            var pos = _lexer.State;
            if (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string desc)) {
                desc = desc[1..].TrimStart();
                while (Expect(Lexer.TokenType.Comment, p => p.StartsWith(":"), out string newDesc)) {
                    if (!string.IsNullOrEmpty(newDesc)) {
                        desc += "\n" + newDesc[1..].TrimStart();
                    }
                }
            }
            desc ??= " ";
            if (_lexer.Eof)
                return Fail(pos);
            if (!TryParseExpression(out var op)) {
                if (TryParseTerm(out var head) && Expect(Lexer.TokenType.Punctuation, p => p.Equals("."), out string _)) {
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
                var clauses = body.GetContents().ToArray();
                var bodyVars = clauses.SelectMany(t => Term.Variables(t))
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
            var predicates = new List<Predicate>();
            while (TryParsePredicate(out var predicate)) {
                predicates.Add(predicate);
            }
            program = new Program(predicates.ToArray());
            return true;
        }

        public void Dispose()
        {
            _lexer.Dispose();
        }
    }
}
