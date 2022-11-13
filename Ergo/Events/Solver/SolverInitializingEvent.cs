using Ergo.Interpreter;
using Ergo.Solver;

namespace Ergo.Events.Solver;

public sealed record class SolverInitializingEvent(ErgoSolver Solver, InterpreterScope Scope) : ErgoEvent;
