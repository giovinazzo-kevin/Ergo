namespace Ergo.Debugger;

public interface IErgoDebugger
{
    event EventHandler Resumed;
    event EventHandler Paused;
    event EventHandler ErrorOccurred;
    void Pause();
    void Resume();
    void SetBreakpoint(string location);
    void RemoveBreakpoint(string location);
    object Evaluate(string expression);
    void SetValue(string variableName, object value);
    IEnumerable<string> GetLocalVariables();
}