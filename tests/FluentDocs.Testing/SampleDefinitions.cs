using FluentDocs.Abstractions.Builders;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Models.Presentations;
using FluentDocs.Abstractions.Models.Spreadsheets;

namespace FluentDocs.Testing;

/// <summary>
/// Provides pre-built sample definitions for testing generators.
/// </summary>
public static class SampleDefinitions
{
    /// <summary>Creates a minimal document definition with one heading and one paragraph.</summary>
    public static DocumentDefinition MinimalDocument(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("test-doc")
            .AddSection(s => s
                .AddHeading("Test Heading")
                .AddParagraph("This is a test paragraph."))
            .Build();

    /// <summary>Creates a rich document with all element types.</summary>
    public static DocumentDefinition RichDocument(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("rich-doc")
            .WithMetadata(m => m
                .Title("Rich Document")
                .Author("Test Author")
                .Subject("Testing")
                .Description("A document with all element types"))
            .WithPageSetup(p => p
                .WithPageSize(PageSize.A4)
                .WithOrientation(PageOrientation.Portrait)
                .WithMargins(25, 25, 25, 25))
            .WithHeader(h => h
                .AddText("Header Text")
                .ShowPageNumber("Page {0}"))
            .WithFooter(f => f.AddText("Footer Text"))
            .AddSection(s => s
                .AddHeading("Chapter 1", HeadingLevel.H1)
                .AddParagraph(p => p
                    .AddText("Normal text, ")
                    .AddFormattedText("bold text", ts => ts.Bold())
                    .AddText(", and ")
                    .AddFormattedText("italic text", ts => ts.Italic())
                    .AddText("."))
                .AddHorizontalRule()
                .AddHeading("Subsection", HeadingLevel.H2)
                .AddCodeBlock("var x = 42;", "csharp")
                .AddBlockQuote("This is a block quote.")
                .AddList(false, l => l
                    .AddItem("Item 1")
                    .AddItem("Item 2")
                    .AddItem("Item 3", n => n.AddItem("Nested")))
                .AddList(true, l => l
                    .AddItem("First")
                    .AddItem("Second"))
                .AddTable(t => t
                    .AddColumn("Name")
                    .AddColumn("Value")
                    .AddRow(r => r.AddCell("Alpha").AddCell("100"))
                    .AddRow(r => r.AddCell("Beta").AddCell("200"))))
            .Build();

    /// <summary>Creates a minimal spreadsheet with one sheet, two columns, two rows.</summary>
    public static SpreadsheetDefinition MinimalSpreadsheet(GeneratedFileFormat format = GeneratedFileFormat.Csv) =>
        GenerationBuilder.Spreadsheet()
            .ForFormat(format)
            .WithFileName("test-sheet")
            .AddSheet("Data", s => s
                .AddColumn("Name")
                .AddColumn("Score")
                .AddRow(r => r.AddCell("Alice").AddCell(95))
                .AddRow(r => r.AddCell("Bob").AddCell(87)))
            .Build();

    /// <summary>Creates a rich spreadsheet with styling, merged cells, etc.</summary>
    public static SpreadsheetDefinition RichSpreadsheet(GeneratedFileFormat format = GeneratedFileFormat.Xlsx) =>
        GenerationBuilder.Spreadsheet()
            .ForFormat(format)
            .WithFileName("rich-sheet")
            .WithMetadata(m => m.Title("Rich Spreadsheet").Author("Tester"))
            .AddSheet("Sales", s => s
                .AddColumn("Product")
                .AddColumn("Q1")
                .AddColumn("Q2")
                .AddColumn("Total")
                .AddRow(r => r.AddCell("Widget A").AddCell(100).AddCell(150).AddFormulaCell("=B2+C2"))
                .AddRow(r => r.AddCell("Widget B").AddCell(200).AddCell(250).AddFormulaCell("=B3+C3"))
                .AddRow(r => r.AddCell("Total").AddFormulaCell("=SUM(B2:B3)").AddFormulaCell("=SUM(C2:C3)").AddFormulaCell("=SUM(D2:D3)"))
                .FreezeTopRow()
                .EnableAutoFilter())
            .Build();

    /// <summary>Creates a minimal presentation with one text slide.</summary>
    public static PresentationDefinition MinimalPresentation() =>
        GenerationBuilder.Presentation()
            .WithFileName("test-pptx")
            .AddSlide(s => s
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(50, 50)
                    .WithSize(200, 40)
                    .AddText("Hello World")))
            .Build();

    /// <summary>Creates a simple chart definition for testing.</summary>
    public static ChartDefinition SimpleBarChart() => new()
    {
        Type = ChartType.Bar,
        Title = "Test Chart",
        Series =
        [
            new ChartDataSeries
            {
                Label = "Series 1",
                Categories = ["A", "B", "C"],
                Values = [10, 20, 30]
            }
        ],
        WidthPx = 400,
        HeightPx = 300
    };

    /// <summary>Creates a 1x1 white PNG for testing image embedding.</summary>
    public static ImageContent TinyPng()
    {
        // Minimal valid 1×1 white PNG
        byte[] png =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG header
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
            0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC,
            0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
            0x44, 0xAE, 0x42, 0x60, 0x82
        ];

        return new ImageContent
        {
            Data = png,
            Format = FluentDocs.Abstractions.Enums.ImageFormat.Png,
            AltText = "Test image",
            WidthMm = 50,
            HeightMm = 50
        };
    }

