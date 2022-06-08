using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
    // interpreter.TryAddDynamicPredicate lets you add native Ergo predicates to the interpreter programmatically
    // interpreter.AddDataSource lets you bind a C# IEnumerable/IAsyncEnumerable to a virtual Ergo predicate
    interpreter.AddDataSource(new DataSource<Person>(new PersonGenerator().Generate()), Maybe<Atom>.None);

}, solver =>
{
    // solver.TryAddBuiltIn lets you write built-in predicates in C#
});
// shell.TryAddCommand lets you extend the command shell

var scope = shell.CreateScope();
await foreach (var _ in shell.EnterRepl(scope)) ;

[Term(Functor = "employee", Marshalling = TermMarshalling.Named)]
public readonly record struct Person(string FirstName, string LastName, string Email, string Phone, string Birthday, string Gender);

public sealed class PersonGenerator
{
    public async IAsyncEnumerable<Person> Generate()
    {
        yield return new Person("Test", "Last", "test@test.com", "1234567890", "2022-02-02", "male");
        await foreach (var p in Generate()) { yield return p; }
    }
}