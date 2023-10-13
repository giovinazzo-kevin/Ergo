using Ergo.Events;
using Ergo.Events.Solver;
using Ergo.Interpreter.Directives;
using Ergo.Solver;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries.Expansions;

public class Expansions : Library
{
    public override Atom Module => WellKnown.Modules.Expansions;

    protected readonly Dictionary<Signature, HashSet<Expansion>> Table = new();
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        .Append(new DefineExpansion())
        ;

    public override void OnErgoEvent(ErgoEvent evt)
    {
        if (evt is SolverInitializingEvent sie)
        {
            var expansions = new Queue<Predicate>();
            var tmpScope = sie.Solver.CreateScope(sie.Scope);
            foreach (var pred in sie.Solver.KnowledgeBase.ToList())
            {
                foreach (var exp in ExpandPredicate(pred, tmpScope))
                {
                    if (!exp.IsSameDefinitionAs(pred))
                        expansions.Enqueue(exp);
                }
                if (expansions.Count > 0)
                {
                    if (!sie.Solver.KnowledgeBase.Retract(pred))
                        throw new InvalidOperationException();
                    while (expansions.TryDequeue(out var exp))
                    {
                        sie.Solver.KnowledgeBase.Retract(exp);
                        sie.Solver.KnowledgeBase.AssertZ(exp);
                    }
                    expansions.Clear();
                }
            }
        }
        if (evt is QuerySubmittedEvent qse)
        {
            var expansions = new Queue<Predicate>();
            var topLevelHead = new Complex(WellKnown.Literals.TopLevel, qse.Query.Goals.Contents.SelectMany(g => g.Variables).Distinct().Cast<ITerm>().ToArray());
            foreach (var match in qse.Solver.KnowledgeBase.GetMatches(qse.Scope.InstantiationContext, topLevelHead, desugar: false)
                .AsEnumerable().SelectMany(x => x))
            {
                var pred = Predicate.Substitute(match.Rhs, match.Substitutions.Select(x => x.Inverted()));

                foreach (var exp in ExpandPredicate(pred, qse.Scope))
                {
                    if (!exp.IsSameDefinitionAs(pred))
                        expansions.Enqueue(exp);
                }
                if (expansions.Count > 0)
                {
                    if (!qse.Solver.KnowledgeBase.Retract(pred))
                        throw new InvalidOperationException();
                    while (expansions.TryDequeue(out var exp))
                    {
                        qse.Solver.KnowledgeBase.Retract(exp);
                        qse.Solver.KnowledgeBase.AssertZ(exp);
                    }
                    expansions.Clear();
                }
            }
        }
    }

    public IEnumerable<Expansion> GetDefinedExpansions() => Table.SelectMany(x => x.Value);
    public IEnumerable<Expansion> GetDefinedExpansions(Atom module, Signature sig) => Table.TryGetValue(sig, out var exp)
        ? exp.Where(e => e.DeclaringModule.Equals(module))
        : Enumerable.Empty<Expansion>();

    public void AddExpansion(Atom module, Variable outVar, Predicate pred)
    {
        var signature = pred.Head.GetSignature();
        if (!Table.TryGetValue(signature, out var set))
            set = Table[signature] = new();
        set.Add(new(module, outVar, pred));
    }


