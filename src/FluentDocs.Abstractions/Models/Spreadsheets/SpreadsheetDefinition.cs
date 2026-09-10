using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Charts;

namespace FluentDocs.Abstractions.Models.Spreadsheets;

/// <summary>
/// Complete definition of a spreadsheet to generate (XLSX, CSV, TSV).
/// </summary>
public sealed record SpreadsheetDefinition : FileDefinition
{
    public SpreadsheetMetadata Metadata { get; init; } = new();
    public IReadOnlyList<SheetDefinition> Sheets { get; init; } = [];
    public CellStyle? DefaultCellStyle { get; init; }
}

/// <summary>
/// Spreadsheet-level metadata.
/// </summary>
public sealed record SpreadsheetMetadata
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// A single sheet (worksheet) within a spreadsheet.
/// </summary>
public sealed record SheetDefinition
{
    public string Name { get; init; } = "Sheet1";
    public IReadOnlyList<ColumnDefinition> Columns { get; init; } = [];
    public IReadOnlyList<RowDefinition> Rows { get; init; } = [];
    public IReadOnlyList<ChartDefinition> Charts { get; init; } = [];
    public IReadOnlyList<ImageContent> Images { get; init; } = [];
    public bool FreezeFirstRow { get; init; }
    public bool FreezeFirstColumn { get; init; }
    public bool AutoFilter { get; init; }
    public IReadOnlyList<MergedCellRange> MergedCells { get; init; } = [];
    public IReadOnlyList<ConditionalFormat> ConditionalFormats { get; init; } = [];
}

/// <summary>
/// Column definition in a spreadsheet sheet.
/// </summary>
public sealed record ColumnDefinition
{
    public string? Header { get; init; }
    public double? WidthMm { get; init; }
    public bool AutoWidth { get; init; } = true;
    public CellStyle? Style { get; init; }
}

/// <summary>
/// A row of cells in a spreadsheet.
/// </summary>
public sealed record RowDefinition
{
    public IReadOnlyList<CellValue> Cells { get; init; } = [];
    public double? HeightMm { get; init; }
    public CellStyle? Style { get; init; }
}

/// <summary>
/// A single cell value in a spreadsheet.
/// </summary>
public sealed record CellValue
{
    /// <summary>Cell value — string, number, DateTime, bool, or null.</summary>
    public object? Value { get; init; }

    public CellStyle? Style { get; init; }
    public string? Formula { get; init; }
    public string? Hyperlink { get; init; }
}

/// <summary>
/// Cell-level styling (font, fill, borders, alignment, number format).
/// </summary>
public sealed record CellStyle
{
    public TextStyle? TextStyle { get; init; }
    public string? BackgroundColor { get; init; }
    public BorderSet? Borders { get; init; }
    public TextAlignment HorizontalAlignment { get; init; } = TextAlignment.Left;
    public VerticalAlignment VerticalAlignment { get; init; } = VerticalAlignment.Top;
    public string? NumberFormat { get; init; }
    public bool WrapText { get; init; }
}

/// <summary>
/// Defines a merged cell range (e.g. "A1:C3").
/// </summary>
public sealed record MergedCellRange
{
    public required int StartRow { get; init; }
    public required int StartColumn { get; init; }
    public required int EndRow { get; init; }
    public required int EndColumn { get; init; }
}

/// <summary>
/// Conditional formatting rule for a cell range.
/// </summary>
public sealed record ConditionalFormat
{
    public required int StartRow { get; init; }
    public required int StartColumn { get; init; }
    public required int EndRow { get; init; }
    public required int EndColumn { get; init; }
    public required string Condition { get; init; }
    public required CellStyle Style { get; init; }
}
