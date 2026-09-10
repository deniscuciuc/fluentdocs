using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Results;

namespace FluentDocs.Abstractions;

/// <summary>
/// Generates a file from a <see cref="FileDefinition"/>.
/// Multiple implementations are registered in DI — consumers dispatch to the correct one
/// based on <see cref="SupportedFormat"/>.
/// </summary>
public interface IFileGenerator
{
    /// <summary>The output format this generator handles.</summary>
    GeneratedFileFormat SupportedFormat { get; }

    /// <summary>
    /// Generates a file from the given definition.
    /// </summary>
    /// <param name="definition">The declarative file structure to render.</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the generated file bytes or an error.</returns>
    Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default);
}
