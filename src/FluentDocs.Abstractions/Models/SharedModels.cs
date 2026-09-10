using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Models;

/// <summary>
/// Styling for a run of inline text (font, size, color, emphasis).
/// </summary>
public sealed record TextStyle
{
    public string? FontFamily { get; init; }
    public double? FontSizePt { get; init; }
    public string? Color { get; init; }
    public string? BackgroundColor { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strikethrough { get; init; }
    public bool Superscript { get; init; }
    public bool Subscript { get; init; }
}

/// <summary>
/// A run of text with optional inline styling.
/// </summary>
public sealed record TextRun
{
    public required string Text { get; init; }
    public TextStyle? Style { get; init; }
}

/// <summary>
/// Paragraph-level style (alignment, spacing, indentation).
/// </summary>
public sealed record ParagraphStyle
{
    public TextAlignment Alignment { get; init; } = TextAlignment.Left;
    public double? LineSpacing { get; init; }
    public double? SpaceBeforePt { get; init; }
    public double? SpaceAfterPt { get; init; }
    public double? IndentMm { get; init; }
}

/// <summary>
/// Page or element margins in millimetres.
/// </summary>
public sealed record Margins
{
    public double TopMm { get; init; }
    public double RightMm { get; init; }
    public double BottomMm { get; init; }
    public double LeftMm { get; init; }

    public static Margins Default => new() { TopMm = 25.4, RightMm = 25.4, BottomMm = 25.4, LeftMm = 25.4 };
    public static Margins Narrow => new() { TopMm = 12.7, RightMm = 12.7, BottomMm = 12.7, LeftMm = 12.7 };
    public static Margins Wide => new() { TopMm = 25.4, RightMm = 50.8, BottomMm = 25.4, LeftMm = 50.8 };
}

/// <summary>
/// Position of an element on a surface (slide, page) in millimetres.
/// </summary>
public sealed record Position
{
    public double XMm { get; init; }
    public double YMm { get; init; }
}

/// <summary>
/// Dimensions of an element in millimetres.
/// </summary>
public sealed record ElementSize
{
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }
}

/// <summary>
/// Border definition for a single side or uniform border.
/// </summary>
public sealed record BorderDefinition
{
    public BorderStyle Style { get; init; } = BorderStyle.None;
    public string? Color { get; init; }
    public double? WidthPt { get; init; }
}

/// <summary>
/// Four-side border specification.
/// </summary>
public sealed record BorderSet
{
    public BorderDefinition? Top { get; init; }
    public BorderDefinition? Right { get; init; }
    public BorderDefinition? Bottom { get; init; }
    public BorderDefinition? Left { get; init; }

    /// <summary>Creates a uniform border on all four sides.</summary>
    public static BorderSet Uniform(BorderDefinition border)
    {
        return new BorderSet { Top = border, Right = border, Bottom = border, Left = border };
    }
}

/// <summary>
/// Embedded image content with metadata.
/// </summary>
public sealed record ImageContent
{
    public required byte[] Data { get; init; }
    public ImageFormat Format { get; init; } = ImageFormat.Png;
    public string? AltText { get; init; }
    public string? Caption { get; init; }
    public double? WidthMm { get; init; }
    public double? HeightMm { get; init; }
    public bool MaintainAspectRatio { get; init; } = true;
    public string? AnchorCell { get; init; }
    public int OffsetXPx { get; init; }
    public int OffsetYPx { get; init; }
}
