using Ergo.Interpreter;
using Ergo.Lang;
using Ergo.Lang.Ast;
using Ergo.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

using var sink = new DataSink<Person>();
var shell = new ErgoShell(interpreter =>
{
    // interpreter.TryAddDirective lets you extend the interpreter
    // interpreter.TryAddDynamicPredicate lets you add native Ergo predicates to the interpreter programmatically

}, solver =>
{
    // solver.TryAddBuiltIn lets you extend the solver
    // solver.BindDataSource lets you pull objects from a C# IEnumerable/IAsyncEnumerable to Ergo
    solver.BindDataSource(new DataSource<Person>(() => new PersonGenerator().Generate()));
    // solver.BindDataSink lets you push terms from Ergo to a C# IAsyncEnumerable/Event
    solver.BindDataSink(sink);
});
// shell.TryAddCommand lets you extend the shell

var scope = shell.CreateScope();
await foreach (var newScope in shell.Repl(scope))
{
    await foreach(var person in sink.Pull())
    {
        Console.WriteLine($"\r\n\tReceived:{person}");
    }
}

[Term(Functor = "employee", Marshalling = TermMarshalling.Positional)]
public readonly record struct Person(string FirstName, string LastName, string Email, string Phone, string Birthday, string Gender);

public sealed class PersonGenerator
{
    private readonly Random Rng = new();

    private static readonly string[] MaleNames = new[] { "Bob", "John", "Kevin", "Mirko", "Alessio" };
    private static readonly string[] FemaleNames = new[] { "Anna", "Barbara", "Jade", "Nicole", "Alessia" };
    private static readonly string[] LastNames = new[] { "Ross", "Smith", "Giovinazzo", "Messina", "Veneruso" };

    private T Choose<T>(T[] arr) => arr[Rng.Next(arr.Length)];
    private string RandomDigits(int len) => String.Join("", Enumerable.Range(0, len).Select(i => Rng.Next(10)));

    public async IAsyncEnumerable<Person> Generate()
    {
        var gender = Rng.Next(2) == 0 ? "male" : "female";
        var firstName = gender == "male" ? Choose(MaleNames) : Choose(FemaleNames);
        var lastName = Choose(LastNames);
        var email = $"{firstName.ToLower()}.{lastName.ToLower()}@company.com";
        var phone = $"+{RandomDigits(2)} {RandomDigits(10)}";
        var birthDate = DateTime.Now.AddYears(-100 + Rng.Next(81)).AddDays(Rng.Next(366 * 10)).ToShortDateString();
        yield return new Person(
            firstName,
            lastName,
            email, 
            phone, 
            birthDate, 
            gender
        );
        await foreach (var p in Generate()) { yield return p; }
    }
}