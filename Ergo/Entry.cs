using Ergo.Facade;


/*
 1. Compile predicate:
Predicate -> PredicateAddress
 
 
 
 */

var facade = ErgoFacade.Standard;
var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
    ;
