using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using System;
using System.Collections.Generic;

internal class NestedParensClassifier : IClassifier
{
    private readonly IClassificationTypeRegistryService _classificationRegistry;

    internal NestedParensClassifier(IClassificationTypeRegistryService registry)
    {
        _classificationRegistry = registry;
    }

    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
    {
        var classifications = new List<ClassificationSpan>();
        var text = span.GetText();
        int nestingLevel = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                nestingLevel++;
                var classificationType = _classificationRegistry.GetClassificationType($"nesting{nestingLevel}");
                if (classificationType == null)
                    continue;
                classifications.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, new Span(span.Start + i, 1)), classificationType));
            }
            else if (text[i] == ')')
            {
                var classificationType = _classificationRegistry.GetClassificationType($"nesting{nestingLevel}");
                if (classificationType == null)
                    continue;
                classifications.Add(new ClassificationSpan(new SnapshotSpan(span.Snapshot, new Span(span.Start + i, 1)), classificationType));
                nestingLevel = Math.Max(0, nestingLevel - 1); // Ensure nesting level doesn't go negative
            }
        }

        return classifications;
    }

    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
}