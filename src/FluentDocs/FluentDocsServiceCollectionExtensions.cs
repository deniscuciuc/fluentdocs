using FluentDocs.Abstractions;
using FluentDocs.Charts;
using FluentDocs.Documents.Docx;
using FluentDocs.Documents.Markdown;
using FluentDocs.Documents.Pdf;
using FluentDocs.Presentations;
using FluentDocs.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluentDocs;

/// <summary>
/// Registers all file generation services with the DI container.
/// </summary>
public static class FluentDocsServiceCollectionExtensions
{
    /// <summary>
    /// Registers all file generators (documents, spreadsheets, presentations) and the chart renderer.
    /// </summary>
    public static IServiceCollection AddFluentDocs(this IServiceCollection services)
    {
        services.AddChartRendering();
        services.AddDocumentGeneration();
        services.AddSpreadsheetGeneration();
        services.AddPresentationGeneration();

        return services;
    }

    /// <summary>
    /// Registers document generators: DOCX, PDF, Markdown, and PlainText.
    /// Also registers chart rendering (required for chart embedding in documents).
    /// </summary>
    public static IServiceCollection AddDocumentGeneration(this IServiceCollection services)
    {
        services.AddChartRendering();

        services.AddSingleton<IFileGenerator, DocxFileGenerator>();
        services.AddSingleton<IFileGenerator, PdfFileGenerator>();
        services.AddSingleton<IFileGenerator, MarkdownFileGenerator>();
        services.AddSingleton<IFileGenerator, PlainTextFileGenerator>();

        return services;
    }

    /// <summary>
    /// Registers spreadsheet generators: XLSX, CSV, and TSV.
    /// Also registers chart rendering (required for chart embedding in XLSX).
    /// </summary>
    public static IServiceCollection AddSpreadsheetGeneration(this IServiceCollection services)
    {
        services.AddChartRendering();

        services.AddSingleton<IFileGenerator, XlsxFileGenerator>();
        services.AddSingleton<IFileGenerator, CsvFileGenerator>();
        services.AddSingleton<IFileGenerator, TsvFileGenerator>();

        return services;
    }

    /// <summary>
    /// Registers the PPTX presentation generator.
    /// Also registers chart rendering (required for chart embedding in presentations).
    /// </summary>
    public static IServiceCollection AddPresentationGeneration(this IServiceCollection services)
    {
        services.AddChartRendering();

        services.AddSingleton<IFileGenerator, PptxFileGenerator>();

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IChartRenderer"/> (ScottPlot-based).
    /// Safe to call multiple times — uses <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService,TImplementation}"/>.
    /// </summary>
    public static IServiceCollection AddChartRendering(this IServiceCollection services)
    {
        services.TryAddSingleton<IChartRenderer, ScottPlotChartRenderer>();

        return services;
    }
}
