using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Models.Documents;

/// <summary>
/// Complete definition of a document to generate (DOCX, PDF, Markdown, PlainText).
/// </summary>
public sealed record DocumentDefinition : FileDefinition
{
    public DocumentMetadata Metadata { get; init; } = new();
    public PageSetup PageSetup { get; init; } = new();
    public DocumentStyles DefaultStyles { get; init; } = new();
    public IReadOnlyList<DocumentSection> Sections { get; init; } = [];
    public HeaderFooterDefinition? Header { get; init; }
    public HeaderFooterDefinition? Footer { get; init; }
}

/// <summary>
/// Document metadata (title, author, keywords, etc.).
/// </summary>
public sealed record DocumentMetadata
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Keywords { get; init; } = [];
    public DateTimeOffset? CreatedAt { get; init; }
    public string? Language { get; init; }
}

/// <summary>
/// Page setup: size, orientation, margins.
/// </summary>
public sealed record PageSetup
{
    public PageSize PageSize { get; init; } = PageSize.A4;
    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;
    public Margins Margins { get; init; } = Margins.Default;
    public double? CustomWidthMm { get; init; }
    public double? CustomHeightMm { get; init; }
}

/// <summary>
/// Default styles applied across the entire document.
/// </summary>
public sealed record DocumentStyles
{
    public TextStyle? DefaultTextStyle { get; init; }
    public ParagraphStyle? DefaultParagraphStyle { get; init; }

    public IReadOnlyDictionary<HeadingLevel, TextStyle> HeadingStyles { get; init; } =
        new Dictionary<HeadingLevel, TextStyle>();
}

/// <summary>
/// A logical section within a document, containing ordered elements.
/// </summary>
public sealed record DocumentSection
{
    public IReadOnlyList<IDocumentElement> Elements { get; init; } = [];
}

/// <summary>
/// Header or footer definition.
/// </summary>
public sealed record HeaderFooterDefinition
{
    public IReadOnlyList<TextRun> Content { get; init; } = [];
    public bool ShowPageNumber { get; init; }
    public string? PageNumberFormat { get; init; }
}