    // ────────────────────────────────────────────────────────────────────
    // Combined / integration test definitions
    // ────────────────────────────────────────────────────────────────────

    /// <summary>Creates a document with an embedded bar chart.</summary>
    public static DocumentDefinition DocumentWithChart(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("doc-with-chart")
            .AddSection(s => s
                .AddHeading("Sales Report")
                .AddParagraph("Below is the quarterly sales chart:")
                .AddChart(c => c
                    .Bar()
                    .WithTitle("Quarterly Sales")
                    .AddSeries(ser => ser
                        .WithLabel("Revenue")
                        .WithCategories("Q1", "Q2", "Q3", "Q4")
                        .WithValues(150, 230, 180, 310))
                    .WithSize(600, 400)))
            .Build();

    /// <summary>Creates a document with an embedded image.</summary>
    public static DocumentDefinition DocumentWithImage(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("doc-with-image")
            .AddSection(s => s
                .AddHeading("Image Test")
                .AddParagraph("An image is embedded below:")
                .AddImage(img =>
                {
                    var png = TinyPng();
                    img.FromBytes(png.Data)
                       .WithFormat(FluentDocs.Abstractions.Enums.ImageFormat.Png)
                       .WithAltText("Test logo")
                       .WithSize(50, 50);
                }))
            .Build();

    /// <summary>Creates a document with both a table and a chart.</summary>
    public static DocumentDefinition DocumentWithTableAndChart(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("doc-table-chart")
            .WithMetadata(m => m.Title("Combined Report").Author("Integration Test"))
            .AddSection(s => s
                .AddHeading("Data Summary", HeadingLevel.H1)
                .AddTable(t => t
                    .AddColumn("Region")
                    .AddColumn("Sales")
                    .AddColumn("Growth")
                    .AddRow(r => r.AddCell("North").AddCell("$1,200").AddCell("12%"))
                    .AddRow(r => r.AddCell("South").AddCell("$980").AddCell("8%"))
                    .AddRow(r => r.AddCell("East").AddCell("$1,450").AddCell("15%"))
                    .AddRow(r => r.AddCell("West").AddCell("$870").AddCell("5%")))
                .AddParagraph("The chart below visualizes the same data:")
                .AddChart(c => c
                    .Bar()
                    .WithTitle("Regional Sales")
                    .AddSeries(ser => ser
                        .WithLabel("Sales ($)")
                        .WithCategories("North", "South", "East", "West")
                        .WithValues(1200, 980, 1450, 870))
                    .WithSize(600, 400)))
            .Build();

    /// <summary>Creates a spreadsheet with an embedded chart (XLSX only).</summary>
    public static SpreadsheetDefinition SpreadsheetWithChart(GeneratedFileFormat format = GeneratedFileFormat.Xlsx) =>
        GenerationBuilder.Spreadsheet()
            .ForFormat(format)
            .WithFileName("sheet-with-chart")
            .AddSheet("Monthly", s => s
                .AddColumn("Month")
                .AddColumn("Revenue")
                .AddColumn("Expenses")
                .AddRow(r => r.AddCell("Jan").AddCell(5000).AddCell(3200))
                .AddRow(r => r.AddCell("Feb").AddCell(5500).AddCell(3400))
                .AddRow(r => r.AddCell("Mar").AddCell(6200).AddCell(3100))
                .AddRow(r => r.AddCell("Apr").AddCell(5800).AddCell(3600))
                .AddChart(c => c
                    .Line()
                    .WithTitle("Revenue vs Expenses")
                    .AddSeries(ser => ser
                        .WithLabel("Revenue")
                        .WithCategories("Jan", "Feb", "Mar", "Apr")
                        .WithValues(5000, 5500, 6200, 5800))
                    .AddSeries(ser => ser
                        .WithLabel("Expenses")
                        .WithCategories("Jan", "Feb", "Mar", "Apr")
                        .WithValues(3200, 3400, 3100, 3600))
                    .WithSize(600, 400)))
            .Build();

    /// <summary>Creates a rich multi-slide presentation with charts, tables, images, shapes.</summary>
    public static PresentationDefinition RichPresentation() =>
        GenerationBuilder.Presentation()
            .WithFileName("rich-presentation")
            .WithMetadata(m => m.Title("Q4 Business Review").Author("Integration Test"))
            .WithTheme(t => t
                .PrimaryColor("#1F4E79")
                .SecondaryColor("#2E75B6")
                .AddAccentColor("#FFC000"))
            .AddSlide(s => s  // Title slide
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(40, 60)
                    .WithSize(240, 50)
                    .AddFormattedText("Q4 Business Review", ts => ts.Bold().FontSize(28)))
                .AddTextBox(tb => tb
                    .AtPosition(40, 120)
                    .WithSize(240, 30)
                    .AddText("Prepared for Executive Committee")))
            .AddSlide(s => s  // Chart slide
                .WithLayout(SlideLayout.Blank)
                .WithNotes("Discuss quarterly revenue trends with the team.")
                .AddTextBox(tb => tb
                    .AtPosition(20, 10)
                    .WithSize(280, 25)
                    .AddFormattedText("Revenue Overview", ts => ts.Bold().FontSize(22)))
                .AddChart(c => c
                    .AtPosition(20, 40)
                    .WithSize(280, 150)
                    .ConfigureChart(ch => ch
                        .Bar()
                        .WithTitle("Quarterly Revenue")
                        .AddSeries(ser => ser
                            .WithLabel("2025")
                            .WithCategories("Q1", "Q2", "Q3", "Q4")
                            .WithValues(1200, 1350, 1280, 1500)))))
            .AddSlide(s => s  // Table slide
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(20, 10)
                    .WithSize(280, 25)
                    .AddFormattedText("Regional Breakdown", ts => ts.Bold().FontSize(22)))
                .AddTable(t => t
                    .AtPosition(20, 45)
                    .WithSize(280, 120)
                    .AddColumn("Region")
                    .AddColumn("Q4 Sales")
                    .AddColumn("YoY Growth")
                    .AddRow(r => r.AddCell("North America").AddCell("$540K").AddCell("+12%"))
                    .AddRow(r => r.AddCell("Europe").AddCell("$380K").AddCell("+8%"))
                    .AddRow(r => r.AddCell("Asia").AddCell("$290K").AddCell("+22%"))))
            .AddSlide(s => s  // Image + shape slide
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(20, 10)
                    .WithSize(280, 25)
                    .AddFormattedText("Brand Assets", ts => ts.Bold().FontSize(22)))
                .AddImage(img =>
                {
                    var png = TinyPng();
                    img.AtPosition(30, 50)
                       .WithSize(60, 60)
                       .FromBytes(png.Data)
                       .WithFormat(FluentDocs.Abstractions.Enums.ImageFormat.Png)
                       .WithAltText("Company logo");
                })
                .AddShape(sh => sh
                    .OfType(ShapeType.Rectangle)
                    .AtPosition(120, 50)
                    .WithSize(160, 60)
                    .WithFillColor("#2E75B6")
                    .WithBorder(BorderStyle.Medium, "#1F4E79", 2)
                    .WithText("Strategic Priorities")))
            .Build();

