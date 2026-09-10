# Getting Started with FluentDocs

FluentDocs is a set of .NET libraries for generating documents, spreadsheets, presentations, and charts from a fluent builder API.

## Installation

Install from nuget.org:

```bash
# All generators + chart rendering
dotnet add package FluentDocs

# Or install individual packages
dotnet add package FluentDocs.Documents.Docx
dotnet add package FluentDocs.Documents.Pdf
dotnet add package FluentDocs.Documents.Markdown
dotnet add package FluentDocs.Tables
dotnet add package FluentDocs.Presentations
dotnet add package FluentDocs.Charts
```

## Registration

Register all generators and chart rendering in one call:

```csharp
services.AddFluentDocs();
```

See [dependency-injection.md](dependency-injection.md) for granular registration options.

## Quick Example

```csharp
// Inject the generator
public class ReportService(IEnumerable<IFileGenerator> generators)
{
    public async Task<byte[]> GeneratePdfReportAsync()
    {
        var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pdf);

        var document = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Pdf)
            .WithFileName("monthly-report")
            .WithMetadata(m => m
                .Title("Monthly Report")
                .Author("Finance Team"))
            .WithPageSetup(p => p
                .WithPageSize(PageSize.A4)
                .WithMargins(25, 25, 25, 25))
            .AddSection(s => s
                .AddHeading("Q4 Financial Summary", HeadingLevel.H1)
                .AddParagraph("Revenue increased by 15% year over year.")
                .AddTable(t => t
                    .AddColumn("Quarter")
                    .AddColumn("Revenue")
                    .AddColumn("Growth")
                    .AddRow(r => r.AddCell("Q1").AddCell("$1.2M").AddCell("+12%"))
                    .AddRow(r => r.AddCell("Q2").AddCell("$1.4M").AddCell("+16%"))))
            .Build();

        var result = await generator.GenerateAsync(document);

        if (result is GenerationResult.Succeeded success)
            return success.Content;

        throw new InvalidOperationException("Generation failed");
    }
}
```

## Supported Formats

| Package | Formats |
|---------|---------|
| `FluentDocs.Documents.Pdf` | PDF |
| `FluentDocs.Documents.Docx` | DOCX |
| `FluentDocs.Documents.Markdown` | Markdown, PlainText |
| `FluentDocs.Tables` | XLSX, CSV, TSV |
| `FluentDocs.Presentations` | PPTX |
| `FluentDocs.Charts` | PNG, SVG (chart images) |

## Next Steps

- [Documents](documents.md) — PDF, DOCX, Markdown, PlainText generation
- [Spreadsheets](spreadsheets.md) — XLSX, CSV, TSV generation
- [Presentations](presentations.md) — PPTX generation
- [Charts](charts.md) — Chart rendering with ScottPlot
- [Dependency Injection](dependency-injection.md) — Registration options
