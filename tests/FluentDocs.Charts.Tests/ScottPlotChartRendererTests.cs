using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Results;
using FluentDocs.Charts;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentDocs.Charts.Tests;

public class ScottPlotChartRendererTests
{
    private readonly ScottPlotChartRenderer _sut = new(NullLogger<ScottPlotChartRenderer>.Instance);

    [Fact]
    public async Task RenderAsync_BarChart_ProducesPng()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Bar,
            Title = "Bar Test",
            Series =
            [
                new ChartDataSeries
                {
                    Label = "S1",
                    Categories = ["A", "B", "C"],
                    Values = [10, 20, 30]
                }
            ],
            WidthPx = 400,
            HeightPx = 300
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
        Assert.Equal(ImageFormat.Png, result.Format);
        Assert.Equal(400, result.WidthPx);
        Assert.Equal(300, result.HeightPx);
        // PNG magic bytes
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, result.ImageData[..4]);
    }

    [Fact]
    public async Task RenderAsync_LineChart_ProducesPng()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Line,
            Series =
            [
                new ChartDataSeries { Label = "Line", Values = [1, 4, 2, 8, 5] }
            ],
            WidthPx = 600,
            HeightPx = 400
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, result.ImageData[..4]);
    }

    [Fact]
    public async Task RenderAsync_PieChart_ProducesPng()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Pie,
            Series =
            [
                new ChartDataSeries
                {
                    Categories = ["Red", "Green", "Blue"],
                    Values = [40, 35, 25]
                }
            ],
            WidthPx = 400,
            HeightPx = 400
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task RenderAsync_ScatterChart_ProducesPng()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Scatter,
            Series =
            [
                new ChartDataSeries { Label = "S1", Values = [1, 3, 2, 6, 4] }
            ]
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
    }

    [Theory]
    [InlineData(ChartType.Bar)]
    [InlineData(ChartType.Line)]
    [InlineData(ChartType.Scatter)]
    [InlineData(ChartType.Area)]
    public async Task RenderAsync_MultipleSeries_ProducesOutput(ChartType type)
    {
        var chart = new ChartDefinition
        {
            Type = type,
            Title = $"{type} multi-series",
            Series =
            [
                new ChartDataSeries { Label = "A", Values = [1, 2, 3] },
                new ChartDataSeries { Label = "B", Values = [3, 2, 1] }
            ]
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task RenderAsync_WithAxes_DoesNotThrow()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Bar,
            Title = "Axes Test",
            Series = [new ChartDataSeries { Label = "D", Values = [5, 10] }],
            XAxis = new ChartAxisDefinition { Title = "X Axis" },
            YAxis = new ChartAxisDefinition { Title = "Y Axis", Min = 0, Max = 20 }
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
    }

    [Fact]
    public async Task RenderAsync_EmptySeries_ProducesOutput()
    {
        var chart = new ChartDefinition
        {
            Type = ChartType.Bar,
            Title = "Empty",
            Series = []
        };

        var result = await _sut.RenderAsync(chart);

        Assert.NotEmpty(result.ImageData);
    }
}
