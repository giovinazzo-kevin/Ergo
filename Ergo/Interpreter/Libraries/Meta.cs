using Ergo.Interpreter.Directives;
using Ergo.Solver.BuiltIns;

namespace Ergo.Interpreter.Libraries;

public class Meta : Library
{
    public override Atom Module => WellKnown.Modules.Meta;
    public override IEnumerable<SolverBuiltIn> GetExportedBuiltins() => Enumerable.Empty<SolverBuiltIn>()
        .Append(new BagOf())
        .Append(new Call())
        .Append(new FindAll())
        .Append(new SetOf())
        .Append(new SetupCallCleanup())
        .Append(new Tabled())
        ;
    public override IEnumerable<InterpreterDirective> GetExportedDirectives() => Enumerable.Empty<InterpreterDirective>()
        .Append(new DeclareTabledPredicate())
        ;

    protected override InterpreterScope Interpreter_OnModuleLoaded(ErgoInterpreter _, InterpreterScope scope)
    {
        TransformTabledPredicates(ref scope);
        return scope;

        static void TransformTabledPredicates(ref InterpreterScope scope)
        {
            var ctx = new InstantiationContext("L");
            foreach (var sig in scope.EntryModule.TabledPredicates)
            {
                var auxFunctor = new Atom(sig.Functor.Explain() + "__aux_");
                var anon = sig.Functor.BuildAnonymousTerm(sig.Arity.GetOr(0));
                var aux = ((ITerm)new Complex(auxFunctor, anon.GetArguments())).Qualified(scope.Entry);

                var tblPred = new Predicate(
                    "(auto-generated auxilliary predicate for tabling)",
                    scope.Entry,
                    anon,
                    new NTuple(new ITerm[] { new Complex(new Atom("tabled"), aux) }),
                    true,
                    true
                );

                foreach (var match in scope.KnowledgeBase.GetMatches(ctx, anon.Qualified(scope.Entry), desugar: false))
                {
                    match.Rhs.Head.GetQualification(out var head);
                    var auxPred = new Predicate(
                        match.Rhs.Documentation,
                        match.Rhs.DeclaringModule,
                        head.WithFunctor(auxFunctor),
                        match.Rhs.Body,
                        match.Rhs.IsDynamic,
                        false
                    );
                    scope.EntryModule.Program.KnowledgeBase.Retract(head);
                    scope.EntryModule.Program.KnowledgeBase.AssertZ(auxPred);
                }
                scope.EntryModule.Program.KnowledgeBase.AssertZ(tblPred);
            }
        }
    }
}
