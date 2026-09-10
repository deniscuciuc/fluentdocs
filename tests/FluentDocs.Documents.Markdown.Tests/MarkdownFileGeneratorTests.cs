using System.Text;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Documents.Markdown;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentDocs.Documents.Markdown.Tests;

public class MarkdownFileGeneratorTests
{
    private readonly IChartRenderer _chartRenderer = Substitute.For<IChartRenderer>();
    private readonly MarkdownFileGenerator _sut;

    public MarkdownFileGeneratorTests()
    {
        _sut = new MarkdownFileGenerator(_chartRenderer, NullLogger<MarkdownFileGenerator>.Instance);
    }

    [Fact]
    public void SupportedFormat_ReturnsMarkdown()
    {
        Assert.Equal(GeneratedFileFormat.Markdown, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ReturnsSuccess()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Markdown);

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsSuccess);
        var success = Assert.IsType<GenerationResult.Succeeded>(result);
        Assert.Equal(GeneratedFileFormat.Markdown, success.Format);
        Assert.Contains("markdown", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_MinimalDocument_ContainsHeadingAndParagraph()
    {
        var doc = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Markdown);

        var result = await _sut.GenerateAsync(doc);

        var success = (GenerationResult.Succeeded)result;
        var md = Encoding.UTF8.GetString(success.Content);
        Assert.Contains("# Test Heading", md);
        Assert.Contains("This is a test paragraph.", md);
    }

    [Fact]
    public async Task GenerateAsync_RichDocument_ContainsAllElements()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Markdown);

        var result = await _sut.GenerateAsync(doc);

        var md = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);

        // Headings
        Assert.Contains("# Chapter 1", md);
        Assert.Contains("## Subsection", md);

        // Bold and italic
        Assert.Contains("**bold text**", md);
        Assert.Contains("*italic text*", md);

        // Code block
        Assert.Contains("```csharp", md);
        Assert.Contains("var x = 42;", md);

        // Block quote
        Assert.Contains("> This is a block quote.", md);

        // List items
        Assert.Contains("- Item 1", md);
        Assert.Contains("Nested", md);

        // Ordered list
        Assert.Contains("1. First", md);

        // Horizontal rule
        Assert.Contains("---", md);

        // Table
        Assert.Contains("| Name | Value |", md);
        Assert.Contains("| Alpha | 100 |", md);
    }

    [Fact]
    public async Task GenerateAsync_WithMetadata_IncludesFrontMatter()
    {
        var doc = SampleDefinitions.RichDocument(GeneratedFileFormat.Markdown);

        var result = await _sut.GenerateAsync(doc);

        var md = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);
        Assert.StartsWith("---", md);
        Assert.Contains("title:", md);
        Assert.Contains("Rich Document", md);
        Assert.Contains("author:", md);
        Assert.Contains("Test Author", md);
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet();

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsFailure);
    }
}
