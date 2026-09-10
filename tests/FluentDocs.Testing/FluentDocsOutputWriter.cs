using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Results;

namespace FluentDocs.Testing;

/// <summary>
/// Writes generated files to the testdata/generation-output/ directory
/// so they can be manually inspected after a test run.
/// </summary>
public static class FluentDocsOutputWriter
{
    private static readonly Lazy<string> OutputRoot = new(FindOutputRoot);

    /// <summary>
    /// Saves a successfully generated file to the appropriate subdirectory.
    /// </summary>
    public static async Task<string> SaveAsync(
        GenerationResult.Succeeded result,
        string subdirectory,
        string fileNameStem)
    {
        var extension = GetExtension(result.Format);
        var dir = Path.Combine(OutputRoot.Value, subdirectory);
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{fileNameStem}{extension}");
        await File.WriteAllBytesAsync(filePath, result.Content);

        return filePath;
    }

    /// <summary>
    /// Saves a rendered chart image to the charts/ subdirectory.
    /// </summary>
    public static async Task<string> SaveChartAsync(
        RenderedChart chart,
        string fileNameStem)
    {
        var extension = chart.Format == ImageFormat.Svg ? ".svg" : ".png";
        var dir = Path.Combine(OutputRoot.Value, "charts");
        Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{fileNameStem}{extension}");
        await File.WriteAllBytesAsync(filePath, chart.ImageData);

        return filePath;
    }

    /// <summary>
    /// Returns the subdirectory path for a given format.
    /// </summary>
    public static string SubdirectoryFor(GeneratedFileFormat format) => format switch
    {
        GeneratedFileFormat.Docx => "documents/docx",
        GeneratedFileFormat.Pdf => "documents/pdf",
        GeneratedFileFormat.Markdown => "documents/markdown",
        GeneratedFileFormat.PlainText => "documents/plaintext",
        GeneratedFileFormat.Csv => "tables/csv",
        GeneratedFileFormat.Tsv => "tables/tsv",
        GeneratedFileFormat.Xlsx => "tables/xlsx",
        GeneratedFileFormat.Pptx => "presentations",
        _ => "other"
    };

    private static string GetExtension(GeneratedFileFormat format) => format switch
    {
        GeneratedFileFormat.Docx => ".docx",
        GeneratedFileFormat.Pdf => ".pdf",
        GeneratedFileFormat.Markdown => ".md",
        GeneratedFileFormat.PlainText => ".txt",
        GeneratedFileFormat.Csv => ".csv",
        GeneratedFileFormat.Tsv => ".tsv",
        GeneratedFileFormat.Xlsx => ".xlsx",
        GeneratedFileFormat.Pptx => ".pptx",
        _ => ".bin"
    };

    private static string FindOutputRoot()
    {
        // Walk up from the test assembly's output directory to find
        // tests/testdata/generation-output/
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "testdata", "generation-output");
            if (Directory.Exists(candidate))
                return candidate;

            // Also check if we're already inside the fluent-files directory
            candidate = Path.Combine(current.FullName, "testdata", "generation-output");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        // Fallback: create in temp
        var fallback = Path.Combine(Path.GetTempPath(), "generation-output");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}
