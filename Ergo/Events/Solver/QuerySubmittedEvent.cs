using Ergo.Solver;

namespace Ergo.Events.Solver;

public record class QuerySubmittedEvent(ErgoSolver Solver, Query Query, SolverScope Scope) : ErgoEvent;