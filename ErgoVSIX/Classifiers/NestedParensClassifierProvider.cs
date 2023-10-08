using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using static Classifications;

[Export(typeof(IClassifierProvider))]
[ContentType("ergo")]
internal class NestedParensClassifierProvider : IClassifierProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationRegistry; // Obtain reference to classification registry

    [Import]
    [Name(Nesting1)]
    internal Nesting1Format Nesting1Type = null;

    public IClassifier GetClassifier(ITextBuffer buffer)
    {
        return buffer.Properties.GetOrCreateSingletonProperty(() => new NestedParensClassifier(ClassificationRegistry));
    }
}
