using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Testing;
using PdfSharp.Pdf.IO;

namespace FluentDocs.IntegrationTests;

/// <summary>
/// Integration tests for document generators (DOCX, PDF, Markdown, PlainText)
/// using real DI-wired services and saving output to disk.
/// </summary>
public sealed class DocumentGenerationIntegrationTests : IDisposable
{
    private readonly FluentDocsTestHost _host = FluentDocsTestHost.Create();

    private static readonly GeneratedFileFormat[] AllDocFormats =
        [GeneratedFileFormat.Docx, GeneratedFileFormat.Pdf, GeneratedFileFormat.Markdown, GeneratedFileFormat.PlainText];

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Minimal document across all formats
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateMinimalDocument_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.MinimalDocument(format);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, $"minimal doc for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 0);
        Assert.Equal(format, success.Format);

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "minimal-doc");
    }

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Rich document across all formats
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateRichDocument_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.RichDocument(format);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, $"rich doc for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 100, "rich document should produce substantial output");

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "rich-doc");
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Document with embedded chart
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateDocumentWithChart_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.DocumentWithChart(format);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, $"doc with chart for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 0);

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "doc-with-chart");
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Document with embedded image
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateDocumentWithImage_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.DocumentWithImage(format);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, $"doc with image for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "doc-with-image");
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Document with table AND chart
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateDocumentWithTableAndChart_AllFormats_Success(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.DocumentWithTableAndChart(format);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, $"doc with table+chart for {format} should succeed");
        var success = (GenerationResult.Succeeded)result;

        await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "doc-table-chart");
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific round-trip: DOCX
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Docx_RichDocument_RoundTrip_HasExpectedStructure()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Docx);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        var body = wordDoc.MainDocumentPart!.Document.Body!;
        Assert.NotNull(body);

        // Verify heading exists
        var headings = body.Descendants<Paragraph>()
            .Where(p => p.ParagraphProperties?.ParagraphStyleId?.Val?.Value?.StartsWith("Heading", StringComparison.Ordinal) == true)
            .ToList();
        Assert.NotEmpty(headings);

        // Verify table exists
        var tables = body.Descendants<Table>().ToList();
        Assert.NotEmpty(tables);

        // Verify metadata
        var coreProps = wordDoc.PackageProperties;
        Assert.Equal("Rich Document", coreProps.Title);
        Assert.Equal("Test Author", coreProps.Creator);
    }

    [Fact]
    public async Task Docx_DocumentWithChart_ContainsEmbeddedImage()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var doc = SampleDefinitions.DocumentWithChart(GeneratedFileFormat.Docx);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        // Chart is rendered as image and embedded — check for image parts
        var imageParts = wordDoc.MainDocumentPart!.ImageParts.ToList();
        Assert.NotEmpty(imageParts);
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific round-trip: PDF
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pdf_RichDocument_HasMultiplePages()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pdf);
        // Use a large document to ensure multiple pages
        var doc = SampleDefinitions.LargeDocument(GeneratedFileFormat.Pdf);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pdfDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);

        Assert.True(pdfDoc.PageCount > 1, "large PDF should span multiple pages");
    }

    // ────────────────────────────────────────────────────────────────────
    // Format-specific: Markdown content verification
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Markdown_RichDocument_ContainsAllElementTypes()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Markdown);
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Markdown);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        var md = Encoding.UTF8.GetString(success.Content);

        Assert.Contains("# Chapter 1", md);
        Assert.Contains("## Subsection", md);
        Assert.Contains("**bold text**", md);
        Assert.Contains("*italic text*", md);
        Assert.Contains("```csharp", md);
        Assert.Contains("> This is a block quote", md);
        Assert.Contains("|", md);
    }

    [Fact]
    public async Task Markdown_DocumentWithChart_ContainsChartImage()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Markdown);
        var doc = SampleDefinitions.DocumentWithChart(GeneratedFileFormat.Markdown);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        var md = Encoding.UTF8.GetString(success.Content);

        // Chart should be rendered and embedded as a base64 data URI or image reference
        Assert.Contains("![", md);
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Empty document
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(DocFormats))]
    public async Task GenerateEmptyDocument_AllFormats_HandledGracefully(GeneratedFileFormat format)
    {
        var generator = _host.ResolveGenerator(format);
        var doc = SampleDefinitions.EmptyDocument(format);

        var result = await generator.GenerateAsync(doc);

        // Either succeeds with minimal output or fails gracefully
        if (result.IsSuccess)
        {
            var success = (GenerationResult.Succeeded)result;
            Assert.NotNull(success.Content);
            await FluentDocsOutputWriter.SaveAsync(success, FluentDocsOutputWriter.SubdirectoryFor(format), "empty-doc");
        }
        else
        {
            var failed = (GenerationResult.Failed)result;
            Assert.NotNull(failed.Error);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Large document stress test
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateLargeDocument_Docx_CompletesSuccessfully()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var doc = SampleDefinitions.LargeDocument(GeneratedFileFormat.Docx);

        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, "large document generation should complete");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 4_000, "50-chapter document should produce substantial output");

        await FluentDocsOutputWriter.SaveAsync(success, "documents/docx", "large-doc");
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Wrong definition type
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateDocument_WrongDefinitionType_ReturnsFailure()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var wrongDef = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Csv);

        var result = await generator.GenerateAsync(wrongDef);

        Assert.True(result.IsFailure, "passing spreadsheet definition to doc generator should fail");
    }

    // ────────────────────────────────────────────────────────────────────
    // PlainText specific: chart placeholder
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlainText_DocumentWithChart_HasChartPlaceholder()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.PlainText);
        var doc = SampleDefinitions.DocumentWithChart(GeneratedFileFormat.PlainText);

        var result = await generator.GenerateAsync(doc);
        var success = (GenerationResult.Succeeded)result;

        var text = Encoding.UTF8.GetString(success.Content);
        Assert.Contains("[Chart:", text);

        await FluentDocsOutputWriter.SaveAsync(success, "documents/plaintext", "doc-with-chart");
    }

    public static TheoryData<GeneratedFileFormat> DocFormats()
    {
        var data = new TheoryData<GeneratedFileFormat>();
        foreach (var f in AllDocFormats) data.Add(f);
        return data;
    }

    public void Dispose() => _host.Dispose();
}
