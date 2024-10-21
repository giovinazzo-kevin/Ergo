using Ergo.Facade;
using Ergo.Lang;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Modules;
using Ergo.Runtime;

namespace Tests;

public class CompilerTestFixture : ErgoTestFixture
{
    protected override string TestsModuleName => "inlining";

    protected override ErgoFacade ConfigureFacade(ErgoFacade facade, string testsPath) => base.ConfigureFacade(facade, testsPath)
        .SetCompilerFlags(facade.CompilerFlags | CompilerFlags.EnableInlining)
        .SetTrimKnowledgeBase(false)
        ;
}


public class ErgoTestFixture : IDisposable
{
    public readonly ExceptionHandler NullExceptionHandler = default;
    public readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;
    public readonly LegacyKnowledgeBase KnowledgeBase;
    public readonly ErgoVM VM;

    protected virtual string TestsModuleName => "tests";

    protected readonly ErgoFacade Facade;

    protected virtual ErgoFacade ConfigureFacade(ErgoFacade facade, string testsPath) => facade
        .SetDecimalType(DecimalType.BigDecimal)
        .ConfigureStdlibScope(scope =>
        {
            scope = scope
                .WithExceptionHandler(ThrowingExceptionHandler)
                .WithoutSearchDirectories()
                .WithSearchDirectory(testsPath);
            return ErgoFacade.Standard.ConfigureStdlibScopeHandler(scope);
        })
        .ConfigureInterpreterScope((interpreter, scope) =>
        {
            var module = interpreter
                .Load(ref scope, new(TestsModuleName))
                .GetOrThrow(new InvalidOperationException());
            scope = scope.WithModule(scope.EntryModule.WithImport(module.Name));
            scope = scope.WithRuntime(true);
            return ErgoFacade.Standard.ConfigureInterpreterScopeHandler(interpreter, scope);
        })
    ;

    public ErgoTestFixture()
    {
        var basePath = Directory.GetCurrentDirectory();
        var testsPath = Path.Combine(basePath, @"..\..\..\ergo");

        Facade = ConfigureFacade(ErgoFacade.Standard, testsPath);

        VM = Facade.BuildVM();
        KnowledgeBase = VM.KB;
        InterpreterScope = VM.KB.Scope;
        Interpreter = Facade.BuildInterpreter();
    }

    ~ErgoTestFixture()
    {
        Dispose();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}


[CollectionDefinition("Default")]
public class ErgoTestCollection : ICollectionFixture<ErgoTestFixture> { }
[CollectionDefinition("Compiler")]
public class CompilerTestCollection : ICollectionFixture<CompilerTestFixture> { }