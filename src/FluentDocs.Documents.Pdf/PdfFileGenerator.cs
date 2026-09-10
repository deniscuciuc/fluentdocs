using System.Diagnostics;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace FluentDocs.Documents.Pdf;

/// <summary>
/// Generates PDF files from a <see cref="DocumentDefinition"/> using PDFsharp.
/// </summary>
public sealed class PdfFileGenerator : IFileGenerator
{
    private const double PtPerMm = 2.8346; // 1 mm = 2.8346 pt
    private const double DefaultFontSize = 11;
    private const string DefaultFontFamily = "Arial";

    private readonly IChartRenderer _chartRenderer;
    private readonly ILogger<PdfFileGenerator> _logger;

    public PdfFileGenerator(IChartRenderer chartRenderer, ILogger<PdfFileGenerator> logger)
    {
        _chartRenderer = chartRenderer;
        _logger = logger;

        // Ensure a font resolver is registered (required on Linux/macOS where PDFsharp
        // cannot discover fonts automatically).
        if (GlobalFontSettings.FontResolver is null)
            GlobalFontSettings.FontResolver = new SystemFontResolver();
    }

    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.Pdf;

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

        _logger.LogInformation("Generating PDF document '{FileName}'.", docDef.FileName);

        try
        {
            var pdfDoc = new PdfDocument();

            // Metadata
            pdfDoc.Info.Title = docDef.Metadata.Title ?? "";
            pdfDoc.Info.Author = docDef.Metadata.Author ?? "";
            pdfDoc.Info.Subject = docDef.Metadata.Subject ?? "";
            if (docDef.Metadata.Keywords.Count > 0)
                pdfDoc.Info.Keywords = string.Join(", ", docDef.Metadata.Keywords);
            if (docDef.Metadata.CreatedAt.HasValue)
                pdfDoc.Info.CreationDate = docDef.Metadata.CreatedAt.Value.UtcDateTime;

            var defaultFont = ResolveFont(docDef.DefaultStyles.DefaultTextStyle);
            var layout = new LayoutContext(pdfDoc, docDef.PageSetup, defaultFont);

            // Render header/footer callback
            layout.HeaderDef = docDef.Header;
            layout.FooterDef = docDef.Footer;

            // If no sections or elements, at least create one page
            if (docDef.Sections.Count == 0 || docDef.Sections.All(s => s.Elements.Count == 0)) layout.EnsurePage();

            foreach (var section in docDef.Sections)
                foreach (var element in section.Elements)
                    await RenderElementAsync(layout, element, docDef.DefaultStyles, ct).ConfigureAwait(false);

            // Dispose the current drawing surface before rendering headers/footers
            layout.DisposeCurrentGfx();
            // Render headers/footers on all pages
            layout.RenderHeadersFooters();

            using var ms = new MemoryStream();
            pdfDoc.Save(ms, false);
            var bytes = ms.ToArray();

            sw.Stop();
            RecordDiagnostics("success", sw, bytes.Length);
            return GenerationResult.Success(bytes, "application/pdf", GeneratedFileFormat.Pdf);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF document '{FileName}'.", docDef.FileName);
            RecordDiagnostics("failure", sw, 0);
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    #region Element Rendering

    private async Task RenderElementAsync(
        LayoutContext ctx, IDocumentElement element, DocumentStyles defaults, CancellationToken ct)
    {
        switch (element)
        {
            case HeadingElement heading:
                RenderHeading(ctx, heading, defaults);
                break;
            case ParagraphElement paragraph:
                RenderParagraph(ctx, paragraph, defaults);
                break;
            case ImageElement image:
                RenderImage(ctx, image.Image);
                break;
            case TableContentElement table:
                RenderTable(ctx, table);
                break;
            case ChartElement chart:
                await RenderChartAsync(ctx, chart.Chart, ct).ConfigureAwait(false);
                break;
            case ListElement list:
                RenderList(ctx, list, 0);
                break;
            case CodeBlockElement codeBlock:
                RenderCodeBlock(ctx, codeBlock);
                break;
            case BlockQuoteElement blockQuote:
                RenderBlockQuote(ctx, blockQuote);
                break;
            case PageBreakElement:
                ctx.NewPage();
                break;
            case HorizontalRuleElement:
                RenderHorizontalRule(ctx);
                break;
        }
    }

    private static void RenderHeading(LayoutContext ctx, HeadingElement heading, DocumentStyles defaults)
    {
        double[] sizes = [24, 20, 16, 14, 12, 11];
        var level = (int)heading.Level;
        var fontSize = sizes[Math.Clamp(level - 1, 0, 5)];

        defaults.HeadingStyles.TryGetValue(heading.Level, out var headingStyle);
        var effectiveSize = heading.Style?.FontSizePt ?? headingStyle?.FontSizePt ?? fontSize;
        var fontFamily = heading.Style?.FontFamily ?? headingStyle?.FontFamily ?? DefaultFontFamily;

        var font = new XFont(fontFamily, effectiveSize, XFontStyleEx.Bold);
        ctx.EnsureSpace(effectiveSize * 2);
        ctx.DrawString(heading.Text, font, ResolveColor(heading.Style?.Color ?? headingStyle?.Color));
        ctx.AdvanceY(effectiveSize * 0.5); // extra space after heading
    }

    private static void RenderParagraph(LayoutContext ctx, ParagraphElement paragraph, DocumentStyles defaults)
    {
        var defaultTextStyle = defaults.DefaultTextStyle;
        var fontSize = defaultTextStyle?.FontSizePt ?? DefaultFontSize;
        var fontFamily = defaultTextStyle?.FontFamily ?? DefaultFontFamily;
        var lineHeight = fontSize * 1.4;

        foreach (var run in paragraph.Runs)
        {
            var fSize = run.Style?.FontSizePt ?? fontSize;
            var fFamily = run.Style?.FontFamily ?? fontFamily;
            var fStyle = XFontStyleEx.Regular;
            if (run.Style?.Bold == true) fStyle |= XFontStyleEx.Bold;
            if (run.Style?.Italic == true) fStyle |= XFontStyleEx.Italic;

            var font = new XFont(fFamily, fSize, fStyle);
            var color = ResolveColor(run.Style?.Color);

            // Simple word-wrapping
            var words = run.Text.Split(' ');
            var line = "";
            foreach (var word in words)
            {
                var test = line.Length > 0 ? $"{line} {word}" : word;
                var textWidth = ctx.MeasureString(test, font);
                if (textWidth > ctx.ContentWidth && line.Length > 0)
                {
                    ctx.EnsureSpace(lineHeight);
                    ctx.DrawString(line, font, color);
                    line = word;
                }
                else
                {
                    line = test;
                }
            }

            if (line.Length > 0)
            {
                ctx.EnsureSpace(lineHeight);
                ctx.DrawString(line, font, color);
            }
        }

        ctx.AdvanceY(fontSize * 0.4); // paragraph spacing
    }

    private static void RenderImage(LayoutContext ctx, ImageContent image)
    {
        try
        {
            using var stream = new MemoryStream(image.Data);
            var xImage = XImage.FromStream(stream);

            var maxWidth = ctx.ContentWidth;
            var imgWidth = image.WidthMm.HasValue ? image.WidthMm.Value * PtPerMm : xImage.PointWidth;
            var imgHeight = image.HeightMm.HasValue ? image.HeightMm.Value * PtPerMm : xImage.PointHeight;

            if (imgWidth > maxWidth)
            {
                var scale = maxWidth / imgWidth;
                imgWidth = maxWidth;
                imgHeight *= scale;
            }

            ctx.EnsureSpace(imgHeight + 5);
            ctx.Gfx.DrawImage(xImage, ctx.LeftX, ctx.CurrentY, imgWidth, imgHeight);
            ctx.AdvanceY(imgHeight + 5);

            // Caption
            if (image.Caption is not null)
            {
                var captionFont = new XFont(DefaultFontFamily, 9, XFontStyleEx.Italic);
                ctx.EnsureSpace(12);
                ctx.DrawStringCentered(image.Caption, captionFont, XBrushes.Gray);
            }
        }
        catch
        {
            // If image cannot be loaded, draw a placeholder
            var font = new XFont(DefaultFontFamily, 9, XFontStyleEx.Italic);
            ctx.EnsureSpace(14);
            ctx.DrawString($"[Image: {image.AltText ?? "unavailable"}]", font, XBrushes.Gray);
        }
    }

    private static void RenderTable(LayoutContext ctx, TableContentElement table)
    {
        if (table.Columns.Count == 0) return;

        var colCount = table.Columns.Count;
        var availWidth = ctx.ContentWidth;
        var colWidth = availWidth / colCount;
        var rowHeight = 18.0;
        var fontSize = 9.0;
        var font = new XFont(DefaultFontFamily, fontSize);
        var boldFont = new XFont(DefaultFontFamily, fontSize, XFontStyleEx.Bold);
        var pen = new XPen(XColors.Gray, 0.5);

        // Calculate total rows (header + data)
        var totalRows = table.Rows.Count + (table.Columns.Any(c => c.Header is not null) ? 1 : 0);
        var tableHeight = totalRows * rowHeight;

        ctx.EnsureSpace(Math.Min(tableHeight, rowHeight * 3)); // at least header + 2 rows

        var startX = ctx.LeftX;
        var startY = ctx.CurrentY;

        // Header row
        if (table.Columns.Any(c => c.Header is not null))
        {
            // Header background
            ctx.Gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(217, 226, 243)),
                startX, startY, availWidth, rowHeight);

            for (var c = 0; c < colCount; c++)
            {
                var cellX = startX + c * colWidth;
                ctx.Gfx.DrawRectangle(pen, cellX, startY, colWidth, rowHeight);
                var headerText = table.Columns[c].Header ?? "";
                ctx.Gfx.DrawString(headerText, boldFont, XBrushes.Black,
                    new XRect(cellX + 3, startY + 2, colWidth - 6, rowHeight - 4),
                    XStringFormats.TopLeft);
            }

            startY += rowHeight;
        }

