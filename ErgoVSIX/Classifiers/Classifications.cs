using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows.Media;

public static class Classifications
{
    // These are the strings that will be used to form the classification types
    // and bind those types to formats
    public const string Nesting = "nesting";
    public const string Nesting1 = "nesting1";


    // These MEF exports define the types themselves
    [Export]
    [Name(Nesting1)]
    [ContentType("ergo")]
    private static Nesting1Format Nesting1Type = null;

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = Nesting1)]
    [Name(Nesting1)]
    internal sealed class Nesting1Format : ClassificationFormatDefinition
    {
        public Nesting1Format()
        {
            DisplayName = "Nesting Level 1";
            ForegroundColor = Colors.Red;

        }
    }
}
