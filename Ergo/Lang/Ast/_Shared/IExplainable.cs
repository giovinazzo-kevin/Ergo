namespace Ergo.Lang.Ast
{
    public interface IExplainable
    {
        string Explain(bool canonical = false);
    }
}