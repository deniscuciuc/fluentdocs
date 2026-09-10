using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Models.Charts;

/// <summary>
/// Complete definition of a chart to render.
/// Charts are rendered as images and embedded into documents, spreadsheets, or presentations.
/// </summary>
public sealed record ChartDefinition
{
    public ChartType Type { get; init; } = ChartType.Bar;
    public string? Title { get; init; }
    public IReadOnlyList<ChartDataSeries> Series { get; init; } = [];
    public ChartAxisDefinition? XAxis { get; init; }
    public ChartAxisDefinition? YAxis { get; init; }
    public ChartLegend? Legend { get; init; }
    public ChartStyle? Style { get; init; }
    public int WidthPx { get; init; } = 800;
    public int HeightPx { get; init; } = 600;
}

/// <summary>
/// A single data series within a chart.
/// </summary>
public sealed record ChartDataSeries
{
    public string? Label { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<double> Values { get; init; } = [];
    public string? Color { get; init; }
}

/// <summary>
/// Axis configuration for a chart.
/// </summary>
public sealed record ChartAxisDefinition
{
    public string? Title { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public bool GridLines { get; init; } = true;
    public string? LabelFormat { get; init; }
}

/// <summary>
/// Chart legend configuration.
/// </summary>
public sealed record ChartLegend
{
    public bool Visible { get; init; } = true;
    public LegendPosition Position { get; init; } = LegendPosition.Bottom;
}

/// <summary>
/// Visual style for a chart.
/// </summary>
public sealed record ChartStyle
{
    public string? BackgroundColor { get; init; }
    public string? FontFamily { get; init; }
    public double? FontSizePt { get; init; }
    public IReadOnlyList<string> Palette { get; init; } = [];
}

/// <summary>
/// Options for rendering a chart to an image.
/// </summary>
public sealed record ChartRenderOptions
{
    public ImageFormat OutputFormat { get; init; } = ImageFormat.Png;
    public int Dpi { get; init; } = 150;
    public bool Transparent { get; init; }
}

/// <summary>
/// Result of rendering a chart to an image.
/// </summary>
public sealed record RenderedChart
{
    public required byte[] ImageData { get; init; }
    public ImageFormat Format { get; init; }
    public int WidthPx { get; init; }
    public int HeightPx { get; init; }
}
