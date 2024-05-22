using Ergo.Facade;

var facade = ErgoFacade.Standard;
var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
    ;
