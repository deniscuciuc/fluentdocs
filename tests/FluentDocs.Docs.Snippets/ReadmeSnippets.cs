using System.Diagnostics;
using FluentDocs;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Builders;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.DependencyInjection;

namespace FluentDocs.Docs.Snippets;

/// <summary>README — registration.</summary>
internal static class Registration
{
    internal static void AddFluentDocs(IServiceCollection services)
    {
        services.AddFluentDocs();
    }
}

/// <summary>README — the quick-start report service.</summary>
internal sealed class ReportService(IEnumerable<IFileGenerator> generators)
{
    public async Task<byte[]> BuildAsync(CancellationToken cancellationToken)
    {
        var document = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Pdf)
            .WithFileName("monthly-report")
            .WithMetadata(m => m.Title("Monthly Report").Author("Finance"))
            .AddSection(s => s
                .AddHeading("Q4 Summary", HeadingLevel.H1)
                .AddParagraph("Revenue increased by 15% year over year.")
                .AddTable(t => t
                    .AddColumn("Quarter")
                    .AddColumn("Revenue")
                    .AddRow(r => r.AddCell("Q1").AddCell("$1.2M"))
                    .AddRow(r => r.AddCell("Q2").AddCell("$1.4M"))))
            .Build();

        var generator = generators.First(g => g.SupportedFormat == GeneratedFileFormat.Pdf);
        var result = await generator.GenerateAsync(document, ct: cancellationToken);

        return result switch
        {
            GenerationResult.Succeeded ok => ok.Content,
            GenerationResult.Failed failed => throw new InvalidOperationException(failed.Error.Message),
            _ => throw new UnreachableException(),
        };
    }
}

/// <summary>README — the three builders.</summary>
internal static class Builders
{
    internal static void Spreadsheet()
    {
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Xlsx)
            .AddSheet("Data", s => s
                .AddColumn(c => c.WithHeader("Name").AutoWidth())
                .AddColumn("Score")
                .AddRow(r => r.AddCell("Alice").AddCell(42)))
            .Build();

        _ = sheet;
    }

    internal static void Presentation()
    {
        var deck = GenerationBuilder.Presentation()
            .WithTheme(t => t.PrimaryColor("#1a73e8"))
            .AddSlide(s => s
                .WithLayout(SlideLayout.TitleSlide)
                .AddTextBox(tb => tb.AtPosition(50, 50).WithSize(200, 40).AddText("Hello")))
            .Build();

        _ = deck;
    }
}
