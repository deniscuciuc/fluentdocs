using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Spreadsheets;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Tables;

/// <summary>
/// Generates CSV files from a <see cref="SpreadsheetDefinition"/> using CsvHelper.
/// Only the first sheet is exported (CSV is a single-sheet format).
/// </summary>
public sealed class CsvFileGenerator(ILogger<CsvFileGenerator> logger) : IFileGenerator
{
    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.Csv;

    public Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not SpreadsheetDefinition sheetDef)
            return Task.FromResult(GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(SpreadsheetDefinition)} but received {definition.GetType().Name}."));

        return Task.FromResult(DelimitedTextHelper.Generate(
            sheetDef, ",", "text/csv", GeneratedFileFormat.Csv, logger));
    }
}
