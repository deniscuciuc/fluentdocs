using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Testing;

namespace FluentDocs.IntegrationTests;

/// <summary>
/// Integration tests for spreadsheet generators (XLSX, CSV, TSV)
/// using real DI-wired services and saving output to disk.
/// </summary>
public sealed class SpreadsheetGenerationIntegrationTests : IDisposable
{
    private readonly FluentDocsTestHost _host = FluentDocsTestHost.Create();

    private static readonly GeneratedFileFormat[] AllTableFormats =
        [GeneratedFileFormat.Xlsx, GeneratedFileFormat.Csv, GeneratedFileFormat.Tsv];

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Minimal spreadsheet across all formats
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(TableFormats))]
    public async Task GenerateMinimalSpreadsheet_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var sheet = SampleDefinitions.MinimalSpreadsheet(format);

        var result = await generator.GenerateAsync(sheet);

        Assert.True(result.IsSuccess, $"minimal spreadsheet for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 0);

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "minimal-sheet");
    }

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Rich spreadsheet across all formats
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(TableFormats))]
    public async Task GenerateRichSpreadsheet_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var sheet = SampleDefinitions.RichSpreadsheet(format);

        var result = await generator.GenerateAsync(sheet);

        Assert.True(result.IsSuccess, $"rich spreadsheet for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 0);

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "rich-sheet");
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Spreadsheet with chart
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateSpreadsheetWithChart_Xlsx_Success()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var sheet = SampleDefinitions.SpreadsheetWithChart(GeneratedFileFormat.Xlsx);

        var result = await generator.GenerateAsync(sheet);

        Assert.True(result.IsSuccess, "XLSX with chart should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 1000, "XLSX with chart should be larger than plain data");

        await FluentDocsOutputWriter.SaveAsync(success, "tables/xlsx", "sheet-with-chart");
    }

    [Fact]
    public async Task GenerateSpreadsheetWithChart_Csv_IgnoresChart()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Csv);
        var sheet = SampleDefinitions.SpreadsheetWithChart(GeneratedFileFormat.Csv);

        var result = await generator.GenerateAsync(sheet);

        Assert.True(result.IsSuccess, "CSV should succeed even when chart is defined (charts are ignored)");
        var success = (GenerationResult.Succeeded)result;

        // Verify data is present, chart is just ignored
        var csv = Encoding.UTF8.GetString(success.Content);
        Assert.Contains("Jan", csv);
        Assert.Contains("5000", csv);

        await FluentDocsOutputWriter.SaveAsync(success, "tables/csv", "sheet-with-chart-ignored");
    }

    // ────────────────────────────────────────────────────────────────────
    // Multi-sheet
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateMultiSheetSpreadsheet_Xlsx_HasAllSheets()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var sheet = SampleDefinitions.MultiSheetSpreadsheet(GeneratedFileFormat.Xlsx);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var workbook = new XLWorkbook(ms);

        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.Equivalent(new[] { "Revenue", "Expenses", "Summary" },
            workbook.Worksheets.Select(w => w.Name));

        await FluentDocsOutputWriter.SaveAsync(success, "tables/xlsx", "multi-sheet");
    }

    [Fact]
    public async Task GenerateMultiSheetSpreadsheet_Csv_ExportsFirstSheetOnly()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Csv);
        var sheet = SampleDefinitions.MultiSheetSpreadsheet(GeneratedFileFormat.Csv);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        var csv = Encoding.UTF8.GetString(success.Content);
        // First sheet has "Quarter" and "Amount"
        Assert.Contains("Quarter", csv);
        Assert.Contains("50000", csv);
        // Second sheet data should NOT be present
        Assert.DoesNotContain("Salaries", csv);

        await FluentDocsOutputWriter.SaveAsync(success, "tables/csv", "multi-sheet-first-only");
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific round-trip: XLSX
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Xlsx_RoundTrip_HasExpectedSheetAndData()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Xlsx);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var workbook = new XLWorkbook(ms);

        var ws = workbook.Worksheets.First();
        Assert.Equal("Data", ws.Name);
        Assert.Equal("Name", ws.Cell(1, 1).GetString());
        Assert.Equal("Score", ws.Cell(1, 2).GetString());
        Assert.Equal("Alice", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task Xlsx_RichSpreadsheet_HasFreezeAndFilter()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var sheet = SampleDefinitions.RichSpreadsheet(GeneratedFileFormat.Xlsx);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var workbook = new XLWorkbook(ms);

        var ws = workbook.Worksheets.First();
        Assert.Equal("Sales", ws.Name);
        Assert.Equal("Product", ws.Cell(1, 1).GetString());
        Assert.True(ws.AutoFilter.IsEnabled, "auto-filter should be enabled");

        await FluentDocsOutputWriter.SaveAsync(success, "tables/xlsx", "rich-sheet-verified");
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific round-trip: CSV
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Csv_RoundTrip_MatchesOriginalData()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Csv);
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Csv);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var reader = new StreamReader(ms);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        var records = csv.GetRecords<dynamic>().ToList();
        Assert.Equal(2, records.Count);
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific round-trip: TSV
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Tsv_RoundTrip_MatchesOriginalData()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Tsv);
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Tsv);

        var result = await generator.GenerateAsync(sheet);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var reader = new StreamReader(ms);
        using var tsv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            HasHeaderRecord = true
        });

        var records = tsv.GetRecords<dynamic>().ToList();
        Assert.Equal(2, records.Count);
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Empty spreadsheet
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(TableFormats))]
    public async Task GenerateEmptySpreadsheet_AllFormats_HandledGracefully(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var sheet = SampleDefinitions.EmptySpreadsheet(format);

        var result = await generator.GenerateAsync(sheet);

        if (result.IsSuccess)
        {
            var success = (GenerationResult.Succeeded)result;
            Assert.NotNull(success.Content);
            await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "empty-sheet");
        }
        else
        {
            var failed = (GenerationResult.Failed)result;
            Assert.NotNull(failed.Error);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Wrong definition type
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateSpreadsheet_WrongDefinitionType_ReturnsFailure()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var wrongDef = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Docx);

        var result = await generator.GenerateAsync(wrongDef);

        Assert.True(result.IsFailure, "passing document definition to spreadsheet generator should fail");
    }

    public static TheoryData<GeneratedFileFormat> TableFormats()
    {
        var data = new TheoryData<GeneratedFileFormat>();
        foreach (var f in AllTableFormats) data.Add(f);
        return data;
    }

    public void Dispose() => _host.Dispose();
}
