# Chart Rendering

`FluentDocs.Charts` provides chart rendering via [ScottPlot](https://scottplot.net/). Charts are rendered to PNG or SVG byte arrays that can be embedded in documents, spreadsheets, and presentations.

## Interface

```csharp
public interface IChartRenderer
{
    Task<ChartRenderResult> RenderAsync(ChartDefinition definition, ChartOutputFormat format);
}
```

`ChartRenderResult` is a discriminated union:

```csharp
case ChartRenderResult.Succeeded success:
    // success.Content — byte[]
    // success.Format  — ChartOutputFormat

case ChartRenderResult.Failed failure:
    // failure.Reason  — string
```

## Defining a Chart

```csharp
var definition = new ChartDefinition
{
    Title = "Monthly Revenue",
    ChartType = ChartType.Bar,
    Width = 800,
    Height = 450,
    XAxisLabel = "Month",
    YAxisLabel = "Revenue (USD)",
    Series =
    [
        new ChartDataSeries
        {
            Name = "2024",
            Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"],
            Values = [120_000, 135_000, 98_000, 145_000, 162_000, 155_000]
        }
    ]
};
```

## Chart Types

| `ChartType` | Description |
|-------------|-------------|
| `Bar` | Horizontal bars |
| `Column` | Vertical bars (most common) |
| `Line` | Line chart — good for time series |
| `Area` | Filled area chart |
| `Pie` | Pie chart — single series |
| `Doughnut` | Doughnut chart — single series |
| `Scatter` | Scatter plot — requires `XValues` and `Values` |
| `Radar` | Radar / spider chart |

## Multiple Series

```csharp
var definition = new ChartDefinition
{
    Title = "Revenue vs Costs",
    ChartType = ChartType.Column,
    Width = 900,
    Height = 480,
    XAxisLabel = "Quarter",
    YAxisLabel = "USD",
    ShowLegend = true,
    Series =
    [
        new ChartDataSeries
        {
            Name = "Revenue",
            Labels = ["Q1", "Q2", "Q3", "Q4"],
            Values = [1_200_000, 1_400_000, 1_100_000, 1_600_000]
        },
        new ChartDataSeries
        {
            Name = "Costs",
            Labels = ["Q1", "Q2", "Q3", "Q4"],
            Values = [800_000, 950_000, 870_000, 1_050_000]
        }
    ]
};
```

## Scatter Chart

For scatter plots, provide both `XValues` and `Values`:

```csharp
new ChartDataSeries
{
    Name = "Data",
    XValues = [1.0, 2.5, 3.1, 4.8, 6.0],
    Values  = [3.2, 4.1, 2.9, 5.5, 6.2]
}
```

## Chart Style

```csharp
var definition = new ChartDefinition
{
    // ...
    Style = new ChartStyle
    {
        BackgroundColor = "#FFFFFF",
        GridColor = "#E5E5E5",
        FontFamily = "Segoe UI",
        FontSize = 12,
        Palette = ChartPalette.Vibrant   // Blue, Orange, Green, ...
    }
};
```

Available palettes: `Default`, `Vibrant`, `Pastel`, `Monochrome`.

## Output Formats

```csharp
// PNG — rasterised, suitable for embedding in DOCX/XLSX/PPTX
var result = await chartRenderer.RenderAsync(definition, ChartOutputFormat.Png);

// SVG — vector, suitable for web/Markdown
var result = await chartRenderer.RenderAsync(definition, ChartOutputFormat.Svg);
```

## Rendering Example

```csharp
public class ReportBuilder(IChartRenderer chartRenderer, IEnumerable<IFileGenerator> generators)
{
    public async Task<byte[]> BuildReportAsync()
    {
        // 1. Render chart
        var chartDef = new ChartDefinition { /* ... */ };
        var chartResult = await chartRenderer.RenderAsync(chartDef, ChartOutputFormat.Png);

        if (chartResult is not ChartRenderResult.Succeeded chartSuccess)
            throw new InvalidOperationException("Chart render failed");

        // 2. Embed in a document
        var document = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Pdf)
            .WithFileName("report")
            .AddSection(s => s
                .AddHeading("Revenue Chart", HeadingLevel.H2)
                .AddImage(i => i
                    .WithData(chartSuccess.Content)
                    .WithFormat(ImageFormat.Png)
                    .WithWidth(500)))
            .Build();

        // 3. Generate
        var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pdf);
        var result = await generator.GenerateAsync(document);

        return result is GenerationResult.Succeeded s
            ? s.Content
            : throw new InvalidOperationException("Document generation failed");
    }
}
```

## DI Registration

```csharp
// Chart rendering only
services.AddChartRendering();

// Everything together
services.AddFluentDocs();
```

See [dependency-injection.md](dependency-injection.md) for details.
