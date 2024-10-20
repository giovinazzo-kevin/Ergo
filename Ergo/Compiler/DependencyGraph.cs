using Ergo.Modules;
using Ergo.Runtime.BuiltIns;
using Ergo.Shell.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Ergo.Compiler;
public class ErgoDependencyGraph
{
    public record ClauseDefinition(ITerm Head, NTuple Body, Atom DeclaringModule, Maybe<Atom> DeclaredModule, bool IsFactual, bool IsTailRecursive, bool IsExported, bool IsDynamic);
    public record PredicateDefinition(HashSet<PredicateDefinition> Dependencies, List<ClauseDefinition> Clauses, Maybe<ErgoBuiltIn> BuiltIn);
    public readonly Dictionary<Signature, PredicateDefinition> Predicates = new()
    {
        { WellKnown.Literals.True.GetSignature(), new([], [], default) },
        { WellKnown.Literals.False.GetSignature(), new([], [], default) }
    };
}
