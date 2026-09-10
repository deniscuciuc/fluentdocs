using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Spreadsheets;

namespace FluentDocs.Abstractions.Builders.Impl;

internal sealed class SpreadsheetBuilder : ISpreadsheetBuilder
{
    private string? _fileName;
    private GeneratedFileFormat _format = GeneratedFileFormat.Xlsx;
    private SpreadsheetMetadata _metadata = new();
    private CellStyle? _defaultCellStyle;
    private readonly List<SheetDefinition> _sheets = [];

    public ISpreadsheetBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public ISpreadsheetBuilder ForFormat(GeneratedFileFormat format)
    {
        _format = format;
        return this;
    }

    public ISpreadsheetBuilder WithMetadata(Action<ISpreadsheetMetadataBuilder> configure)
    {
        var builder = new SpreadsheetMetadataBuilder();
        configure(builder);
        _metadata = builder.Build();
        return this;
    }

    public ISpreadsheetBuilder WithDefaultCellStyle(Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _defaultCellStyle = builder.Build();
        return this;
    }

    public ISpreadsheetBuilder AddSheet(Action<ISheetBuilder> configure)
    {
        var builder = new SheetBuilder();
        configure(builder);
        _sheets.Add(builder.Build());
        return this;
    }

    public ISpreadsheetBuilder AddSheet(string name, Action<ISheetBuilder> configure)
    {
        var builder = new SheetBuilder();
        builder.WithName(name);
        configure(builder);
        _sheets.Add(builder.Build());
        return this;
    }

    public SpreadsheetDefinition Build()
    {
        return new SpreadsheetDefinition
        {
            FileName = _fileName,
            Format = _format,
            Metadata = _metadata,
            DefaultCellStyle = _defaultCellStyle,
            Sheets = _sheets
        };
    }
}

internal sealed class SpreadsheetMetadataBuilder : ISpreadsheetMetadataBuilder
{
    private string? _title;
    private string? _author;
    private string? _subject;
    private string? _description;
    private DateTimeOffset? _createdAt;

    public ISpreadsheetMetadataBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    public ISpreadsheetMetadataBuilder Author(string author)
    {
        _author = author;
        return this;
    }

    public ISpreadsheetMetadataBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    public ISpreadsheetMetadataBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public ISpreadsheetMetadataBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    internal SpreadsheetMetadata Build()
    {
        return new SpreadsheetMetadata
        {
            Title = _title,
            Author = _author,
            Subject = _subject,
            Description = _description,
            CreatedAt = _createdAt
        };
    }
}

internal sealed class SheetBuilder : ISheetBuilder
{
    private string _name = "Sheet1";
    private readonly List<ColumnDefinition> _columns = [];
    private readonly List<RowDefinition> _rows = [];
    private readonly List<ChartDefinition> _charts = [];
    private readonly List<ImageContent> _images = [];
    private bool _freezeFirstRow;
    private bool _freezeFirstColumn;
    private bool _autoFilter;
    private readonly List<MergedCellRange> _mergedCells = [];
    private readonly List<ConditionalFormat> _conditionalFormats = [];

    public ISheetBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ISheetBuilder AddColumn(string? header, double? widthMm, bool autoWidth)
    {
        _columns.Add(new ColumnDefinition { Header = header, WidthMm = widthMm, AutoWidth = autoWidth });
        return this;
    }

    public ISheetBuilder AddColumn(Action<IColumnBuilder> configure)
    {
        var builder = new ColumnBuilder();
        configure(builder);
        _columns.Add(builder.Build());
        return this;
    }

    public ISheetBuilder AddRow(Action<IRowBuilder> configure)
    {
        var builder = new RowBuilder();
        configure(builder);
        _rows.Add(builder.Build());
        return this;
    }

    public ISheetBuilder AddChart(Action<IChartBuilder> configure)
    {
        var builder = new ChartBuilder();
        configure(builder);
        _charts.Add(builder.Build());
        return this;
    }

    public ISheetBuilder AddImage(Action<IImageBuilder> configure)
    {
        var builder = new ImageBuilder();
        configure(builder);
        _images.Add(builder.Build());
        return this;
    }

    public ISheetBuilder FreezeTopRow()
    {
        _freezeFirstRow = true;
        return this;
    }

    public ISheetBuilder FreezeFirstColumn()
    {
        _freezeFirstColumn = true;
        return this;
    }

    public ISheetBuilder EnableAutoFilter()
    {
        _autoFilter = true;
        return this;
    }

    public ISheetBuilder AddMergedCells(int startRow, int startCol, int endRow, int endCol)
    {
        _mergedCells.Add(new MergedCellRange
        { StartRow = startRow, StartColumn = startCol, EndRow = endRow, EndColumn = endCol });
        return this;
    }

    public ISheetBuilder AddConditionalFormat(Action<IConditionalFormatBuilder> configure)
    {
        var builder = new ConditionalFormatBuilder();
        configure(builder);
        _conditionalFormats.Add(builder.Build());
        return this;
    }

