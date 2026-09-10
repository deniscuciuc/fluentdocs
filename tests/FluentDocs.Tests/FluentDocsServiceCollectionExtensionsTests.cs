using FluentDocs;
using FluentDocs.Abstractions;
using FluentDocs.Charts;
using FluentDocs.Documents.Docx;
using FluentDocs.Documents.Markdown;
using FluentDocs.Documents.Pdf;
using FluentDocs.Presentations;
using FluentDocs.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace FluentDocs.Tests;

public class FluentDocsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFluentDocs_RegistersAllGenerators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFluentDocs();

        var provider = services.BuildServiceProvider();
        var generators = provider.GetServices<IFileGenerator>().ToList();

        Assert.Equal(8, generators.Count);
        Assert.Single(generators.Where(g => g is DocxFileGenerator));
        Assert.Single(generators.Where(g => g is PdfFileGenerator));
        Assert.Single(generators.Where(g => g is MarkdownFileGenerator));
        Assert.Single(generators.Where(g => g is PlainTextFileGenerator));
        Assert.Single(generators.Where(g => g is XlsxFileGenerator));
        Assert.Single(generators.Where(g => g is CsvFileGenerator));
        Assert.Single(generators.Where(g => g is TsvFileGenerator));
        Assert.Single(generators.Where(g => g is PptxFileGenerator));
    }

    [Fact]
    public void AddFluentDocs_RegistersChartRenderer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFluentDocs();

        var provider = services.BuildServiceProvider();
        var renderer = provider.GetService<IChartRenderer>();

        Assert.NotNull(renderer);
        Assert.IsType<ScottPlotChartRenderer>(renderer);
    }

    [Fact]
    public void AddDocumentGeneration_RegistersOnlyDocGenerators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDocumentGeneration();

        var provider = services.BuildServiceProvider();
        var generators = provider.GetServices<IFileGenerator>().ToList();

        Assert.Equal(4, generators.Count);
        Assert.Single(generators.Where(g => g is DocxFileGenerator));
        Assert.Single(generators.Where(g => g is PdfFileGenerator));
        Assert.Single(generators.Where(g => g is MarkdownFileGenerator));
        Assert.Single(generators.Where(g => g is PlainTextFileGenerator));
    }

    [Fact]
    public void AddSpreadsheetGeneration_RegistersOnlySpreadsheetGenerators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSpreadsheetGeneration();

        var provider = services.BuildServiceProvider();
        var generators = provider.GetServices<IFileGenerator>().ToList();

        Assert.Equal(3, generators.Count);
        Assert.Single(generators.Where(g => g is XlsxFileGenerator));
        Assert.Single(generators.Where(g => g is CsvFileGenerator));
        Assert.Single(generators.Where(g => g is TsvFileGenerator));
    }

    [Fact]
    public void AddPresentationGeneration_RegistersOnlyPptx()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPresentationGeneration();

        var provider = services.BuildServiceProvider();
        var generators = provider.GetServices<IFileGenerator>().ToList();

        Assert.Single(generators.Where(g => g is PptxFileGenerator));
    }

    [Fact]
    public void AddChartRendering_CalledMultipleTimes_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddChartRendering();
        services.AddChartRendering();
        services.AddChartRendering();

        var provider = services.BuildServiceProvider();
        var renderers = provider.GetServices<IChartRenderer>().ToList();

        Assert.Single(renderers);
    }
}
