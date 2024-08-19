namespace Ergo.Runtime;

[Flags]
public enum VMFlags
{
    None = 0,
    /// <summary>
    /// If set, the rest of the current execution path (@continue) is known to be determinate.
    /// </summary>
    ContinuationIsDet = 1
}
