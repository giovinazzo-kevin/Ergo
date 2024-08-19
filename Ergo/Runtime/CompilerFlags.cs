namespace Ergo.Runtime;

[Flags]
public enum CompilerFlags
{
    Default = EnableInlining | EnableOptimizations,
    None = 0,
    EnableInlining = 1,
    EnableOptimizations = 4,
}
