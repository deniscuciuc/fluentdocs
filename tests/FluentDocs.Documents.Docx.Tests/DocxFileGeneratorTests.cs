using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Documents.Docx;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentDocs.Documents.Docx.Tests;

public class DocxFileGeneratorTests
{
    private readonly IChartRenderer _chartRenderer = Substitute.For<IChartRenderer>();
    private readonly DocxFileGenerator _sut;

    public DocxFileGeneratorTests()
    {
        _sut = new DocxFileGenerator(_chartRenderer, NullLogger<DocxFileGenerator>.Instance);
    }

    [Fact]
    public void SupportedFormat_ReturnsDocx()
    {
        Assert.Equal(GeneratedFileFormat.Docx, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ReturnsSuccess()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsSuccess);
        var success = Assert.IsType<GenerationResult.Succeeded>(result);
        Assert.Equal(GeneratedFileFormat.Docx, success.Format);
        Assert.Contains("wordprocessingml", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_ContainsHeading()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);
        var body = wordDoc.MainDocumentPart!.Document.Body!;

        var paragraphs = body.Elements<Paragraph>().ToList();
        Assert.NotEmpty(paragraphs);

        // The first paragraph should have the heading style
        var firstPara = paragraphs[0];
        var pStyle = firstPara.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        Assert.Equal("Heading1", pStyle);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_ContainsParagraphText()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);
        var body = wordDoc.MainDocumentPart!.Document.Body!;

        var allText = body.InnerText;
        Assert.Contains("This is a test paragraph.", allText);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_ContainsMetadata()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        var props = wordDoc.PackageProperties;
        Assert.Equal("Rich Document", props.Title);
        Assert.Equal("Test Author", props.Creator);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_ContainsTable()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);
        var body = wordDoc.MainDocumentPart!.Document.Body!;

        Assert.NotEmpty(body.Elements<Table>());
        Assert.Contains("Alpha", body.InnerText);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_HasHeaderAndFooter()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        Assert.NotEmpty(wordDoc.MainDocumentPart!.HeaderParts);
        Assert.NotEmpty(wordDoc.MainDocumentPart!.FooterParts);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_HasStyles()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Docx);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wordDoc = WordprocessingDocument.Open(ms, false);

        Assert.NotNull(wordDoc.MainDocumentPart!.StyleDefinitionsPart);
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet();

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsFailure);
    }
}
