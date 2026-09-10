using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Documents.Pdf;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PdfSharp.Pdf.IO;

namespace FluentDocs.Documents.Pdf.Tests;

public class PdfFileGeneratorTests
{
    private readonly IChartRenderer _chartRenderer = Substitute.For<IChartRenderer>();
    private readonly PdfFileGenerator _sut;

    public PdfFileGeneratorTests()
    {
        _sut = new PdfFileGenerator(_chartRenderer, NullLogger<PdfFileGenerator>.Instance);
    }

    [Fact]
    public void SupportedFormat_ReturnsPdf()
    {
        Assert.Equal(GeneratedFileFormat.Pdf, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ReturnsSuccess()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Pdf);

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsSuccess);
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Pdf, success.Format);
        Assert.Equal("application/pdf", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ProducesValidPdf()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Pdf);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;

        // PDF magic bytes: %PDF-
        Assert.Equal(0x25, bytes[0]); // %
        Assert.Equal(0x50, bytes[1]); // P
        Assert.Equal(0x44, bytes[2]); // D
        Assert.Equal(0x46, bytes[3]); // F
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_HasAtLeastOnePage()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Pdf);

        var result = await _sut.GenerateAsync(doc);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        var pdfDoc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        Assert.True(pdfDoc.PageCount >= 1);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_ProducesValidPdf()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Pdf);

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsSuccess);
        var bytes = ((GenerationResult.Succeeded)result).Content;
        Assert.True(bytes.Length > 100);

        // Validate magic bytes
        Assert.Equal(0x25, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet();

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsFailure);
    }
}
