// See https://aka.ms/new-console-template for more information
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

class CompletionHandler : ICompletionHandler
{
    private readonly ILanguageServerFacade _router;
    private readonly BufferManager _bufferManager;
    private readonly ErgoAutoCompleteService _autoComplete;

    private readonly TextDocumentSelector _documentSelector = new TextDocumentSelector(
        new TextDocumentFilter()
        {
            Pattern = "**/*.ergo"
        }
    );


    public CompletionHandler(ILanguageServerFacade router, BufferManager bufferManager, ErgoAutoCompleteService autocomplete)
    {
        _router = router;
        _bufferManager = bufferManager;
        _autoComplete = autocomplete;
    }

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        var documentPath = request.TextDocument.Uri.ToString();
        var buffer = _bufferManager.GetBuffer(documentPath);

        if (buffer == null)
        {
            return new CompletionList();
        }
        return new CompletionList(new CompletionItem()
        {
            Label = "test",
            InsertText = "test"
        });
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = _documentSelector,
            ResolveProvider = false
        };
}
