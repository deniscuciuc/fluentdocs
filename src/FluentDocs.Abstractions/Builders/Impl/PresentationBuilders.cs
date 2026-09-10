using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Charts;
using FluentDocs.Abstractions.Models.Documents;
using FluentDocs.Abstractions.Models.Presentations;

namespace FluentDocs.Abstractions.Builders.Impl;

internal sealed class PresentationBuilder : IPresentationBuilder
{
    private string? _fileName;
    private PresentationMetadata _metadata = new();
    private SlideSize _slideSize = SlideSize.Widescreen;
    private ThemeDefinition? _theme;
    private readonly List<SlideDefinition> _slides = [];

    public IPresentationBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public IPresentationBuilder WithMetadata(Action<IPresentationMetadataBuilder> configure)
    {
        var builder = new PresentationMetadataBuilder();
        configure(builder);
        _metadata = builder.Build();
        return this;
    }

    public IPresentationBuilder WithSlideSize(SlideSize size)
    {
        _slideSize = size;
        return this;
    }

    public IPresentationBuilder WithTheme(Action<IThemeBuilder> configure)
    {
        var builder = new ThemeBuilder();
        configure(builder);
        _theme = builder.Build();
        return this;
    }

    public IPresentationBuilder AddSlide(Action<ISlideBuilder> configure)
    {
        var builder = new SlideBuilder();
        configure(builder);
        _slides.Add(builder.Build());
        return this;
    }

    public PresentationDefinition Build()
    {
        return new PresentationDefinition
        {
            FileName = _fileName,
            Format = GeneratedFileFormat.Pptx,
            Metadata = _metadata,
            SlideSize = _slideSize,
            Theme = _theme,
            Slides = _slides
        };
    }
}

internal sealed class PresentationMetadataBuilder : IPresentationMetadataBuilder
{
    private string? _title;
    private string? _author;
    private string? _subject;
    private string? _description;
    private DateTimeOffset? _createdAt;

    public IPresentationMetadataBuilder Title(string title)
    {
        _title = title;
        return this;
    }

    public IPresentationMetadataBuilder Author(string author)
    {
        _author = author;
        return this;
    }

    public IPresentationMetadataBuilder Subject(string subject)
    {
        _subject = subject;
        return this;
    }

    public IPresentationMetadataBuilder Description(string description)
    {
        _description = description;
        return this;
    }

    public IPresentationMetadataBuilder CreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    internal PresentationMetadata Build()
    {
        return new PresentationMetadata
        {
            Title = _title,
            Author = _author,
            Subject = _subject,
            Description = _description,
            CreatedAt = _createdAt
        };
    }
}

internal sealed class ThemeBuilder : IThemeBuilder
{
    private string? _primaryColor;
    private string? _secondaryColor;
    private readonly List<string> _accentColors = [];
    private string? _defaultFont;
    private string? _headingFont;
    private string? _backgroundColor;

    public IThemeBuilder PrimaryColor(string hex)
    {
        _primaryColor = hex;
        return this;
    }

    public IThemeBuilder SecondaryColor(string hex)
    {
        _secondaryColor = hex;
        return this;
    }

    public IThemeBuilder AddAccentColor(string hex)
    {
        _accentColors.Add(hex);
        return this;
    }

    public IThemeBuilder DefaultFont(string fontFamily)
    {
        _defaultFont = fontFamily;
        return this;
    }

    public IThemeBuilder HeadingFont(string fontFamily)
    {
        _headingFont = fontFamily;
        return this;
    }

    public IThemeBuilder BackgroundColor(string hex)
    {
        _backgroundColor = hex;
        return this;
    }

    internal ThemeDefinition Build()
    {
        return new ThemeDefinition
        {
            PrimaryColor = _primaryColor,
            SecondaryColor = _secondaryColor,
            AccentColors = _accentColors,
            DefaultFont = _defaultFont,
            HeadingFont = _headingFont,
            BackgroundColor = _backgroundColor
        };
    }
}

internal sealed class SlideBuilder : ISlideBuilder
{
    private SlideLayout _layout = SlideLayout.Blank;
    private string? _notes;
    private SlideTransition? _transition;
    private string? _backgroundColor;
    private ImageContent? _backgroundImage;
    private readonly List<ISlideElement> _elements = [];

    public ISlideBuilder WithLayout(SlideLayout layout)
    {
        _layout = layout;
        return this;
    }

    public ISlideBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public ISlideBuilder WithTransition(Action<ITransitionBuilder> configure)
    {
        var builder = new TransitionBuilder();
        configure(builder);
        _transition = builder.Build();
        return this;
    }

    public ISlideBuilder WithBackground(string colorHex)
    {
        _backgroundColor = colorHex;
        return this;
    }

