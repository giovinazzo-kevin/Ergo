﻿using Ergo.Lang.Ast.Terms.Interfaces;
using System.Diagnostics;

namespace Ergo.Lang.Ast;

[DebuggerDisplay("{ Explain(false) }")]
public abstract class AbstractList : AbstractTerm
{
    public readonly ImmutableArray<ITerm> Contents;
    public readonly bool IsEmpty;

    public override bool IsQualified => CanonicalForm.IsQualified;
    public override bool IsParenthesized => CanonicalForm.IsParenthesized;
    public override bool IsGround => CanonicalForm.IsGround;
    public override IEnumerable<Variable> Variables => CanonicalForm.Variables;
    public override int CompareTo(ITerm other) => CanonicalForm.CompareTo(other);
    public override bool Equals(ITerm other) => CanonicalForm.Equals(other);
    public abstract Operator Operator { get; }
    public abstract Atom EmptyElement { get; }
    public abstract (string Open, string Close) Braces { get; }
    public Signature Signature { get; }

    protected abstract ITerm CanonicalForm { get; }

    public AbstractList(ImmutableArray<ITerm> head, Maybe<ParserScope> scope)
        : base(scope)
    {
        Contents = head;
        IsEmpty = head.Length == 0;
    }
    public AbstractList(IEnumerable<ITerm> args, Maybe<ParserScope> scope) : this(ImmutableArray.CreateRange(args), scope) { }
    protected abstract AbstractList Create(ImmutableArray<ITerm> head, Maybe<ParserScope> scope);

    public override string Explain(bool canonical)
    {
        if (IsEmpty)
            return EmptyElement.Explain(canonical);
        var joined = Contents.Join(t => t.Explain(canonical));
        return $"{Braces.Open}{joined}{Braces.Close}";
    }
    public override Maybe<SubstitutionMap> UnifyLeftToRight(ITerm other)
    {
        return CanonicalForm.Unify(other);
    }
    public override Signature GetSignature() => CanonicalForm.GetSignature();
    public override AbstractTerm Instantiate(InstantiationContext ctx, Dictionary<string, Variable> vars = null)
    {
        vars ??= new();
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Instantiate(ctx, vars);
        }
        return Create(builder.ToImmutableArray(), Scope);
    }
    public override AbstractTerm Substitute(Substitution s)
    {
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Substitute(s);
        }
        return Create(builder.ToImmutableArray(), Scope);
    }
    public virtual AbstractTerm Substitute(SubstitutionMap s)
    {
        var builder = Contents.ToArray();
        for (int i = 0; i < builder.Length; i++)
        {
            builder[i] = Contents[i].Substitute(s);
        }
        return Create(builder.ToImmutableArray(), Scope);
    }
    public override ITerm NumberVars() => CanonicalForm.NumberVars();
    /// <summary>
    /// Folds a list in the canonical way by composing f/2 recursively, appending the empty element at the end.
    /// </summary>
    public static ITerm Fold(Operator op, ITerm tail, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        if (args.Length == 0)
            return tail;
        if (args.Length == 1)
            return new Complex(op.CanonicalFunctor, args[0], tail)
                .AsOperator(op);
        return args
            .Append(tail)
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    /// <summary>
    /// Folds a list in a non-canonical way that omits the trailing empty element.
    /// Note that the empty element is still returned for 0-length lists.
    /// </summary>
    public static ITerm FoldNoEmptyTail(Operator op, ITerm emptyElement, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        // NOTE: It seems to make more sense to fold tuples and sets this way, since pattern matching is reserved to lists.
        if (args.Length == 0)
            return emptyElement;
        if (args.Length == 1 && args[0].Equals(emptyElement))
            return args[0];
        if (args.Length == 1)
            return new Complex(op.CanonicalFunctor, args[0]);
        return args
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    /// <summary>
    /// Folds a list in a non-canonical way that omits the trailing empty element and parenthesizes the single element instead of returning a malformed complex.
    /// Note that the empty element is still returned for 0-length lists.
    /// </summary>
    public static ITerm FoldNoEmptyTailParensSingle(Operator op, ITerm emptyElement, ImmutableArray<ITerm> args)
    {
        if (op.Fixity != Fixity.Infix)
            throw new InvalidOperationException("Cannot fold a non-infix operator.");
        // NOTE: It seems to make more sense to fold tuples and sets this way, since pattern matching is reserved to lists.
        if (args.Length == 0)
            return emptyElement;
        if (args.Length == 1 && args[0].Equals(emptyElement))
            return args[0];
        if (args.Length == 1)
            return args[0].AsParenthesized(true);
        return args
            .Reverse()
            .Aggregate((a, b) => new Complex(op.CanonicalFunctor, b, a)
                .AsOperator(op));
    }

    public static explicit operator Complex(AbstractList list)
    {
        try
        {
            return (Complex)list.CanonicalForm;
        }
        catch (Exception e)
        {
            ;
        }
        return default;
    }
    public static Maybe<IEnumerable<ITerm>> Unfold(ITerm term, ITerm emptyElement, Func<ITerm, bool> matchTail, HashSet<Atom> functors)
    {
        if (term is Complex { Functor: var f } c && functors.Contains(f))
            return Maybe.Some(Inner());
        if (term is Atom && term.Equals(emptyElement))
            return Maybe.Some<IEnumerable<ITerm>>(Array.Empty<ITerm>());
        if (term is AbstractList abs && abs.Operator.Synonyms.Intersect(functors).Any())
        {
            if (abs.Contents.Length > 0)
                return Unfold((Complex)abs, emptyElement, matchTail, functors);
            return Maybe.Some<IEnumerable<ITerm>>(Array.Empty<ITerm>());
        }
        return default;

        IEnumerable<ITerm> Inner()
        {
            var list = new List<ITerm>();
            while (term is Complex { Functor: var f } c && functors.Contains(f))
            {
                if (c.Arity == 2)
                {
                    list.AddRange(Unfold(c.Arguments[0], emptyElement, matchTail, functors)
                        .Or(() => Maybe.Some<IEnumerable<ITerm>>(new ITerm[] { c.Arguments[0] }))
                        .AsEnumerable()
                        .SelectMany(x => x));
                    term = c.Arguments[1];
                }
                else if (c.Arity == 1)
                {
                    term = c.Arguments[0];
                }
                else break;
            }

            if (!matchTail(term))
            {
                return Enumerable.Empty<ITerm>();
            }

            list.Add(term);
            return list;
        }
    }
}