    /// <summary>Creates a presentation with a single chart slide.</summary>
    public static PresentationDefinition PresentationWithChart() =>
        GenerationBuilder.Presentation()
            .WithFileName("pptx-with-chart")
            .AddSlide(s => s
                .WithLayout(SlideLayout.Blank)
                .AddTextBox(tb => tb
                    .AtPosition(20, 10)
                    .WithSize(280, 25)
                    .AddText("Performance Metrics"))
                .AddChart(c => c
                    .AtPosition(20, 45)
                    .WithSize(280, 150)
                    .ConfigureChart(ch => ch
                        .Pie()
                        .WithTitle("Market Share")
                        .AddSeries(ser => ser
                            .WithLabel("Share")
                            .WithCategories("Product A", "Product B", "Product C", "Other")
                            .WithValues(35, 28, 22, 15)))))
            .Build();

    /// <summary>Creates an empty document with no sections (edge case).</summary>
    public static DocumentDefinition EmptyDocument(GeneratedFileFormat format = GeneratedFileFormat.Docx) =>
        GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("empty-doc")
            .Build();

    /// <summary>Creates a large document with many sections for stress testing.</summary>
    public static DocumentDefinition LargeDocument(GeneratedFileFormat format = GeneratedFileFormat.Docx)
    {
        var builder = GenerationBuilder.Document()
            .ForFormat(format)
            .WithFileName("large-doc")
            .WithMetadata(m => m.Title("Large Document").Author("Stress Test"));

        for (var i = 1; i <= 50; i++)
        {
            var sectionNumber = i;
            builder.AddSection(s => s
                .AddHeading($"Chapter {sectionNumber}", HeadingLevel.H1)
                .AddParagraph($"This is the content of chapter {sectionNumber}. " +
                              "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                              "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                              "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.")
                .AddParagraph($"Section {sectionNumber} continues with additional detail. " +
                              "Duis aute irure dolor in reprehenderit in voluptate velit esse " +
                              "cillum dolore eu fugiat nulla pariatur.")
                .AddTable(t => t
                    .AddColumn("Item")
                    .AddColumn("Value")
                    .AddRow(r => r.AddCell($"Metric {sectionNumber}-A").AddCell($"{sectionNumber * 100}"))
                    .AddRow(r => r.AddCell($"Metric {sectionNumber}-B").AddCell($"{sectionNumber * 200}"))));
        }

        return builder.Build();
    }

