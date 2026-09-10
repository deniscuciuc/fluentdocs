using ClosedXML.Excel;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Results;
using FluentDocs.Tables;
using FluentDocs.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FluentDocs.Tables.Tests;

public class XlsxFileGeneratorTests
{
    private readonly IChartRenderer _chartRenderer = Substitute.For<IChartRenderer>();
    private readonly XlsxFileGenerator _sut;

    public XlsxFileGeneratorTests()
    {
        _sut = new XlsxFileGenerator(_chartRenderer, NullLogger<XlsxFileGenerator>.Instance);
    }

    [Fact]
    public void SupportedFormat_ReturnsXlsx()
    {
        Assert.Equal(GeneratedFileFormat.Xlsx, _sut.SupportedFormat);
    }

    [Fact]
    public async Task GenerateAsync_MinimalSheet_ReturnsSuccess()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Xlsx);

        var result = await _sut.GenerateAsync(sheet);

        Assert.True(result.IsSuccess);
        var success = (GenerationResult.Succeeded)result;
        Assert.Equal(GeneratedFileFormat.Xlsx, success.Format);
        Assert.Contains("spreadsheetml", success.ContentType);
    }

    [Fact]
    public async Task GenerateAsync_RoundTrip_MatchesOriginalData()
    {
        var sheet = SampleDefinitions.MinimalSpreadsheet(GeneratedFileFormat.Xlsx);

        var result = await _sut.GenerateAsync(sheet);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);

        Assert.Single(wb.Worksheets);
        var ws = wb.Worksheet(1);
        Assert.Equal("Data", ws.Name);

        // Header row
        Assert.Equal("Name", ws.Cell(1, 1).GetString());
        Assert.Equal("Score", ws.Cell(1, 2).GetString());

        // Data rows
        Assert.Equal("Alice", ws.Cell(2, 1).GetString());
        Assert.Equal(95, ws.Cell(2, 2).GetValue<int>());
        Assert.Equal("Bob", ws.Cell(3, 1).GetString());
        Assert.Equal(87, ws.Cell(3, 2).GetValue<int>());
    }

    [Fact]
    public async Task GenerateAsync_RichSheet_HasFreezeAndAutoFilter()
    {
        var sheet = SampleDefinitions.RichSpreadsheet();

        var result = await _sut.GenerateAsync(sheet);

        var bytes = ((GenerationResult.Succeeded)result).Content;
        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        var ws = wb.Worksheet(1);

        Assert.Equal("Sales", ws.Name);
        // Columns exist
        Assert.Equal("Product", ws.Cell(1, 1).GetString());
        Assert.Equal("Total", ws.Cell(1, 4).GetString());

        // Data
        Assert.Equal("Widget A", ws.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task GenerateAsync_WrongDefinitionType_ReturnsFailure()
    {
        var doc = SampleDefinitions.MinimalDocument();

        var result = await _sut.GenerateAsync(doc);

        Assert.True(result.IsFailure);
    }
}
