using Ergo.Lang.Parser;
using Ergo.Pipelines;

namespace Ergo.Lang;

public class ErgoParser(IErgoEnv env, IEnumerable<IAbstractTermParser> abstractTermParsers) : IErgoParser
{
    public T Parse<T>() where T : IErgoAst
    {
        throw new NotSupportedException(typeof(T).Name);
    }
}
