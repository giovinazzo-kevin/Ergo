// See https://aka.ms/new-console-template for more information
class ErgoAutoCompleteService
{
    private HttpClient _client = new HttpClient();

    public async Task<IReadOnlyCollection<string>> GetPackages(string query)
    {
        return new[] { "we" };
    }
}
