// See https://aka.ms/new-console-template for more information
using Ergo.Interpreter;

class ErgoAutoCompleteService
{
    public readonly ErgoInterpreter Ergo;
    public readonly InterpreterScope Scope;

    public ErgoAutoCompleteService(ErgoInterpreter ergo)
    {
        Ergo = ergo;
        Scope = Ergo.CreateScope(x => x
            .WithSearchDirectory(@"ErgoLS\ergo\"));
    }

    public async Task<IReadOnlyCollection<string>> GetPackages(string query)
    {
        return [Scope.Entry.ToString()!];
    }
}
