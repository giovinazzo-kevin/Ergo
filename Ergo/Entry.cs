using Ergo.Lang;
using System;
using System.Linq;
using System.Text;

var interpreter = new Interpreter();
interpreter.TryAddBuiltIn(new MyBuiltIn());
var shell = new Shell(interpreter);
shell.EnterRepl();
