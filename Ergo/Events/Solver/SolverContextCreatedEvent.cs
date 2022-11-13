using Ergo.Solver;

namespace Ergo.Events.Solver;

public class SolverContextCreatedEvent : ErgoEvent
{
    public SolverContextCreatedEvent(SolverContext sender) : base(sender, null) { }
    public new SolverContext Sender { get => (SolverContext)base.Sender; }
    public new SolverContext Arg { get => (SolverContext)base.Sender; }
}
