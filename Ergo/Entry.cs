using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Compiler;

// The "Standard" Ergo Facade is the recommended pre-configured default environment.
// You can extend it, modify it, or start from an empty facade.
// The main way to customize Ergo is to add custom Libraries to your environment.
// Libraries are where you export and scope your BuiltIns and Directives.
var facade = ErgoFacade.Standard;
//   .AddLibrary(() => new MyLibrary());

// The main component is the ErgoInterpreter, which reads source files into a module tree.
var interpreter = facade.BuildInterpreter(InterpreterFlags.Default);
// The "scope" that it creates represents the state of the module tree, which is modified at load time by directives.
var interpreterScope = interpreter.CreateScope();
// From the InterpreterScope we can then create a KnowledgeBase containing all the predicates that were loaded, 
// as well as any that we want to define directly in C#. These are then compiled into ErgoVM.Ops which run on the VM.
var kb = interpreterScope.BuildKnowledgeBase(CompilerFlags.Default, beforeCompile: kb =>
{
    // Predicates can be defined from compiled code (Ops), making them a quick and dirty alternative to BuiltIns.
    // Note that BuiltIns support optimizations, while virtual predicates don't. Regardless, they both allow Ergo
    // code to call arbitrary C# code, acting as a one-way bridge between the two languages. Hooks are the opposite.
    if (interpreterScope.Parse<Complex>("message_sent_delegate(M)")
        .TryGetValue(out var head))
        kb.AssertA(Predicate.FromOp(WellKnown.Modules.User, head, vm =>
        {
            BuiltInNode.SetArgs(head.Arguments)(vm);
            // ITerm.IsClr() can be used to extract raw C# objects contained within atoms.
            // It is not idiomatic to use non-primitive types in such a way, but it is allowed.
            if (vm.Arg(0).IsClr(out string value)) // in this case, we know that our atom will wrap a string, which is a primitive in Ergo
                Console.WriteLine($"D: {value}");
        }));
    if (interpreterScope.Parse<Complex>("message_sent_event(M)")
        .TryGetValue(out head))
        kb.AssertA(Predicate.FromOp(WellKnown.Modules.User, head, vm =>
        {
            BuiltInNode.SetArgs(head.Arguments)(vm);
            // ITerm.Match() is much more powerful because it uses the TermMarshall behind the scenes, which allows for complex pattern matching,
            // as well as custom serialization of C# objects. This is the idiomatic way to share data between the two languages.
            if (vm.Arg(0).Match(out string value)) // in this case, no complex pattern matching is necessary so it is equivalent to calling ITerm.IsClr()
                Console.WriteLine($"E: {value}");
        }));
});
// With a compiled KnowledgeBase, we can now create the VM and run queries on it! 
var vm = facade.BuildVM(kb);
// Ergo supports advanced marshalling to and from C#. Hooks provide a complete example that showcases how
// objects, delegates and events can be marshalled easily. Hooks are essentially precompiled queries, and
// they can be used to call arbitrary Ergo code from C#, acting as a one-way bridge between the two langauges.
var eventTest = new EventTest();
var sendMessage = Hook.MarshallDelegate(eventTest.SendMessage, new Atom("message_sent_delegate"), WellKnown.Modules.User, insertAfterCall: true)(vm);
var messageEvent = typeof(EventTest).GetEvent(nameof(EventTest.MessageSent));
using (var hook = Hook.MarshallEvent(messageEvent, eventTest, new Atom("message_sent_event"), WellKnown.Modules.User)(vm))
{
    // these will call message_event_event/1 as well as message_sent_delegate/1
    sendMessage("hello,");
    sendMessage("world!");
}
// this will only call message_sent_delegate/1
if (sendMessage("don't read this!"))
{
    // Marshalling works transparently on both on Actions and Funcs, as well as instance and static methods.
    // Ergo code can not change the return value of the patched delegates directly, but it can call C# in response to an event. 
    Console.WriteLine("I read this");
}
// Expected output:
// E: hello,
// D: hello,
// E: world!
// D: world!
// D: don't read this!
Console.ReadKey();

// You can use the Ergo VM directly or through the Ergo Shell, which provides a
// bunch of useful commands to interact with knowledge bases and module files.
// The Shell manages its own interpreter scope, knowledge base, and VM(s).
var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
{
    ;
}


sealed class EventTest
{
    // When this event fires, a hook will call message_sent_event/1 automatically,
    // by marshalling the string argument into a term, in this case an Atom(string).
    public event Func<string, bool> MessageSent = msg => msg.Contains("this");
    // When the combined delegate `sendMessage` is called, a hook will call message_sent_delegate/1 automatically,
    // same as for MessageSent, except that in the case of delegates it works via composition instead of subscription.
    public bool SendMessage(string msg)
    {
        return MessageSent.Invoke(msg);
    }
    // When either (virtual) predicate is called, the control will pass to its execution graph, which we implemented as a custom Op.
    // This Op will need to handle the VM's arguments and it is at that point that it can marshall them back into CLR objects.
}