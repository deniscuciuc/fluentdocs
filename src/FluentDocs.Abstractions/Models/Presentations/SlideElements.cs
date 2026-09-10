using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions.Models.Presentations;

/// <summary>
/// Marker interface for all elements placeable on a slide.
/// </summary>
public interface ISlideElement
{
    /// <summary>Element position on the slide.</summary>
    Position Position { get; }

    /// <summary>Element dimensions.</summary>
    ElementSize Size { get; }

    /// <summary>Optional entrance/emphasis animation.</summary>
    ElementAnimation? Animation { get; }
}

/// <summary>
/// Animation configuration for a slide element.
/// </summary>
public sealed record ElementAnimation
{
    public AnimationType Type { get; init; } = AnimationType.Appear;
    public int DelayMs { get; init; }
    public int DurationMs { get; init; } = 500;
    public AnimationTrigger Trigger { get; init; } = AnimationTrigger.OnClick;
}

/// <summary>
/// A text box on a slide.
/// </summary>
public sealed record TextBoxElement : ISlideElement
{
    public IReadOnlyList<TextRun> Runs { get; init; } = [];
    public required Position Position { get; init; }
    public required ElementSize Size { get; init; }
    public ElementAnimation? Animation { get; init; }
    public TextBoxStyle? Style { get; init; }
}

/// <summary>
/// Styling for a text box container.
/// </summary>
public sealed record TextBoxStyle
{
    public string? BackgroundColor { get; init; }
    public BorderDefinition? Border { get; init; }
    public Margins? Padding { get; init; }
    public TextAlignment TextAlignment { get; init; } = TextAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; init; } = VerticalAlignment.Top;
}

/// <summary>
/// An image placed on a slide.
/// </summary>
public sealed record SlideImageElement : ISlideElement
{
    public required ImageContent Image { get; init; }
    public required Position Position { get; init; }
    public required ElementSize Size { get; init; }
    public ElementAnimation? Animation { get; init; }
}

/// <summary>
/// A chart placed on a slide, rendered as an embedded image.
/// </summary>
public sealed record SlideChartElement : ISlideElement
{
    public required ChartDefinition Chart { get; init; }
    public required Position Position { get; init; }
    public required ElementSize Size { get; init; }
    public ElementAnimation? Animation { get; init; }
}

/// <summary>
/// A table placed on a slide.
/// </summary>
public sealed record SlideTableElement : ISlideElement
{
    public IReadOnlyList<Documents.TableColumn> Columns { get; init; } = [];
    public IReadOnlyList<Documents.TableRow> Rows { get; init; } = [];
    public required Position Position { get; init; }
    public required ElementSize Size { get; init; }
    public ElementAnimation? Animation { get; init; }
    public BorderSet? Borders { get; init; }
    public TextStyle? HeaderStyle { get; init; }
    public TextStyle? CellStyle { get; init; }
}

/// <summary>
/// A geometric shape placed on a slide.
/// </summary>
public sealed record ShapeElement : ISlideElement
{
    public ShapeType ShapeType { get; init; } = ShapeType.Rectangle;
    public required Position Position { get; init; }
    public required ElementSize Size { get; init; }
    public ElementAnimation? Animation { get; init; }
    public ShapeStyle? Style { get; init; }
    public string? Text { get; init; }
    public TextStyle? TextStyle { get; init; }
}

/// <summary>
/// Styling for geometric shapes.
/// </summary>
public sealed record ShapeStyle
{
    public string? FillColor { get; init; }
    public BorderDefinition? Border { get; init; }
    public double Opacity { get; init; } = 1.0;
    public double RotationDegrees { get; init; }
}
