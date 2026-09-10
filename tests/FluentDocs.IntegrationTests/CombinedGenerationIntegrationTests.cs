using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Builders;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Results;
using FluentDocs.Testing;

namespace FluentDocs.IntegrationTests;

/// <summary>
/// Cross-cutting integration tests that verify combined generation scenarios:
/// chart rendering → embedding in documents/spreadsheets/presentations,
/// and same-definition multi-format generation.
/// </summary>
public sealed class CombinedGenerationIntegrationTests : IDisposable
{
    private readonly FluentDocsTestHost _host = FluentDocsTestHost.Create();

    // ────────────────────────────────────────────────────────────────────
    // Workflow: Chart → PNG → Embed as image in DOCX
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChartToPng_ThenEmbedInDocx_ProducesValidDocx()
    {
        // Step 1: Render chart to PNG bytes
        var chartRenderer = _host.ResolveChartRenderer();
        var chartDef = SampleDefinitions.SimpleBarChart();
        var rendered = await chartRenderer.RenderAsync(chartDef);

        Assert.NotEmpty(rendered.ImageData);

        // Step 2: Build a document embedding the rendered chart bytes as an image
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .WithFileName("chart-as-image")
            .AddSection(s => s
                .AddHeading("Pre-Rendered Chart")
                .AddParagraph("This chart was rendered first, then embedded as a raw image:")
                .AddImage(img => img
                    .FromBytes(rendered.ImageData)
                    .WithFormat(ImageFormat.Png)
                    .WithAltText("Pre-rendered bar chart")
                    .WithSize(150, 100)))
            .Build();

        // Step 3: Generate DOCX
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var result = await generator.GenerateAsync(doc);

        Assert.True(result.IsSuccess, "embedding pre-rendered chart as image should succeed");
        var success = (GenerationResult.Succeeded)result;

        // Verify the DOCX contains the image
        using var ms = new MemoryStream(success.Content);
        using var wordDoc = WordprocessingDocument.Open(ms, false);
        Assert.NotEmpty(wordDoc.MainDocumentPart!.ImageParts);

        await FluentDocsOutputWriter.SaveAsync(success, "combined", "chart-as-image-docx");
    }

    // ────────────────────────────────────────────────────────────────────
    // Workflow: Chart → PNG → Embed as image in XLSX
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChartToPng_ThenEmbedInXlsx_ProducesValidXlsx()
    {
        // Step 1: Render chart
        var chartRenderer = _host.ResolveChartRenderer();
        var rendered = await chartRenderer.RenderAsync(SampleDefinitions.SimpleBarChart());

        // Step 2: Build spreadsheet with the image embedded
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Xlsx)
            .WithFileName("chart-as-image-xlsx")
            .AddSheet("Data", s => s
                .AddColumn("Category")
                .AddColumn("Value")
                .AddRow(r => r.AddCell("A").AddCell(10))
                .AddRow(r => r.AddCell("B").AddCell(20))
                .AddImage(img => img
                    .FromBytes(rendered.ImageData)
                    .WithFormat(ImageFormat.Png)
                    .WithAltText("Chart image")))
            .Build();

        // Step 3: Generate XLSX
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var result = await generator.GenerateAsync(sheet);

        Assert.True(result.IsSuccess, "embedding chart image in XLSX should succeed");
        var success = (GenerationResult.Succeeded)result;

        // Round-trip verify
        using var ms = new MemoryStream(success.Content);
        using var workbook = new XLWorkbook(ms);
        Assert.True(workbook.Worksheets.First().Pictures.Count > 0,
            "worksheet should contain the embedded chart image");

