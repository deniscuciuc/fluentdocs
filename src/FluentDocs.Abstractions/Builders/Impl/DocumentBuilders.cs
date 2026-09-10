using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Documents;

namespace FluentDocs.Abstractions.Builders.Impl;

internal sealed class DocumentBuilder : IDocumentBuilder
{
    private string? _fileName;
    private GeneratedFileFormat _format = GeneratedFileFormat.Docx;
    private DocumentMetadata _metadata = new();
    private PageSetup _pageSetup = new();
    private DocumentStyles _defaultStyles = new();
    private HeaderFooterDefinition? _header;
    private HeaderFooterDefinition? _footer;
    private readonly List<DocumentSection> _sections = [];

    public IDocumentBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public IDocumentBuilder ForFormat(GeneratedFileFormat format)
    {
        _format = format;
        return this;
    }

    public IDocumentBuilder WithMetadata(Action<IDocumentMetadataBuilder> configure)
    {
        var builder = new DocumentMetadataBuilder();
        configure(builder);
        _metadata = builder.Build();
        return this;
    }

    public IDocumentBuilder WithPageSetup(Action<IPageSetupBuilder> configure)
    {
        var builder = new PageSetupBuilder();
        configure(builder);
        _pageSetup = builder.Build();
        return this;
    }

    public IDocumentBuilder WithDefaultStyles(Action<IDocumentStylesBuilder> configure)
    {
        var builder = new DocumentStylesBuilder();
        configure(builder);
        _defaultStyles = builder.Build();
        return this;
    }

    public IDocumentBuilder WithHeader(Action<IHeaderFooterBuilder> configure)
    {
        var builder = new HeaderFooterBuilder();
        configure(builder);
        _header = builder.Build();
        return this;
    }

    public IDocumentBuilder WithFooter(Action<IHeaderFooterBuilder> configure)
    {
        var builder = new HeaderFooterBuilder();
        configure(builder);
        _footer = builder.Build();
        return this;
    }

    public IDocumentBuilder AddSection(Action<ISectionBuilder> configure)
    {
        var builder = new SectionBuilder();
        configure(builder);
        _sections.Add(builder.Build());
        return this;
    }

    public DocumentDefinition Build()
    {
        return new DocumentDefinition
        {
            FileName = _fileName,
            Format = _format,
            Metadata = _metadata,
            PageSetup = _pageSetup,
            DefaultStyles = _defaultStyles,
            Header = _header,
            Footer = _footer,
            Sections = _sections
        };
    }
}

internal sealed class DocumentMetadataBuilder : IDocumentMetadataBuilder
{
    private string? _title;
    private string? _author;
    private string? _subject;
    private string? _description;
    private readonly List<string> _keywords = [];
    private string? _language;
    private DateTimeOffset? _createdAt;

    public IDocumentMetadataBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    public IDocumentMetadataBuilder Author(string author)
    {
        _author = author;
        return this;
    }

    public IDocumentMetadataBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    public IDocumentMetadataBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public IDocumentMetadataBuilder AddKeyword(string keyword)
    {
        _keywords.Add(keyword);
        return this;
    }

    public IDocumentMetadataBuilder Language(string language)
    {
        _language = language;
        return this;
    }

    public IDocumentMetadataBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    internal DocumentMetadata Build()
    {
        return new DocumentMetadata
        {
            Title = _title,
            Author = _author,
            Subject = _subject,
            Description = _description,
            Keywords = _keywords,
            Language = _language,
            CreatedAt = _createdAt
        };
    }
}

internal sealed class PageSetupBuilder : IPageSetupBuilder
{
    private PageSize _pageSize = PageSize.A4;
    private PageOrientation _orientation = PageOrientation.Portrait;
    private Margins _margins = Margins.Default;
    private double? _customWidth;
    private double? _customHeight;

    public IPageSetupBuilder WithPageSize(PageSize pageSize)
    {
        _pageSize = pageSize;
        return this;
    }

    public IPageSetupBuilder WithOrientation(PageOrientation orientation)
    {
        _orientation = orientation;
        return this;
    }

    public IPageSetupBuilder WithMargins(Margins margins)
    {
        _margins = margins;
        return this;
    }

    public IPageSetupBuilder WithMargins(double topMm, double rightMm, double bottomMm, double leftMm)
    {
        _margins = new Margins { TopMm = topMm, RightMm = rightMm, BottomMm = bottomMm, LeftMm = leftMm };
        return this;
    }

    public IPageSetupBuilder WithCustomSize(double widthMm, double heightMm)
    {
        _customWidth = widthMm;
        _customHeight = heightMm;
        _pageSize = PageSize.Custom;
        return this;
    }

