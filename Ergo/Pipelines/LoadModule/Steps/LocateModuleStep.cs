using Ergo.Lang;
using Ergo.Lang.Ast;

namespace Ergo;


public interface ILocateModuleStep : IErgoPipeline<Atom, string, ILocateModuleStep.Env>
{
    public interface Env
    {
        IList<string> SearchDirectories { get; }
    }
}

public class LocateModuleStep : ILocateModuleStep
{
    public Either<string, PipelineError> Run(Atom module, ILocateModuleStep.Env env)
    {
        var moduleName = module.Explain(false);
        moduleName = moduleName.Replace("/", @"\");
        var i = moduleName.LastIndexOf(@"\");
        var (prefix, name) = i > -1
            ? (moduleName[..(i + 1)], moduleName[(i + 1)..])
            : (string.Empty, moduleName);
        var nameNoExt = Path.GetFileNameWithoutExtension(name);
        var fileName = env.SearchDirectories
            .Select(d => Path.Combine(d, prefix))
            .Where(Directory.Exists)
            .SelectMany(d => {
                try { return Directory.EnumerateFiles(d, "*.ergo", SearchOption.AllDirectories); }
                catch { return []; }
            })
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(nameNoExt));
        if (fileName is null)
            return new PipelineError(this, new FileNotFoundException(null, moduleName));
        return fileName;
    }
}

