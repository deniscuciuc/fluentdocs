# Presentation Generation

FluentDocs supports generating presentations in **PPTX** format, compatible with Microsoft PowerPoint, LibreOffice Impress, and Google Slides.

## Building a Presentation

Use `GenerationBuilder.Presentation()` to start:

```csharp
var presentation = GenerationBuilder.Presentation()
    .ForFormat(GeneratedFileFormat.Pptx)
    .WithFileName("company-overview")
    .WithMetadata(m => m
        .Title("Company Overview")
        .Author("Marketing Team"))
    .WithTheme(PresentationTheme.Corporate)
    .AddSlide(s => s
        .WithTitle("Welcome")
        .AddTextBox(t => t.WithText("Q4 Results").WithFontSize(32).Bold()))
    .AddSlide(s => s
        .WithTitle("Agenda")
        .AddBullets(b => b
            .AddItem("Financial highlights")
            .AddItem("Product updates")
            .AddItem("Next steps")))
    .Build();
```

## Slides

### Slide Types

```csharp
// Title slide
presentation.AddSlide(s => s
    .AsTitleSlide()
    .WithTitle("Annual Report 2024")
    .WithSubtitle("Finance Department"));

// Content slide (default)
presentation.AddSlide(s => s
    .WithTitle("Key Metrics"));

// Blank slide
presentation.AddSlide(s => s.AsBlankSlide());
```

### Speaker Notes

```csharp
presentation.AddSlide(s => s
    .WithTitle("Revenue Breakdown")
    .WithNotes("Emphasise the 15% YoY growth. Q3 dip was due to seasonal factors."));
```

## Content Elements

### Text Boxes

```csharp
slide.AddTextBox(t => t
    .WithText("Hello World")
    .WithFontSize(24)
    .Bold()
    .Italic()
    .WithColor("#1A2B3C")
    .AtPosition(left: 50, top: 100)
    .WithSize(width: 400, height: 60));
```

### Bullet Lists

```csharp
slide.AddBullets(b => b
    .AddItem("Revenue grew 15% YoY")
    .AddItem("Costs reduced by 8%")
    .AddItem(i => i
        .WithText("New markets")
        .AddSubItem("APAC: +22%")
        .AddSubItem("EMEA: +18%")));
```

### Shapes

```csharp
slide.AddShape(sh => sh
    .AsRectangle()
    .WithFill("#0078D4")
    .WithBorder("#005A9E", thickness: 2)
    .AtPosition(left: 20, top: 20)
    .WithSize(width: 120, height: 40));
```

### Tables

```csharp
slide.AddTable(t => t
    .AddColumn("Region", width: 120)
    .AddColumn("Revenue", width: 100)
    .AddColumn("Growth", width: 80)
    .AddRow(r => r.AddCell("North").AddCell("$1.2M").AddCell("+15%"))
    .AddRow(r => r.AddCell("South").AddCell("$0.9M").AddCell("+11%")));
```

### Images

```csharp
slide.AddImage(i => i
    .WithData(pngBytes)
    .WithFormat(ImageFormat.Png)
    .AtPosition(left: 400, top: 80)
    .WithSize(width: 300, height: 200)
    .WithAltText("Sales chart"));
```

### Charts

Render a chart to PNG via `IChartRenderer` and embed it:

```csharp
var renderResult = await chartRenderer.RenderAsync(definition, ChartOutputFormat.Png);

if (renderResult is ChartRenderResult.Succeeded success)
{
    slide.AddImage(i => i
        .WithData(success.Content)
        .WithFormat(ImageFormat.Png)
        .AtPosition(left: 60, top: 120)
        .WithSize(width: 560, height: 300));
}
```

## Themes

```csharp
presentation.WithTheme(PresentationTheme.Default);
presentation.WithTheme(PresentationTheme.Corporate);
presentation.WithTheme(PresentationTheme.Dark);
presentation.WithTheme(PresentationTheme.Light);
```

## Format Notes

- Powered by [Open XML SDK](https://github.com/dotnet/Open-XML-SDK).
- Produces `.pptx` files with a proper slide master and theme part.
- Speaker notes are embedded in the note slide XML.
- Text boxes, shapes, and images use EMU (English Metric Units) positioning internally.

## Calling the Generator

```csharp
var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pptx);
var result = await generator.GenerateAsync(presentation);

if (result is GenerationResult.Succeeded success)
    await File.WriteAllBytesAsync("presentation.pptx", success.Content);
```