    public ISlideBuilder WithBackgroundImage(Action<IImageBuilder> configure)
    {
        var builder = new ImageBuilder();
        configure(builder);
        _backgroundImage = builder.Build();
        return this;
    }

    public ISlideBuilder AddTextBox(Action<ITextBoxBuilder> configure)
    {
        var builder = new TextBoxBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISlideBuilder AddImage(Action<ISlideImageBuilder> configure)
    {
        var builder = new SlideImageBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISlideBuilder AddChart(Action<ISlideChartBuilder> configure)
    {
        var builder = new SlideChartBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISlideBuilder AddTable(Action<ISlideTableBuilder> configure)
    {
        var builder = new SlideTableBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    public ISlideBuilder AddShape(Action<IShapeBuilder> configure)
    {
        var builder = new ShapeBuilder();
        configure(builder);
        _elements.Add(builder.Build());
        return this;
    }

    internal SlideDefinition Build()
    {
        return new SlideDefinition
        {
            Layout = _layout,
            Notes = _notes,
            Elements = _elements,
            Transition = _transition,
            BackgroundColor = _backgroundColor,
            BackgroundImage = _backgroundImage
        };
    }
}

internal sealed class TransitionBuilder : ITransitionBuilder
{
    private TransitionType _type = TransitionType.None;
    private int _durationMs = 500;
    private bool _advanceOnClick = true;
    private int? _advanceAfterMs;

    public ITransitionBuilder OfType(TransitionType type)
    {
        _type = type;
        return this;
    }

    public ITransitionBuilder Fade()
    {
        return OfType(TransitionType.Fade);
    }

    public ITransitionBuilder Push()
    {
        return OfType(TransitionType.Push);
    }

    public ITransitionBuilder Wipe()
    {
        return OfType(TransitionType.Wipe);
    }

    public ITransitionBuilder Dissolve()
    {
        return OfType(TransitionType.Dissolve);
    }

    public ITransitionBuilder WithDuration(int durationMs)
    {
        _durationMs = durationMs;
        return this;
    }

    public ITransitionBuilder AdvanceOnClick(bool advance)
    {
        _advanceOnClick = advance;
        return this;
    }

    public ITransitionBuilder AdvanceAfter(int delayMs)
    {
        _advanceAfterMs = delayMs;
        return this;
    }

    internal SlideTransition Build()
    {
        return new SlideTransition
        {
            Type = _type,
            DurationMs = _durationMs,
            AdvanceOnClick = _advanceOnClick,
            AdvanceAfterMs = _advanceAfterMs
        };
    }
}

internal sealed class TextBoxBuilder : ITextBoxBuilder
{
    private Position _position = new();
    private ElementSize _size = new();
    private readonly List<TextRun> _runs = [];
    private string? _bgColor;
    private BorderDefinition? _border;
    private Margins? _padding;
    private TextAlignment _textAlignment = TextAlignment.Left;
    private VerticalAlignment _vAlignment = VerticalAlignment.Top;
    private ElementAnimation? _animation;

    public ITextBoxBuilder AtPosition(double xMm, double yMm)
    {
        _position = new Position { XMm = xMm, YMm = yMm };
        return this;
    }

    public ITextBoxBuilder WithSize(double widthMm, double heightMm)
    {
        _size = new ElementSize { WidthMm = widthMm, HeightMm = heightMm };
        return this;
    }

    public ITextBoxBuilder AddText(string text)
    {
        _runs.Add(new TextRun { Text = text });
        return this;
    }

    public ITextBoxBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _runs.Add(new TextRun { Text = text, Style = builder.Build() });
        return this;
    }

    public ITextBoxBuilder WithBackgroundColor(string hex)
    {
        _bgColor = hex;
        return this;
    }

    public ITextBoxBuilder WithBorder(BorderStyle style, string? color, double? widthPt)
    {
        _border = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    public ITextBoxBuilder WithPadding(double allSidesMm)
    {
        _padding = new Margins { TopMm = allSidesMm, RightMm = allSidesMm, BottomMm = allSidesMm, LeftMm = allSidesMm };
        return this;
    }

    public ITextBoxBuilder WithPadding(double topMm, double rightMm, double bottomMm, double leftMm)
    {
        _padding = new Margins { TopMm = topMm, RightMm = rightMm, BottomMm = bottomMm, LeftMm = leftMm };
        return this;
    }

    public ITextBoxBuilder WithTextAlignment(TextAlignment alignment)
    {
        _textAlignment = alignment;
        return this;
    }

    public ITextBoxBuilder WithVerticalAlignment(VerticalAlignment alignment)
    {
        _vAlignment = alignment;
        return this;
    }

    public ITextBoxBuilder WithAnimation(Action<IAnimationBuilder> configure)
    {
        var builder = new AnimationBuilder();
        configure(builder);
        _animation = builder.Build();
        return this;
    }

    internal TextBoxElement Build()
    {
        return new TextBoxElement
        {
            Runs = _runs,
            Position = _position,
            Size = _size,
            Animation = _animation,
            Style = new TextBoxStyle
            {
                BackgroundColor = _bgColor,
                Border = _border,
                Padding = _padding,
                TextAlignment = _textAlignment,
                VerticalAlignment = _vAlignment
            }
        };
    }
}

internal sealed class SlideImageBuilder : ISlideImageBuilder
{
    private Position _position = new();
    private ElementSize _size = new();
    private byte[]? _data;
    private ImageFormat _format = ImageFormat.Png;
    private string? _altText;
    private ElementAnimation? _animation;

    public ISlideImageBuilder AtPosition(double xMm, double yMm)
    {
        _position = new Position { XMm = xMm, YMm = yMm };
        return this;
    }

    public ISlideImageBuilder WithSize(double widthMm, double heightMm)
    {
        _size = new ElementSize { WidthMm = widthMm, HeightMm = heightMm };
        return this;
    }

    public ISlideImageBuilder FromBytes(byte[] data)
    {
        _data = data;
        return this;
    }

    public ISlideImageBuilder FromStream(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _data = ms.ToArray();
        return this;
    }

    public ISlideImageBuilder WithFormat(ImageFormat format)
    {
        _format = format;
        return this;
    }

    public ISlideImageBuilder WithAltText(string altText)
    {
        _altText = altText;
        return this;
    }

    public ISlideImageBuilder WithAnimation(Action<IAnimationBuilder> configure)
    {
        var builder = new AnimationBuilder();
        configure(builder);
        _animation = builder.Build();
        return this;
    }

    internal SlideImageElement Build()
    {
        return new SlideImageElement
        {
            Image = new ImageContent
            {
                Data = _data ??
                       throw new InvalidOperationException("Image data is required. Call FromBytes() or FromStream()."),
                Format = _format,
                AltText = _altText
            },
            Position = _position,
            Size = _size,
            Animation = _animation
        };
    }
}

internal sealed class SlideChartBuilder : ISlideChartBuilder
{
    private Position _position = new();
    private ElementSize _size = new();
    private ChartDefinition? _chart;
    private ElementAnimation? _animation;

    public ISlideChartBuilder AtPosition(double xMm, double yMm)
    {
        _position = new Position { XMm = xMm, YMm = yMm };
        return this;
    }

    public ISlideChartBuilder WithSize(double widthMm, double heightMm)
    {
        _size = new ElementSize { WidthMm = widthMm, HeightMm = heightMm };
        return this;
    }

    public ISlideChartBuilder ConfigureChart(Action<IChartBuilder> configure)
    {
        var builder = new ChartBuilder();
        configure(builder);
        _chart = builder.Build();
        return this;
    }

    public ISlideChartBuilder WithAnimation(Action<IAnimationBuilder> configure)
    {
        var builder = new AnimationBuilder();
        configure(builder);
        _animation = builder.Build();
        return this;
    }

    internal SlideChartElement Build()
    {
        return new SlideChartElement
        {
            Chart = _chart ?? throw new InvalidOperationException("Chart is required. Call ConfigureChart()."),
            Position = _position,
            Size = _size,
            Animation = _animation
        };
    }
}

internal sealed class SlideTableBuilder : ISlideTableBuilder
{
    private Position _position = new();
    private ElementSize _size = new();
    private readonly List<TableColumn> _columns = [];
    private readonly List<TableRow> _rows = [];
    private BorderSet? _borders;
    private TextStyle? _headerStyle;
    private TextStyle? _cellStyle;
    private ElementAnimation? _animation;

    public ISlideTableBuilder AtPosition(double xMm, double yMm)
    {
        _position = new Position { XMm = xMm, YMm = yMm };
        return this;
    }

    public ISlideTableBuilder WithSize(double widthMm, double heightMm)
    {
        _size = new ElementSize { WidthMm = widthMm, HeightMm = heightMm };
        return this;
    }

    public ISlideTableBuilder AddColumn(string? header, double? widthMm, TextAlignment alignment)
    {
        _columns.Add(new TableColumn { Header = header, WidthMm = widthMm, Alignment = alignment });
        return this;
    }

    public ISlideTableBuilder AddRow(Action<ITableRowBuilder> configure)
    {
        var builder = new TableRowBuilder();
        configure(builder);
        _rows.Add(builder.Build());
        return this;
    }

    public ISlideTableBuilder WithBorders(Action<IBorderSetBuilder> configure)
    {
        var builder = new BorderSetBuilder();
        configure(builder);
        _borders = builder.Build();
        return this;
    }

    public ISlideTableBuilder WithHeaderStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _headerStyle = builder.Build();
        return this;
    }

    public ISlideTableBuilder WithCellStyle(Action<ITextStyleBuilder> configure)
    {
        var builder = new TextStyleBuilder();
        configure(builder);
        _cellStyle = builder.Build();
        return this;
    }

    public ISlideTableBuilder WithAnimation(Action<IAnimationBuilder> configure)
    {
        var builder = new AnimationBuilder();
        configure(builder);
        _animation = builder.Build();
        return this;
    }

    internal SlideTableElement Build()
    {
        return new SlideTableElement
        {
            Columns = _columns,
            Rows = _rows,
            Position = _position,
            Size = _size,
            Animation = _animation,
            Borders = _borders,
            HeaderStyle = _headerStyle,
            CellStyle = _cellStyle
        };
    }
}

internal sealed class ShapeBuilder : IShapeBuilder
{
    private ShapeType _shapeType = ShapeType.Rectangle;
    private Position _position = new();
    private ElementSize _size = new();
    private string? _fillColor;
    private BorderDefinition? _border;
    private double _opacity = 1.0;
    private double _rotation;
    private string? _text;
    private TextStyle? _textStyle;
    private ElementAnimation? _animation;

    public IShapeBuilder OfType(ShapeType type)
    {
        _shapeType = type;
        return this;
    }

    public IShapeBuilder AtPosition(double xMm, double yMm)
    {
        _position = new Position { XMm = xMm, YMm = yMm };
        return this;
    }

    public IShapeBuilder WithSize(double widthMm, double heightMm)
    {
        _size = new ElementSize { WidthMm = widthMm, HeightMm = heightMm };
        return this;
    }

    public IShapeBuilder WithFillColor(string hex)
    {
        _fillColor = hex;
        return this;
    }

    public IShapeBuilder WithBorder(BorderStyle style, string? color, double? widthPt)
    {
        _border = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    public IShapeBuilder WithOpacity(double opacity)
    {
        _opacity = opacity;
        return this;
    }

    public IShapeBuilder WithRotation(double degrees)
    {
        _rotation = degrees;
        return this;
    }

    public IShapeBuilder WithText(string text, Action<ITextStyleBuilder>? configureStyle)
    {
        _text = text;
        if (configureStyle is not null)
        {
            var builder = new TextStyleBuilder();
            configureStyle(builder);
            _textStyle = builder.Build();
        }

        return this;
    }

    public IShapeBuilder WithAnimation(Action<IAnimationBuilder> configure)
    {
        var builder = new AnimationBuilder();
        configure(builder);
        _animation = builder.Build();
        return this;
    }

    internal ShapeElement Build()
    {
        return new ShapeElement
        {
            ShapeType = _shapeType,
            Position = _position,
            Size = _size,
            Animation = _animation,
            Style = new ShapeStyle
            {
                FillColor = _fillColor,
                Border = _border,
                Opacity = _opacity,
                RotationDegrees = _rotation
            },
            Text = _text,
            TextStyle = _textStyle
        };
    }
}

internal sealed class AnimationBuilder : IAnimationBuilder
{
    private AnimationType _type = AnimationType.Appear;
    private int _delayMs;
    private int _durationMs = 500;
    private AnimationTrigger _trigger = AnimationTrigger.OnClick;

    public IAnimationBuilder OfType(AnimationType type)
    {
        _type = type;
        return this;
    }

    public IAnimationBuilder FadeIn()
    {
        return OfType(AnimationType.FadeIn);
    }

    public IAnimationBuilder SlideIn()
    {
        return OfType(AnimationType.SlideIn);
    }

    public IAnimationBuilder ZoomIn()
    {
        return OfType(AnimationType.ZoomIn);
    }

    public IAnimationBuilder Appear()
    {
        return OfType(AnimationType.Appear);
    }

    public IAnimationBuilder WithDelay(int delayMs)
    {
        _delayMs = delayMs;
        return this;
    }

    public IAnimationBuilder WithDuration(int durationMs)
    {
        _durationMs = durationMs;
        return this;
    }

    public IAnimationBuilder OnClick()
    {
        _trigger = AnimationTrigger.OnClick;
        return this;
    }

    public IAnimationBuilder WithPrevious()
    {
        _trigger = AnimationTrigger.WithPrevious;
        return this;
    }

    public IAnimationBuilder AfterPrevious()
    {
        _trigger = AnimationTrigger.AfterPrevious;
        return this;
    }

    internal ElementAnimation Build()
    {
        return new ElementAnimation
        {
            Type = _type,
            DelayMs = _delayMs,
            DurationMs = _durationMs,
            Trigger = _trigger
        };
    }
}
