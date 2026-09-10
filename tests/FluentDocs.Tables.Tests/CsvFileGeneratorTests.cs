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

public class CsvFileGeneratorTests
{
    private readonly CsvFileGenerator _sut = new(NullLogger<CsvFileGenerator>.Instance);

    [Fact]
    public void SupportedFormat_ReturnsCsv()
    {
        Assert.Equal(GeneratedFileFormat.Csv, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalSheet_ReturnsSuccess()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Csv);

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsSuccess);
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Csv, success.Format);
        Assert.Equal("text/csv", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_MatchesOriginalData()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Csv);

        var result = await _sut.GenerateAsync(sheet);

        var csv = Encoding.UTF8.GetString(((GenerationResult.Succeeded)result).Content);
        using var reader = new StringReader(csv);
        using var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        csvReader.Read();
        csvReader.ReadHeader();
        Assert.Contains("Name", csvReader.HeaderRecord!);
        Assert.Contains("Score", csvReader.HeaderRecord!);

        csvReader.Read();
        Assert.Equal("Alice", csvReader.GetField(0));
        Assert.Equal("95", csvReader.GetField(1));

        csvReader.Read();
        Assert.Equal("Bob", csvReader.GetField(0));
        Assert.Equal("87", csvReader.GetField(1));
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var doc = SampleDefinitions.MinimalDocument();

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsFailure);
    }
}
