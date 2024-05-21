using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Interpreter.Libraries;

// The "Standard" Ergo Facade is the recommended pre-configured default environment.
// You can extend it, modify it, or start from an empty facade.
var facade = ErgoFacade.Standard;

var interpreter = facade.BuildInterpreter(InterpreterFlags.Default);
var interpreterScope = interpreter.CreateScope();

var kb = interpreterScope.BuildKnowledgeBase(CompilerFlags.Default, beforeCompile: kb =>
{
    if (interpreterScope.Parse<Predicate>("message_sent(M) :- write('User: ', M), nl.")
        .TryGetValue(out var pred))
        kb.AssertA(pred);
});
var vm = facade.BuildVM(kb);

var eventTest = new EventTest();
using var hook = Hook.MarshallEvent(typeof(EventTest).GetEvent(nameof(EventTest.MessageSent)), eventTest, new Atom("message_sent"), WellKnown.Modules.User)(vm);
eventTest.SendMessage("hello,");
eventTest.SendMessage("world!");

var del = Hook.MarshallDelegate<Action<string>>((s) => { }, new Atom("message_sent"), WellKnown.Modules.User)(vm);
del("also hello,");
del("world!!!!!!");

// You can use the Ergo VM directly or through the Ergo Shell, which provides a
// bunch of useful commands to interact with knowledge bases and module files.
//var shell = facade.BuildShell();

//await foreach (var _ in shell.Repl())
//{
//    ;
//}


public class EventTest
{
    public event Action<string> MessageSent = _ => { };
    public void SendMessage(string msg)
    {
        MessageSent?.Invoke(msg);
    }
}