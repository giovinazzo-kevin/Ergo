using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Runtime;
using System.Diagnostics;

namespace Tests;

public class CompilerTestFixture : ErgoTestFixture
{
    protected override string TestsModuleName => "inlining";

    protected override ErgoFacade Facade => base.Facade
        .SetTrimKnowledgeBase(false)
        .SetCompilerFlags(base.Facade.CompilerFlags | CompilerFlags.EnableInlining);
}


public class ErgoTestFixture : IDisposable
{
    public readonly ExceptionHandler NullExceptionHandler = default;
    public readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;
    public readonly ErgoVM VM;

    protected virtual string TestsModuleName => "tests";

    protected virtual ErgoFacade Facade { get; private set; }

    public ErgoTestFixture()
    {
        var basePath = Directory.GetCurrentDirectory();
        var testsPath = Path.Combine(basePath, @"..\..\..\ergo");

        Facade = ErgoFacade.Standard
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
        Interpreter = Facade.BuildInterpreter();
        InterpreterScope = Interpreter.CreateScope();
        KnowledgeBase = InterpreterScope.BuildKnowledgeBase();
        VM = Facade.BuildVM();
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