namespace Ergo.Modules.Libraries;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ModuleAttribute(string module) : Attribute
{
    public readonly Atom Module = new Atom(module);
}