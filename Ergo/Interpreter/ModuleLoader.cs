using Ergo.Lang.Exceptions;
using Ergo.Lang.Utils;
using System.IO;

namespace Ergo.Interpreter;

public class ModuleLoader
{
    protected static readonly Dictionary<string, Stream> OpenStreams = new();
    protected static void CloseStream(string fn)
    {
        if (OpenStreams.Remove(fn, out var fs))
        {
            fs.Dispose();
        }
    }

    private readonly Stream _fs;
    private readonly string _fn;

    protected InterpreterScope Scope;
    public readonly ErgoInterpreter Interpreter;

    public InterpreterScope GetScope() => Scope;

    public ModuleLoader(ErgoInterpreter interpreter, InterpreterScope scope, string fileName, Stream file)
    {
        _fs = file;
        _fn = fileName;

        Scope = scope;
        Interpreter = interpreter;
    }

    private static Stream GetStream(InterpreterScope scope, string fileName)
    {
        var dir = scope.SearchDirectories
            .Concat(scope.SearchDirectories.Select(s => s + fileName + "/")) // Allows structuring modules within folders of the same name; TODO: proper refactor
            .FirstOrDefault(d => File.Exists(Path.ChangeExtension(Path.Combine(d, fileName), "ergo")));
        if (dir == null)
        {
            throw new FileNotFoundException(fileName);
        }
        fileName = Path.ChangeExtension(Path.Combine(dir, fileName), "ergo");
        if (!OpenStreams.TryGetValue(fileName, out var fs))
        {
            fs = OpenStreams[fileName] = FileStreamUtils.EncodedFileStream(File.OpenRead(fileName), closeStream: true);
        }
        fs.Seek(0, SeekOrigin.Begin);
        return fs;
    }

    public static Module LoadDirectives(ErgoInterpreter interpreter, ref InterpreterScope scope, string fileName)
    {
        var fs = GetStream(scope, fileName);
        return LoadDirectives(interpreter, ref scope, fileName, fs);
    }

    public static Module LoadDirectives(ErgoInterpreter interpreter, ref InterpreterScope scope, string fileName, Stream fs)
    {
        var loader = new ModuleLoader(interpreter, scope, fileName, fs);
        var module = loader.LoadDirectives();
        scope = loader.GetScope();
        return module;
    }

    public virtual Module LoadDirectives()
    {
        var operators = Scope.Operators.Value;
        var lexer = new Lexer(_fs, _fn, Scope.Operators.Value);
        var parser = new Parser(lexer);
        if (!parser.TryParseProgramDirectives(out var program))
        {
            CloseStream(_fn);
            throw new InterpreterException(InterpreterError.CouldNotLoadFile, Scope);
        }
        var directives = program.Directives.Select(d =>
        {
            if (Interpreter.Directives.TryGetValue(d.Body.GetSignature(), out var directive))
                return (Ast: d, Builtin: directive);
            throw new InterpreterException(InterpreterError.UndefinedDirective, Scope, d.Body.GetSignature().Explain());
        });
        foreach (var (Ast, Builtin) in directives.OrderBy(x => x.Builtin.Priority))
        {
            Builtin.Execute(Interpreter, ref Scope, ((Complex)Ast.Body).Arguments);
        }
        var module = Scope.Modules[Scope.Module]
            .WithProgram(program);
        foreach (Atom import in module.Imports.Contents)
        {
            LoadImport(import);
        }
        _fs.Seek(0, SeekOrigin.Begin);
        return module;

        void LoadImport(Atom import)
        {
            if (!Scope.Modules.TryGetValue(import, out var importedModule))
            {
                var importScope = Scope.WithoutModule(import);
                importedModule = LoadDirectives(Interpreter, ref importScope, import.Explain());
                Scope = Scope.WithModule(importedModule);
            }
            foreach (Atom innerImport in importedModule.Imports.Contents)
            {
                if (Scope.Modules.ContainsKey(innerImport)) continue;
                LoadImport(innerImport);
            }
        }
    }

    public static Module Load(ErgoInterpreter interpreter, ref InterpreterScope scope, string fileName)
    {
        var fs = GetStream(scope, fileName);
        return Load(interpreter, ref scope, fileName, fs);
    }

    public static Module Load(ErgoInterpreter interpreter, ref InterpreterScope scope, string fileName, Stream fs)
    {
        var loader = new ModuleLoader(interpreter, scope, fileName, fs);
        var module = loader.Load();
        scope = loader.GetScope();
        return module;
    }

    public virtual Module Load()
    {
        var module = LoadDirectives();
        // By now, all imports have been loaded but some may still be partially loaded (only the directives exist)
        foreach (Atom import in module.Imports.Contents)
        {
            if (Scope.Modules[import].Program.IsPartial)
            {
                var importScope = Scope.WithoutModule(import);
                var importModule = Load(Interpreter, ref importScope, import.Explain());
                Scope = Scope.WithModule(importModule);
            }
        }
        var lexer = new Lexer(_fs, _fn, Scope.Operators.Value);
        var parser = new Parser(lexer);
        if (!parser.TryParseProgram(out var program))
        {
            CloseStream(_fn);
            throw new InterpreterException(InterpreterError.CouldNotLoadFile, Scope);
        }
        CloseStream(_fn);
        Scope = Scope.WithModule(module = module.WithProgram(program));
        return module;
    }
}
