using Ergo.Facade;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design;

var facade = ErgoFacade.Standard;
var shell = facade.BuildShell();

await foreach (var _ in shell.Repl())
    ;
