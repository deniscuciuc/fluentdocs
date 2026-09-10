using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Spreadsheets;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="SpreadsheetDefinition"/>.
/// </summary>
public interface ISpreadsheetBuilder
{
    ISpreadsheetBuilder WithFileName(string fileName);
    ISpreadsheetBuilder ForFormat(GeneratedFileFormat format);
    ISpreadsheetBuilder WithMetadata(Action<ISpreadsheetMetadataBuilder> configure);
    ISpreadsheetBuilder WithDefaultCellStyle(Action<ICellStyleBuilder> configure);
    ISpreadsheetBuilder AddSheet(Action<ISheetBuilder> configure);
    ISpreadsheetBuilder AddSheet(string name, Action<ISheetBuilder> configure);
    SpreadsheetDefinition Build();
}

/// <summary>
/// Builder for spreadsheet metadata.
/// </summary>
public interface ISpreadsheetMetadataBuilder
{
    ISpreadsheetMetadataBuilder Title(string title);
    ISpreadsheetMetadataBuilder Author(string author);
    ISpreadsheetMetadataBuilder Subject(string subject);
    ISpreadsheetMetadataBuilder Description(string description);
    ISpreadsheetMetadataBuilder CreatedAt(DateTimeOffset createdAt);
}

/// <summary>
/// Builder for a single worksheet.
/// </summary>
public interface ISheetBuilder
{
    ISheetBuilder WithName(string name);
    ISheetBuilder AddColumn(string? header = null, double? widthMm = null, bool autoWidth = true);
    ISheetBuilder AddColumn(Action<IColumnBuilder> configure);
    ISheetBuilder AddRow(Action<IRowBuilder> configure);
    ISheetBuilder AddChart(Action<IChartBuilder> configure);
    ISheetBuilder AddImage(Action<IImageBuilder> configure);
    ISheetBuilder FreezeTopRow();
    ISheetBuilder FreezeFirstColumn();
    ISheetBuilder EnableAutoFilter();
    ISheetBuilder AddMergedCells(int startRow, int startCol, int endRow, int endCol);
    ISheetBuilder AddConditionalFormat(Action<IConditionalFormatBuilder> configure);
}

/// <summary>
/// Builder for a spreadsheet column definition.
/// </summary>
public interface IColumnBuilder
{
    IColumnBuilder WithHeader(string header);
    IColumnBuilder WithWidth(double widthMm);
    IColumnBuilder AutoWidth(bool enabled = true);
    IColumnBuilder WithStyle(Action<ICellStyleBuilder> configure);
}

/// <summary>
/// Builder for a spreadsheet row.
/// </summary>
public interface IRowBuilder
{
    IRowBuilder AddCell(object? value);
    IRowBuilder AddCell(object? value, Action<ICellStyleBuilder> configure);
    IRowBuilder AddFormulaCell(string formula);
    IRowBuilder AddFormulaCell(string formula, Action<ICellStyleBuilder> configure);
    IRowBuilder AddHyperlinkCell(string text, string url);
    IRowBuilder WithHeight(double heightMm);
    IRowBuilder WithStyle(Action<ICellStyleBuilder> configure);
}

/// <summary>
/// Builder for cell-level styling.
/// </summary>
public interface ICellStyleBuilder
{
    ICellStyleBuilder WithTextStyle(Action<ITextStyleBuilder> configure);
    ICellStyleBuilder WithBackgroundColor(string hex);
    ICellStyleBuilder WithBorders(Action<IBorderSetBuilder> configure);
    ICellStyleBuilder HorizontalAlignment(TextAlignment alignment);
    ICellStyleBuilder VerticalAlignment(VerticalAlignment alignment);
    ICellStyleBuilder NumberFormat(string format);
    ICellStyleBuilder WrapText(bool wrap = true);
}

/// <summary>
/// Builder for conditional formatting rules.
/// </summary>
public interface IConditionalFormatBuilder
{
    IConditionalFormatBuilder ForRange(int startRow, int startCol, int endRow, int endCol);
    IConditionalFormatBuilder WithCondition(string condition);
    IConditionalFormatBuilder WithStyle(Action<ICellStyleBuilder> configure);
}
