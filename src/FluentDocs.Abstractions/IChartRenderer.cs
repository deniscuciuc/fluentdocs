using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions;

/// <summary>
/// Renders a <see cref="ChartDefinition"/> to a raster or vector image.
/// Used internally by file generators to embed charts in documents, spreadsheets, and presentations.
/// </summary>
public interface IChartRenderer
{
    /// <summary>
    /// Renders the chart definition to an image.
    /// </summary>
    /// <param name="chart">The chart to render.</param>
    /// <param name="options">Optional render options (format, DPI, transparency).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The rendered chart image data.</returns>
    Task<RenderedChart> RenderAsync(
        ChartDefinition chart,
        ChartRenderOptions? options = null,
        CancellationToken ct = default);
}
