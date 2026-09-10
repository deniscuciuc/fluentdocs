using FluentDocs.Abstractions.Builders;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Models.Spreadsheets;

namespace FluentDocs.Abstractions.Tests;

public class DocumentBuilderTests
{
    [Fact]
    public void Build_MinimalDocument_HasCorrectFormat()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .Build();

        Assert.Equal(GeneratedFileFormat.Docx, doc.Format);
    }

    [Fact]
    public void Build_WithMetadata_SetsAllFields()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Pdf)
            .WithFileName("report")
            .WithMetadata(m => m
                .Title("My Report")
                .Author("Author Name")
                .Subject("Test")
                .Description("Desc")
                .AddKeyword("test")
                .Language("en"))
            .Build();

        Assert.Equal("report", doc.FileName);
        Assert.Equal("My Report", doc.Metadata.Title);
        Assert.Equal("Author Name", doc.Metadata.Author);
        Assert.Equal("Test", doc.Metadata.Subject);
        Assert.Equal("Desc", doc.Metadata.Description);
        Assert.Contains("test", doc.Metadata.Keywords);
        Assert.Equal("en", doc.Metadata.Language);
    }

    [Fact]
    public void Build_WithPageSetup_SetsValues()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .WithPageSetup(p => p
                .WithPageSize(PageSize.Letter)
                .WithOrientation(PageOrientation.Landscape)
                .WithMargins(10, 20, 30, 40))
            .Build();

        Assert.Equal(PageSize.Letter, doc.PageSetup.PageSize);
        Assert.Equal(PageOrientation.Landscape, doc.PageSetup.Orientation);
        Assert.Equal(10, doc.PageSetup.Margins.TopMm);
        Assert.Equal(20, doc.PageSetup.Margins.RightMm);
        Assert.Equal(30, doc.PageSetup.Margins.BottomMm);
        Assert.Equal(40, doc.PageSetup.Margins.LeftMm);
    }

    [Fact]
    public void Build_WithSections_CreatesElements()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Markdown)
            .AddSection(s => s
                .AddHeading("Title", HeadingLevel.H1)
                .AddParagraph("Hello world")
                .AddCodeBlock("x = 1", "python")
                .AddHorizontalRule()
                .AddPageBreak())
            .Build();

        Assert.Single(doc.Sections);
        var elements = doc.Sections[0].Elements;
        Assert.Equal(5, elements.Count);
        Assert.IsType<HeadingElement>(elements[0]);
        Assert.IsType<ParagraphElement>(elements[1]);
        Assert.IsType<CodeBlockElement>(elements[2]);
        Assert.IsType<HorizontalRuleElement>(elements[3]);
        Assert.IsType<PageBreakElement>(elements[4]);
    }

    [Fact]
    public void Build_HeaderAndFooter_Present()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .WithHeader(h => h.AddText("Header").ShowPageNumber())
            .WithFooter(f => f.AddText("Footer"))
            .Build();

        Assert.NotNull(doc.Header);
        Assert.Single(doc.Header!.Content.Where(r => r.Text == "Header"));
        Assert.True(doc.Header.ShowPageNumber);
        Assert.NotNull(doc.Footer);
        Assert.Single(doc.Footer!.Content.Where(r => r.Text == "Footer"));
    }

    [Fact]
    public void Build_WithList_CreatesNestedItems()
    {
        var doc = GenerationBuilder.Document()
            .ForFormat(GeneratedFileFormat.Docx)
            .AddSection(s => s
                .AddList(true, l => l
                    .AddItem("First")
                    .AddItem("Second", n => n.AddItem("Nested"))))
            .Build();

        var list = Assert.IsType<ListElement>(doc.Sections[0].Elements[0]);
        Assert.True(list.Ordered);
        Assert.Equal(2, list.Items.Count);
        Assert.Single(list.Items[1].Nested);
        Assert.Equal("Nested", list.Items[1].Nested[0].Content[0].Text);
    }
}

public class SpreadsheetBuilderTests
{
    [Fact]
    public void Build_MinimalSheet_HasCorrectStructure()
    {
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Csv)
            .AddSheet("Data", s => s
                .AddColumn("A")
                .AddColumn("B")
                .AddRow(r => r.AddCell("x").AddCell(1)))
            .Build();

        Assert.Equal(GeneratedFileFormat.Csv, sheet.Format);
        Assert.Single(sheet.Sheets);
        Assert.Equal("Data", sheet.Sheets[0].Name);
        Assert.Equal(2, sheet.Sheets[0].Columns.Count);
        Assert.Single(sheet.Sheets[0].Rows);
    }

    [Fact]
    public void Build_WithFormulaCells_SetsFormula()
    {
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Xlsx)
            .AddSheet(s => s
                .AddColumn("Val")
                .AddRow(r => r.AddCell(10))
                .AddRow(r => r.AddFormulaCell("=SUM(A1:A1)")))
            .Build();

        Assert.Equal("=SUM(A1:A1)", sheet.Sheets[0].Rows[1].Cells[0].Formula);
    }

    [Fact]
    public void Build_WithOptions_SetsSheetProperties()
    {
        var sheet = GenerationBuilder.Spreadsheet()
            .ForFormat(GeneratedFileFormat.Xlsx)
            .AddSheet(s => s
                .AddColumn("X")
                .AddRow(r => r.AddCell(1))
                .FreezeTopRow()
                .FreezeFirstColumn()
                .EnableAutoFilter())
            .Build();

        Assert.True(sheet.Sheets[0].FreezeFirstRow);
        Assert.True(sheet.Sheets[0].FreezeFirstColumn);
        Assert.True(sheet.Sheets[0].AutoFilter);
    }
}
