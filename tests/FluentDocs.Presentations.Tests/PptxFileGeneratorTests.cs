using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Presentations;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentDocs.Presentations.Tests;

public class PptxFileGeneratorTests
{
    private readonly IChartRenderer _chartRenderer = Substitute.For<IChartRenderer>();
    private readonly PptxFileGenerator _sut;

    public PptxFileGeneratorTests()
    {
        _sut = new PptxFileGenerator(_chartRenderer, NullLogger<PptxFileGenerator>.Instance);
    }

    [Fact]
    public void SupportedFormat_ReturnsPptx()
    {
        Assert.Equal(GeneratedFileFormat.Pptx, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalPresentation_ReturnsSuccess()
    {
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await _sut.GenerateAsync(pres);

        Assert.True(result.IsSuccess);
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Pptx, success.Format);
        Assert.Contains("presentationml", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_HasOneSlide()
    {
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await _sut.GenerateAsync(pres);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        var slideIdList = pptxDoc.PresentationPart!.Presentation.SlideIdList;
        Assert.NotNull(slideIdList);
        Assert.Single(slideIdList!.Elements<SlideId>());
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_SlideContainsText()
    {
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await _sut.GenerateAsync(pres);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        var slidePart = pptxDoc.PresentationPart!.SlideParts.First();
        var slideText = slidePart.Slide.InnerText;
        Assert.Contains("Hello World", slideText);
    }

    [Fact]
    public async Task GenerateAsync_HasThemePart()
    {
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await _sut.GenerateAsync(pres);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        Assert.NotNull(pptxDoc.PresentationPart!.ThemePart);
    }

    [Fact]
    public async Task GenerateAsync_HasSlideMaster()
    {
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await _sut.GenerateAsync(pres);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        Assert.NotEmpty(pptxDoc.PresentationPart!.SlideMasterParts);
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var doc = SampleDefinitions.MinimalDocument();

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsFailure);
    }
}
