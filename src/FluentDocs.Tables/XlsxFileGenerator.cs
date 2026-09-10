using System.Diagnostics;
using ClosedXML.Excel;
using FluentDocs.Abstractions;
using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Spreadsheets;
using FluentDocs.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace FluentDocs.Tables;

/// <summary>
/// Generates XLSX files from a <see cref="SpreadsheetDefinition"/> using ClosedXML.
/// </summary>
public sealed class XlsxFileGenerator(IChartRenderer chartRenderer, ILogger<XlsxFileGenerator> logger)
    : IFileGenerator
{
    public GeneratedFileFormat SupportedFormat => GeneratedFileFormat.Xlsx;

    public async Task<GenerationResult> GenerateAsync(
        FileDefinition definition,
        GenerationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition is not SpreadsheetDefinition sheetDef)
            return GenerationResult.Failure(
                "INVALID_DEFINITION",
                $"Expected {nameof(SpreadsheetDefinition)} but received {definition.GetType().Name}.");

        using var activity = GenerationDiagnostics.StartGenerationActivity(SupportedFormat.ToString());
        var sw = Stopwatch.StartNew();

        logger.LogInformation("Generating XLSX spreadsheet '{FileName}'.", sheetDef.FileName);

        try
        {
            using var workbook = new XLWorkbook();

            // Set workbook properties
            if (sheetDef.Metadata.Title is not null)
                workbook.Properties.Title = sheetDef.Metadata.Title;
            if (sheetDef.Metadata.Author is not null)
                workbook.Properties.Author = sheetDef.Metadata.Author;
            if (sheetDef.Metadata.Subject is not null)
                workbook.Properties.Subject = sheetDef.Metadata.Subject;
            if (sheetDef.Metadata.Description is not null)
                workbook.Properties.Comments = sheetDef.Metadata.Description;

            foreach (var sheetDefinition in sheetDef.Sheets)
                await RenderSheetAsync(workbook, sheetDefinition, sheetDef.DefaultCellStyle, ct).ConfigureAwait(false);

            // If no sheets defined, add an empty one
            if (sheetDef.Sheets.Count == 0)
                workbook.Worksheets.Add("Sheet1");

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            sw.Stop();
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "success"));
            GenerationDiagnostics.GenerationDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));
            GenerationDiagnostics.OutputSize.Record(bytes.Length,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()));

            return GenerationResult.Success(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                GeneratedFileFormat.Xlsx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate XLSX spreadsheet '{FileName}'.", sheetDef.FileName);
            GenerationDiagnostics.FilesGenerated.Add(1,
                new KeyValuePair<string, object?>("format", SupportedFormat.ToString()),
                new KeyValuePair<string, object?>("outcome", "failure"));
            return GenerationResult.Failure("GENERATION_FAILED", ex.Message, ex);
        }
    }

    private async Task RenderSheetAsync(
        XLWorkbook workbook,
        SheetDefinition sheet,
        CellStyle? defaultStyle,
        CancellationToken ct)
    {
        var ws = workbook.Worksheets.Add(sheet.Name);
        var startRow = 1;

        // Column headers — only render header row when at least one header has text
        var hasHeaders = sheet.Columns.Any(c => !string.IsNullOrWhiteSpace(c.Header));
        if (hasHeaders)
        {
            for (var c = 0; c < sheet.Columns.Count; c++)
            {
                var col = sheet.Columns[c];
                var cell = ws.Cell(1, c + 1);
                cell.Value = col.Header ?? "";

                if (col.Style is not null)
                    ApplyCellStyle(cell, col.Style);
            }

            startRow = 2;
        }

        // Column widths (always applied, even without header row)
        for (var c = 0; c < sheet.Columns.Count; c++)
        {
            var col = sheet.Columns[c];
            if (col.AutoWidth)
                ws.Column(c + 1).AdjustToContents();
            else if (col.WidthMm.HasValue)
                ws.Column(c + 1).Width = col.WidthMm.Value / 2.54;
        }

        // Data rows
        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var row = sheet.Rows[r];
            var rowNum = r + startRow;

            if (row.HeightMm.HasValue)
                ws.Row(rowNum).Height = row.HeightMm.Value * 2.835; // mm to points

            for (var c = 0; c < row.Cells.Count; c++)
            {
                var cellDef = row.Cells[c];
                var cell = ws.Cell(rowNum, c + 1);

                // Set value
                SetCellValue(cell, cellDef);

                // Set formula
                if (cellDef.Formula is not null)
                    cell.FormulaA1 = cellDef.Formula;

                // Set hyperlink
                if (cellDef.Hyperlink is not null)
                    try
                    {
                        cell.SetHyperlink(new XLHyperlink(cellDef.Hyperlink));
                    }
                    catch
                    {
                        /* ignore invalid hyperlinks */
                    }

                // Apply cell-level style
                var style = cellDef.Style ?? row.Style ?? defaultStyle;
                if (style is not null)
                    ApplyCellStyle(cell, style);
            }
        }

        // Merged cells
        foreach (var merge in sheet.MergedCells)
            ws.Range(merge.StartRow + startRow, merge.StartColumn + 1,
                merge.EndRow + startRow, merge.EndColumn + 1).Merge();

        // Freeze first row/column
        if (sheet.FreezeFirstRow && sheet.FreezeFirstColumn)
            ws.SheetView.Freeze(1, 1);
        else if (sheet.FreezeFirstRow)
            ws.SheetView.Freeze(1, 0);
        else if (sheet.FreezeFirstColumn)
            ws.SheetView.Freeze(0, 1);

        // Auto filter
        if (sheet.AutoFilter && ws.RangeUsed() is not null)
            ws.RangeUsed()!.SetAutoFilter();

        // Conditional formats
        foreach (var cf in sheet.ConditionalFormats)
        {
            var range = ws.Range(cf.StartRow + startRow, cf.StartColumn + 1,
                cf.EndRow + startRow, cf.EndColumn + 1);
            var rule = range.AddConditionalFormat();
            rule.WhenIsTrue(cf.Condition);

            if (cf.Style.BackgroundColor is not null)
                rule.Style.Fill.BackgroundColor = XLColor.FromHtml(cf.Style.BackgroundColor);
            if (cf.Style.TextStyle?.Bold == true)
                rule.Style.Font.Bold = true;
            if (cf.Style.TextStyle?.Color is not null)
                rule.Style.Font.FontColor = XLColor.FromHtml(cf.Style.TextStyle.Color);
        }

        // Embed chart images — placed after all data rows
        var chartColumn = Math.Max(1, sheet.Columns.Count + 2);
        var chartStartRow = startRow + sheet.Rows.Count;
        foreach (var chartDef in sheet.Charts)
        {
            var rendered = await chartRenderer.RenderAsync(chartDef, ct: ct).ConfigureAwait(false);
            using var imgStream = new MemoryStream(rendered.ImageData);
            var picture = ws.AddPicture(imgStream)
                .MoveTo(ws.Cell(chartStartRow, chartColumn));

            if (chartDef.WidthPx > 0)
                picture.Width = chartDef.WidthPx;

            if (chartDef.HeightPx > 0)
                picture.Height = chartDef.HeightPx;

            // Keep a visual gap between charts to avoid accidental overlap.
            chartStartRow += Math.Max(4, (chartDef.HeightPx / 20) + 3);
        }

        // Embed plain images
        var imageColumn = Math.Max(1, sheet.Columns.Count + 2);
        var imageStartRow = chartStartRow;
        foreach (var img in sheet.Images)
        {
            using var imgStream = new MemoryStream(img.Data);
            var picture = ws.AddPicture(imgStream);

            if (!string.IsNullOrWhiteSpace(img.AnchorCell))
            {
                picture.MoveTo(ws.Cell(img.AnchorCell!), img.OffsetXPx, img.OffsetYPx);
            }
            else
            {
                picture.MoveTo(ws.Cell(imageStartRow, imageColumn));
                imageStartRow += Math.Max(4, (picture.Height / 20) + 3);
            }

            if (img.WidthMm.HasValue)
                picture.Width = MmToPixels(img.WidthMm.Value);

            if (img.HeightMm.HasValue)
                picture.Height = MmToPixels(img.HeightMm.Value);
        }

        // Auto-adjust all columns at the end
        if (sheet.Columns.Any(c => c.AutoWidth))
            ws.Columns().AdjustToContents();
    }

    private static void SetCellValue(IXLCell cell, CellValue cellDef)
    {
        switch (cellDef.Value)
        {
            case null:
                break;
            case string s:
                cell.Value = s;
                break;
            case double d:
                cell.Value = d;
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case decimal m:
                cell.Value = (double)m;
                break;
            case float f:
                cell.Value = (double)f;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                break;
            case DateTimeOffset dto:
                cell.Value = dto.DateTime;
                break;
            default:
                cell.Value = cellDef.Value.ToString();
                break;
        }
    }

    private static void ApplyCellStyle(IXLCell cell, CellStyle style)
    {
        if (style.TextStyle is not null)
        {
            if (style.TextStyle.FontFamily is not null)
                cell.Style.Font.FontName = style.TextStyle.FontFamily;
            if (style.TextStyle.FontSizePt.HasValue)
                cell.Style.Font.FontSize = style.TextStyle.FontSizePt.Value;
            if (style.TextStyle.Bold)
                cell.Style.Font.Bold = true;
            if (style.TextStyle.Italic)
                cell.Style.Font.Italic = true;
            if (style.TextStyle.Underline)
                cell.Style.Font.Underline = XLFontUnderlineValues.Single;
            if (style.TextStyle.Strikethrough)
                cell.Style.Font.Strikethrough = true;
            if (style.TextStyle.Color is not null)
                cell.Style.Font.FontColor = XLColor.FromHtml(style.TextStyle.Color);
        }

        if (style.BackgroundColor is not null)
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(style.BackgroundColor);

        cell.Style.Alignment.Horizontal = style.HorizontalAlignment switch
        {
            TextAlignment.Center => XLAlignmentHorizontalValues.Center,
            TextAlignment.Right => XLAlignmentHorizontalValues.Right,
            TextAlignment.Justify => XLAlignmentHorizontalValues.Justify,
            _ => XLAlignmentHorizontalValues.Left
        };

        cell.Style.Alignment.Vertical = style.VerticalAlignment switch
        {
            VerticalAlignment.Middle => XLAlignmentVerticalValues.Center,
            VerticalAlignment.Bottom => XLAlignmentVerticalValues.Bottom,
            _ => XLAlignmentVerticalValues.Top
        };

        if (style.NumberFormat is not null)
            cell.Style.NumberFormat.Format = style.NumberFormat;

        if (style.WrapText)
            cell.Style.Alignment.WrapText = true;

        if (style.Borders is not null)
            ApplyBorders(cell, style.Borders);
    }

    private static void ApplyBorders(IXLCell cell, BorderSet borders)
    {
        if (borders.Top is not null)
        {
            cell.Style.Border.TopBorder = MapBorderStyle(borders.Top.Style);
            if (borders.Top.Color is not null)
                cell.Style.Border.TopBorderColor = XLColor.FromHtml(borders.Top.Color);
        }

        if (borders.Right is not null)
        {
            cell.Style.Border.RightBorder = MapBorderStyle(borders.Right.Style);
            if (borders.Right.Color is not null)
                cell.Style.Border.RightBorderColor = XLColor.FromHtml(borders.Right.Color);
        }

        if (borders.Bottom is not null)
        {
            cell.Style.Border.BottomBorder = MapBorderStyle(borders.Bottom.Style);
            if (borders.Bottom.Color is not null)
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml(borders.Bottom.Color);
        }

        if (borders.Left is not null)
        {
            cell.Style.Border.LeftBorder = MapBorderStyle(borders.Left.Style);
            if (borders.Left.Color is not null)
                cell.Style.Border.LeftBorderColor = XLColor.FromHtml(borders.Left.Color);
        }
    }

    private static XLBorderStyleValues MapBorderStyle(BorderStyle style)
    {
        return style switch
        {
            BorderStyle.Thin => XLBorderStyleValues.Thin,
            BorderStyle.Medium => XLBorderStyleValues.Medium,
            BorderStyle.Thick => XLBorderStyleValues.Thick,
            BorderStyle.Double => XLBorderStyleValues.Double,
            BorderStyle.Dashed => XLBorderStyleValues.Dashed,
            BorderStyle.Dotted => XLBorderStyleValues.Dotted,
            _ => XLBorderStyleValues.None
        };
    }

    private static int MmToPixels(double millimeters)
    {
        const double dpi = 96d;
        return (int)Math.Round((millimeters / 25.4d) * dpi);
    }
}
