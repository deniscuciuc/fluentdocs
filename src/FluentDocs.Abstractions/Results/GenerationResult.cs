using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Results;

/// <summary>
/// Result of a file generation operation.
/// Uses a sealed hierarchy to represent Success | Failure.
/// </summary>
public abstract record GenerationResult
{
    private GenerationResult()
    {
    }

    /// <summary>File was generated successfully.</summary>
    public sealed record Succeeded(
        byte[] Content,
        string ContentType,
        GeneratedFileFormat Format) : GenerationResult;

    /// <summary>File generation failed.</summary>
    public sealed record Failed(GenerationError Error) : GenerationResult;

    public bool IsSuccess => this is Succeeded;
    public bool IsFailure => this is Failed;

    /// <summary>Creates a success result.</summary>
    public static GenerationResult Success(byte[] content, string contentType, GeneratedFileFormat format)
    {
        return new Succeeded(content, contentType, format);
    }

    /// <summary>Creates a failure result.</summary>
    public static GenerationResult Failure(GenerationError error)
    {
        return new Failed(error);
    }

    /// <summary>Creates a failure result from an exception.</summary>
    public static GenerationResult Failure(string code, string message, Exception? exception = null)
    {
        return new Failed(new GenerationError { Code = code, Message = message, Exception = exception });
    }
}
