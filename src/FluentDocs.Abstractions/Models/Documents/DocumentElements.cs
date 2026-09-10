using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions.Models.Documents;

/// <summary>
/// Marker interface for all elements that can appear in a document section.
/// </summary>
public interface IDocumentElement;

/// <summary>
/// A heading element (H1–H6).
/// </summary>
public sealed record HeadingElement : IDocumentElement
{
    public required string Text { get; init; }
    public HeadingLevel Level { get; init; } = HeadingLevel.H1;
    public TextStyle? Style { get; init; }
}

/// <summary>
/// A paragraph containing one or more styled text runs.
/// </summary>
public sealed record ParagraphElement : IDocumentElement
{
    public IReadOnlyList<TextRun> Runs { get; init; } = [];
    public ParagraphStyle? Style { get; init; }
}

/// <summary>
/// An embedded image within a document.
/// </summary>
public sealed record ImageElement : IDocumentElement
{
    public required ImageContent Image { get; init; }
}

/// <summary>
/// An inline table within a document.
/// </summary>
public sealed record TableContentElement : IDocumentElement
{
    public IReadOnlyList<TableColumn> Columns { get; init; } = [];
    public IReadOnlyList<TableRow> Rows { get; init; } = [];
    public BorderSet? Borders { get; init; }
    public TextStyle? HeaderStyle { get; init; }
    public TextStyle? CellStyle { get; init; }
}

/// <summary>Column definition for an inline document table.</summary>
public sealed record TableColumn
{
    public string? Header { get; init; }
    public double? WidthMm { get; init; }
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;
}

/// <summary>Row in an inline document table.</summary>
public sealed record TableRow
{
    public IReadOnlyList<TableCell> Cells { get; init; } = [];
}

/// <summary>Cell in an inline document table.</summary>
public sealed record TableCell
{
    public string? Text { get; init; }
    public IReadOnlyList<TextRun>? Runs { get; init; }
    public TextStyle? Style { get; init; }
}

/// <summary>
/// An embedded chart within a document, rendered as an image.
/// </summary>
public sealed record ChartElement : IDocumentElement
{
    public required ChartDefinition Chart { get; init; }
}

/// <summary>
/// A page break element.
/// </summary>
public sealed record PageBreakElement : IDocumentElement;

/// <summary>
/// A horizontal rule / separator line.
/// </summary>
public sealed record HorizontalRuleElement : IDocumentElement;

/// <summary>
/// An ordered or unordered list.
/// </summary>
public sealed record ListElement : IDocumentElement
{
    public bool Ordered { get; init; }
    public IReadOnlyList<ListItem> Items { get; init; } = [];
}

/// <summary>
/// A single list item, optionally with nested sub-items.
/// </summary>
public sealed record ListItem
{
    public IReadOnlyList<TextRun> Content { get; init; } = [];
    public IReadOnlyList<ListItem> Nested { get; init; } = [];
}

/// <summary>
/// A code block with optional language for syntax highlighting.
/// </summary>
public sealed record CodeBlockElement : IDocumentElement
{
    public required string Code { get; init; }
    public string? Language { get; init; }
    public TextStyle? Style { get; init; }
}

/// <summary>
/// A block quote element.
/// </summary>
public sealed record BlockQuoteElement : IDocumentElement
{
    public IReadOnlyList<TextRun> Content { get; init; } = [];
    public ParagraphStyle? Style { get; init; }
}
