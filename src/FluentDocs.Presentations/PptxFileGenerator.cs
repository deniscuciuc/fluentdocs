using System.Diagnostics;
using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Presentations;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace FluentDocs.Presentations;

/// <summary>
/// Generates PPTX files from a <see cref="PresentationDefinition"/> using the Open XML SDK.
/// </summary>
public sealed class PptxFileGenerator(IChartRenderer chartRenderer, ILogger<PptxFileGenerator> logger)
    : IFileGenerator
{
    private const int EmuPerMm = 36000;
    private const int EmuPerPt = 12700;

    private int _imageCounter;
    private int _shapeIdCounter;

    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.Pptx;

    public async Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not PresentationDefinition presDef)
            return GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(PresentationDefinition)} but received {definition.GetType().Name}.");

        using var activity = GenerationDiagnostics.StartGenerationActivity(SupportedFormat.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating PPTX presentation '{FileName}'.", presDef.FileName);
        _imageCounter = 0;
        _shapeIdCounter = 1; // shape IDs are 1-based

        try
        {
            using var ms = new MemoryStream();
            using (var presDoc = PresentationDocument.Create(ms, PresentationDocumentType.Presentation, true))
            {
                // Presentation part
                var presPart = presDoc.AddPresentationPart();
                presPart.Presentation = new Presentation(
                    new SlideIdList(),
                    new P.SlideSize
                    {
                        Cx = (int)(presDef.SlideSize.WidthMm * EmuPerMm),
                        Cy = (int)(presDef.SlideSize.HeightMm * EmuPerMm),
                        Type = SlideSizeValues.Custom
                    },
                    new NotesSize { Cx = 6858000, Cy = 9144000 });

                // Metadata
                SetMetadata(presDoc, presDef.Metadata);

                // Theme
                var themePart = CreateThemePart(presPart, presDef.Theme);

                // Slide master + layout
                var slideMasterPart = CreateSlideMasterPart(presPart, themePart);

                // Slides
                uint slideId = 256; // start from 256 (convention)
                var slideIdList = presPart.Presentation.SlideIdList!;

                foreach (var slideDef in presDef.Slides)
                {
                    var slidePart = presPart.AddNewPart<SlidePart>();
                    var slideRelId = presPart.GetIdOfPart(slidePart);

                    slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = slideRelId });

                    // Link to slide layout
                    slidePart.AddPart(slideMasterPart.SlideLayoutParts.First());

                    await RenderSlideAsync(slidePart, slideDef, presDef, ct).ConfigureAwait(false);
                }
            }

            var bytes = ms.ToArray();
            sw.Stop();
            RecordDiagnostics("success", sw, bytes.Length);
            return GenerationResult.Success(bytes,
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                GeneratedFileFormat.Pptx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate PPTX presentation '{FileName}'.", presDef.FileName);
            RecordDiagnostics("failure", sw, 0);
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    #region Metadata

    private static void SetMetadata(PresentationDocument doc, PresentationMetadata meta)
    {
        doc.PackageProperties.Title = meta.Title;
        doc.PackageProperties.Creator = meta.Author;
        doc.PackageProperties.Subject = meta.Subject;
        doc.PackageProperties.Description = meta.Description;
        if (meta.CreatedAt.HasValue)
            doc.PackageProperties.Created = meta.CreatedAt.Value.UtcDateTime;
    }

    #endregion

    #region Theme

    private static ThemePart CreateThemePart(PresentationPart presPart, ThemeDefinition? themeDef)
    {
        var themePart = presPart.AddNewPart<ThemePart>();

        var primaryColor = themeDef?.PrimaryColor ?? "#4472C4";
        var secondaryColor = themeDef?.SecondaryColor ?? "#44546A";
        var bgColor = themeDef?.BackgroundColor ?? "#FFFFFF";
        var defaultFont = themeDef?.DefaultFont ?? "Calibri";
        var headingFont = themeDef?.HeadingFont ?? "Calibri Light";

        var theme = new Theme(
                new ThemeElements(
                    new ColorScheme(
                            new Dark1Color(new SystemColor { Val = SystemColorValues.WindowText }),
                            new Light1Color(new SystemColor { Val = SystemColorValues.Window }),
                            new Dark2Color(new RgbColorModelHex { Val = secondaryColor.TrimStart('#') }),
                            new Light2Color(new RgbColorModelHex { Val = bgColor.TrimStart('#') }),
                            new Accent1Color(new RgbColorModelHex { Val = primaryColor.TrimStart('#') }),
                            new Accent2Color(new RgbColorModelHex { Val = "ED7D31" }),
                            new Accent3Color(new RgbColorModelHex { Val = "A5A5A5" }),
                            new Accent4Color(new RgbColorModelHex { Val = "FFC000" }),
                            new Accent5Color(new RgbColorModelHex { Val = "5B9BD5" }),
                            new Accent6Color(new RgbColorModelHex { Val = "70AD47" }),
                            new Hyperlink(new RgbColorModelHex { Val = "0563C1" }),
                            new FollowedHyperlinkColor(new RgbColorModelHex { Val = "954F72" })
                        )
                    { Name = "FluentDocs" },
                    new FontScheme(
                            new MajorFont(new LatinFont { Typeface = headingFont }),
                            new MinorFont(new LatinFont { Typeface = defaultFont })
                        )
                    { Name = "FluentDocs" },
                    new FormatScheme(
                            new FillStyleList(new SolidFill(new SchemeColor { Val = SchemeColorValues.PhColor })),
                            new LineStyleList(new Outline(new SolidFill(new SchemeColor
                            { Val = SchemeColorValues.PhColor }))),
                            new EffectStyleList(new EffectStyle(new EffectList())),
                            new BackgroundFillStyleList(new SolidFill(new SchemeColor
                            { Val = SchemeColorValues.PhColor }))
                        )
                    { Name = "FluentDocs" })
            )
        { Name = "FluentDocs" };

        themePart.Theme = theme;
        return themePart;
    }

    #endregion

    #region Slide Master & Layout

    private static SlideMasterPart CreateSlideMasterPart(PresentationPart presPart, ThemePart themePart)
    {
        var slideMasterPart = presPart.AddNewPart<SlideMasterPart>();
        slideMasterPart.AddPart(themePart);

        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        slideLayoutPart.SlideLayout = new DocumentFormat.OpenXml.Presentation.SlideLayout(
                new CommonSlideData(new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new TransformGroup()))))
        { Type = SlideLayoutValues.Blank };
        slideLayoutPart.AddPart(slideMasterPart);

        var slideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new TransformGroup()))),
            new SlideLayoutIdList(
                new SlideLayoutId
                {
                    Id = 2147483649,
                    RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart)
                }));

        slideMasterPart.SlideMaster = slideMaster;

        // Register slide master in presentation
        var masterIdList = new SlideMasterIdList(
            new SlideMasterId
            {
                Id = 2147483648,
                RelationshipId = presPart.GetIdOfPart(slideMasterPart)
            });
        presPart.Presentation!.InsertBefore(masterIdList, presPart.Presentation!.SlideIdList);

        return slideMasterPart;
    }

    #endregion

    #region Slide Rendering

    private async Task RenderSlideAsync(
        SlidePart slidePart, SlideDefinition slideDef, PresentationDefinition presDef, CancellationToken ct)
    {
        var shapeTree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = NextShapeId(), Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new TransformGroup()));

        // Background
        Background? background = null;
        if (slideDef.BackgroundColor is not null)
            background = new Background(
                new BackgroundProperties(
                    new SolidFill(
                        new RgbColorModelHex { Val = slideDef.BackgroundColor.TrimStart('#') })));

        // Render elements
        foreach (var element in slideDef.Elements) await RenderSlideElementAsync(slidePart, shapeTree, element, ct).ConfigureAwait(false);

        var csd = new CommonSlideData(shapeTree);
        if (background is not null)
            csd.Background = background;

        var slide = new Slide(csd, new ColorMapOverride(new MasterColorMapping()));

        // Transition
        if (slideDef.Transition is not null && slideDef.Transition.Type != TransitionType.None)
            slide.AppendChild(CreateTransition(slideDef.Transition));

        // Animations
        var animatedElements = slideDef.Elements.Where(e => e.Animation is not null).ToList();
        if (animatedElements.Count > 0)
        {
            // We'd collect animations but the Open XML timing model is complex.
            // For now, set the slide to advance on click.
        }

        slidePart.Slide = slide;

        // Speaker notes
        if (slideDef.Notes is not null) AddSpeakerNotes(slidePart, slideDef.Notes);
    }

    private async Task RenderSlideElementAsync(
        SlidePart slidePart, ShapeTree shapeTree, ISlideElement element, CancellationToken ct)
    {
        switch (element)
        {
            case TextBoxElement textBox:
                RenderTextBox(shapeTree, textBox);
                break;
            case SlideImageElement image:
                RenderSlideImage(slidePart, shapeTree, image);
                break;
            case SlideChartElement chart:
                await RenderSlideChartAsync(slidePart, shapeTree, chart, ct).ConfigureAwait(false);
                break;
            case SlideTableElement table:
                RenderSlideTable(shapeTree, table);
                break;
            case ShapeElement shape:
                RenderShape(shapeTree, shape);
                break;
        }
    }

    private void RenderTextBox(ShapeTree tree, TextBoxElement textBox)
    {
        var shapeId = NextShapeId();
        var shape = new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = shapeId, Name = $"TextBox{shapeId}" },
                new P.NonVisualShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                CreateTransform(textBox.Position, textBox.Size),
                new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle }),
            CreateTextBody(textBox.Runs, textBox.Style));

        // Background fill for text box
        if (textBox.Style?.BackgroundColor is not null)
            shape.ShapeProperties!.AppendChild(
                new SolidFill(new RgbColorModelHex { Val = textBox.Style.BackgroundColor.TrimStart('#') }));

        tree.AppendChild(shape);
    }

    private void RenderSlideImage(SlidePart slidePart, ShapeTree tree, SlideImageElement imageElem)
    {
        _imageCounter++;
        var imagePart = slidePart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(imageElem.Image.Data))
        {
            imagePart.FeedData(stream);
        }

        var relId = slidePart.GetIdOfPart(imagePart);
        var shapeId = NextShapeId();

        var pic = new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = shapeId, Name = $"Image{shapeId}" },
                new P.NonVisualPictureDrawingProperties(new PictureLocks { NoChangeAspect = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.BlipFill(
                new Blip { Embed = relId },
                new Stretch(new FillRectangle())),
            new P.ShapeProperties(
                CreateTransform(imageElem.Position, imageElem.Size),
                new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle }));

        tree.AppendChild(pic);
    }

    private async Task RenderSlideChartAsync(
        SlidePart slidePart, ShapeTree tree, SlideChartElement chartElem, CancellationToken ct)
    {
        var rendered = await chartRenderer.RenderAsync(chartElem.Chart, ct: ct).ConfigureAwait(false);

        _imageCounter++;
        var imagePart = slidePart.AddImagePart(ImagePartType.Png);
        using (var stream = new MemoryStream(rendered.ImageData))
        {
            imagePart.FeedData(stream);
        }

        var relId = slidePart.GetIdOfPart(imagePart);
        var shapeId = NextShapeId();

        var pic = new P.Picture(
            new P.NonVisualPictureProperties(
                new P.NonVisualDrawingProperties { Id = shapeId, Name = $"Chart{shapeId}" },
                new P.NonVisualPictureDrawingProperties(new PictureLocks { NoChangeAspect = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.BlipFill(
                new Blip { Embed = relId },
                new Stretch(new FillRectangle())),
            new P.ShapeProperties(
                CreateTransform(chartElem.Position, chartElem.Size),
                new PresetGeometry(new AdjustValueList()) { Preset = ShapeTypeValues.Rectangle }));

        tree.AppendChild(pic);
    }

    private void RenderSlideTable(ShapeTree tree, SlideTableElement tableElem)
    {
        var shapeId = NextShapeId();
        var colCount = tableElem.Columns.Count;
        var rowCount = tableElem.Rows.Count + (tableElem.Columns.Any(c => c.Header is not null) ? 1 : 0);

        if (colCount == 0 || rowCount == 0) return;

        var tableWidth = (long)(tableElem.Size.WidthMm * EmuPerMm);
        var tableHeight = (long)(tableElem.Size.HeightMm * EmuPerMm);
        var colWidth = tableWidth / colCount;
        var rowHeight = tableHeight / rowCount;

        var tbl = new Table();
        var tblProps = new TableProperties { FirstRow = true, BandRow = true };
        tbl.AppendChild(tblProps);

        var tblGrid = new TableGrid();
        for (var c = 0; c < colCount; c++)
            tblGrid.AppendChild(new GridColumn { Width = colWidth });
        tbl.AppendChild(tblGrid);

        // Header row
        if (tableElem.Columns.Any(c => c.Header is not null))
        {
            var headerRow = new A.TableRow { Height = rowHeight };
            foreach (var col in tableElem.Columns)
            {
                var cell = new A.TableCell(
                    new A.TextBody(
                        new BodyProperties(),
                        new ListStyle(),
                        new Paragraph(
                            new Run(
                                new RunProperties { Bold = true },
                                new A.Text { Text = col.Header ?? "" }))),
                    new TableCellProperties());
                headerRow.AppendChild(cell);
            }

            tbl.AppendChild(headerRow);
        }

        // Data rows
        foreach (var row in tableElem.Rows)
        {
            var tr = new A.TableRow { Height = rowHeight };
            for (var c = 0; c < colCount; c++)
            {
                var text = c < row.Cells.Count ? row.Cells[c].Text ?? "" : "";
                var cell = new A.TableCell(
                    new A.TextBody(
                        new BodyProperties(),
                        new ListStyle(),
                        new Paragraph(new Run(new A.Text { Text = text }))),
                    new TableCellProperties());
                tr.AppendChild(cell);
            }

            tbl.AppendChild(tr);
        }

        var graphicFrame = new P.GraphicFrame(
            new P.NonVisualGraphicFrameProperties(
                new P.NonVisualDrawingProperties { Id = shapeId, Name = $"Table{shapeId}" },
                new P.NonVisualGraphicFrameDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new Transform(
                new Offset
                {
                    X = (long)(tableElem.Position.XMm * EmuPerMm),
                    Y = (long)(tableElem.Position.YMm * EmuPerMm)
                },
                new Extents
                {
                    Cx = tableWidth,
                    Cy = tableHeight
                }),
            new Graphic(
                new GraphicData(tbl)
                { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" }));

        tree.AppendChild(graphicFrame);
    }

    private void RenderShape(ShapeTree tree, ShapeElement shapeElem)
    {
        var shapeId = NextShapeId();
        var presetShape = shapeElem.ShapeType switch
        {
            ShapeType.Ellipse => ShapeTypeValues.Ellipse,
            ShapeType.RoundedRectangle => ShapeTypeValues.RoundRectangle,
            ShapeType.Triangle => ShapeTypeValues.Triangle,
            ShapeType.Arrow => ShapeTypeValues.RightArrow,
            ShapeType.Star => ShapeTypeValues.Star5,
            ShapeType.Line => ShapeTypeValues.Line,
            ShapeType.Callout => ShapeTypeValues.WedgeRoundRectangleCallout,
            _ => ShapeTypeValues.Rectangle
        };

        var shapeProps = new P.ShapeProperties(
            CreateTransform(shapeElem.Position, shapeElem.Size),
            new PresetGeometry(new AdjustValueList()) { Preset = presetShape });

        // Fill
        if (shapeElem.Style?.FillColor is not null)
            shapeProps.AppendChild(new SolidFill(
                new RgbColorModelHex { Val = shapeElem.Style.FillColor.TrimStart('#') }));

        // Border
        if (shapeElem.Style?.Border is not null)
        {
            var outline = new Outline { Width = (int)((shapeElem.Style.Border.WidthPt ?? 1) * EmuPerPt) };
            if (shapeElem.Style.Border.Color is not null)
                outline.AppendChild(new SolidFill(
                    new RgbColorModelHex { Val = shapeElem.Style.Border.Color.TrimStart('#') }));
            shapeProps.AppendChild(outline);
        }

        // Rotation
        if (shapeElem.Style is { RotationDegrees: not 0 })
        {
            var xfrm = shapeProps.GetFirstChild<Transform2D>();
            if (xfrm is not null)
                xfrm.Rotation = (int)(shapeElem.Style.RotationDegrees * 60000); // 60000 EMU per degree
        }

        var shape = new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = shapeId, Name = $"Shape{shapeId}" },
                new P.NonVisualShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            shapeProps);

        // Text in shape
        if (shapeElem.Text is not null)
        {
            var textRp = new RunProperties();
            if (shapeElem.TextStyle is not null)
            {
                if (shapeElem.TextStyle.Bold) textRp.Bold = true;
                if (shapeElem.TextStyle.Italic) textRp.Italic = true;
                if (shapeElem.TextStyle.FontSizePt.HasValue)
                    textRp.FontSize = (int)(shapeElem.TextStyle.FontSizePt.Value * 100);
                if (shapeElem.TextStyle.Color is not null)
                    textRp.AppendChild(new SolidFill(
                        new RgbColorModelHex { Val = shapeElem.TextStyle.Color.TrimStart('#') }));
            }

            shape.AppendChild(new P.TextBody(
                new BodyProperties { Anchor = TextAnchoringTypeValues.Center },
                new ListStyle(),
                new Paragraph(
                    new ParagraphProperties { Alignment = TextAlignmentTypeValues.Center },
                    new Run(textRp, new A.Text { Text = shapeElem.Text }))));
        }

        tree.AppendChild(shape);
    }

    #endregion

    #region Transitions

    private static Transition CreateTransition(SlideTransition transDef)
    {
        var transition = new Transition();

        if (transDef.DurationMs > 0)
            transition.Duration = transDef.DurationMs.ToString(CultureInfo.InvariantCulture);

        transition.AdvanceOnClick = transDef.AdvanceOnClick;

        if (transDef.AdvanceAfterMs.HasValue)
            transition.AdvanceAfterTime = transDef.AdvanceAfterMs.Value.ToString(CultureInfo.InvariantCulture);

        // Transition type child element
        OpenXmlElement? transChild = transDef.Type switch
        {
            TransitionType.Fade => new FadeTransition(),
            TransitionType.Push => new PushTransition(),
            TransitionType.Wipe => new WipeTransition(),
            TransitionType.Split => new SplitTransition(),
            TransitionType.Cover => new CoverTransition(),
            TransitionType.Cut => new CutTransition(),
            TransitionType.Dissolve => new DissolveTransition(),
            _ => null
        };

        if (transChild is not null)
            transition.AppendChild(transChild);

        return transition;
    }

    #endregion

    #region Speaker Notes

    private static void AddSpeakerNotes(SlidePart slidePart, string notes)
    {
        var notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();
        notesSlidePart.NotesSlide = new NotesSlide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(),
                    new P.Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 2, Name = "Notes Placeholder" },
                            new P.NonVisualShapeDrawingProperties(),
                            new ApplicationNonVisualDrawingProperties(
                                new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1 })),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new BodyProperties(),
                            new ListStyle(),
                            new Paragraph(
                                new Run(new A.Text { Text = notes })))))));
    }

    #endregion

    #region Helpers

    private uint NextShapeId()
    {
        return (uint)Interlocked.Increment(ref _shapeIdCounter);
    }

    private static Transform2D CreateTransform(Abstractions.Models.Position pos, ElementSize size)
    {
        return new Transform2D(
            new Offset
            {
                X = (long)(pos.XMm * EmuPerMm),
                Y = (long)(pos.YMm * EmuPerMm)
            },
            new Extents
            {
                Cx = (long)(size.WidthMm * EmuPerMm),
                Cy = (long)(size.HeightMm * EmuPerMm)
            });
    }

    private static P.TextBody CreateTextBody(
        IReadOnlyList<TextRun> runs, TextBoxStyle? style)
    {
        var bodyProps = new BodyProperties { Wrap = TextWrappingValues.Square };
        if (style?.VerticalAlignment == VerticalAlignment.Middle)
            bodyProps.Anchor = TextAnchoringTypeValues.Center;
        else if (style?.VerticalAlignment == VerticalAlignment.Bottom)
            bodyProps.Anchor = TextAnchoringTypeValues.Bottom;

        var textBody = new P.TextBody(bodyProps, new ListStyle());

        // Group runs into a single paragraph (for simplicity)
        var para = new Paragraph();

        if (style is not null)
        {
            var pp = new ParagraphProperties();
            pp.Alignment = style.TextAlignment switch
            {
                TextAlignment.Center => TextAlignmentTypeValues.Center,
                TextAlignment.Right => TextAlignmentTypeValues.Right,
                TextAlignment.Justify => TextAlignmentTypeValues.Justified,
                _ => TextAlignmentTypeValues.Left
            };
            para.AppendChild(pp);
        }

        foreach (var textRun in runs)
        {
            var rp = new RunProperties();
            if (textRun.Style is not null)
            {
                if (textRun.Style.Bold) rp.Bold = true;
                if (textRun.Style.Italic) rp.Italic = true;
                if (textRun.Style.Underline) rp.Underline = TextUnderlineValues.Single;
                if (textRun.Style.Strikethrough) rp.Strike = TextStrikeValues.SingleStrike;
                if (textRun.Style.FontSizePt.HasValue)
                    rp.FontSize = (int)(textRun.Style.FontSizePt.Value * 100);
                if (textRun.Style.Color is not null)
                    rp.AppendChild(new SolidFill(
                        new RgbColorModelHex { Val = textRun.Style.Color.TrimStart('#') }));
                if (textRun.Style.FontFamily is not null)
                    rp.AppendChild(new LatinFont { Typeface = textRun.Style.FontFamily });
            }

            para.AppendChild(new Run(rp, new A.Text { Text = textRun.Text }));
        }

        textBody.AppendChild(para);
        return textBody;
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
