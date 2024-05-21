using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Runtime;
using System.Diagnostics;

var facade = ErgoFacade.Standard;

int N_BENCHMARKS = 100;
var times = new List<string[]>();

for (int i = 0; i < N_BENCHMARKS; i++)
{
    var interpreter = ErgoBenchmarks.MeasureInterpreterCreation(facade);
    var scope = ErgoBenchmarks.MeasureInterpreterScopeCreation(interpreter.Value);
    var kb = ErgoBenchmarks.MeasureKnowledgeBaseCreation(scope.Value);
    var vm = ErgoBenchmarks.MeasureSpinUpTime(facade, kb.Value);
    times.Add([interpreter.Str, scope.Str, kb.Str, vm.Str]);
}

var shell = facade.BuildShell();
shell.WriteTable(["Interpreter", "Int. Scope", "KnowledgeBase", "VM"], times.ToArray());


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
    public static Measured<KnowledgeBase> MeasureKnowledgeBaseCreation(InterpreterScope scope)
    {
        return Measured.Measure(() =>
        {
            return scope.BuildKnowledgeBase();
        });
    }
    public static Measured<ErgoVM> MeasureSpinUpTime(ErgoFacade facade, KnowledgeBase kb)
    {
        return Measured.Measure(() =>
        {
            return facade.BuildVM(kb);
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
