using Ergo.Solver;

namespace Ergo.Events.Solver;
public sealed record class SolverContextDisposedEvent(SolverContext Context) : ErgoEvent;