    internal PageSetup Build()
    {
        return new PageSetup
        {
            PageSize = _pageSize,
            Orientation = _orientation,
            Margins = _margins,
            CustomWidthMm = _customWidth,
            CustomHeightMm = _customHeight
        };
    }
}

internal sealed class DocumentStylesBuilder : IDocumentStylesBuilder
{
    private TextStyle? _defaultTextStyle;
    private ParagraphStyle? _defaultParagraphStyle;
    private readonly Dictionary<HeadingLevel, TextStyle> _headingStyles = [];

    public IDocumentStylesBuilder WithDefaultTextStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _defaultTextStyle = builder.Build();
        return this;
    }

    public IDocumentStylesBuilder WithDefaultParagraphStyle(Action<IParagraphStyleBuilder> configure)
    {
        var builder = new ParagraphStyleBuilder();
        configure(builder);
        _defaultParagraphStyle = builder.Build();
        return this;
    }

    public IDocumentStylesBuilder WithHeadingStyle(HeadingLevel level, Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _headingStyles[level] = builder.Build();
        return this;
    }

    internal DocumentStyles Build()
    {
        return new DocumentStyles
        {
            DefaultTextStyle = _defaultTextStyle,
            DefaultParagraphStyle = _defaultParagraphStyle,
            HeadingStyles = _headingStyles
        };
    }
}

internal sealed class HeaderFooterBuilder : IHeaderFooterBuilder
{
    private readonly List<TextRun> _content = [];
    private bool _showPageNumber;
    private string? _pageNumberFormat;

    public IHeaderFooterBuilder AddText(string text)
    {
        _content.Add(new TextRun { Text = text });
        return this;
    }

    public IHeaderFooterBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _content.Add(new TextRun { Text = text, Style = builder.Build() });
        return this;
    }

    public IHeaderFooterBuilder ShowPageNumber(string? format)
    {
        _showPageNumber = true;
        _pageNumberFormat = format;
        return this;
    }

    internal HeaderFooterDefinition Build()
    {
        return new HeaderFooterDefinition
        {
            Content = _content,
            ShowPageNumber = _showPageNumber,
            PageNumberFormat = _pageNumberFormat
        };
    }
}

internal sealed class SectionBuilder : ISectionBuilder
{
    private readonly List<IDocumentElement> _elements = [];

    public ISectionBuilder AddHeading(string text, HeadingLevel level)
    {
        _elements.Add(new HeadingElement { Text = text, Level = level });
        return this;
    }

    public ISectionBuilder AddHeading(string text, HeadingLevel level, Action<ITextStyleBuilder> configureStyle)
    {
        var builder = new TextStyleBuilder();
        configureStyle(builder);
        _elements.Add(new HeadingElement { Text = text, Level = level, Style = builder.Build() });
        return this;
    }

    public ISectionBuilder AddParagraph(string text)
    {
        _elements.Add(new ParagraphElement { Runs = [new TextRun { Text = text }] });
        return this;
    }

    public ISectionBuilder AddParagraph(Action<IParagraphBuilder> configure)
    {
        var builder = new ParagraphBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISectionBuilder AddImage(Action<IImageBuilder> configure)
    {
        var builder = new ImageBuilder();
        configure(builder);
        _elements.Add(new ImageElement { Image = builder.Build() });
        return this;
    }

    public ISectionBuilder AddTable(Action<ITableContentBuilder> configure)
    {
        var builder = new TableContentBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISectionBuilder AddChart(Action<IChartBuilder> configure)
    {
        var builder = new ChartBuilder();
        configure(builder);
        _elements.Add(new ChartElement { Chart = builder.Build() });
        return this;
    }

    public ISectionBuilder AddPageBreak()
    {
        _elements.Add(new PageBreakElement());
        return this;
    }

    public ISectionBuilder AddHorizontalRule()
    {
        _elements.Add(new HorizontalRuleElement());
        return this;
    }

    public ISectionBuilder AddList(bool ordered, Action<IListBuilder> configure)
    {
        var builder = new ListBuilder();
        configure(builder);
        _elements.Add(new ListElement { Ordered = ordered, Items = builder.Build() });
        return this;
    }

    public ISectionBuilder AddCodeBlock(string code, string? language)
    {
        _elements.Add(new CodeBlockElement { Code = code, Language = language });
        return this;
    }

    public ISectionBuilder AddBlockQuote(string text)
    {
        _elements.Add(new BlockQuoteElement { Content = [new TextRun { Text = text }] });
        return this;
    }

    public ISectionBuilder AddBlockQuote(Action<IParagraphBuilder> configure)
    {
        var builder = new ParagraphBuilder();
        configure(builder);
        var paragraph = builder.Build();
        _elements.Add(new BlockQuoteElement { Content = paragraph.Runs });
        return this;
    }

    internal DocumentSection Build()
    {
        return new DocumentSection { Elements = _elements };
    }
}

internal sealed class ParagraphBuilder : IParagraphBuilder
{
    private readonly List<TextRun> _runs = [];
    private ParagraphStyle? _style;

    public IParagraphBuilder AddText(string text)
    {
        _runs.Add(new TextRun { Text = text });
        return this;
    }

    public IParagraphBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _runs.Add(new TextRun { Text = text, Style = builder.Build() });
        return this;
    }

    public IParagraphBuilder AddLineBreak()
    {
        _runs.Add(new TextRun { Text = "\n" });
        return this;
    }

    public IParagraphBuilder WithStyle(Action<IParagraphStyleBuilder> configure)
    {
        var builder = new ParagraphStyleBuilder();
        configure(builder);
        _style = builder.Build();
        return this;
    }

    internal ParagraphElement Build()
    {
        return new ParagraphElement { Runs = _runs, Style = _style };
    }
}

internal sealed class ListBuilder : IListBuilder
{
    private readonly List<ListItem> _items = [];

