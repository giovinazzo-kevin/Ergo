using System.Diagnostics;

namespace Ergo.Lang.Utils;

public sealed class DiagnosticProbe : IDisposable
{
    private record struct Datum(int Hits, int Leaves, int Recursion, TimeSpan TotalTime, TimeSpan AverageTime, ImmutableDictionary<string, int> Counters);
    private Dictionary<string, Datum> _data = new();

    public string GetCurrentMethodName([CallerMemberName] string callerName = "") => callerName;
    public Stopwatch Enter([CallerMemberName] string callerName = "")
    {
        var sw = new Stopwatch();
        sw.Start();
        if (!_data.TryGetValue(callerName, out var datum))
            _data[callerName] = datum = new(0, 0, 0, default, default, ImmutableDictionary.Create<string, int>());
        _data[callerName] = datum with { Hits = datum.Hits + 1 };
        return sw;
    }

    public void Leave(Stopwatch sw, [CallerMemberName] string callerName = "")
    {
        if (_data.TryGetValue(callerName, out var datum))
        {
            _data[callerName] = datum with { Leaves = datum.Leaves + 1, TotalTime = sw.Elapsed + datum.TotalTime, AverageTime = datum.TotalTime / datum.Hits };
            sw.Stop();
        }
        else throw new InvalidOperationException(callerName);
    }

    public void Count(string counter, int amount = 1, [CallerMemberName] string callerName = "")
    {
        if (_data.TryGetValue(callerName, out var datum))
        {
            if (datum.Counters.TryGetValue(counter, out var oldAmount))
                amount += oldAmount;
            _data[callerName] = datum with { Counters = datum.Counters.SetItem(counter, amount) };
        }
        else throw new InvalidOperationException(callerName);
    }

    public void Dispose()
    {
        if (_data.FirstOrDefault(d => d.Value.Hits != d.Value.Leaves || d.Value.Recursion != 0) is { Key: { } } item)
            throw new InternalErgoException($"{item.Key}: {item.Value}");
    }

    public string GetDiagnostics()
    {
        var totalTime = _data.Values.Sum(x => x.TotalTime.TotalMilliseconds);
        return _data
            .Select(kv => (kv.Key, kv.Value, SelfTimePct: (float)(kv.Value.TotalTime.TotalMilliseconds / totalTime) * 100))
            .OrderBy(kv => kv.SelfTimePct)
            .Select(x => $"{x.Key,20}: HIT={x.Value.Hits:00000} TOT={x.Value.TotalTime.TotalMilliseconds:00000.000000} AVG={x.Value.AverageTime.TotalMilliseconds:00000.000000} SLF={x.SelfTimePct:000.00}%{(x.Value.Counters.Any() ? $"\r\n\t{{ {x.Value.Counters.Select(kv => $"{kv.Key}={kv.Value:00000}").Join()} }}" : "")}")
            .Join("\r\n") + "\r\n";
    }
}