    /// <summary>Creates a multi-sheet spreadsheet (3 sheets).</summary>
    public static SpreadsheetDefinition MultiSheetSpreadsheet(GeneratedFileFormat format = GeneratedFileFormat.Xlsx) =>
        GenerationBuilder.Spreadsheet()
            .ForFormat(format)
            .WithFileName("multi-sheet")
            .WithMetadata(m => m.Title("Multi-Sheet Workbook"))
            .AddSheet("Revenue", s => s
                .AddColumn("Quarter")
                .AddColumn("Amount")
                .AddRow(r => r.AddCell("Q1").AddCell(50000))
                .AddRow(r => r.AddCell("Q2").AddCell(62000))
                .AddRow(r => r.AddCell("Q3").AddCell(58000))
                .AddRow(r => r.AddCell("Q4").AddCell(71000)))
            .AddSheet("Expenses", s => s
                .AddColumn("Category")
                .AddColumn("Amount")
                .AddRow(r => r.AddCell("Salaries").AddCell(120000))
                .AddRow(r => r.AddCell("Marketing").AddCell(35000))
                .AddRow(r => r.AddCell("Operations").AddCell(28000)))
            .AddSheet("Summary", s => s
                .AddColumn("Metric")
                .AddColumn("Value")
                .AddRow(r => r.AddCell("Total Revenue").AddCell(241000))
                .AddRow(r => r.AddCell("Total Expenses").AddCell(183000))
                .AddRow(r => r.AddCell("Net Profit").AddCell(58000)))
            .Build();

    /// <summary>Creates one <see cref="ChartDefinition"/> for every <see cref="ChartType"/>.</summary>
    public static IEnumerable<(ChartType Type, ChartDefinition Definition)> AllChartTypes()
    {
        var types = Enum.GetValues<ChartType>();
        foreach (var type in types)
        {
            yield return (type, new ChartDefinition
            {
                Type = type,
                Title = $"{type} Chart",
                Series =
                [
                    new ChartDataSeries
                    {
                        Label = "Data",
                        Categories = ["Alpha", "Beta", "Gamma", "Delta"],
                        Values = [25, 40, 15, 30]
                    }
                ],
                WidthPx = 500,
                HeightPx = 350
            });
        }
    }

    /// <summary>Creates a spreadsheet with an empty sheet (columns but no rows).</summary>
    public static SpreadsheetDefinition EmptySpreadsheet(GeneratedFileFormat format = GeneratedFileFormat.Xlsx) =>
        GenerationBuilder.Spreadsheet()
            .ForFormat(format)
            .WithFileName("empty-sheet")
            .AddSheet("Empty", s => s
                .AddColumn("Col A")
                .AddColumn("Col B"))
            .Build();
}
