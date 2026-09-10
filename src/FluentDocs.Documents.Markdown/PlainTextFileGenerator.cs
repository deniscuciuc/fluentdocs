using System.Diagnostics;
using System.Globalization;
using System.Text;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Documents.Markdown;

/// <summary>
/// Generates plain text files from a <see cref="DocumentDefinition"/>.
/// Strips all formatting — pure text content extraction.
/// </summary>
public sealed class PlainTextFileGenerator(ILogger<PlainTextFileGenerator> logger) : IFileGenerator
{
    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.PlainText;

    public Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not DocumentDefinition docDef)
            return Task.FromResult(GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(DocumentDefinition)} but received {definition.GetType().Name}."));

        using var activity = GenerationDiagnostics.StartGenerationActivity(SupportedFormat.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating plain text document '{FileName}'.", docDef.FileName);

        try
        {
            var sb = new StringBuilder();

            foreach (var section in docDef.Sections)
                foreach (var element in section.Elements)
                    RenderElement(sb, element);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            sw.Stop();
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "success"));
            GenerationDiagnostics.GenerationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));
            GenerationDiagnostics.OutputSize.Record(bytes.Length,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));

            return Task.FromResult(GenerationResult.Success(bytes, "text/plain", GeneratedFileFormat.PlainText));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate plain text document '{FileName}'.", docDef.FileName);
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "failure"));
            return Task.FromResult(GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex));
        }
    }

    private static void RenderElement(StringBuilder sb, IDocumentElement element)
    {
        switch (element)
        {
            case HeadingElement heading:
                RenderHeading(sb, heading);
                break;
            case ParagraphElement paragraph:
                RenderParagraph(sb, paragraph);
                break;
            case ImageElement image:
                sb.AppendLine(CultureInfo.InvariantCulture, $"[Image: {image.Image.AltText ?? "image"}]");
                sb.AppendLine();
                break;
            case TableContentElement table:
                RenderTable(sb, table);
                break;
            case ChartElement chart:
                sb.AppendLine(CultureInfo.InvariantCulture, $"[Chart: {chart.Chart.Title ?? "chart"}]");
                sb.AppendLine();
                break;
            case ListElement list:
                RenderList(sb, list, 0);
                break;
            case CodeBlockElement code:
                RenderCodeBlock(sb, code);
                break;
            case BlockQuoteElement quote:
                RenderBlockQuote(sb, quote);
                break;
            case PageBreakElement:
                sb.AppendLine();
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();
                break;
            case HorizontalRuleElement:
                sb.AppendLine();
                sb.AppendLine(new string('-', 80));
                sb.AppendLine();
                break;
        }
    }

    private static void RenderHeading(StringBuilder sb, HeadingElement heading)
    {
        sb.AppendLine();
        var text = heading.Text.ToUpperInvariant();
        sb.AppendLine(text);

        var underlineChar = heading.Level switch
        {
            HeadingLevel.H1 => '=',
            HeadingLevel.H2 => '-',
            _ => (char?)null
        };

        if (underlineChar.HasValue)
            sb.AppendLine(new string(underlineChar.Value, text.Length));

        sb.AppendLine();
    }

    private static void RenderParagraph(StringBuilder sb, ParagraphElement paragraph)
    {
        foreach (var run in paragraph.Runs) sb.Append(run.Text);

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void RenderTable(StringBuilder sb, TableContentElement table)
    {
        sb.AppendLine();

        // Calculate column widths
        var colCount = table.Columns.Count;
        var widths = new int[colCount];

        for (var i = 0; i < colCount; i++) widths[i] = (table.Columns[i].Header ?? "").Length;

        foreach (var row in table.Rows)
            for (var i = 0; i < Math.Min(row.Cells.Count, colCount); i++)
            {
                var text = GetCellText(row.Cells[i]);
                widths[i] = Math.Max(widths[i], text.Length);
            }

        // Header row
        for (var i = 0; i < colCount; i++)
        {
            var header = table.Columns[i].Header ?? "";
            sb.Append(header.PadRight(widths[i]));
            if (i < colCount - 1) sb.Append("  ");
        }

        sb.AppendLine();

        // Separator
        for (var i = 0; i < colCount; i++)
        {
            sb.Append(new string('-', widths[i]));
            if (i < colCount - 1) sb.Append("  ");
        }

        sb.AppendLine();

        // Data rows
        foreach (var row in table.Rows)
        {
            for (var i = 0; i < colCount; i++)
            {
                var text = i < row.Cells.Count ? GetCellText(row.Cells[i]) : "";
                sb.Append(text.PadRight(widths[i]));
                if (i < colCount - 1) sb.Append("  ");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
    }

    private static void RenderList(StringBuilder sb, ListElement list, int indent)
    {
        var counter = 1;
        foreach (var item in list.Items)
        {
            var prefix = list.Ordered ? $"{counter}) " : "- ";
            var indentStr = new string(' ', indent * 3);
            var text = string.Concat(item.Content.Select(r => r.Text));
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indentStr}{prefix}{text}");

            if (item.Nested.Count > 0)
            {
                var nestedList = new ListElement
                {
                    Ordered = list.Ordered,
                    Items = item.Nested
                };
                RenderList(sb, nestedList, indent + 1);
            }

            counter++;
        }

        if (indent == 0)
            sb.AppendLine();
    }

    private static void RenderCodeBlock(StringBuilder sb, CodeBlockElement code)
    {
        sb.AppendLine();
        foreach (var line in code.Code.Split('\n')) sb.AppendLine(CultureInfo.InvariantCulture, $"    {line}");

        sb.AppendLine();
    }

    private static void RenderBlockQuote(StringBuilder sb, BlockQuoteElement quote)
    {
        sb.AppendLine();
        foreach (var run in quote.Content)
        {
            var lines = run.Text.Split('\n');
            foreach (var line in lines) sb.AppendLine(CultureInfo.InvariantCulture, $"| {line}");
        }

        sb.AppendLine();
    }

    private static string GetCellText(TableCell cell)
    {
        if (cell.Text is not null) return cell.Text;
        if (cell.Runs is not null) return string.Concat(cell.Runs.Select(r => r.Text));
        return "";
    }
}
