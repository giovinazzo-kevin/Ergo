using Ergo.Pipelines.LoadModule;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Modules.Directives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ergo.Modules;

namespace Ergo.Pipelines;

public interface IErgoEnv
    :
        IBuildModuleTreePipeline.Env,
        IBuildDependencyGraphPipeline.Env
    ;

public class ErgoEnv : IErgoEnv
{
    public IList<string> SearchDirectories { get; set; } = [@".\ergo\", @".\user\"];
    public ISet<Operator> Operators { get; set; } = new HashSet<Operator>() { 
        WellKnown.Operators.UnaryHorn, 
        WellKnown.Operators.BinaryHorn,
        WellKnown.Operators.Conjunction,
        WellKnown.Operators.ArityIndicator,
    };
    public IDictionary<Signature, ErgoDirective> Directives { get; set; } = new Dictionary<Signature, ErgoDirective>();
    public ErgoLexer.StreamState StreamState { get; set; }
    public Maybe<Atom> CurrentModule { get; set; }
    public Maybe<ErgoModuleTree> ModuleTree { get; set; }
    public int LoadOrder { get; set; }
}
