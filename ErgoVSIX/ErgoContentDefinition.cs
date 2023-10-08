using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace ErgoVSIX
{
    public class ErgoContentDefinition
    {
        [Export]
        [Name("ergo")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition ErgoContentTypeDefinition;

        [Export]
        [FileExtension(".ergo")]
        [ContentType("ergo")]
        internal static FileExtensionToContentTypeDefinition ErgoFileExtensionDefinition;
    }
}
