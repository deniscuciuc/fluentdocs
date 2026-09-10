# Spreadsheet Generation

FluentDocs supports generating spreadsheets in three formats: **XLSX**, **CSV**, and **TSV**.

## Building a Spreadsheet

Use `GenerationBuilder.Spreadsheet()` to start:

```csharp
var spreadsheet = GenerationBuilder.Spreadsheet()
    .ForFormat(GeneratedFileFormat.Xlsx)
    .WithFileName("sales-data")
    .WithMetadata(m => m
        .Title("Sales Report")
        .Author("Sales Team"))
    .AddSheet("Sales", s => s
        .AddColumn("Region", width: 20)
        .AddColumn("Q1", width: 12)
        .AddColumn("Q2", width: 12)
        .AddRow(r => r.AddCell("North").AddCell(125_000m).AddCell(140_000m))
        .AddRow(r => r.AddCell("South").AddCell(98_000m).AddCell(110_000m)))
    .Build();
```

## Columns

```csharp
sheet.AddColumn("Label", width: 30);        // text column
sheet.AddColumn("Amount", width: 15, format: "€#,##0.00");   // number format
sheet.AddColumn("Date", width: 14, format: "yyyy-MM-dd");     // date format
```

## Rows and Cells

```csharp
sheet.AddRow(r => r
    .AddCell("North")           // string
    .AddCell(125_000m)          // decimal → numeric cell
    .AddCell(0.15m)             // decimal → numeric cell
    .AddCell(new DateTime(2024, 1, 1)));  // DateTime → date cell
```

### Formula Cells

```csharp
sheet.AddRow(r => r
    .AddCell("Total")
    .AddFormulaCell("=SUM(B2:B10)")
    .AddFormulaCell("=AVERAGE(B2:B10)"));
```

## Multiple Sheets

```csharp
var spreadsheet = GenerationBuilder.Spreadsheet()
    .ForFormat(GeneratedFileFormat.Xlsx)
    .WithFileName("multi-sheet")
    .AddSheet("Summary", s => s /* ... */)
    .AddSheet("Detail", s => s /* ... */)
    .AddSheet("Metadata", s => s /* ... */)
    .Build();
```

> CSV and TSV export only the **first sheet**. If you need all sheets exported, generate each separately.

## XLSX-Specific Features

### Freeze Panes

```csharp
sheet.WithFreezeRows(1);             // freeze header row
sheet.WithFreezeColumns(1);          // freeze first column
sheet.WithFreezeRowsAndColumns(1, 1); // freeze both
```

### Auto-Filter

```csharp
sheet.WithAutoFilter();
```

### Images in XLSX

```csharp
sheet.AddImage(i => i
    .WithData(pngBytes)
    .WithFormat(ImageFormat.Png)
    .AtCell("D2")
    .WithWidth(200));
```

### Embedded Charts

Render a chart via `IChartRenderer`, then embed the PNG in the sheet:

```csharp
var renderResult = await chartRenderer.RenderAsync(definition, ChartOutputFormat.Png);

if (renderResult is ChartRenderResult.Succeeded success)
{
    sheet.AddImage(i => i
        .WithData(success.Content)
        .WithFormat(ImageFormat.Png)
        .AtCell("F2")
        .WithWidth(480));
}
```

## Format-Specific Notes

### XLSX (`GeneratedFileFormat.Xlsx`)

- Powered by [ClosedXML](https://github.com/ClosedXML/ClosedXML).
- Supports multi-sheet workbooks, freeze panes, auto-filter, formulas, and number formats.
- Images are embedded as floating anchors.

### CSV (`GeneratedFileFormat.Csv`)

- Comma-separated values, UTF-8 with BOM, RFC 4180 compliant.
- Only the first sheet is exported.
- Cell values are formatted as strings; formulas are written as the formula text.
- Powered by [CsvHelper](https://joshclose.github.io/CsvHelper/).

### TSV (`GeneratedFileFormat.Tsv`)

- Tab-separated values, UTF-8 with BOM.
- Same constraints as CSV (first sheet only).
- Values containing tabs or newlines are quoted automatically.

## Calling the Generator

```csharp
var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Xlsx);
var result = await generator.GenerateAsync(spreadsheet);

if (result is GenerationResult.Succeeded success)
    await File.WriteAllBytesAsync("report.xlsx", success.Content);
```
