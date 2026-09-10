using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Testing;

namespace FluentDocs.IntegrationTests;

/// <summary>
/// Integration tests for the <see cref="IChartRenderer"/> — renders real charts
/// using ScottPlot and saves the output to disk.
/// </summary>
public sealed class ChartRenderingIntegrationTests : IDisposable
{
    private readonly FluentDocsTestHost _host = FluentDocsTestHost.Create();

    [Fact]
    public async Task RenderAsync_BarChart_ProducesPng()
    {
        var renderer = _host.ResolveChartRenderer();
        var chart = SampleDefinitions.SimpleBarChart();

        var result = await renderer.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
        Assert.True(result.ImageData.Length > 100);
        Assert.Equal(ImageFormat.Png, result.Format);
        // PNG magic bytes
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, result.ImageData[..4]);

        await FluentDocsOutputWriter.SaveChartAsync(result, "bar-chart");
    }

    [Theory]
    [MemberData(nameof(AllChartTypeData))]
    public async Task RenderAsync_AllChartTypes_ProduceOutput(ChartType type, ChartDefinition definition)
    {
        var renderer = _host.ResolveChartRenderer();

        var result = await renderer.RenderAsync(definition);

        Assert.True(result.ImageData.Length > 0, $"chart type '{type}' should produce output");
        Assert.True(result.WidthPx > 0);
        Assert.True(result.HeightPx > 0);

        await FluentDocsOutputWriter.SaveChartAsync(result, $"all-types-{type.ToString().ToLowerInvariant()}");
    }

    [Fact]
    public async Task RenderAsync_SvgOutput_ProducesSvgXml()
    {
        var renderer = _host.ResolveChartRenderer();
        var chart = SampleDefinitions.SimpleBarChart();
        var options = new ChartRenderOptions { OutputFormat = ImageFormat.Svg };

        var result = await renderer.RenderAsync(chart, options);

        Assert.NotEmpty(result.ImageData);
        Assert.Equal(ImageFormat.Svg, result.Format);

        var svg = System.Text.Encoding.UTF8.GetString(result.ImageData);
        Assert.Contains("<svg", svg);

        await FluentDocsOutputWriter.SaveChartAsync(result, "bar-chart-svg");
    }

    [Fact]
    public async Task RenderAsync_WithCustomStyle_ProducesOutput()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Bar,
            Title = "Styled Chart",
            Series =
            [
                new ChartDataSeries
                {
                    Label = "Custom",
                    Categories = ["A", "B", "C"],
                    Values = [15, 25, 35]
                }
            ],
            Style = new ChartStyle
            {
                BackgroundColor = "#F0F0F0",
                Palette = ["#FF6384", "#36A2EB", "#FFCE56"]
            },
            WidthPx = 600,
            HeightPx = 400
        };

        var renderer = _host.ResolveChartRenderer();
        var result = await renderer.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
        await FluentDocsOutputWriter.SaveChartAsync(result, "styled-chart");
    }

    [Fact]
    public async Task RenderAsync_EmptySeries_DoesNotThrow()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Bar,
            Title = "Empty Series",
            Series = [],
            WidthPx = 400,
            HeightPx = 300
        };

        var renderer = _host.ResolveChartRenderer();

        var result = await renderer.RenderAsync(chart);

        Assert.True(result.ImageData.Length > 0, "even an empty chart should produce an image");
        await FluentDocsOutputWriter.SaveChartAsync(result, "empty-series");
    }

    [Fact]
    public async Task RenderAsync_LargeDataSet_ProducesOutput()
    {
        var categories = Enumerable.Range(1, 200).Select(i => $"Pt{i}").ToArray();
        var values = Enumerable.Range(1, 200).Select(i => (double)(Math.Sin(i * 0.1) * 50 + 50)).ToArray();

        var chart = new ChartDefinition
        {
            Type = ChartType.Line,
            Title = "Large Dataset",
            Series =
            [
                new ChartDataSeries
                {
                    Label = "Sine Wave",
                    Categories = categories,
                    Values = values
                }
            ],
            WidthPx = 1200,
            HeightPx = 600
        };

        var renderer = _host.ResolveChartRenderer();
        var result = await renderer.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
        Assert.True(result.ImageData.Length > 1000, "a large chart should produce substantial output");

        await FluentDocsOutputWriter.SaveChartAsync(result, "large-dataset");
    }

    [Fact]
    public async Task RenderAsync_MultiSeries_AllChartTypes_ProduceOutput()
    {
        var multiSeriesTypes = new[] { ChartType.Bar, ChartType.Line, ChartType.Area, ChartType.Scatter };

        var renderer = _host.ResolveChartRenderer();

        foreach (var type in multiSeriesTypes)
        {
            var chart = new ChartDefinition
            {
                Type = type,
                Title = $"Multi-Series {type}",
                Series =
                [
                    new ChartDataSeries
                    {
                        Label = "Series A",
                        Categories = ["X", "Y", "Z"],
                        Values = [10, 30, 20]
                    },
                    new ChartDataSeries
                    {
                        Label = "Series B",
                        Categories = ["X", "Y", "Z"],
                        Values = [25, 15, 35]
                    }
                ],
                WidthPx = 500,
                HeightPx = 350
            };

            var result = await renderer.RenderAsync(chart);

            Assert.True(result.ImageData.Length > 0, $"multi-series {type} should produce output");
            await FluentDocsOutputWriter.SaveChartAsync(result, $"multi-series-{type.ToString().ToLowerInvariant()}");
        }
    }

    public static TheoryData<ChartType, ChartDefinition> AllChartTypeData()
    {
        var data = new TheoryData<ChartType, ChartDefinition>();
        foreach (var (type, definition) in SampleDefinitions.AllChartTypes())
            data.Add(type, definition);
        return data;
    }

    public void Dispose() => _host.Dispose();
}
