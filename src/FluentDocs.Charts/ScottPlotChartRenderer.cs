using System.Text;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;
using Microsoft.Extensions.Logging;
using ScottPlot;
using GenImageFormat = FluentDocs.Abstractions.Enums.ImageFormat;

namespace FluentDocs.Charts;

/// <summary>
/// Renders <see cref="ChartDefinition"/> objects to raster/vector images using ScottPlot.
/// </summary>
public sealed class ScottPlotChartRenderer(ILogger<ScottPlotChartRenderer> logger) : IChartRenderer
{
    public Task<RenderedChart> RenderAsync(
        ChartDefinition chart,
        ChartRenderOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chart);

        options ??= new ChartRenderOptions();

        using var activity = GenerationDiagnostics.StartChartRenderActivity(chart.Type.ToString());

        logger.LogInformation("Rendering {ChartType} chart '{Title}' ({Width}x{Height}px).",
            chart.Type, chart.Title, chart.WidthPx, chart.HeightPx);

        // Plot owns native SkiaSharp surfaces; dispose it when the render completes.
        using var plot = new Plot();

        // Apply style
        if (chart.Style is not null)
        {
            if (chart.Style.BackgroundColor is not null)
                plot.FigureBackground.Color = Color.FromHex(chart.Style.BackgroundColor);
            if (chart.Style.FontFamily is not null)
                plot.Font.Set(chart.Style.FontFamily);
        }

        // Title and axis labels
        if (chart.Title is not null)
            plot.Title(chart.Title);

        if (chart.XAxis is not null)
        {
            if (chart.XAxis.Title is not null)
                plot.XLabel(chart.XAxis.Title);
            if (chart.XAxis.Min.HasValue || chart.XAxis.Max.HasValue)
                plot.Axes.Bottom.Min = chart.XAxis.Min ?? double.NaN;
            if (chart.XAxis.Max.HasValue)
                plot.Axes.Bottom.Max = chart.XAxis.Max.Value;
        }

        if (chart.YAxis is not null)
        {
            if (chart.YAxis.Title is not null)
                plot.YLabel(chart.YAxis.Title);
            if (chart.YAxis.Min.HasValue)
                plot.Axes.Left.Min = chart.YAxis.Min.Value;
            if (chart.YAxis.Max.HasValue)
                plot.Axes.Left.Max = chart.YAxis.Max.Value;
        }

        // Plot data according to chart type
        switch (chart.Type)
        {
            case ChartType.Bar:
                RenderBarChart(plot, chart, true);
                break;
            case ChartType.Column:
                RenderBarChart(plot, chart, false);
                break;
            case ChartType.Line:
                RenderLineChart(plot, chart);
                break;
            case ChartType.Area:
                RenderAreaChart(plot, chart);
                break;
            case ChartType.Pie:
                RenderPieChart(plot, chart, false);
                break;
            case ChartType.Doughnut:
                RenderPieChart(plot, chart, true);
                break;
            case ChartType.Scatter:
                RenderScatterChart(plot, chart);
                break;
            case ChartType.Radar:
                RenderRadarChart(plot, chart);
                break;
        }

        // Configure legend
        if (chart.Legend is { Visible: true })
        {
            var alignment = chart.Legend.Position switch
            {
                LegendPosition.Top => Alignment.UpperCenter,
                LegendPosition.Bottom => Alignment.LowerCenter,
                LegendPosition.Left => Alignment.MiddleLeft,
                LegendPosition.Right => Alignment.MiddleRight,
                _ => Alignment.LowerCenter
            };
            plot.ShowLegend(alignment);
        }
        else
        {
            plot.HideLegend();
        }

        // Render output
        byte[] imageData;
        GenImageFormat outputFormat;

        if (options.OutputFormat == GenImageFormat.Svg)
        {
            var svgXml = plot.GetSvgXml(chart.WidthPx, chart.HeightPx);
            imageData = Encoding.UTF8.GetBytes(svgXml);
            outputFormat = GenImageFormat.Svg;
        }
        else
        {
            imageData = plot.GetImageBytes(chart.WidthPx, chart.HeightPx, ScottPlot.ImageFormat.Png);
            outputFormat = GenImageFormat.Png;
        }

        var result = new RenderedChart
        {
            ImageData = imageData,
            Format = outputFormat,
            WidthPx = chart.WidthPx,
            HeightPx = chart.HeightPx
        };

