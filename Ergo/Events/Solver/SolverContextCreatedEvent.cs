using Ergo.Solver;

namespace Ergo.Events.Solver;

public sealed record class SolverContextCreatedEvent(SolverContext Context) : ErgoEvent;
