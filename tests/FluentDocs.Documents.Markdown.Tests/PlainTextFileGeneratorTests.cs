using System.Text;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Documents.Markdown;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentDocs.Documents.Markdown.Tests;

public class PlainTextFileGeneratorTests
{
    private readonly PlainTextFileGenerator _sut = new(NullLogger<PlainTextFileGenerator>.Instance);

    [Fact]
    public void SupportedFormat_ReturnsPlainText()
    {
        Assert.Equal(GeneratedFileFormat.PlainText, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ReturnsSuccess()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.PlainText);

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsSuccess);
        var success = Assert.IsType<GenerationResult.Succeeded>(result);
        Assert.Equal(GeneratedFileFormat.PlainText, success.Format);
        Assert.Equal("text/plain", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ContainsHeadingAndParagraph()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.PlainText);

        var result = await _sut.GenerateAsync(doc);

        var text = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);
        Assert.Contains("TEST HEADING", text);
        Assert.Contains("This is a test paragraph.", text);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_ContainsAllElements()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.PlainText);

        var result = await _sut.GenerateAsync(doc);

        var text = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);

        // ALL CAPS headings
        Assert.Contains("CHAPTER 1", text);
        Assert.Contains("SUBSECTION", text);

        // Code block (indented)
        Assert.Contains("var x = 42;", text);

        // Block quote
        Assert.Contains("This is a block quote.", text);

        // List items
        Assert.Contains("Item 1", text);

        // Horizontal rule — implementation may use various dash characters
        Assert.Matches("[─\\-]{5,}", text);

        // Table
        Assert.Contains("Name", text);
        Assert.Contains("Alpha", text);
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet();

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsFailure);
    }
}
