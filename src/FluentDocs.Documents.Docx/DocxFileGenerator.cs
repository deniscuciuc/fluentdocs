using System.Diagnostics;
using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using GenEnums = FluentDocs.Abstractions.Enums;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using WTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using WTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;

namespace FluentDocs.Documents.Docx;

/// <summary>
/// Generates DOCX files from a <see cref="DocumentDefinition"/> using the Open XML SDK.
/// </summary>
public sealed class DocxFileGenerator(IChartRenderer chartRenderer, ILogger<DocxFileGenerator> logger)
    : IFileGenerator
{
    private const int EmuPerMm = 36000; // English Metric Units per mm
    private const int TwipsPerMm = 56; // 1 mm ≈ 56.7 twips (1/1440 inch, 1 inch = 25.4mm)
    private const int HalfPointsPerPt = 2;

    private int _imageCounter;
    private int _listNumberId;
    private Body _body = null!;
    private MainDocumentPart _mainPart = null!;

    public GenEnums.GeneratedFileFormat SupportedFormat => GenEnums.GeneratedFileFormat.Docx;

    public async Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not DocumentDefinition docDef)
            return GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(DocumentDefinition)} but received {definition.GetType().Name}.");

        using var activity = GenerationDiagnostics.StartGenerationActivity(SupportedFormat.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating DOCX document '{FileName}'.", docDef.FileName);
        _imageCounter = 0;
        _listNumberId = 0;

        try
        {
            using var ms = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                _mainPart = doc.AddMainDocumentPart();
                _mainPart.Document = new Document();
                _body = _mainPart.Document.AppendChild(new Body());

                // Metadata
                SetDocumentMetadata(doc, docDef.Metadata);

                // Styles
                AddStylesPart(docDef.DefaultStyles);

                // Numbering definitions
                AddNumberingPart();

                // Header / Footer
                if (docDef.Header is not null)
                    AddHeaderPart(docDef.Header);
                if (docDef.Footer is not null)
                    AddFooterPart(docDef.Footer);

                // Sections → elements
                foreach (var section in docDef.Sections)
                    foreach (var element in section.Elements)
                        await RenderElementAsync(element, ct).ConfigureAwait(false);

                // Page setup as section properties at end of body
                _body.AppendChild(CreateSectionProperties(docDef));
            }

            var bytes = ms.ToArray();

            sw.Stop();
            RecordDiagnostics("success", sw, bytes.Length);
            return GenerationResult.Success(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                GenEnums.GeneratedFileFormat.Docx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate DOCX document '{FileName}'.", docDef.FileName);
            RecordDiagnostics("failure", sw, 0);
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    #region Metadata

    private static void SetDocumentMetadata(WordprocessingDocument doc, DocumentMetadata meta)
    {
        doc.PackageProperties.Title = meta.Title;
        doc.PackageProperties.Creator = meta.Author;
        doc.PackageProperties.Subject = meta.Subject;
        doc.PackageProperties.Description = meta.Description;
        doc.PackageProperties.Language = meta.Language;
        if (meta.Keywords.Count > 0)
            doc.PackageProperties.Keywords = string.Join(", ", meta.Keywords);
        if (meta.CreatedAt.HasValue)
            doc.PackageProperties.Created = meta.CreatedAt.Value.UtcDateTime;
    }

    #endregion

    #region Styles

    private void AddStylesPart(DocumentStyles defaults)
    {
        var stylesPart = _mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();

        // Default document style
        var docDefaults = new DocDefaults(
            new RunPropertiesDefault(CreateRunProperties(defaults.DefaultTextStyle)),
            new ParagraphPropertiesDefault());
        styles.AppendChild(docDefaults);

        // Heading styles
        for (var level = 1; level <= 6; level++)
        {
            var hl = (GenEnums.HeadingLevel)level;
            defaults.HeadingStyles.TryGetValue(hl, out var ts);
            styles.AppendChild(CreateHeadingStyle(level, ts));
        }

        stylesPart.Styles = styles;
    }

    private static Style CreateHeadingStyle(int level, TextStyle? textStyle)
    {
        double[] defaultSizes = [28, 24, 20, 18, 16, 14]; // pt
        var fontSize = textStyle?.FontSizePt ?? defaultSizes[level - 1];

        var rp = new StyleRunProperties();
        rp.AppendChild(new Bold());
        rp.AppendChild(new FontSize { Val = ((int)(fontSize * HalfPointsPerPt)).ToString(CultureInfo.InvariantCulture) });
        rp.AppendChild(new FontSizeComplexScript { Val = ((int)(fontSize * HalfPointsPerPt)).ToString(CultureInfo.InvariantCulture) });

        if (textStyle?.FontFamily is not null)
            rp.AppendChild(new RunFonts { Ascii = textStyle.FontFamily, HighAnsi = textStyle.FontFamily });
        if (textStyle?.Color is not null)
            rp.AppendChild(new Color { Val = textStyle.Color.TrimStart('#') });

        var style = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = $"Heading{level}",
            StyleName = new StyleName { Val = $"heading {level}" },
            BasedOn = new BasedOn { Val = "Normal" },
            NextParagraphStyle = new NextParagraphStyle { Val = "Normal" }
        };
        style.AppendChild(new StyleParagraphProperties(
            new SpacingBetweenLines { Before = "240", After = "120" }));
        style.AppendChild(rp);
        return style;
    }

    #endregion

    #region Numbering (Lists)

    private void AddNumberingPart()
    {
        var numberingPart = _mainPart.AddNewPart<NumberingDefinitionsPart>();
        var numbering = new Numbering();

        // Abstract numbering for bullets
        var bulletAbstract = new AbstractNum { AbstractNumberId = 0 };
        for (var i = 0; i < 9; i++)
        {
            var lvl = new Level { LevelIndex = i };
            lvl.AppendChild(new NumberingFormat { Val = NumberFormatValues.Bullet });
            lvl.AppendChild(new LevelText { Val = i % 3 == 0 ? "•" : i % 3 == 1 ? "◦" : "▪" });
            lvl.AppendChild(new PreviousParagraphProperties(
                new Indentation { Left = ((i + 1) * 720).ToString(CultureInfo.InvariantCulture), Hanging = "360" }));
            bulletAbstract.AppendChild(lvl);
        }

        numbering.AppendChild(bulletAbstract);

        // Abstract numbering for ordered
        var orderedAbstract = new AbstractNum { AbstractNumberId = 1 };
        for (var i = 0; i < 9; i++)
        {
            var lvl = new Level { LevelIndex = i };
            lvl.AppendChild(new NumberingFormat { Val = NumberFormatValues.Decimal });
            lvl.AppendChild(new LevelText { Val = $"%{i + 1}." });
            lvl.AppendChild(new StartNumberingValue { Val = 1 });
            lvl.AppendChild(new PreviousParagraphProperties(
                new Indentation { Left = ((i + 1) * 720).ToString(CultureInfo.InvariantCulture), Hanging = "360" }));
            orderedAbstract.AppendChild(lvl);
        }

        numbering.AppendChild(orderedAbstract);
        numberingPart.Numbering = numbering;
    }

    private int AllocateNumberingInstance(bool ordered)
    {
        _listNumberId++;
        var numId = _listNumberId;

        var numbering = _mainPart.NumberingDefinitionsPart!.Numbering!;
        var numInstance = new NumberingInstance { NumberID = numId };
        numInstance.AppendChild(new AbstractNumId { Val = ordered ? 1 : 0 });
        numbering.AppendChild(numInstance);
        return numId;
    }

    #endregion

    #region Header / Footer

    private void AddHeaderPart(HeaderFooterDefinition headerDef)
    {
        var headerPart = _mainPart.AddNewPart<HeaderPart>();
        var header = new Header();
        var para = new Paragraph();

        foreach (var run in headerDef.Content)
            para.AppendChild(CreateRun(run));

        if (headerDef.ShowPageNumber)
        {
            para.AppendChild(new Run(new TabChar()));
            para.AppendChild(new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }));
        }

        header.AppendChild(para);
        headerPart.Header = header;
    }

    private void AddFooterPart(HeaderFooterDefinition footerDef)
    {
        var footerPart = _mainPart.AddNewPart<FooterPart>();
        var footer = new Footer();
        var para = new Paragraph();

        foreach (var run in footerDef.Content)
            para.AppendChild(CreateRun(run));

        if (footerDef.ShowPageNumber)
        {
            para.AppendChild(new Run(new TabChar()));
            para.AppendChild(new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }));
        }

        footer.AppendChild(para);
        footerPart.Footer = footer;
    }

    #endregion

    #region Section Properties (Page Setup)

    private SectionProperties CreateSectionProperties(DocumentDefinition docDef)
    {
        var sp = new SectionProperties();

        // Page size
        var (w, h) = GetPageSizeMm(docDef.PageSetup);
        var pgSz = new PageSize
        {
            Width = (uint)(w * TwipsPerMm),
            Height = (uint)(h * TwipsPerMm)
        };
        if (docDef.PageSetup.Orientation == GenEnums.PageOrientation.Landscape)
            pgSz.Orient = PageOrientationValues.Landscape;
        sp.AppendChild(pgSz);

        // Margins
        var m = docDef.PageSetup.Margins;
        sp.AppendChild(new PageMargin
        {
            Top = (int)(m.TopMm * TwipsPerMm),
            Right = (uint)(m.RightMm * TwipsPerMm),
            Bottom = (int)(m.BottomMm * TwipsPerMm),
            Left = (uint)(m.LeftMm * TwipsPerMm)
        });

        // Header/Footer references
        if (_mainPart.HeaderParts.Any())
        {
            var hId = _mainPart.GetIdOfPart(_mainPart.HeaderParts.First());
            sp.AppendChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = hId });
        }

        if (_mainPart.FooterParts.Any())
        {
            var fId = _mainPart.GetIdOfPart(_mainPart.FooterParts.First());
            sp.AppendChild(new FooterReference { Type = HeaderFooterValues.Default, Id = fId });
        }

        return sp;
    }

    private static (double w, double h) GetPageSizeMm(PageSetup ps)
    {
        double w, h;
        switch (ps.PageSize)
        {
            case GenEnums.PageSize.A4:
                w = 210;
                h = 297;
                break;
            case GenEnums.PageSize.Letter:
                w = 215.9;
                h = 279.4;
                break;
            case GenEnums.PageSize.Legal:
                w = 215.9;
                h = 355.6;
                break;
            case GenEnums.PageSize.A3:
                w = 297;
                h = 420;
                break;
            case GenEnums.PageSize.A5:
                w = 148;
                h = 210;
                break;
            case GenEnums.PageSize.B5:
                w = 176;
                h = 250;
                break;
            case GenEnums.PageSize.Custom:
                w = ps.CustomWidthMm ?? 210;
                h = ps.CustomHeightMm ?? 297;
                break;
            default:
                w = 210;
                h = 297;
                break;
        }

        if (ps.Orientation == GenEnums.PageOrientation.Landscape)
            (w, h) = (h, w);

        return (w, h);
    }

    #endregion

    #region Element Rendering

    private async Task RenderElementAsync(IDocumentElement element, CancellationToken ct)
    {
        switch (element)
        {
            case HeadingElement heading:
                RenderHeading(heading);
                break;
            case ParagraphElement paragraph:
                RenderParagraph(paragraph);
                break;
            case ImageElement image:
                RenderImage(image.Image);
                break;
            case TableContentElement table:
                RenderTable(table);
                break;
            case ChartElement chart:
                await RenderChartAsync(chart.Chart, ct).ConfigureAwait(false);
                break;
            case ListElement list:
                RenderList(list);
                break;
            case CodeBlockElement codeBlock:
                RenderCodeBlock(codeBlock);
                break;
            case BlockQuoteElement blockQuote:
                RenderBlockQuote(blockQuote);
                break;
            case PageBreakElement:
                RenderPageBreak();
                break;
            case HorizontalRuleElement:
                RenderHorizontalRule();
                break;
        }
    }

    private void RenderHeading(HeadingElement heading)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{(int)heading.Level}" }));

        var run = new Run(new Text(heading.Text));
        if (heading.Style is not null)
            run.PrependChild(CreateRunProperties(heading.Style));
        para.AppendChild(run);
        _body.AppendChild(para);
    }

    private void RenderParagraph(ParagraphElement paragraph)
    {
        var para = new Paragraph();

        if (paragraph.Style is not null)
            para.AppendChild(CreateParagraphProperties(paragraph.Style));

        foreach (var textRun in paragraph.Runs)
            para.AppendChild(CreateRun(textRun));

        _body.AppendChild(para);
    }

    private void RenderImage(ImageContent image)
    {
        _imageCounter++;
        var imagePartType = image.Format switch
        {
            GenEnums.ImageFormat.Jpeg => ImagePartType.Jpeg,
            GenEnums.ImageFormat.Gif => ImagePartType.Gif,
            GenEnums.ImageFormat.Bmp => ImagePartType.Bmp,
            GenEnums.ImageFormat.Tiff => ImagePartType.Tiff,
            _ => ImagePartType.Png
        };

        var imagePart = _mainPart.AddImagePart(imagePartType);
        using (var stream = new MemoryStream(image.Data))
        {
            imagePart.FeedData(stream);
        }

        var relId = _mainPart.GetIdOfPart(imagePart);

        // Default or explicit size
        var widthEmu = (long)((image.WidthMm ?? 150) * EmuPerMm);
        var heightEmu = (long)((image.HeightMm ?? 100) * EmuPerMm);

        var drawing = CreateInlineDrawing(relId, widthEmu, heightEmu, image.AltText ?? "image");

        var para = new Paragraph(new Run(drawing));
        _body.AppendChild(para);

        // Caption
        if (image.Caption is not null)
            _body.AppendChild(new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(new Italic()),
                    new Text(image.Caption))));
    }

    private void RenderTable(TableContentElement table)
    {
        var tbl = new Table();

        // Table properties
        var tblPr = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }));
        tbl.AppendChild(tblPr);

        // Grid columns
        var grid = new TableGrid();
        foreach (var col in table.Columns)
        {
            var gc = new GridColumn();
            if (col.WidthMm.HasValue)
                gc.Width = ((int)(col.WidthMm.Value * TwipsPerMm)).ToString(CultureInfo.InvariantCulture);
            grid.AppendChild(gc);
        }

        tbl.AppendChild(grid);

        // Header row
        if (table.Columns.Any(c => c.Header is not null))
        {
            var headerRow = new WTableRow();
            foreach (var col in table.Columns)
            {
                var tc = new WTableCell(
                    new TableCellProperties(
                        new Shading { Fill = "D9E2F3", Val = ShadingPatternValues.Clear }),
                    new Paragraph(
                        new Run(
                            new RunProperties(new Bold()),
                            new Text(col.Header ?? ""))));
                headerRow.AppendChild(tc);
            }

            tbl.AppendChild(headerRow);
        }

        // Data rows
        foreach (var row in table.Rows)
        {
            var tr = new WTableRow();
            foreach (var cell in row.Cells)
            {
                var para = new Paragraph();
                if (cell.Runs is { Count: > 0 })
                {
                    foreach (var run in cell.Runs)
                        para.AppendChild(CreateRun(run));
                }
                else
                {
                    var run = new Run(new Text(cell.Text ?? ""));
                    if (cell.Style is not null)
                        run.PrependChild(CreateRunProperties(cell.Style));
                    para.AppendChild(run);
                }

                tr.AppendChild(new WTableCell(para));
            }

            tbl.AppendChild(tr);
        }

        _body.AppendChild(tbl);
        _body.AppendChild(new Paragraph()); // spacer after table
    }

    private async Task RenderChartAsync(ChartDefinition chart, CancellationToken ct)
    {
        var rendered = await chartRenderer.RenderAsync(chart, ct: ct).ConfigureAwait(false);
        var image = new ImageContent
        {
            Data = rendered.ImageData,
            Format = rendered.Format,
            AltText = chart.Title ?? "Chart",
            WidthMm = chart.WidthPx * 25.4 / 96, // approx px→mm at 96 DPI
            HeightMm = chart.HeightPx * 25.4 / 96
        };
        RenderImage(image);
    }

    private void RenderList(ListElement list)
    {
        var numId = AllocateNumberingInstance(list.Ordered);
        RenderListItems(list.Items, numId, 0);
    }

    private void RenderListItems(IReadOnlyList<FluentDocs.Abstractions.Models.Documents.ListItem> items,
        int numId, int level)
    {
        foreach (var item in items)
        {
            var para = new Paragraph();
            var pp = new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = level },
                    new NumberingId { Val = numId }));
            para.AppendChild(pp);

            foreach (var run in item.Content)
                para.AppendChild(CreateRun(run));

            _body.AppendChild(para);

            if (item.Nested.Count > 0)
                RenderListItems(item.Nested, numId, level + 1);
        }
    }

    private void RenderCodeBlock(CodeBlockElement codeBlock)
    {
        var lines = codeBlock.Code.Split('\n');
        foreach (var line in lines)
        {
            var rp = new RunProperties(
                new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" },
                new FontSize { Val = "20" }); // 10pt
            if (codeBlock.Style?.Color is not null)
                rp.AppendChild(new Color { Val = codeBlock.Style.Color.TrimStart('#') });

            var para = new Paragraph(
                new ParagraphProperties(
                    new Shading { Fill = "F5F5F5", Val = ShadingPatternValues.Clear },
                    new Indentation { Left = "360" }),
                new Run(rp, new Text(line) { Space = SpaceProcessingModeValues.Preserve }));
            _body.AppendChild(para);
        }
    }

    private void RenderBlockQuote(BlockQuoteElement blockQuote)
    {
        var para = new Paragraph(
            new ParagraphProperties(
                new Indentation { Left = "720" },
                new ParagraphBorders(
                    new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "AAAAAA", Space = 4 })));

        if (blockQuote.Style is not null)
            ApplyParagraphStyle(para, blockQuote.Style);

        foreach (var run in blockQuote.Content)
            para.AppendChild(CreateRun(run));

        _body.AppendChild(para);
    }

    private void RenderPageBreak()
    {
        _body.AppendChild(new Paragraph(
            new Run(new Break { Type = BreakValues.Page })));
    }

    private void RenderHorizontalRule()
    {
        _body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "999999" })),
            new Run(new Text(""))));
    }

    #endregion

    #region Helpers

    private static Run CreateRun(TextRun textRun)
    {
        var run = new Run(new Text(textRun.Text) { Space = SpaceProcessingModeValues.Preserve });
        if (textRun.Style is not null)
            run.PrependChild(CreateRunProperties(textRun.Style));
        return run;
    }

    private static RunProperties CreateRunProperties(TextStyle? style)
    {
        var rp = new RunProperties();
        if (style is null) return rp;

        if (style.FontFamily is not null)
            rp.AppendChild(new RunFonts { Ascii = style.FontFamily, HighAnsi = style.FontFamily });
        if (style.FontSizePt.HasValue)
        {
            var halfPts = ((int)(style.FontSizePt.Value * HalfPointsPerPt)).ToString(CultureInfo.InvariantCulture);
            rp.AppendChild(new FontSize { Val = halfPts });
            rp.AppendChild(new FontSizeComplexScript { Val = halfPts });
        }

        if (style.Bold) rp.AppendChild(new Bold());
        if (style.Italic) rp.AppendChild(new Italic());
        if (style.Underline) rp.AppendChild(new Underline { Val = UnderlineValues.Single });
        if (style.Strikethrough) rp.AppendChild(new Strike());
        if (style.Superscript) rp.AppendChild(new VerticalTextAlignment { Val = VerticalPositionValues.Superscript });
        if (style.Subscript) rp.AppendChild(new VerticalTextAlignment { Val = VerticalPositionValues.Subscript });
        if (style.Color is not null) rp.AppendChild(new Color { Val = style.Color.TrimStart('#') });
        if (style.BackgroundColor is not null)
            rp.AppendChild(
                new Shading { Fill = style.BackgroundColor.TrimStart('#'), Val = ShadingPatternValues.Clear });

        return rp;
    }

    private static ParagraphProperties CreateParagraphProperties(ParagraphStyle style)
    {
        var pp = new ParagraphProperties();

        pp.AppendChild(new Justification
        {
            Val = style.Alignment switch
            {
                GenEnums.TextAlignment.Center => JustificationValues.Center,
                GenEnums.TextAlignment.Right => JustificationValues.Right,
                GenEnums.TextAlignment.Justify => JustificationValues.Both,
                _ => JustificationValues.Left
            }
        });

        var spacing = new SpacingBetweenLines();
        if (style.LineSpacing.HasValue)
            spacing.Line = ((int)(style.LineSpacing.Value * 240)).ToString(CultureInfo.InvariantCulture); // 240 = single
        if (style.SpaceBeforePt.HasValue)
            spacing.Before = ((int)(style.SpaceBeforePt.Value * 20)).ToString(CultureInfo.InvariantCulture); // twips
        if (style.SpaceAfterPt.HasValue)
            spacing.After = ((int)(style.SpaceAfterPt.Value * 20)).ToString(CultureInfo.InvariantCulture);
        pp.AppendChild(spacing);

        if (style.IndentMm.HasValue)
            pp.AppendChild(new Indentation { Left = ((int)(style.IndentMm.Value * TwipsPerMm)).ToString(CultureInfo.InvariantCulture) });

        return pp;
    }

    private static void ApplyParagraphStyle(Paragraph para, ParagraphStyle style)
    {
        var existing = para.GetFirstChild<ParagraphProperties>();
        if (existing is null)
        {
            para.PrependChild(CreateParagraphProperties(style));
            return;
        }

        // Merge alignment
        var justification = existing.GetFirstChild<Justification>();
        if (justification is null)
            existing.AppendChild(new Justification
            {
                Val = style.Alignment switch
                {
                    GenEnums.TextAlignment.Center => JustificationValues.Center,
                    GenEnums.TextAlignment.Right => JustificationValues.Right,
                    GenEnums.TextAlignment.Justify => JustificationValues.Both,
                    _ => JustificationValues.Left
                }
            });
    }

    private Drawing CreateInlineDrawing(string relId, long widthEmu, long heightEmu, string alt)
    {
        var id = (uint)_imageCounter;
        return new Drawing(
            new DW.Inline(
                    new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                    new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                    new DW.DocProperties { Id = id, Name = $"Image{id}", Description = alt },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties { Id = id, Name = $"Image{id}" },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(
                                        new A.Blip { Embed = relId },
                                        new A.Stretch(new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(
                                            new A.Offset { X = 0, Y = 0 },
                                            new A.Extents { Cx = widthEmu, Cy = heightEmu }),
                                        new A.PresetGeometry(new A.AdjustValueList())
                                        { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
            { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 });
    }

    private void RecordDiagnostics(string outcome, Stopwatch sw, long size)
    {
        GenerationDiagnostics.FilesGenerated.Add(1,
            new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
            new KeyValuePair<string, object?>("outcome", outcome));
        GenerationDiagnostics.GenerationDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));
        if (size > 0)
            GenerationDiagnostics.OutputSize.Record(size,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));
    }

    #endregion
}
