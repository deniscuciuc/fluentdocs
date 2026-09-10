using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FluentDocs.Abstractions;

/// <summary>
/// Centralized diagnostics constants and instruments for the FluentDocs module.
/// Provides OpenTelemetry-compatible <see cref="ActivitySource"/> and <see cref="Meter"/>
/// for distributed tracing and metrics.
/// </summary>
public static class GenerationDiagnostics
{
    /// <summary>Diagnostic source name for all generation activities.</summary>
    public const string ActivitySourceName = "FluentDocs";

    /// <summary>Meter name for all generation metrics.</summary>
    public const string MeterName = "FluentDocs";

    /// <summary>Activity source for distributed tracing of file generation operations.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Meter for generation metrics (counters, histograms).</summary>
    public static readonly Meter Meter = new(MeterName);

    /// <summary>Counter: total number of files generated (tagged by format and outcome).</summary>
    public static readonly Counter<long> FilesGenerated =
        Meter.CreateCounter<long>(
            "generation.files.created",
            "files",
            "Total number of files generated");

    /// <summary>Histogram: generation duration in milliseconds (tagged by format).</summary>
    public static readonly Histogram<double> GenerationDuration =
        Meter.CreateHistogram<double>(
            "generation.duration.ms",
            "ms",
            "File generation duration in milliseconds");

    /// <summary>Histogram: output file size in bytes (tagged by format).</summary>
    public static readonly Histogram<long> OutputSize =
        Meter.CreateHistogram<long>(
            "generation.output.bytes",
            "bytes",
            "Generated file size in bytes");

    /// <summary>Starts a new activity for a file generation operation.</summary>
    public static Activity? StartGenerationActivity(string format)
    {
        var activity = ActivitySource.StartActivity("generation.generate", ActivityKind.Internal);
        activity?.SetTag("generation.format", format);
        return activity;
    }

    /// <summary>Starts a new activity for a chart rendering operation.</summary>
    public static Activity? StartChartRenderActivity(string chartType)
    {
        var activity = ActivitySource.StartActivity("generation.chart.render", ActivityKind.Internal);
        activity?.SetTag("generation.chart.type", chartType);
        return activity;
    }
}
