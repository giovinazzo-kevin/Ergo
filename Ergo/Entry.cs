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
var u = mem.Unify(a2, a3);
var x1 = mem.Dereference(a1);
;
mem.LoadState(cleanState);
ErgoVM.Op testOp = ErgoVM.Ops.Setup(vm =>
{
    return mem.StoreTerm(t2);
}, head => vm =>
{
    var t2 = mem.Dereference(head);
});

var testkb = interpreterScope.BuildKnowledgeBase();
var vm = facade.BuildVM(testkb);

vm.Query = testOp;
vm.Run();