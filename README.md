![FluentDocs](https://raw.githubusercontent.com/deniscuciuc/fluentdocs/main/assets/banner.png)

# FluentDocs

[![CI](https://github.com/deniscuciuc/fluentdocs/actions/workflows/ci.yml/badge.svg)](https://github.com/deniscuciuc/fluentdocs/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FluentDocs.svg?label=FluentDocs)](https://www.nuget.org/packages/FluentDocs/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)

> One fluent API. DOCX, PDF, XLSX, PPTX, Markdown, CSV and charts.

Describe a document once as a definition, then render it to whichever format you need. The
builder produces a serializable model; renderers turn that model into bytes. Adding a format
means adding a renderer, not rewriting the document.

```
dotnet add package FluentDocs
```

```csharp
using FluentDocs;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Builders;
using FluentDocs.Abstractions.Enums;

builder.Services.AddFluentDocs();
```

```csharp
public sealed class ReportService(IEnumerable<IFileGenerator> generators)
{
    public async Task<byte[]> BuildAsync(CancellationToken cancellationToken)
    {
        var document = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Pdf)
            .WithFileName("monthly-report")
            .WithMetadata(m => m.Title("Monthly Report").Author("Finance"))
            .AddSection(s => s
                .AddHeading("Q4 Summary", HeadingLevel.H1)
                .AddParagraph("Revenue increased by 15% year over year.")
                .AddTable(t => t
                    .AddColumn("Quarter")
                    .AddColumn("Revenue")
                    .AddRow(r => r.AddCell("Q1").AddCell("$1.2M"))
                    .AddRow(r => r.AddCell("Q2").AddCell("$1.4M"))))
            .Build();

        var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pdf);
        var result = await generator.GenerateAsync(document, ct: cancellationToken);

        return result switch
        {
            GenerationResult.Succeeded ok => ok.Content,
            GenerationResult.Failed failed => throw new InvalidOperationException(failed.Error.Message),
            _ => throw new UnreachableException(),
        };
    }
}
```

Switch `ForFormat` to `GeneratedFileFormat.Docx` and pick the matching generator — the
document definition does not change.

Failures come back as a `GenerationResult.Failed` carrying a `GenerationError`, not as an
exception, so a malformed table in one report does not take down a batch of them.

## Packages

| Package | What you get |
|---|---|
| [`FluentDocs`](https://www.nuget.org/packages/FluentDocs/) | Everything below, plus `AddFluentDocs()` |
| [`FluentDocs.Abstractions`](https://www.nuget.org/packages/FluentDocs.Abstractions/) | Builders, definition models, `IFileGenerator`, `GenerationResult` |
| [`FluentDocs.Documents.Docx`](https://www.nuget.org/packages/FluentDocs.Documents.Docx/) | DOCX, via DocumentFormat.OpenXml |
| [`FluentDocs.Documents.Pdf`](https://www.nuget.org/packages/FluentDocs.Documents.Pdf/) | PDF, via PDFsharp |
| [`FluentDocs.Documents.Markdown`](https://www.nuget.org/packages/FluentDocs.Documents.Markdown/) | Markdown and plain text |
| [`FluentDocs.Tables`](https://www.nuget.org/packages/FluentDocs.Tables/) | XLSX via ClosedXML, CSV and TSV via CsvHelper |
| [`FluentDocs.Presentations`](https://www.nuget.org/packages/FluentDocs.Presentations/) | PPTX, via DocumentFormat.OpenXml |
| [`FluentDocs.Charts`](https://www.nuget.org/packages/FluentDocs.Charts/) | PNG and SVG charts, via ScottPlot |

Take `FluentDocs` if you want all of it, or reference only the renderers you need — each one
registers itself with `AddDocumentGeneration()`, `AddSpreadsheetGeneration()`,
`AddPresentationGeneration()` or `AddChartRendering()`.

`FluentDocs.Abstractions` is the one to reference from a project that only needs to *produce*
or *accept* a definition without rendering it — a domain library, say, that hands a report
definition to a worker.

## Three builders

```csharp
GenerationBuilder.Document()      // → DOCX, PDF, Markdown, plain text
GenerationBuilder.Spreadsheet()   // → XLSX, CSV, TSV
GenerationBuilder.Presentation()  // → PPTX
```

```csharp
var sheet = GenerationBuilder.Spreadsheet()
    .ForFormat(GeneratedFileFormat.Xlsx)
    .AddSheet("Data", s => s
        .AddColumn(c => c.WithHeader("Name").AutoWidth())
        .AddColumn("Score")
        .AddRow(r => r.AddCell("Alice").AddCell(42)))
    .Build();
```

```csharp
var deck = GenerationBuilder.Presentation()
    .WithTheme(t => t.PrimaryColor("#1a73e8"))
    .AddSlide(s => s
        .WithLayout(SlideLayout.TitleSlide)
        .AddTextBox(tb => tb.AtPosition(50, 50).WithSize(200, 40).AddText("Hello")))
    .Build();
```

Charts embed into any of the three — the renderer produces an image and the document
renderer places it.

## Culture

Every renderer formats numbers and dates with `CultureInfo.InvariantCulture`. A report
generated on a machine with a comma decimal separator is byte-identical to one generated
anywhere else — which matters most for XLSX, where a locale-formatted number is silently
stored as text.

## Documentation

- [Getting started](docs/getting-started.md)
- [Documents](docs/documents.md) — DOCX, PDF, Markdown
- [Spreadsheets](docs/spreadsheets.md) — XLSX, CSV, TSV
- [Presentations](docs/presentations.md) — PPTX
- [Charts](docs/charts.md)
- [Dependency injection](docs/dependency-injection.md)
- [Release process](docs/release-process.md)

## Versioning

[Semantic versioning](https://semver.org/). All packages are versioned and released
together, so a fix ships as one version bump across the set. Breaking changes are listed in
[CHANGELOG.md](CHANGELOG.md).

Targets **net10.0**. Building from source needs the **.NET 10 SDK**.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md),
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).

## License

[MIT](LICENSE)
