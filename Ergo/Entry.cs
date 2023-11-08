using Ergo.Facade;
using Ergo.Lang.Compiler;
using Ergo.Solver;
using Ergo.Solver.DataBindings;

var kb = new KnowledgeBase();
kb.AssertA(new Predicate("", new Atom("user"), new Atom("fiero"), new NTuple(new ITerm[] { new Complex(new Atom("cool"), new Variable("X")) }), false, false, default));
kb.AssertA(new Predicate("", new Atom("user"), new Complex(new Atom("cool"), new Atom("1")), new NTuple(new ITerm[] { }), false, false, default));
var vm = new ErgoVM() { KnowledgeBase = kb };
vm.Query = vm.Goal(new Atom("fiero"));
vm.Run();

using var consoleSink = new DataSink<Person>();
using var feedbackSink = new DataSink<Person>();
var personGenSource = new DataSource<Person>(
    source: () => new PersonGenerator().Generate(),
    functor: new Atom("person_generator"),
    dataSemantics: RejectionData.Discard,
    ctrlSemantics: RejectionControl.Break
);
var feedbackSource = new DataSource<Person>(
    source: () => feedbackSink.Pull(),
    functor: new Atom("person"),
    dataSemantics: RejectionData.Recycle,
    ctrlSemantics: RejectionControl.Continue
);

// The "Standard" Ergo Facade is the recommended pre-configured default environment.
// You can extend it, modify it, or start from an empty facade.
var facade = ErgoFacade.Standard
    .AddDataSink(consoleSink)
    .AddDataSink(feedbackSink)
    .AddDataSource(personGenSource)
    .AddDataSource(feedbackSource)
    ;

var shell = facade.BuildShell();
await foreach (var _ in shell.Repl())
{

    await foreach (var person in consoleSink.Pull())
    {
        Console.WriteLine($"Received:{person}");
    }
}

[Term(Functor = "employee", Marshalling = TermMarshalling.Named)]
public readonly record struct Person(string FirstName, string LastName, string Email, string Phone, string Birthday, string Gender);

public sealed class PersonGenerator
{
    private readonly Random Rng = new();

    private static readonly string[] MaleNames = new[] { "Bob", "John", "Kevin", "Mirko", "Alessio" };
    private static readonly string[] FemaleNames = new[] { "Anna", "Barbara", "Jade", "Nicole", "Alessia" };
    private static readonly string[] LastNames = new[] { "Ross", "Smith", "Giovinazzo", "Messina", "Veneruso" };

    private T Choose<T>(T[] arr) => arr[Rng.Next(arr.Length)];
    private string RandomDigits(int len) => Enumerable.Range(0, len).Select(i => Rng.Next(10)).Join("");

    public async IAsyncEnumerable<Person> Generate()
    {
        while (true)
        {
            var gender = Rng.Next(2) == 0 ? "male" : "female";
            var firstName = gender == "male" ? Choose(MaleNames) : Choose(FemaleNames);
            var lastName = Choose(LastNames);
            var email = Rng.Next(2) == 0 ? null : $"{firstName.ToLower()}.{lastName.ToLower()}@company.com";
            var phone = Rng.Next(2) == 0 ? null : $"+{RandomDigits(2)} {RandomDigits(10)}";
            var birthDate = DateTime.Now.AddYears(-100 + Rng.Next(81)).AddDays(Rng.Next(366 * 10)).ToShortDateString();
            yield return new Person(firstName, lastName, email, phone, birthDate, gender);
            await Task.Delay(100);
        }
    }
}