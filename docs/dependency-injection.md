# Dependency Injection

`FluentDocs` provides extension methods for `IServiceCollection` to register file generators and chart rendering.

## Registration Methods

### `AddFluentDocs()` — Recommended

Registers **all** generators and chart rendering in a single call:

```csharp
services.AddFluentDocs();
```

Equivalent to calling all four methods below.

---

### `AddDocumentGeneration()`

Registers document generators: **PDF**, **DOCX**, **Markdown**, **PlainText**.

```csharp
services.AddDocumentGeneration();
```

Requires packages:
- `FluentDocs.Documents.Docx`
- `FluentDocs.Documents.Pdf`
- `FluentDocs.Documents.Markdown`

---

### `AddSpreadsheetGeneration()`

Registers spreadsheet generators: **XLSX**, **CSV**, **TSV**.

```csharp
services.AddSpreadsheetGeneration();
```

Requires package:
- `FluentDocs.Tables`

---

### `AddPresentationGeneration()`

Registers presentation generators: **PPTX**.

```csharp
services.AddPresentationGeneration();
```

Requires package:
- `FluentDocs.Presentations`

---

### `AddChartRendering()`

Registers `IChartRenderer` → `ScottPlotChartRenderer`.

```csharp
services.AddChartRendering();
```

Requires package:
- `FluentDocs.Charts`

---

## Idempotency

All registration methods are idempotent. Calling each more than once does not register duplicates. This makes it safe to call `AddFluentDocs()` in a shared startup helper and individual `Add*` methods in integration tests or feature modules.

---

## Resolving Generators

### All generators

```csharp
public class MyService(IEnumerable<IFileGenerator> generators)
{
    public async Task<byte[]> GenerateForFormat(GeneratedFileFormat format, IFileDefinition definition)
    {
        var generator = generators.FirstOrDefault(g => g.SupportedFormat == format)
            ?? throw new NotSupportedException($"No generator for {format}");

        var result = await generator.GenerateAsync(definition);

        return result is GenerationResult.Succeeded s
            ? s.Content
            : throw new InvalidOperationException($"Generation failed");
    }
}
```

### Specific generator

If you need only one generator, you can resolve it directly if you know the concrete type; however, resolving via `IEnumerable<IFileGenerator>` and filtering by `SupportedFormat` is preferred for portability.

### Chart renderer

```csharp
public class ChartService(IChartRenderer chartRenderer)
{
    public Task<ChartRenderResult> RenderAsync(ChartDefinition definition)
        => chartRenderer.RenderAsync(definition, ChartOutputFormat.Png);
}
```

---

## Service Lifetimes

All registered services use **Singleton** lifetime. Chart renderers and file generators are stateless and safe to share.

---

## Minimal Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluentDocs();

var app = builder.Build();

app.MapGet("/report.pdf", async (IEnumerable<IFileGenerator> generators) =>
{
    var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pdf);

    var document = GenerationBuilder.Document()
        .ForFormat(GeneratedFileFormat.Pdf)
        .WithFileName("report")
        .AddSection(s => s.AddHeading("Hello, World!", HeadingLevel.H1))
        .Build();

    var result = await generator.GenerateAsync(document);

    return result is GenerationResult.Succeeded s
        ? Results.File(s.Content, "application/pdf", "report.pdf")
        : Results.Problem("Generation failed");
});

app.Run();
```
