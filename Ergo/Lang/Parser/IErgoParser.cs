namespace Ergo.Lang;

public interface IErgoParser
{
    public T Parse<T>() where T : IErgoAst;
}
