using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Runtime;

namespace Tests;

public class CompilerTestFixture : ErgoTestFixture
{
    protected override string TestsModuleName => "inlining";
    protected override CompilerFlags CompilerFlags => base.CompilerFlags
        | CompilerFlags.EnableInlining;
    protected override bool TrimKnowledgeBase => false;
}

public class ErgoTestFixture : IDisposable
{
    public readonly ExceptionHandler NullExceptionHandler = default;
    public readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;

    protected virtual string TestsModuleName => "tests";
    protected virtual InterpreterFlags InterpreterFlags => InterpreterFlags.Default;
    protected virtual CompilerFlags CompilerFlags => CompilerFlags.Default;
    protected virtual bool TrimKnowledgeBase => true;

    public ErgoTestFixture()
    {
        var basePath = Directory.GetCurrentDirectory();
        var testsPath = Path.Combine(basePath, @"..\..\..\ergo");

        Interpreter = CreateInterpreter();
        InterpreterScope = CreateScope(testsPath);
        LoadTestsModule(ref InterpreterScope);
        KnowledgeBase = InterpreterScope.BuildKnowledgeBase(CompilerFlags);
        if(TrimKnowledgeBase)
            KnowledgeBase.Trim();
    }

    protected virtual ErgoInterpreter CreateInterpreter()
    {
        return ErgoFacade.Standard
            .BuildInterpreter(InterpreterFlags);
    }

    protected virtual InterpreterScope CreateScope(string testsPath) 
    {
        return Interpreter.CreateScope(x => x
                .WithExceptionHandler(ThrowingExceptionHandler)
                .WithoutSearchDirectories()
                .WithSearchDirectory(testsPath))
            .WithRuntime(true);
    }

    protected virtual void LoadTestsModule(ref InterpreterScope scope)
    {
        var module = Interpreter
            .Load(ref scope, new(TestsModuleName))
            .GetOrThrow(new InvalidOperationException());
        scope = scope.WithModule(scope.EntryModule.WithImport(module.Name));
    }

    ~ErgoTestFixture()
    {
        Dispose();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
