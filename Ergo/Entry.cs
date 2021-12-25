using Ergo.Lang;
using Ergo.Interpreter;
using Ergo.Shell;

var interpreter = new ErgoInterpreter();
var shell = new ErgoShell(interpreter);
var scope = shell.CreateScope();
shell.EnterRepl(scope);