        await FluentDocsOutputWriter.SaveAsync(success, "combined", "chart-as-image-xlsx");
    }

    // ────────────────────────────────────────────────────────────────────
    // Workflow: Chart → PNG → Embed as image in PPTX
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ChartToPng_ThenEmbedInPptx_ProducesValidPptx()
    {
        // Step 1: Render chart
        var chartRenderer = _host.ResolveChartRenderer();
        var rendered = await chartRenderer.RenderAsync(SampleDefinitions.SimpleBarChart());

        // Step 2: Build presentation with the pre-rendered chart as an image
        var pres = GenerationBuilder.Presentation()
            .WithFileName("chart-as-image-pptx")
            .AddSlide(s => s
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(20, 10)
                    .WithSize(280, 25)
                    .AddText("Pre-Rendered Chart"))
                .AddImage(img => img
                    .AtPosition(30, 45)
                    .WithSize(260, 145)
                    .FromBytes(rendered.ImageData)
                    .WithFormat(ImageFormat.Png)
                    .WithAltText("Pre-rendered chart")))
            .Build();

        // Step 3: Generate PPTX
        var generator = _host.ResolveGenerator(GeneratedFileFormat.Pptx);
        var result = await generator.GenerateAsync(pres);

        Assert.True(result.IsSuccess, "embedding pre-rendered chart in PPTX should succeed");
        var success = (GenerationResult.Succeeded)result;

        using var ms = new MemoryStream(success.Content);
        using var pptxDoc = PresentationDocument.Open(ms, false);
        var slide = pptxDoc.PresentationPart!.SlideParts.First();
        Assert.NotEmpty(slide.ImageParts);

        await FluentDocsOutputWriter.SaveAsync(success, "combined", "chart-as-image-pptx");
    }

    // ────────────────────────────────────────────────────────────────────
    // Same definition → all document formats
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SameDefinition_AllDocumentFormats_AllSucceed()
    {
        var formats = new[]
        {
            GeneratedFileFormat.Docx,
            GeneratedFileFormat.Pdf,
            GeneratedFileFormat.Markdown,
            GeneratedFileFormat.PlainText
        };

        foreach (var format in formats)
        {
            var generator = _host.ResolveGenerator(format);
            var doc = SampleDefinitions.DocumentWithTableAndChart(format);

            var result = await generator.GenerateAsync(doc);

            Assert.True(result.IsSuccess, $"format {format} should succeed");
            var success = (GenerationResult.Succeeded)result;
            Assert.True(success.Content.Length > 0);

            await FluentDocsOutputWriter.SaveAsync(success, "combined", $"same-definition-{format.ToString().ToLowerInvariant()}");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Same spreadsheet → all table formats
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SameSpreadsheet_AllTableFormats_AllSucceed()
    {
        var formats = new[]
        {
            GeneratedFileFormat.Xlsx,
            GeneratedFileFormat.Csv,
            GeneratedFileFormat.Tsv
        };

        foreach (var format in formats)
        {
            var generator = _host.ResolveGenerator(format);
            var sheet = SampleDefinitions.RichSpreadsheet(format);

            var result = await generator.GenerateAsync(sheet);

            Assert.True(result.IsSuccess, $"format {format} should succeed");
            var success = (GenerationResult.Succeeded)result;

            await FluentDocsOutputWriter.SaveAsync(success, "combined", $"same-spreadsheet-{format.ToString().ToLowerInvariant()}");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // Full workflow: chart + doc + spreadsheet all generated
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullWorkflow_ChartAndDocAndSpreadsheet_AllGenerated()
    {
        // 1. Render a chart
        var chartRenderer = _host.ResolveChartRenderer();
        var chart = new ChartDefinition
        {
            Type = ChartType.Line,
            Title = "Monthly Trend",
            Series =
            [
                new ChartDataSeries
                {
                    Label = "Revenue",
                    Categories = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"],
                    Values = [10, 15, 13, 18, 20, 25]
                }
            ],
            WidthPx = 600,
            HeightPx = 400
        };
        var renderedChart = await chartRenderer.RenderAsync(chart);
        await FluentDocsOutputWriter.SaveChartAsync(renderedChart, "workflow-chart");

        // 2. Generate a document that uses the chart via AddChart
        var docGenerator = _host.ResolveGenerator(GeneratedFileFormat.Docx);
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .WithFileName("workflow-doc")
            .WithMetadata(m => m.Title("Monthly Report").Author("Pipeline"))
            .AddSection(s => s
                .AddHeading("Monthly Revenue Report")
                .AddParagraph("This report shows the revenue trend for H1.")
                .AddChart(c => c
                    .Line()
                    .WithTitle("Monthly Trend")
                    .AddSeries(ser => ser
                        .WithLabel("Revenue")
                        .WithCategories("Jan", "Feb", "Mar", "Apr", "May", "Jun")
                        .WithValues(10, 15, 13, 18, 20, 25))
                    .WithSize(600, 400))
                .AddTable(t => t
                    .AddColumn("Month").AddColumn("Revenue")
                    .AddRow(r => r.AddCell("Jan").AddCell("$10K"))
                    .AddRow(r => r.AddCell("Feb").AddCell("$15K"))
                    .AddRow(r => r.AddCell("Mar").AddCell("$13K"))))
            .Build();
        var docResult = await docGenerator.GenerateAsync(doc);
        Assert.True(docResult.IsSuccess, "document generation should succeed");
        await FluentDocsOutputWriter.SaveAsync((GenerationResult.Succeeded)docResult, "combined", "workflow-doc");

        // 3. Generate a spreadsheet with the same data
        var xlsxGenerator = _host.ResolveGenerator(GeneratedFileFormat.Xlsx);
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Xlsx)
            .WithFileName("workflow-sheet")
            .AddSheet("Revenue", s => s
                .AddColumn("Month")
                .AddColumn("Revenue")
                .AddRow(r => r.AddCell("Jan").AddCell(10))
                .AddRow(r => r.AddCell("Feb").AddCell(15))
                .AddRow(r => r.AddCell("Mar").AddCell(13))
                .AddRow(r => r.AddCell("Apr").AddCell(18))
                .AddRow(r => r.AddCell("May").AddCell(20))
                .AddRow(r => r.AddCell("Jun").AddCell(25))
                .AddChart(c => c
                    .Line()
                    .WithTitle("Revenue Trend")
                    .AddSeries(ser => ser
                        .WithLabel("Revenue")
                        .WithCategories("Jan", "Feb", "Mar", "Apr", "May", "Jun")
                        .WithValues(10, 15, 13, 18, 20, 25))
                    .WithSize(500, 300)))
            .Build();
        var sheetResult = await xlsxGenerator.GenerateAsync(sheet);
        Assert.True(sheetResult.IsSuccess, "spreadsheet generation should succeed");
        await FluentDocsOutputWriter.SaveAsync((GenerationResult.Succeeded)sheetResult, "combined", "workflow-sheet");
    }

    public void Dispose() => _host.Dispose();
}