        // Data rows
        foreach (var row in table.Rows)
        {
            if (startY + rowHeight > ctx.BottomY)
            {
                ctx.NewPage();
                startY = ctx.CurrentY;
            }

            for (var c = 0; c < Math.Min(colCount, row.Cells.Count); c++)
            {
                var cellX = startX + c * colWidth;
                ctx.Gfx.DrawRectangle(pen, cellX, startY, colWidth, rowHeight);
                var cellText = row.Cells[c].Text ?? "";
                ctx.Gfx.DrawString(cellText, font, XBrushes.Black,
                    new XRect(cellX + 3, startY + 2, colWidth - 6, rowHeight - 4),
                    XStringFormats.TopLeft);
            }

            startY += rowHeight;
        }

        ctx.SetY(startY + 5);
    }

    private async Task RenderChartAsync(LayoutContext ctx, ChartDefinition chart, CancellationToken ct)
    {
        var rendered = await _chartRenderer.RenderAsync(chart, ct: ct).ConfigureAwait(false);
        var image = new ImageContent
        {
            Data = rendered.ImageData,
            Format = rendered.Format,
            AltText = chart.Title ?? "Chart",
            WidthMm = chart.WidthPx * 25.4 / 96,
            HeightMm = chart.HeightPx * 25.4 / 96
        };
        RenderImage(ctx, image);
    }

    private static void RenderList(LayoutContext ctx, ListElement list, int depth)
    {
        var fontSize = DefaultFontSize;
        var font = new XFont(DefaultFontFamily, fontSize);
        var lineHeight = fontSize * 1.4;
        var indent = 20 * (depth + 1);

        for (var i = 0; i < list.Items.Count; i++)
        {
            var item = list.Items[i];
            var bullet = list.Ordered ? $"{i + 1}. " : "• ";
            var text = string.Join("", item.Content.Select(r => r.Text));

            ctx.EnsureSpace(lineHeight);
            ctx.DrawStringAt(ctx.LeftX + indent, $"{bullet}{text}", font, XBrushes.Black);

            // Nested items
            foreach (var nested in item.Nested) RenderListItems(ctx, nested, depth + 1, font, lineHeight);
        }

        ctx.AdvanceY(fontSize * 0.3);
    }

    private static void RenderListItems(LayoutContext ctx, ListItem item, int depth, XFont font, double lineHeight)
    {
        var indent = 20 * (depth + 1);
        var text = string.Join("", item.Content.Select(r => r.Text));

        ctx.EnsureSpace(lineHeight);
        ctx.DrawStringAt(ctx.LeftX + indent, $"◦ {text}", font, XBrushes.Black);

        foreach (var nested in item.Nested) RenderListItems(ctx, nested, depth + 1, font, lineHeight);
    }

    private static void RenderCodeBlock(LayoutContext ctx, CodeBlockElement codeBlock)
    {
        var fontSize = 9.0;
        var font = new XFont("Courier New", fontSize);
        var lineHeight = fontSize * 1.3;
        var lines = codeBlock.Code.Split('\n');
        var blockHeight = lines.Length * lineHeight + 10;
        var padding = 5.0;

        ctx.EnsureSpace(Math.Min(blockHeight, lineHeight * 3));

        // Background
        var bgBrush = new XSolidBrush(XColor.FromArgb(245, 245, 245));

        foreach (var line in lines)
        {
            ctx.EnsureSpace(lineHeight);
            // Draw background for this line
            ctx.Gfx.DrawRectangle(bgBrush,
                ctx.LeftX, ctx.CurrentY - 1, ctx.ContentWidth, lineHeight + 2);
            ctx.DrawStringAt(ctx.LeftX + padding, line, font, XBrushes.Black);
        }

        ctx.AdvanceY(5);
    }

    private static void RenderBlockQuote(LayoutContext ctx, BlockQuoteElement blockQuote)
    {
        var fontSize = DefaultFontSize;
        var font = new XFont(DefaultFontFamily, fontSize, XFontStyleEx.Italic);
        var lineHeight = fontSize * 1.4;
        var indent = 20.0;
        var text = string.Join("", blockQuote.Content.Select(r => r.Text));

        ctx.EnsureSpace(lineHeight);

        // Left border line
        var borderPen = new XPen(XColor.FromArgb(170, 170, 170), 2);
        ctx.Gfx.DrawLine(borderPen,
            ctx.LeftX + indent - 5, ctx.CurrentY,
            ctx.LeftX + indent - 5, ctx.CurrentY + lineHeight);

        ctx.DrawStringAt(ctx.LeftX + indent, text, font, XBrushes.DarkGray);
        ctx.AdvanceY(fontSize * 0.3);
    }

    private static void RenderHorizontalRule(LayoutContext ctx)
    {
        ctx.EnsureSpace(10);
        var pen = new XPen(XColor.FromArgb(153, 153, 153), 1);
        var y = ctx.CurrentY + 4;
        ctx.Gfx.DrawLine(pen, ctx.LeftX, y, ctx.LeftX + ctx.ContentWidth, y);
        ctx.AdvanceY(10);
    }

    #endregion

    #region Helpers

    private static XFont ResolveFont(TextStyle? style)
    {
        var family = style?.FontFamily ?? DefaultFontFamily;
        var size = style?.FontSizePt ?? DefaultFontSize;
        var fStyle = XFontStyleEx.Regular;
        if (style?.Bold == true) fStyle |= XFontStyleEx.Bold;
        if (style?.Italic == true) fStyle |= XFontStyleEx.Italic;
        return new XFont(family, size, fStyle);
    }

    private static XSolidBrush ResolveColor(string? color)
    {
        if (color is null) return XBrushes.Black;
        try
        {
            var hex = color.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToInt32(hex[..2], 16);
                var g = Convert.ToInt32(hex[2..4], 16);
                var b = Convert.ToInt32(hex[4..6], 16);
                return new XSolidBrush(XColor.FromArgb(r, g, b));
            }
        }
        catch
        {
            /* ignore */
        }

        return XBrushes.Black;
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

    #region Layout Engine

    /// <summary>
    /// Tracks the current page and Y position, handling page breaks automatically.
    /// </summary>
    private sealed class LayoutContext
    {
        private readonly PdfDocument _doc;
        private readonly PageSetup _pageSetup;
        private readonly XFont _defaultFont;
        private readonly string _defaultFontFamily;
        private readonly double _pageWidthPt;
        private readonly double _pageHeightPt;
        private readonly double _marginTop;
        private readonly double _marginRight;
        private readonly double _marginBottom;
        private readonly double _marginLeft;

        private PdfPage? _currentPage;
        private XGraphics? _gfx;
        private double _y;

        public HeaderFooterDefinition? HeaderDef { get; set; }
        public HeaderFooterDefinition? FooterDef { get; set; }

        public LayoutContext(PdfDocument doc, PageSetup pageSetup, XFont defaultFont)
        {
            _doc = doc;
            _pageSetup = pageSetup;
            _defaultFont = defaultFont;
            _defaultFontFamily = DefaultFontFamily;

            var (w, h) = GetPageSizePt(pageSetup);
            _pageWidthPt = w;
            _pageHeightPt = h;

            _marginTop = pageSetup.Margins.TopMm * PtPerMm;
            _marginRight = pageSetup.Margins.RightMm * PtPerMm;
            _marginBottom = pageSetup.Margins.BottomMm * PtPerMm;
            _marginLeft = pageSetup.Margins.LeftMm * PtPerMm;
        }

        public XGraphics Gfx => _gfx ?? EnsurePage();
        public double CurrentY => _y;
        public double LeftX => _marginLeft;
        public double ContentWidth => _pageWidthPt - _marginLeft - _marginRight;
        public double BottomY => _pageHeightPt - _marginBottom;

        public XGraphics EnsurePage()
        {
            if (_gfx is not null) return _gfx;
            return NewPage();
        }

        public XGraphics NewPage()
        {
            _gfx?.Dispose();
            _currentPage = _doc.AddPage();
            _currentPage.Width = XUnit.FromPoint(_pageWidthPt);
            _currentPage.Height = XUnit.FromPoint(_pageHeightPt);

            if (_pageSetup.Orientation == PageOrientation.Landscape)
                _currentPage.Orientation = PdfSharp.PageOrientation.Landscape;

            _gfx = XGraphics.FromPdfPage(_currentPage);
            _y = _marginTop;
            return _gfx;
        }

        /// <summary>
        /// Disposes the current XGraphics so a new one can be created from the same page
        /// (e.g. for header/footer rendering).
        /// </summary>
        public void DisposeCurrentGfx()
        {
            _gfx?.Dispose();
            _gfx = null;
        }

        public void EnsureSpace(double needed)
        {
            if (_gfx is null)
            {
                EnsurePage();
                return;
            }

            if (_y + needed > BottomY) NewPage();
        }

        public void AdvanceY(double amount)
        {
            _y += amount;
        }

        public void SetY(double y)
        {
            _y = y;
        }

        public double MeasureString(string text, XFont font)
        {
            EnsurePage();
            return Gfx.MeasureString(text, font).Width;
        }

        public void DrawString(string text, XFont font, XBrush? brush = null)
        {
            EnsurePage();
            var lineHeight = font.Size * 1.4;
            Gfx.DrawString(text, font, brush ?? XBrushes.Black,
                new XRect(_marginLeft, _y, ContentWidth, lineHeight),
                XStringFormats.TopLeft);
            _y += lineHeight;
        }

        public void DrawStringCentered(string text, XFont font, XBrush brush)
        {
            EnsurePage();
            var lineHeight = font.Size * 1.4;
            Gfx.DrawString(text, font, brush,
                new XRect(_marginLeft, _y, ContentWidth, lineHeight),
                XStringFormats.TopCenter);
            _y += lineHeight;
        }

        public void DrawStringAt(double x, string text, XFont font, XBrush brush)
        {
            EnsurePage();
            var lineHeight = font.Size * 1.4;
            Gfx.DrawString(text, font, brush,
                new XRect(x, _y, ContentWidth - (x - _marginLeft), lineHeight),
                XStringFormats.TopLeft);
            _y += lineHeight;
        }

        public void RenderHeadersFooters()
        {
            var headerFont = new XFont(_defaultFontFamily, 8);
            var footerFont = new XFont(_defaultFontFamily, 8);

            for (var i = 0; i < _doc.PageCount; i++)
            {
                var page = _doc.Pages[i];
                using var gfx = XGraphics.FromPdfPage(page);

                if (HeaderDef is not null)
                {
                    var headerText = string.Join("", HeaderDef.Content.Select(r => r.Text));
                    if (HeaderDef.ShowPageNumber)
                        headerText += $" — Page {i + 1}";
                    gfx.DrawString(headerText, headerFont, XBrushes.Gray,
                        new XRect(_marginLeft, _marginTop / 2, ContentWidth, 12),
                        XStringFormats.TopLeft);
                }

                if (FooterDef is not null)
                {
                    var footerText = string.Join("", FooterDef.Content.Select(r => r.Text));
                    if (FooterDef.ShowPageNumber)
                        footerText += $" — Page {i + 1}";
                    var footerY = page.Height.Point - _marginBottom / 2;
                    gfx.DrawString(footerText, footerFont, XBrushes.Gray,
                        new XRect(_marginLeft, footerY, ContentWidth, 12),
                        XStringFormats.TopLeft);
                }
            }
        }

        private static (double w, double h) GetPageSizePt(PageSetup ps)
        {
            double w, h;
            switch (ps.PageSize)
            {
                case PageSize.A4:
                    w = 595;
                    h = 842;
                    break;
                case PageSize.Letter:
                    w = 612;
                    h = 792;
                    break;
                case PageSize.Legal:
                    w = 612;
                    h = 1008;
                    break;
                case PageSize.A3:
                    w = 842;
                    h = 1191;
                    break;
                case PageSize.A5:
                    w = 420;
                    h = 595;
                    break;
                case PageSize.B5:
                    w = 499;
                    h = 709;
                    break;
                case PageSize.Custom:
                    w = (ps.CustomWidthMm ?? 210) * PtPerMm;
                    h = (ps.CustomHeightMm ?? 297) * PtPerMm;
                    break;
                default:
                    w = 595;
                    h = 842;
                    break;
            }

            if (ps.Orientation == PageOrientation.Landscape)
                (w, h) = (h, w);

            return (w, h);
        }
    }

    #endregion
}
