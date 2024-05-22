using Ergo.Facade;
using Ergo.Interpreter;
using Ergo.Lang.Compiler;

var facade = ErgoFacade.Standard;
var interpreter = facade.BuildInterpreter(InterpreterFlags.Default);
var interpreterScope = interpreter.CreateScope();

var mem = new TermMemory();
var t1 = interpreterScope.Parse<ITerm>("A").GetOrThrow();
var t2 = interpreterScope.Parse<ITerm>("complex(a, A, c)").GetOrThrow();
var t3 = interpreterScope.Parse<ITerm>("complex(a, f(x, y), c)").GetOrThrow();
var a1 = mem.StoreTerm(t1);
var a2 = mem.StoreTerm(t2);
var a3 = mem.StoreTerm(t3);
var cleanState = mem.SaveState();
var u = mem.Unify(a2, a3, transaction: false);
var x1 = mem.Dereference(a1);
mem.LoadState(cleanState);
var testkb = interpreterScope.BuildKnowledgeBase();
var vm = facade.BuildVM(testkb);

vm.Query = vm.CompileQuery(interpreterScope.Parse<Query>("A = fiero{a:B}, B = 1").GetOrThrow());
vm.Run();

foreach (var sol in vm.Solutions)
{
    Console.WriteLine(sol.Substitutions.Select(x => x.Explain()).Join());
}