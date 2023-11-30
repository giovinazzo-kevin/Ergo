using Ergo.Facade;

// The "Standard" Ergo Facade is the recommended pre-configured default environment.
// You can extend it, modify it, or start from an empty facade.
var facade = ErgoFacade.Standard;
var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
{
    ;
}