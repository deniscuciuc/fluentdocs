using FluentDocs;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Testing;

/// <summary>
/// Builds a real DI service provider using the production registration extensions.
/// Resolves <see cref="IFileGenerator"/> and <see cref="IChartRenderer"/> services
/// for integration tests — no mocks, no stubs.
/// </summary>
public sealed class FluentDocsTestHost : IDisposable
{
    private readonly ServiceProvider _rootProvider;
    private readonly IServiceScope _scope;

    public IServiceProvider Services => _scope.ServiceProvider;

    private FluentDocsTestHost(ServiceProvider rootProvider, IServiceScope scope)
    {
        _rootProvider = rootProvider;
        _scope = scope;
    }

    /// <summary>
    /// Creates a host with all file generators and chart rendering registered.
    /// </summary>
    public static FluentDocsTestHost Create()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddConsole());

        services.AddFluentDocs();

        var rootProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        var scope = rootProvider.CreateScope();

        return new FluentDocsTestHost(rootProvider, scope);
    }

    /// <summary>
    /// Resolves the <see cref="IFileGenerator"/> for the specified format.
    /// </summary>
    public IFileGenerator ResolveGenerator(GeneratedFileFormat format) =>
        Services.GetServices<IFileGenerator>()
            .FirstOrDefault(g => g.SupportedFormat == format)
        ?? throw new InvalidOperationException($"No generator registered for format '{format}'.");

    /// <summary>
    /// Resolves all registered <see cref="IFileGenerator"/> instances.
    /// </summary>
    public IEnumerable<IFileGenerator> ResolveAllGenerators() =>
        Services.GetServices<IFileGenerator>();

    /// <summary>
    /// Resolves the <see cref="IChartRenderer"/>.
    /// </summary>
    public IChartRenderer ResolveChartRenderer() =>
        Services.GetRequiredService<IChartRenderer>();

    public void Dispose()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }
}
