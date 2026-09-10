# Document Generation

FluentDocs supports generating documents in four formats: **PDF**, **DOCX**, **Markdown**, and **PlainText**.

## Building a Document

Use `GenerationBuilder.Document()` to start:

```csharp
var document = GenerationBuilder.Document()
    .ForFormat(GeneratedFileFormat.Pdf)
    .WithFileName("report")
    .WithMetadata(m => m
        .Title("Annual Report")
        .Author("Analytics Team")
        .Subject("Year-end summary")
        .Keywords("annual", "report", "finance"))
    .WithPageSetup(p => p
        .WithPageSize(PageSize.A4)
        .WithOrientation(PageOrientation.Portrait)
        .WithMargins(top: 25, right: 20, bottom: 25, left: 20))
    .AddSection(s => s
        .AddHeading("Introduction", HeadingLevel.H1)
        .AddParagraph("This report summarises the year's activity."))
    .Build();
```

## Elements

### Headings

```csharp
section.AddHeading("Chapter 1", HeadingLevel.H1);
section.AddHeading("Sub-section", HeadingLevel.H2);
```

Available levels: `H1` through `H6`.

### Paragraphs

```csharp
section.AddParagraph("Plain paragraph text.");
section.AddParagraph(p => p
    .WithText("This is ")
    .Bold("important")
    .WithText(" and this is ")
    .Italic("emphasised")
    .WithText("."));
```

### Lists

```csharp
// Unordered
section.AddList(l => l
    .Unordered()
    .AddItem("First item")
    .AddItem("Second item")
    .AddItem(i => i.WithText("Nested").AddSubItem("Sub-item")));

// Ordered
section.AddList(l => l
    .Ordered()
    .AddItem("Step one")
    .AddItem("Step two"));
```

### Code Blocks

```csharp
section.AddCodeBlock("var x = 42;", language: "csharp");
```

### Tables

```csharp
section.AddTable(t => t
    .AddColumn("Name", width: 40)
    .AddColumn("Value", width: 20)
    .AddRow(r => r.AddCell("Revenue").AddCell("$1.2M"))
    .AddRow(r => r.AddCell("Costs").AddCell("$0.8M")));
```

### Images

```csharp
section.AddImage(i => i
    .WithData(pngBytes)
    .WithFormat(ImageFormat.Png)
    .WithWidth(200)
    .WithAltText("Company logo"));
```

### Charts

To embed a chart, first render it to PNG using `IChartRenderer`, then add as an image:

```csharp
var chartRenderer = serviceProvider.GetRequiredService<IChartRenderer>();

var definition = new ChartDefinition { /* ... */ };
var renderResult = await chartRenderer.RenderAsync(definition, ChartOutputFormat.Png);

if (renderResult is ChartRenderResult.Succeeded success)
{
    section.AddImage(i => i
        .WithData(success.Content)
        .WithFormat(ImageFormat.Png)
        .WithWidth(400));
}
```

### Headers and Footers

```csharp
document.WithHeader(h => h.AddParagraph("My Company — Confidential"))
        .WithFooter(f => f.AddParagraph("Page {page} of {total}"));
```

## Format-Specific Notes

### PDF (`GeneratedFileFormat.Pdf`)

- Powered by [PDFsharp](https://docs.pdfsharp.net/).
- Page size and margins are applied exactly.
- Images, tables, and multi-section documents are fully supported.

### DOCX (`GeneratedFileFormat.Docx`)

- Powered by [Open XML SDK](https://github.com/dotnet/Open-XML-SDK).
- Produces `.docx` files compatible with Microsoft Word, LibreOffice, and Google Docs.
- Headers, footers, and styles are embedded.

### Markdown (`GeneratedFileFormat.Markdown`)

- Produces a YAML front-matter block followed by CommonMark-compliant Markdown.
- Front matter fields: `title`, `author`, `subject`, `keywords`, `date`.
- Tables are rendered as GFM pipe tables.
- Code blocks use fenced syntax (` ``` `).
- Images are rendered as `![alt](base64-uri)`.

### PlainText (`GeneratedFileFormat.PlainText`)

- Headings are rendered in ALL CAPS with underline separators.
- Tables are rendered as aligned ASCII art.
- Images are replaced with `[Image: alt text]` placeholders.
- Charts are replaced with `[Chart: title]` placeholders.

## Calling the Generator

```csharp
var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Docx);
var result = await generator.GenerateAsync(document);

switch (result)
{
    case GenerationResult.Succeeded success:
        await File.WriteAllBytesAsync("output.docx", success.Content);
        break;

    case GenerationResult.Failed failure:
        logger.LogError("Generation failed: {Reason}", failure.Reason);
        break;
}
```
