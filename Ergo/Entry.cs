using Ergo.Lang;
using System;
using System.Linq;
using System.Text;

var interpreter = new Interpreter();
var shell = new Shell(interpreter);
shell.EnterRepl();
