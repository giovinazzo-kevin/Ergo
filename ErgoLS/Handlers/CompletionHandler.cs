// See https://aka.ms/new-console-template for more information
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

class CompletionHandler : ICompletionHandler
{
    private const string PackageReferenceElement = "PackageReference";
    private const string IncludeAttribute = "Include";
    private const string VersionAttribute = "Version";

    private readonly ILanguageServerFacade _router;
    private readonly BufferManager _bufferManager;
    private readonly ErgoAutoCompleteService _nuGetService;

    private readonly TextDocumentSelector _documentSelector = new TextDocumentSelector(
        new TextDocumentFilter()
        {
            Pattern = "**/*.ergo"
        }
    );

    private CompletionCapability _capability;

    public CompletionHandler(ILanguageServerFacade router, BufferManager bufferManager, ErgoAutoCompleteService nuGetService)
    {
        _router = router;
        _bufferManager = bufferManager;
        _nuGetService = nuGetService;
    }

    public CompletionRegistrationOptions GetRegistrationOptions()
    {
        return new CompletionRegistrationOptions
        {
            DocumentSelector = _documentSelector,
            ResolveProvider = false
        };
    }

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var documentPath = request.TextDocument.Uri.ToString();
        var buffer = _bufferManager.GetBuffer(documentPath);

        if (buffer == null)
        {
            return new CompletionList();
        }

        return new CompletionList();
    }

    private static int GetPosition(string buffer, int line, int col)
    {
        var position = 0;
        for (var i = 0; i < line; i++)
        {
            position = buffer.IndexOf('\n', position) + 1;
        }
        return position + col;
    }

    public void SetCapability(CompletionCapability capability)
    {
        _capability = capability;
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {

        };
}
