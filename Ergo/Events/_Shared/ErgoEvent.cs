namespace Ergo.Events;

public abstract class ErgoEvent
{
    public object Sender { get; protected set; }
    public object Arg { get; set; }
    public ErgoEvent(object sender, object arg)
    {
        Sender = sender;
        Arg = arg;
    }
}
