using Ergo.Facade;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Modules;
using Ergo.Runtime;
using System.Diagnostics;

var facade = ErgoFacade.Standard;

int N_BENCHMARKS = 10;
var times = new List<string[]>();
var interpreter = ErgoBenchmarks.MeasureInterpreterCreation(facade);
var scope = ErgoBenchmarks.MeasureInterpreterScopeCreation(interpreter.Value);
var kb = ErgoBenchmarks.MeasureKnowledgeBaseCreation(scope.Value);
var vm = ErgoBenchmarks.MeasureSpinUpTime(facade, kb.Value);

for (int i = 0; i < N_BENCHMARKS; i++)
{
    var query_1 = ErgoBenchmarks.MeasureQueryParseTime(scope.Value, "range(0 <= X < 1000000), Y := X * 10");
    var query_1_value = query_1.Value.GetOrThrow();
    var query_1_comp = ErgoBenchmarks.MeasureQueryCompileTime(vm.Value, query_1_value);
    var query_1_exec = ErgoBenchmarks.MeasureQueryExecutionTime(vm.Value, query_1_comp.Value);
    times.Add([query_1.Str, query_1_comp.Str, query_1_exec.Str, query_1_exec.Value.ToString()]);
}

var shell = facade.BuildShell();
shell.WriteTable(["Query Parse", "Query Compile", "Query Execute", "Num Solutions"], [.. times]);


public sealed class ErgoBenchmarks
{
    public static Measured<ErgoInterpreter> MeasureInterpreterCreation(ErgoFacade facade)
    {
        return Measured.Measure(() =>
        {
            return facade.BuildInterpreter();
        });
    }
    public static Measured<InterpreterScope> MeasureInterpreterScopeCreation(ErgoInterpreter interpreter)
    {
        return Measured.Measure(() =>
        {
            return interpreter.CreateScope();
        });
    }
    public static Measured<ErgoKnowledgeBase> MeasureKnowledgeBaseCreation(InterpreterScope scope)
    {
        return Measured.Measure(() =>
        {
            return scope.BuildKnowledgeBase();
        });
    }
    public static Measured<ErgoVM> MeasureSpinUpTime(ErgoFacade facade, ErgoKnowledgeBase kb)
    {
        return Measured.Measure(() =>
        {
            return facade.BuildVM(kb);
        });
    }
    public static Measured<Maybe<Query>> MeasureQueryParseTime(InterpreterScope scope, string str)
    {
        return Measured.Measure(() =>
        {
            return scope.Parse<Query>(str);
        });
    }
    public static Measured<Op> MeasureQueryCompileTime(ErgoVM vm, Query query)
    {
        return Measured.Measure(() =>
        {
            return vm.CompileQuery(query);
        });
    }
    public static Measured<int> MeasureQueryExecutionTime(ErgoVM vm, Op op)
    {
        return Measured.Measure(() =>
        {
            vm.Query = op;
            vm.Run();
            return vm.Solutions.Count();
        });
    }
}
public static class Measured
{
    public static Measured<T> Measure<T>(Func<T> get)
    {
        var sw = new Stopwatch();
        sw.Start();
        var ret = get();
        sw.Stop();
        return new(sw.Elapsed, ret);
    }
}
public readonly record struct Measured<T>(TimeSpan Duration, T Value)
{
    public string Str => Duration.TotalSeconds.ToString("0.0000");
}
