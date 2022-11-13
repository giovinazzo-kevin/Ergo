using Ergo.Solver;

namespace Ergo.Events.Solver;

public class SolverContextDisposedEvent : ErgoEvent
{
    public SolverContextDisposedEvent(SolverContext sender) : base(sender, null) { }
    public new SolverContext Sender { get => (SolverContext)base.Sender; }
    public new SolverContext Arg { get => (SolverContext)base.Sender; }
}