    public IListBuilder AddItem(string text)
    {
        _items.Add(new ListItem { Content = [new TextRun { Text = text }] });
        return this;
    }

    public IListBuilder AddItem(Action<IParagraphBuilder> configure)
    {
        var builder = new ParagraphBuilder();
        configure(builder);
        var paragraph = builder.Build();
        _items.Add(new ListItem { Content = paragraph.Runs });
        return this;
    }

    public IListBuilder AddItem(string text, Action<IListBuilder> configureNested)
    {
        var nestedBuilder = new ListBuilder();
        configureNested(nestedBuilder);
        _items.Add(new ListItem
        {
            Content = [new TextRun { Text = text }],
            Nested = nestedBuilder.Build()
        });
        return this;
    }

    internal IReadOnlyList<ListItem> Build()
    {
        return _items;
    }
}

internal sealed class TableContentBuilder : ITableContentBuilder
{
    private readonly List<TableColumn> _columns = [];
    private readonly List<TableRow> _rows = [];
    private BorderSet? _borders;
    private TextStyle? _headerStyle;
    private TextStyle? _cellStyle;

    public ITableContentBuilder AddColumn(string? header, double? widthMm, TextAlignment alignment)
    {
        _columns.Add(new TableColumn { Header = header, WidthMm = widthMm, Alignment = alignment });
        return this;
    }

    public ITableContentBuilder AddRow(Action<ITableRowBuilder> configure)
    {
        var builder = new TableRowBuilder();
        configure(builder);
        _rows.Add(builder.Build());
        return this;
    }

    public ITableContentBuilder WithBorders(Action<IBorderSetBuilder> configure)
    {
        var builder = new BorderSetBuilder();
        configure(builder);
        _borders = builder.Build();
        return this;
    }

    public ITableContentBuilder WithHeaderStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _headerStyle = builder.Build();
        return this;
    }

    public ITableContentBuilder WithCellStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _cellStyle = builder.Build();
        return this;
    }

    internal TableContentElement Build()
    {
        return new TableContentElement
        {
            Columns = _columns,
            Rows = _rows,
            Borders = _borders,
            HeaderStyle = _headerStyle,
            CellStyle = _cellStyle
        };
    }
}

internal sealed class TableRowBuilder : ITableRowBuilder
{
    private readonly List<TableCell> _cells = [];

    public ITableRowBuilder AddCell(string? text)
    {
        _cells.Add(new TableCell { Text = text });
        return this;
    }

    public ITableRowBuilder AddCell(Action<IParagraphBuilder> configure)
    {
        var builder = new ParagraphBuilder();
        configure(builder);
        var paragraph = builder.Build();
        _cells.Add(new TableCell { Runs = paragraph.Runs });
        return this;
    }

    public ITableRowBuilder AddCell(string? text, Action<ITextStyleBuilder> configureStyle)
    {
        var styleBuilder = new TextStyleBuilder();
        configureStyle(styleBuilder);
        _cells.Add(new TableCell { Text = text, Style = styleBuilder.Build() });
        return this;
    }

    internal TableRow Build()
    {
        return new TableRow { Cells = _cells };
    }
}