        return Task.FromResult(result);
    }

    private static void RenderBarChart(Plot plot, ChartDefinition chart, bool horizontal)
    {
        var allBars = new List<Bar>();
        var seriesIndex = 0;

        foreach (var series in chart.Series)
        {
            var color = series.Color is not null
                ? Color.FromHex(series.Color)
                : plot.Add.GetNextColor(true);

            for (var i = 0; i < series.Values.Count; i++)
            {
                var bar = new Bar
                {
                    Position = i + seriesIndex * 0.25,
                    Value = series.Values[i],
                    FillColor = color,
                    Label = series.Label ?? ""
                };
                if (horizontal) bar.Orientation = Orientation.Horizontal;
                allBars.Add(bar);
            }

            seriesIndex++;
        }

        var barPlot = plot.Add.Bars(allBars.ToArray());
        if (horizontal) barPlot.Horizontal = true;

        // Set category tick labels from first series
        if (chart.Series.Count > 0 && chart.Series[0].Categories.Count > 0)
        {
            var positions = chart.Series[0].Categories
                .Select((_, i) => (double)i).ToArray();
            var labels = chart.Series[0].Categories.ToArray();
            if (horizontal)
                plot.Axes.Left.SetTicks(positions, labels);
            else
                plot.Axes.Bottom.SetTicks(positions, labels);
        }
    }

    private static void RenderLineChart(Plot plot, ChartDefinition chart)
    {
        foreach (var series in chart.Series)
        {
            var xs = Enumerable.Range(0, series.Values.Count).Select(i => (double)i).ToArray();
            var ys = series.Values.ToArray();
            var color = series.Color is not null ? Color.FromHex(series.Color) : (Color?)null;
            var scatter = color.HasValue ? plot.Add.Scatter(xs, ys, color.Value) : plot.Add.Scatter(xs, ys);
            scatter.LegendText = series.Label ?? "";
        }

        SetCategoryTicks(plot, chart);
    }

    private static void RenderAreaChart(Plot plot, ChartDefinition chart)
    {
        foreach (var series in chart.Series)
        {
            var xs = Enumerable.Range(0, series.Values.Count).Select(i => (double)i).ToArray();
            var ys = series.Values.ToArray();
            var zeros = new double[ys.Length];
            var fillY = plot.Add.FillY(xs, ys, zeros);
            if (series.Color is not null)
                fillY.FillStyle.Color = Color.FromHex(series.Color);
            fillY.LegendText = series.Label ?? "";
        }

        SetCategoryTicks(plot, chart);
    }

    private static void RenderPieChart(Plot plot, ChartDefinition chart, bool donut)
    {
        var slices = new List<PieSlice>();

        foreach (var series in chart.Series)
            for (var i = 0; i < series.Values.Count; i++)
            {
                var label = i < series.Categories.Count ? series.Categories[i] : $"Slice {i + 1}";
                var color = series.Color is not null
                    ? Color.FromHex(series.Color)
                    : plot.Add.GetNextColor(true);
                slices.Add(new PieSlice(series.Values[i], color, label));
            }

        var pie = plot.Add.Pie(slices);
        if (donut)
            pie.DonutFraction = 0.5;
    }

    private static void RenderScatterChart(Plot plot, ChartDefinition chart)
    {
        foreach (var series in chart.Series)
        {
            var xs = Enumerable.Range(0, series.Values.Count).Select(i => (double)i).ToArray();
            var ys = series.Values.ToArray();
            var color = series.Color is not null ? Color.FromHex(series.Color) : (Color?)null;
            var sp = color.HasValue ? plot.Add.ScatterPoints(xs, ys, color.Value) : plot.Add.ScatterPoints(xs, ys);
            sp.LegendText = series.Label ?? "";
        }

        SetCategoryTicks(plot, chart);
    }

    private static void RenderRadarChart(Plot plot, ChartDefinition chart)
    {
        var seriesData = chart.Series.Select(s => s.Values.AsEnumerable()).ToList();
        var radar = plot.Add.Radar(seriesData);

        for (var i = 0; i < chart.Series.Count; i++)
            if (i < radar.Series.Count)
            {
                radar.Series[i].LegendText = chart.Series[i].Label ?? "";
                if (chart.Series[i].Color is not null)
                    radar.Series[i].FillColor = Color.FromHex(chart.Series[i].Color!).WithAlpha(100);
            }
    }

    private static void SetCategoryTicks(Plot plot, ChartDefinition chart)
    {
        if (chart.Series.Count > 0 && chart.Series[0].Categories.Count > 0)
        {
            var positions = chart.Series[0].Categories
                .Select((_, i) => (double)i).ToArray();
            var labels = chart.Series[0].Categories.ToArray();
            plot.Axes.Bottom.SetTicks(positions, labels);
        }
    }
}
