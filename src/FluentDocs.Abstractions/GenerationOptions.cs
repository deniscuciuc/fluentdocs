using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions;

/// <summary>
/// Options that control the file generation process.
/// </summary>
public sealed record GenerationOptions
{
    /// <summary>Whether to embed fonts in the output (for PDF/DOCX).</summary>
    public bool EmbedFonts { get; init; }

    /// <summary>Image compression level for embedded images.</summary>
    public ImageCompression ImageCompression { get; init; } = ImageCompression.Medium;

    /// <summary>Per-file timeout. Null means no timeout.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Arbitrary metadata to include in the generated file.</summary>
    public IReadOnlyDictionary<string, string> CustomMetadata { get; init; } =
        new Dictionary<string, string>();
}
