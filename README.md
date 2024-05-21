![example workflow](https://github.com/G3Kappa/Ergo/actions/workflows/dotnet.yml/badge.svg)
[![license](https://img.shields.io/badge/License-AGPL-purple.svg)](LICENSE)

## Design Goals
Ergo brings first-order logic to the .NET world through a lightweight and extensible Prolog implementation written entirely in C#. It is a relatively young project, so it's neither ISO-compliant nor stable, but it's been consistently improving over the past few years. 

Its main design goals are to be flexible and customizable, to handle interop with C# seamlessly, and to be efficient enough to be worthwhile as a scripting language in high-demand applications such as games.
Thanks to its versatile syntax and extensible architecture, Ergo can be adapted to any use case and lends itself well to the creation of domain-specific languages. 
Unification allows for very complex pattern-matching, and users can even implement their own parsers for their own *abstract types* that override standard unification, or add their own built-ins.

Ergo already supports several advanced features, including:

- Compilation (Ergo targets a VM -- the ErgoVM)
- Optimization (Custom built-ins can be further optimized at compile time)
- Libraries (C# entry points for various Ergo extensions; linked to Ergo modules)
- Hooks (compiled queries that call specific predicates, one-way bridge between C# and Ergo)
- Tail Call Optimization (for the execution of tail recursive predicates)
- Predicate Expansions (macros/term rewriting)
- Dynamic Predicates (assert/retract)
- Lambdas & Higher-Kinded Predicates
- "Virtual" Predicates (which execute custom VM instructions, a quick and dirty alternative to BuiltIns)
- Tabling (memoization)
- Abstract Terms & Abstract Term Parsers (for custom types implemented on top of canonical terms)
    - Dictionaries (akin to SWI-Prolog)
    - Ordered Sets
    - Lists
    - Tuples (comma-lists)
- Marshalling of CLR objects, **delegates** and **events** to/from Ergo terms (both complex-style and dictionary-style)
- Unbounded Numeric Types (i.e. BigDecimal as the underlying numeric type)
    - In the future, optimizations for integer and float arithmetic could be added, but performance-critical codepaths can be delegated to C#

## Example: Shell
Setting up an Ergo shell is easy:
```csharp
// The "Standard" Ergo Facade is the recommended pre-configured default environment.
// You can extend it, modify it, or start from an empty facade.
var facade = ErgoFacade.Standard;
// You can use the Ergo VM directly or through the Ergo Shell, which provides a
// bunch of useful commands to interact with knowledge bases and module files.
// The Shell manages its own interpreter scope, knowledge base, and VM(s).
var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
{
    ;
}
```
With it, you can start experimenting in seconds! Refer to the wiki for more details.
## Example: Marshalling
This example showcases several advanced features and demonstrates how to bridge Ergo with C# and vice-versa.
```csharp
using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Interpreter.Libraries;
using Ergo.Lang.Compiler;

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
        kb.AssertA(Predicate.Virtual(WellKnown.Modules.User, head, vm =>
        {
            BuiltInNode.SetArgs(head.Arguments)(vm);
            // ITerm.IsClr() can be used to extract raw C# objects contained within atoms.
            // It is not idiomatic to use non-primitive types in such a way, but it is allowed.
            if (vm.Arg(0).IsClr(out string value)) // in this case, we know that our atom will wrap a string, which is a primitive in Ergo
                Console.WriteLine($"D: {value}");
        }));
    if (interpreterScope.Parse<Complex>("message_sent_event(M)")
        .TryGetValue(out head))
        kb.AssertA(Predicate.Virtual(WellKnown.Modules.User, head, vm =>
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
sendMessage("don't read this!");
// Expected output:
// E: hello,
// D: hello,
// E: world!
// D: world!
// D: don't read this!

sealed class EventTest
{
    // When this event fires, a hook will call message_sent_event/1 automatically,
    // by marshalling the string argument into a term, in this case an Atom(string).
    public event Action<string> MessageSent = _ => { };
    // When the combined delegate `sendMessage` is called, a hook will call message_sent_delegate/1 automatically,
    // same as for MessageSent, except that in the case of delegates it works via composition instead of subscription.
    public void SendMessage(string msg)
    {
        MessageSent?.Invoke(msg);
    }
    // When either (virtual) predicate is called, the control will pass to its execution graph, which we implemented as a custom Op.
    // This Op will need to handle the VM's arguments and it is at that point that it can marshall them back into CLR objects.
}
```

## Roadmap
At the time of writing, Ergo is a ~~fully interpreted~~ **partially compiled** toy language with much room for optimization. 

For a rough roadmap, please refer to: https://github.com/users/G3Kappa/projects/1/views/1
