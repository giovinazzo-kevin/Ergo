using Ergo.Pipelines.LoadModule;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Modules.Directives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ergo.Pipelines;

public interface IErgoEnv
    :
        ILoadModulePipeline.Env
    ;

internal class ErgoEnv : IErgoEnv
{
    public IList<string> SearchDirectories { get; set; } = [];
    public ISet<Operator> Operators { get; set; } = new HashSet<Operator>();
    public IDictionary<Signature, ErgoDirective> Directives { get; set; } = new Dictionary<Signature, ErgoDirective>();
    public ErgoLexer.StreamState StreamState { get; set; }
}
