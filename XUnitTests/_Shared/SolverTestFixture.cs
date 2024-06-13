﻿using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Compiler;
using Ergo.Lang.Exceptions.Handler;
using Ergo.Runtime;

namespace Tests;

public class ErgoTestFixture : IDisposable
{
    public readonly ExceptionHandler NullExceptionHandler = default;
    public readonly ExceptionHandler ThrowingExceptionHandler = new(ex => throw ex);
    public readonly ErgoInterpreter Interpreter;
    public readonly InterpreterScope InterpreterScope;
    public readonly KnowledgeBase KnowledgeBase;
    public readonly TermMemory Memory = new();

    public ErgoTestFixture()
    {
        var basePath = Directory.GetCurrentDirectory();
        var testsPath = Path.Combine(basePath, @"..\..\..\ergo");
        var moduleName = "tests";

        Interpreter = ErgoFacade.Standard
            .BuildInterpreter(InterpreterFlags.Default);
        var scope = Interpreter.CreateScope(x => x
            .WithExceptionHandler(ThrowingExceptionHandler)
            .WithoutSearchDirectories()
            .WithSearchDirectory(testsPath)
        );
        scope = scope.WithRuntime(true);
        var module = Interpreter
            .Load(ref scope, moduleName)
            .GetOrThrow(new InvalidOperationException());
        InterpreterScope = scope
            .WithModule(scope.EntryModule.WithImport(module.Name));
        KnowledgeBase = InterpreterScope.BuildKnowledgeBase(CompilerFlags.Default);
    }

    ~ErgoTestFixture()
    {
        Dispose();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
