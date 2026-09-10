using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Spreadsheets;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Tables;

/// <summary>
/// Shared helper for generating delimited text (CSV/TSV) from a <see cref="SpreadsheetDefinition"/>.
/// </summary>
internal static class DelimitedTextHelper
{
    public static GenerationResult Generate(
        SpreadsheetDefinition sheetDef,
        string delimiter,
        string contentType,
        GeneratedFileFormat format,
        ILogger logger)
    {
        using var activity = GenerationDiagnostics.StartGenerationActivity(format.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating {Format} spreadsheet '{FileName}'.", format, sheetDef.FileName);

        try
        {
            if (sheetDef.Sheets.Count == 0)
                return GenerationResult.Failure("EMPTY_DEFINITION", "Spreadsheet has no sheets to export.");

            var sheet = sheetDef.Sheets[0];

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, new UTF8Encoding(false), leaveOpen: true);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = false
            };

            using (var csv = new CsvWriter(writer, config, true))
            {
                // Write column headers
                if (sheet.Columns.Count > 0)
                {
                    foreach (var col in sheet.Columns) csv.WriteField(col.Header ?? "");

                    csv.NextRecord();
                }

                // Write data rows
                foreach (var row in sheet.Rows)
                {
                    var colCount = Math.Max(sheet.Columns.Count, row.Cells.Count);
                    for (var i = 0; i < colCount; i++)
                        if (i < row.Cells.Count)
                            csv.WriteField(FormatCellValue(row.Cells[i]));
                        else
                            csv.WriteField("");

                    csv.NextRecord();
                }
            }

            writer.Flush();
            var bytes = memoryStream.ToArray();

            sw.Stop();
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", format.ToString()),
                new KeyValuePair<string, object?>("outcome", "success"));
            GenerationDiagnostics.GenerationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("format", format.ToString()));
            GenerationDiagnostics.OutputSize.Record(bytes.Length,
                new KeyValuePair<string, object?>("format", format.ToString()));

            return GenerationResult.Success(bytes, contentType, format);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate {Format} spreadsheet '{FileName}'.", format, sheetDef.FileName);
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", format.ToString()),
                new KeyValuePair<string, object?>("outcome", "failure"));
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    private static string FormatCellValue(CellValue cell)
    {
        if (cell.Value is null) return "";
        return cell.Value switch
        {
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "TRUE" : "FALSE",
            _ => cell.Value.ToString() ?? ""
        };
    }
}