    // See: https://github.com/G3Kappa/Ergo/issues/36
    public IEnumerable<Predicate> ExpandPredicate(Predicate p, SolverScope scope)
    {
        Predicate currentPredicate = p;
        while (true)
        {
            var newPredicate = default(Predicate);
            foreach (var expanded in ExpandPredicateOnce(currentPredicate, scope))
            {
                newPredicate = expanded;
                yield return expanded;
            }
            if (newPredicate.Equals(default) || newPredicate.IsSameDefinitionAs(currentPredicate))
            {
                // No further expansion possible
                break;
            }
            currentPredicate = newPredicate;
        }
        IEnumerable<Predicate> ExpandPredicateOnce(Predicate p, SolverScope scope)
        {
            // Predicates are expanded only once, when they're loaded. The same applies to queries.
            // Expansions are defined as lambdas that define a predicate and capture one variable:
            //   - The head of the predicate is matched against the current term; if they unify:
            //      - The body of the expansion is inserted in the current predicate in a sensible location;
            //      - Previous references to the term are replaced with references to the captured variable.
            // TODO: Sensible location means that an expansion should be placed right above the line that invokes it.
            foreach (var headExp in ExpandTerm(p.Head, scope))
            {
                var newHead = headExp.Reduce(e => e.Binding
                    .Select(v => (ITerm)v).GetOr(e.Match), a => a);
                var headClauses = headExp.Reduce(e => e.Expansion.Contents, _ => ImmutableArray<ITerm>.Empty);
                var bodyExpansions = new List<Either<ExpansionResult, ITerm>>[p.Body.Contents.Length];
                for (int i = 0; i < p.Body.Contents.Length; i++)
                {
                    bodyExpansions[i] = new();
                    foreach (var bodyExp in ExpandTerm(p.Body.Contents[i], scope))
                        bodyExpansions[i].Add(bodyExp);
                    if (bodyExpansions[i].Count == 0)
                        bodyExpansions[i].Add(Either<ExpansionResult, ITerm>.FromB(p.Body.Contents[i]));
                }
                var cartesian = bodyExpansions.CartesianProduct();
                foreach (var variant in cartesian)
                {
                    var newBody = new List<ITerm>();
                    // We need to add each head clause in a "sensible" place within the body of the predicate
                    newBody.AddRange(headClauses);
                    foreach (var clause in variant)
                    {
                        newBody.AddRange(clause.Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>()));
                        newBody.Add(clause.Reduce(e => e.Binding.Select(x => (ITerm)x).GetOr(e.Match), a => a));
                    }
                    yield return new Predicate(
                        p.Documentation,
                        p.DeclaringModule,
                        newHead,
                        new(newBody, p.Head.Scope),
                        p.IsDynamic,
                        p.IsExported
                    );
                }
            }
        }
        IEnumerable<Either<ExpansionResult, ITerm>> ExpandTerm(ITerm term, SolverScope scope)
        {
            if (term is Variable)
                yield break;
            if (term is NTuple ntuple)
            {

            }
            foreach (var exp in GetExpansions(term, scope)
                .Select(x => Either<ExpansionResult, ITerm>.FromA(x))
                .DefaultIfEmpty(Either<ExpansionResult, ITerm>.FromB(term)))
            {
                // If this is a complex term, expand all of its arguments recursively and produce a combination of all solutions
                if (exp.Reduce(e => e.Match, a => a) is Complex cplx)
                {
                    var expansions = new List<Either<ExpansionResult, ITerm>>[cplx.Arity];
                    for (var i = 0; i < cplx.Arity; i++)
                    {
                        expansions[i] = new();
                        foreach (var argExp in ExpandTerm(cplx.Arguments[i], scope).Distinct())
                        {
                            expansions[i].Add(argExp);
                        }
                        if (expansions[i].Count == 0)
                            expansions[i].Add(Either<ExpansionResult, ITerm>.FromB(cplx.Arguments[i]));
                    }
                    var cartesian = expansions.CartesianProduct();
                    var isLambda = WellKnown.Functors.Lambda.Contains(cplx.Functor) && cplx.Arity == 2
                        && cplx.Arguments[0].IsAbstract<Lang.Ast.List>().TryGetValue(out _);
                    foreach (var argList in cartesian)
                    {
                        var newCplx = cplx
                            .WithArguments(argList
                            .Select(x => x.Reduce(exp => exp.Binding
                               .Select(v => (ITerm)v).GetOr(exp.Match), a => a))
                               .ToImmutableArray());
                        var expClauses = new NTuple(
                            exp.Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>())
                               .Concat(argList.SelectMany(x => x
                                  .Reduce(e => e.Expansion.Contents, _ => Enumerable.Empty<ITerm>()))),
                            cplx.Scope);
                        if (isLambda)
                        {
                            // Stuff 'expClauses' inside the lambda instead of returning them to the parent predicate
                            var body = newCplx.Arguments[1];
                            var closure = new NTuple(expClauses.Contents.Append(body), cplx.Scope);
                            newCplx = newCplx.WithArguments(newCplx.Arguments.SetItem(1, closure));
                            yield return Either<ExpansionResult, ITerm>.FromB(newCplx);
                        }
                        else
                        {
                            yield return Either<ExpansionResult, ITerm>.FromA(new(newCplx, expClauses, exp.Reduce(e => e.Binding, _ => default)));
                        }
                    }
                }
                else
                {
                    yield return exp;
                }
            }
        }

        IEnumerable<ExpansionResult> GetExpansions(ITerm term, SolverScope scope)
        {
            var sig = term.GetSignature();
            // Try all modules in import order
            var modules = scope.InterpreterScope.VisibleModules;
            foreach (var mod in modules.Reverse())
            {
                scope = scope.WithModule(mod);
                foreach (var exp in GetDefinedExpansions(mod, sig))
                {
                    // Example expansion:
                    // [Output] >> (head(A) :- body(A, Output)).
                    // 1. Instantiate expansion, keep track of Output:
                    // [__K1] >> (head(__K2) :- body(__K2, __K1)).
                    // Output = __K1
                    // 2. Unify term with expansion head:
                    // head(__K2) / head(test)
                    // __K2 = test
                    // 3. Substitute expansion:
                    // [__K1] >> (head(test) :- body(test, __K1)).
                    var expVars = new Dictionary<string, Variable>();
                    var expInst = exp.Predicate.Instantiate(scope.InstantiationContext, expVars);
                    if (!term.Unify(expInst.Head).TryGetValue(out var subs))
                        continue;
                    var pred = Predicate.Substitute(expInst, subs);
                    yield return new(pred.Head, pred.Body, expVars[exp.OutVariable.Name]);
                }
            }
        }
    }


}
