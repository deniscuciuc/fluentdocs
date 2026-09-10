using FluentDocs.Abstractions.Builders.Impl;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Static entry point for constructing file definitions using the fluent builder API.
/// <para>
/// Usage:
/// <code>
/// var doc = GenerationBuilder.Document()
///     .WithMetadata(m => m.Title("Report").Author("AI Agent"))
///     .AddSection(s => s
///         .AddHeading("Summary", HeadingLevel.H1)
///         .AddParagraph("This is the content."))
///     .Build();
///
/// var sheet = GenerationBuilder.Spreadsheet()
///     .AddSheet("Data", s => s
///         .AddColumn("Name")
///         .AddRow(r => r.AddCell("Alice").AddCell(42)))
///     .Build();
///
/// var pptx = GenerationBuilder.Presentation()
///     .WithTheme(t => t.PrimaryColor("#1a73e8"))
///     .AddSlide(s => s
///         .WithLayout(SlideLayout.TitleSlide)
///         .AddTextBox(tb => tb
///             .AtPosition(50, 50).WithSize(200, 40)
///             .AddText("Hello World")))
///     .Build();
/// </code>
/// </para>
/// </summary>
public static class GenerationBuilder
{
    /// <summary>
    /// Creates a new document builder for DOCX, PDF, Markdown, or PlainText output.
    /// </summary>
    public static IDocumentBuilder Document()
    {
        return new DocumentBuilder();
    }

    /// <summary>
    /// Creates a new spreadsheet builder for XLSX, CSV, or TSV output.
    /// </summary>
    public static ISpreadsheetBuilder Spreadsheet()
    {
        return new SpreadsheetBuilder();
    }

    /// <summary>
    /// Creates a new presentation builder for PPTX output.
    /// </summary>
    public static IPresentationBuilder Presentation()
    {
        return new PresentationBuilder();
    }
}
