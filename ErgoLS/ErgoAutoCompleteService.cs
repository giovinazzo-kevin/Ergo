// See https://aka.ms/new-console-template for more information
using Ergo.Interpreter;

class ErgoAutoCompleteService
{
    private readonly ErgoInterpreter Ergo;
    private readonly InterpreterScope Scope;

    public ErgoAutoCompleteService(ErgoInterpreter ergo)
    {
        Ergo = ergo;
        Scope = Ergo.CreateScope();
    }

    public async Task<IReadOnlyCollection<string>> GetPackages(string query)
    {
        return new[] { Scope.Entry.ToString()! };
    }
}
