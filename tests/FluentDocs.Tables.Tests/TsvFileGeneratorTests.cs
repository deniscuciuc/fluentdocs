using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Tables;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace FluentDocs.Tables.Tests;

public class TsvFileGeneratorTests
{
    private readonly TsvFileGenerator _sut = new(NullLogger<TsvFileGenerator>.Instance);

    [Fact]
    public void SupportedFormat_ReturnsTsv()
    {
        Assert.Equal(GeneratedFileFormat.Tsv, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalSheet_ReturnsSuccess()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Tsv);

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsSuccess);
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Tsv, success.Format);
        Assert.Equal("text/tab-separated-values", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_UsesTabDelimiter()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Tsv);

        var result = await _sut.GenerateAsync(sheet);

        var tsv = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);
        using var reader = new StringReader(tsv);
        using var csvReader = new CsvReader(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = "\t" });

        csvReader.Read();
        csvReader.ReadHeader();
        Assert.Contains("Name", csvReader.HeaderRecord!);

        csvReader.Read();
        Assert.Equal("Alice", csvReader.GetField(0));
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var doc = SampleDefinitions.MinimalDocument();

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsFailure);
    }
}