    internal SheetDefinition Build()
    {
        return new SheetDefinition
        {
            Name = _name,
            Columns = _columns,
            Rows = _rows,
            Charts = _charts,
            Images = _images,
            FreezeFirstRow = _freezeFirstRow,
            FreezeFirstColumn = _freezeFirstColumn,
            AutoFilter = _autoFilter,
            MergedCells = _mergedCells,
            ConditionalFormats = _conditionalFormats
        };
    }
}

internal sealed class ColumnBuilder : IColumnBuilder
{
    private string? _header;
    private double? _widthMm;
    private bool _autoWidth = true;
    private CellStyle? _style;

    public IColumnBuilder WithHeader(string header)
    {
        _header = header;
        return this;
    }

    public IColumnBuilder WithWidth(double widthMm)
    {
        _widthMm = widthMm;
        return this;
    }

    public IColumnBuilder AutoWidth(bool enabled)
    {
        _autoWidth = enabled;
        return this;
    }

    public IColumnBuilder WithStyle(Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _style = builder.Build();
        return this;
    }

    internal ColumnDefinition Build()
    {
        return new ColumnDefinition
        {
            Header = _header,
            WidthMm = _widthMm,
            AutoWidth = _autoWidth,
            Style = _style
        };
    }
}

internal sealed class RowBuilder : IRowBuilder
{
    private readonly List<CellValue> _cells = [];
    private double? _heightMm;
    private CellStyle? _style;

    public IRowBuilder AddCell(object? value)
    {
        _cells.Add(new CellValue { Value = value });
        return this;
    }

    public IRowBuilder AddCell(object? value, Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _cells.Add(new CellValue { Value = value, Style = builder.Build() });
        return this;
    }

    public IRowBuilder AddFormulaCell(string formula)
    {
        _cells.Add(new CellValue { Formula = formula });
        return this;
    }

    public IRowBuilder AddFormulaCell(string formula, Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _cells.Add(new CellValue { Formula = formula, Style = builder.Build() });
        return this;
    }

    public IRowBuilder AddHyperlinkCell(string text, string url)
    {
        _cells.Add(new CellValue { Value = text, Hyperlink = url });
        return this;
    }

    public IRowBuilder WithHeight(double heightMm)
    {
        _heightMm = heightMm;
        return this;
    }

    public IRowBuilder WithStyle(Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _style = builder.Build();
        return this;
    }

    internal RowDefinition Build()
    {
        return new RowDefinition
        {
            Cells = _cells,
            HeightMm = _heightMm,
            Style = _style
        };
    }
}

internal sealed class CellStyleBuilder : ICellStyleBuilder
{
    private TextStyle? _textStyle;
    private string? _bgColor;
    private BorderSet? _borders;
    private TextAlignment _hAlign = TextAlignment.Left;
    private VerticalAlignment _vAlign = Enums.VerticalAlignment.Top;
    private string? _numberFormat;
    private bool _wrapText;

    public ICellStyleBuilder WithTextStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _textStyle = builder.Build();
        return this;
    }

    public ICellStyleBuilder WithBackgroundColor(string hex)
    {
        _bgColor = hex;
        return this;
    }

    public ICellStyleBuilder WithBorders(Action<IBorderSetBuilder> configure)
    {
        var builder = new BorderSetBuilder();
        configure(builder);
        _borders = builder.Build();
        return this;
    }

    public ICellStyleBuilder HorizontalAlignment(TextAlignment alignment)
    {
        _hAlign = alignment;
        return this;
    }

    public ICellStyleBuilder VerticalAlignment(VerticalAlignment alignment)
    {
        _vAlign = alignment;
        return this;
    }

    public ICellStyleBuilder NumberFormat(string format)
    {
        _numberFormat = format;
        return this;
    }

    public ICellStyleBuilder WrapText(bool wrap)
    {
        _wrapText = wrap;
        return this;
    }

    internal CellStyle Build()
    {
        return new CellStyle
        {
            TextStyle = _textStyle,
            BackgroundColor = _bgColor,
            Borders = _borders,
            HorizontalAlignment = _hAlign,
            VerticalAlignment = _vAlign,
            NumberFormat = _numberFormat,
            WrapText = _wrapText
        };
    }
}

internal sealed class ConditionalFormatBuilder : IConditionalFormatBuilder
{
    private int _startRow;
    private int _startCol;
    private int _endRow;
    private int _endCol;
    private string _condition = string.Empty;
    private CellStyle _style = new();

    public IConditionalFormatBuilder ForRange(int startRow, int startCol, int endRow, int endCol)
    {
        _startRow = startRow;
        _startCol = startCol;
        _endRow = endRow;
        _endCol = endCol;
        return this;
    }

    public IConditionalFormatBuilder WithCondition(string condition)
    {
        _condition = condition;
        return this;
    }

    public IConditionalFormatBuilder WithStyle(Action<ICellStyleBuilder> configure)
    {
        var builder = new CellStyleBuilder();
        configure(builder);
        _style = builder.Build();
        return this;
    }

    internal ConditionalFormat Build()
    {
        return new ConditionalFormat
        {
            StartRow = _startRow,
            StartColumn = _startCol,
            EndRow = _endRow,
            EndColumn = _endCol,
            Condition = _condition,
            Style = _style
        };
    }
}
