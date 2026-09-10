using DocumentFormat.OpenXml.Packaging;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Testing;

using OxmlPresentation = DocumentFormat.OpenXml.Presentation;

namespace FluentDocs.IntegrationTests;

/// <summary>
/// Integration tests for the PPTX presentation generator
/// using real DI-wired services and saving output to disk.
/// </summary>
public sealed class PresentationGenerationIntegrationTests : IDisposable
{
    private readonly FluentDocsTestHost _host = FluentDocsTestHost.Create();

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Minimal presentation
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateMinimalPresentation_Success()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.MinimalPresentation();

        var result = await generator.GenerateAsync(pres);

        Assert.True(result.IsSuccess, "minimal presentation should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Pptx, success.Format);
        Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", success.ContentType);

        await FluentDocsOutputWriter.SaveAsync(success, "presentations", "minimal-pptx");
    }

    // ────────────────────────────────────────────────────────────────────
    // Happy-path: Rich multi-slide presentation
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRichPresentation_Success()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.RichPresentation();

        var result = await generator.GenerateAsync(pres);

        Assert.True(result.IsSuccess, "rich presentation should succeed");
        var success = (GenerationResult.Succeeded)result;
        Assert.True(success.Content.Length > 1000, "rich presentation should produce substantial output");

        await FluentDocsOutputWriter.SaveAsync(success, "presentations", "rich-presentation");
    }

    [Fact]
    public async Task GenerateRichPresentation_RoundTrip_HasExpectedSlideCount()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.RichPresentation();

        var result = await generator.GenerateAsync(pres);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        var slides = pptxDoc.PresentationPart!.SlideParts.ToList();
        Assert.Equal(4, slides.Count);
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Presentation with chart
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePresentationWithChart_HasEmbeddedImage()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.PresentationWithChart();

        var result = await generator.GenerateAsync(pres);
        Assert.True(result.IsSuccess, "presentation with chart should succeed");
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        // Chart is rendered as image and embedded — check for image parts in the slide
        var slide = pptxDoc.PresentationPart!.SlideParts.First();
        var imageParts = slide.ImageParts.ToList();
        Assert.NotEmpty(imageParts);

        await FluentDocsOutputWriter.SaveAsync(success, "presentations", "pptx-with-chart");
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined: Presentation with table
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRichPresentation_HasTableOnSlide()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.RichPresentation();

        var result = await generator.GenerateAsync(pres);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        // Slide 3 (index 2) has a table
        var slides = pptxDoc.PresentationPart!.SlideParts.ToList();
        Assert.True(slides.Count >= 3);

        // Look for table elements across all slides
        var hasTable = slides.Any(s =>
            s.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>().Any());
        Assert.True(hasTable, "rich presentation should have a table on at least one slide");
    }

    // ────────────────────────────────────────────────────────────────────
    // Speaker notes
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateRichPresentation_HasSpeakerNotes()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var pres = SampleDefinitions.RichPresentation();

        var result = await generator.GenerateAsync(pres);
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pptxDoc = PresentationDocument.Open(ms, false);

        // Slide 2 (chart slide) has speaker notes
        var slides = pptxDoc.PresentationPart!.SlideParts.ToList();
        var hasNotes = slides.Any(s => s.NotesSlidePart != null);
        Assert.True(hasNotes, "at least one slide should have speaker notes");
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Empty slide
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePresentation_EmptySlide_Success()
    {
        var pres = FluentDocs.Abstractions.Builders.GenerationBuilder.Presentation()
            .WithFileName("empty-slide")
            .AddSlide(s => s.WithLayout(SlideLayout.Blank))
            .Build();

        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var result = await generator.GenerateAsync(pres);

        if (result.IsSuccess)
        {
            var success = (GenerationResult.Succeeded)result;
            await FluentDocsOutputWriter.SaveAsync(success, "presentations", "empty-slide");

            using var ms = new MemoryStream(success.Content);
            using var pptxDoc = PresentationDocument.Open(ms, false);
            Assert.Equal(1, pptxDoc.PresentationPart!.SlideParts.Count());
        }
        else
        {
            // Even if it fails, the failure should be graceful
            var failed = (GenerationResult.Failed)result;
            Assert.NotNull(failed.Error);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Edge-case: Wrong definition type
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePresentation_WrongDefinitionType_ReturnsFailure()
    {
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var wrongDef = SampleDefinitions.MinimalDocument(GeneratedFileFormat.Docx);

        var result = await generator.GenerateAsync(wrongDef);

        Assert.True(result.IsFailure, "passing document definition to PPTX generator should fail");
    }

    public void Dispose() => _host.Dispose();
}
