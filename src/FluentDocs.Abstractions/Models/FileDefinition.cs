using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Models;

/// <summary>
/// Abstract base for all file definition models.
/// Each format-specific definition (document, spreadsheet, presentation)
/// inherits from this and captures the full declarative structure of the file to generate.
/// </summary>
public abstract record FileDefinition
{
    /// <summary>Suggested output file name (without extension).</summary>
    public string? FileName { get; init; }

    /// <summary>Target output format.</summary>
    public required GeneratedFileFormat Format { get; init; }
}
