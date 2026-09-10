using System.Diagnostics;
using System.Globalization;
using System.Text;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Documents.Markdown;

/// <summary>
/// Generates Markdown files from a <see cref="DocumentDefinition"/>.
/// </summary>
public sealed class MarkdownFileGenerator(IChartRenderer chartRenderer, ILogger<MarkdownFileGenerator> logger)
    : IFileGenerator
{
    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.Markdown;

    public async Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not DocumentDefinition docDef)
            return GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(DocumentDefinition)} but received {definition.GetType().Name}.");

        using var activity = GenerationDiagnostics.StartGenerationActivity(SupportedFormat.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating Markdown document '{FileName}'.", docDef.FileName);

        try
        {
            var sb = new StringBuilder();

            RenderFrontMatter(sb, docDef.Metadata);

            foreach (var section in docDef.Sections)
                foreach (var element in section.Elements)
                    await RenderElementAsync(sb, element, ct).ConfigureAwait(false);

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            sw.Stop();
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "success"));
            GenerationDiagnostics.GenerationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));
            GenerationDiagnostics.OutputSize.Record(bytes.Length,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));

            return GenerationResult.Success(bytes, "text/markdown", GeneratedFileFormat.Markdown);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate Markdown document '{FileName}'.", docDef.FileName);
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "failure"));
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    private static void RenderFrontMatter(StringBuilder sb, DocumentMetadata metadata)
    {
        if (metadata.Title is null && metadata.Author is null && metadata.CreatedAt is null
            && metadata.Keywords.Count == 0)
            return;

        sb.AppendLine("---");
        if (metadata.Title is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"title: \"{EscapeYaml(metadata.Title)}\"");
        if (metadata.Author is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"author: \"{EscapeYaml(metadata.Author)}\"");
        if (metadata.Subject is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"subject: \"{EscapeYaml(metadata.Subject)}\"");
        if (metadata.Description is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"description: \"{EscapeYaml(metadata.Description)}\"");
        if (metadata.Language is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"lang: \"{EscapeYaml(metadata.Language)}\"");
        if (metadata.CreatedAt.HasValue)
            sb.AppendLine(CultureInfo.InvariantCulture, $"date: \"{metadata.CreatedAt.Value:yyyy-MM-dd}\"");
        if (metadata.Keywords.Count > 0)
        {
            var kws = string.Join(", ", metadata.Keywords.Select(k => $"\"{EscapeYaml(k)}\""));
            sb.AppendLine(CultureInfo.InvariantCulture, $"keywords: [{kws}]");
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    private async Task RenderElementAsync(StringBuilder sb, IDocumentElement element, CancellationToken ct)
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
                RenderImage(sb, image);
                break;
            case TableContentElement table:
                RenderTable(sb, table);
                break;
            case ChartElement chart:
                await RenderChartAsync(sb, chart, ct).ConfigureAwait(false);
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
                sb.AppendLine("<div style=\"page-break-after: always\"></div>");
                sb.AppendLine();
                break;
            case HorizontalRuleElement:
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
                break;
        }
    }

    private static void RenderHeading(StringBuilder sb, HeadingElement heading)
    {
        var prefix = new string('#', (int)heading.Level);
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{prefix} {heading.Text}");
        sb.AppendLine();
    }

    private static void RenderParagraph(StringBuilder sb, ParagraphElement paragraph)
    {
        foreach (var run in paragraph.Runs) sb.Append(FormatTextRun(run));

        sb.AppendLine();
        sb.AppendLine();
    }

    private static void RenderImage(StringBuilder sb, ImageElement image)
    {
        var alt = image.Image.AltText ?? "image";
        var format = image.Image.Format.ToString().ToLowerInvariant();
        var base64 = Convert.ToBase64String(image.Image.Data);
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"![{alt}](data:image/{format};base64,{base64})");
        if (image.Image.Caption is not null)
            sb.AppendLine(CultureInfo.InvariantCulture, $"*{image.Image.Caption}*");
        sb.AppendLine();
    }

    private static void RenderTable(StringBuilder sb, TableContentElement table)
    {
        sb.AppendLine();

        // Header row
        sb.Append('|');
        foreach (var col in table.Columns) sb.Append(CultureInfo.InvariantCulture, $" {col.Header ?? ""} |");

        sb.AppendLine();

        // Separator row with alignment
        sb.Append('|');
        foreach (var col in table.Columns)
            sb.Append(col.Alignment switch
            {
                TextAlignment.Center => " :---: |",
                TextAlignment.Right => " ---: |",
                _ => " --- |"
            });

        sb.AppendLine();

        // Data rows
        foreach (var row in table.Rows)
        {
            sb.Append('|');
            foreach (var cell in row.Cells)
            {
                var text = cell.Text ?? (cell.Runs is not null
                    ? string.Concat(cell.Runs.Select(r => r.Text))
                    : "");
                sb.Append(CultureInfo.InvariantCulture, $" {text} |");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
    }

    private async Task RenderChartAsync(StringBuilder sb, ChartElement chart, CancellationToken ct)
    {
        var rendered = await chartRenderer.RenderAsync(chart.Chart, new ChartRenderOptions(), ct).ConfigureAwait(false);
        var base64 = Convert.ToBase64String(rendered.ImageData);
        var format = rendered.Format.ToString().ToLowerInvariant();
        var alt = chart.Chart.Title ?? "chart";

        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"![{alt}](data:image/{format};base64,{base64})");
        sb.AppendLine();
    }

    private static void RenderList(StringBuilder sb, ListElement list, int indent)
    {
        var counter = 1;
        foreach (var item in list.Items)
        {
            var prefix = list.Ordered ? $"{counter}. " : "- ";
            var indentStr = new string(' ', indent * 2);
            var text = string.Concat(item.Content.Select(r => FormatTextRun(r)));
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
        sb.AppendLine(CultureInfo.InvariantCulture, $"```{code.Language ?? ""}");
        sb.AppendLine(code.Code);
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private static void RenderBlockQuote(StringBuilder sb, BlockQuoteElement quote)
    {
        sb.AppendLine();
        foreach (var run in quote.Content)
        {
            var lines = FormatTextRun(run).Split('\n');
            foreach (var line in lines) sb.AppendLine(CultureInfo.InvariantCulture, $"> {line}");
        }

        sb.AppendLine();
    }

    private static string FormatTextRun(TextRun run)
    {
        var text = run.Text;
        if (run.Style is null) return text;

        if (run.Style.Bold && run.Style.Italic)
            text = $"***{text}***";
        else if (run.Style.Bold)
            text = $"**{text}**";
        else if (run.Style.Italic)
            text = $"*{text}*";

        if (run.Style.Strikethrough)
            text = $"~~{text}~~";
        if (run.Style.Superscript)
            text = $"<sup>{text}</sup>";
        if (run.Style.Subscript)
            text = $"<sub>{text}</sub>";

        return text;
    }

    private static string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
