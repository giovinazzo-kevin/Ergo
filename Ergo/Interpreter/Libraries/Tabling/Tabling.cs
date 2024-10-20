﻿using Ergo.Events;
using Ergo.Events.Interpreter;
using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;

namespace Ergo.Modules.Libraries.Tabling;

public class Tabling : IErgoLibrary
{
    public override int LoadOrder => 10;
    public override Atom Module => WellKnown.Modules.Tabling;

    protected readonly Dictionary<ErgoVM, MemoizationContext> MemoizationContextTable = new();
    protected readonly Dictionary<Atom, HashSet<Signature>> TabledPredicates = new();

    private readonly ErgoBuiltIn[] _exportedBuiltIns = [
        new Tabled(),
    ];
    private readonly ErgoDirective[] _interpreterDirectives = [
        new DeclareTabledPredicate(),
    ];

    public override IEnumerable<ErgoBuiltIn> ExportedBuiltins => _exportedBuiltIns;
    public override IEnumerable<ErgoDirective> ExportedDirectives => _interpreterDirectives;

    public void AddTabledPredicate(Atom module, Signature sig)
    {
        if (!TabledPredicates.TryGetValue(module, out var sigs))
            TabledPredicates[module] = sigs = new();
        sigs.Add(sig);
    }
    public override void OnErgoEvent(ErgoEvent e)
    {
        if (e is ModuleLoadedEvent { Scope: var scope } mle)
        {
            TransformTabledPredicates(ref scope, mle.ModuleName);
            mle.Scope = scope; // Update the scope
        }
        void TransformTabledPredicates(ref InterpreterScope scope, Atom moduleName)
        {
            var ctx = new InstantiationContext("L");
            if (!TabledPredicates.TryGetValue(moduleName, out var signatures))
                return;
            foreach (var sig in signatures)
            {
                var auxFunctor = new Atom(sig.Functor.Explain() + "__aux_");
                var anon = sig.Functor.BuildAnonymousTerm(sig.Arity.GetOr(0));
                var aux = ((ITerm)new Complex(auxFunctor, anon.GetArguments())).Qualified(moduleName);

                var tblPred = new Clause(
                    "(auto-generated auxilliary predicate for tabling)",
                    moduleName,
                    anon,
                    new NTuple(new ITerm[] { new Complex(new Atom("tabled"), aux) }, moduleName.Scope),
                    true,
                    true,
                    default
                );
                var kb = scope.Modules[moduleName].Program.KnowledgeBase;
                foreach (var match in kb.GetMatches(ctx, anon, desugar: false)
                    .AsEnumerable().SelectMany(x => x))
                {
                    match.Predicate.Head.GetQualification(out var head);
                    var auxPred = new Clause(
                        match.Predicate.Documentation,
                        match.Predicate.DeclaringModule,
                        head.WithFunctor(auxFunctor),
                        match.Predicate.Body,
                        match.Predicate.IsDynamic,
                        false,
                        default
                    );
                    if (!kb.Retract(head))
                    {
                        scope.Throw(ErgoInterpreter.ErrorType.TransformationFailed, head);
                        return;
                    }
                    kb.AssertZ(auxPred);
                }
                kb.AssertZ(tblPred);
            }
        }
    }

}
