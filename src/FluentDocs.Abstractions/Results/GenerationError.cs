namespace FluentDocs.Abstractions.Results;

/// <summary>
/// Describes a generation error.
/// </summary>
public sealed record GenerationError
{
    /// <summary>Machine-readable error code (e.g. "INVALID_DEFINITION", "RENDER_FAILED").</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable error description.</summary>
    public required string Message { get; init; }

    /// <summary>Original exception, if any.</summary>
    public Exception? Exception { get; init; }
}
