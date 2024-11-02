using Ergo.Events;
using Ergo.Modules.Directives;
using Ergo.Runtime.BuiltIns;
using Microsoft.Extensions.DependencyInjection;

namespace Ergo.Modules.Libraries;

public interface IExportsDirective<T> where T : ErgoDirective;
public interface IExportsBuiltIn<T> where T : ErgoBuiltIn;

// see https://github.com/G3Kappa/Ergo/issues/10
public interface IErgoLibrary
{
    int LoadOrder { get; }
    Atom Module { get; }
    IEnumerable<ErgoDirective> ExportedDirectives { get; }
    IEnumerable<ErgoBuiltIn> ExportedBuiltins { get; }
    void OnErgoEvent(ErgoEvent evt);
}

public abstract class ErgoLibrary : IErgoLibrary
{
    public virtual int LoadOrder => 0;
    public Atom Module { get; }
    public IEnumerable<ErgoDirective> ExportedDirectives { get; }
    public IEnumerable<ErgoBuiltIn> ExportedBuiltins { get; }
    public virtual void OnErgoEvent(ErgoEvent evt) { }

    public static IEnumerable<Type> GetExportedDirectives(Type type) => type
        .GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExportsDirective<>))
        .Select(i => i.GetGenericArguments().Single());
    public static IEnumerable<Type> GetExportedBuiltIns(Type type) => type
        .GetInterfaces()
        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExportsBuiltIn<>))
        .Select(i => i.GetGenericArguments().Single());

    public ErgoLibrary(IServiceProvider sp)
    {
        Module = new Atom(GetType().Name.ToErgoCase());
        ExportedDirectives = GetExportedDirectives(GetType())
            .Select(sp.GetRequiredService)
            .Cast<ErgoDirective>();
        ExportedBuiltins = GetExportedBuiltIns(GetType())
            .Select(sp.GetRequiredService)
            .Cast<ErgoBuiltIn>();
    }

}
