using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;

namespace FluentDocs.Abstractions.Builders.Impl;

internal sealed class TextStyleBuilder : ITextStyleBuilder
{
    private string? _fontFamily;
    private double? _fontSize;
    private string? _color;
    private string? _bgColor;
    private bool _bold;
    private bool _italic;
    private bool _underline;
    private bool _strikethrough;
    private bool _superscript;
    private bool _subscript;

    public ITextStyleBuilder FontFamily(string fontFamily)
    {
        _fontFamily = fontFamily;
        return this;
    }

    public ITextStyleBuilder FontSize(double sizePt)
    {
        _fontSize = sizePt;
        return this;
    }

    public ITextStyleBuilder Color(string hex)
    {
        _color = hex;
        return this;
    }

    public ITextStyleBuilder BackgroundColor(string hex)
    {
        _bgColor = hex;
        return this;
    }

    public ITextStyleBuilder Bold()
    {
        _bold = true;
        return this;
    }

    public ITextStyleBuilder Italic()
    {
        _italic = true;
        return this;
    }

    public ITextStyleBuilder Underline()
    {
        _underline = true;
        return this;
    }

    public ITextStyleBuilder Strikethrough()
    {
        _strikethrough = true;
        return this;
    }

    public ITextStyleBuilder Superscript()
    {
        _superscript = true;
        return this;
    }

    public ITextStyleBuilder Subscript()
    {
        _subscript = true;
        return this;
    }

    internal TextStyle Build()
    {
        return new TextStyle
        {
            FontFamily = _fontFamily,
            FontSizePt = _fontSize,
            Color = _color,
            BackgroundColor = _bgColor,
            Bold = _bold,
            Italic = _italic,
            Underline = _underline,
            Strikethrough = _strikethrough,
            Superscript = _superscript,
            Subscript = _subscript
        };
    }
}

internal sealed class ParagraphStyleBuilder : IParagraphStyleBuilder
{
    private TextAlignment _alignment = TextAlignment.Left;
    private double? _lineSpacing;
    private double? _spaceBefore;
    private double? _spaceAfter;
    private double? _indent;

    public IParagraphStyleBuilder Alignment(TextAlignment alignment)
    {
        _alignment = alignment;
        return this;
    }

    public IParagraphStyleBuilder LineSpacing(double spacing)
    {
        _lineSpacing = spacing;
        return this;
    }

    public IParagraphStyleBuilder SpaceBefore(double pt)
    {
        _spaceBefore = pt;
        return this;
    }

    public IParagraphStyleBuilder SpaceAfter(double pt)
    {
        _spaceAfter = pt;
        return this;
    }

    public IParagraphStyleBuilder Indent(double mm)
    {
        _indent = mm;
        return this;
    }

    internal ParagraphStyle Build()
    {
        return new ParagraphStyle
        {
            Alignment = _alignment,
            LineSpacing = _lineSpacing,
            SpaceBeforePt = _spaceBefore,
            SpaceAfterPt = _spaceAfter,
            IndentMm = _indent
        };
    }
}

internal sealed class ImageBuilder : IImageBuilder
{
    private byte[]? _data;
    private ImageFormat _format = ImageFormat.Png;
    private string? _altText;
    private string? _caption;
    private double? _widthMm;
    private double? _heightMm;
    private bool _maintainAspectRatio = true;
    private string? _anchorCell;
    private int _offsetXPx;
    private int _offsetYPx;

    public IImageBuilder FromBytes(byte[] data)
    {
        _data = data;
        return this;
    }

    public IImageBuilder FromStream(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _data = ms.ToArray();
        return this;
    }

    public IImageBuilder WithFormat(ImageFormat format)
    {
        _format = format;
        return this;
    }

    public IImageBuilder WithSize(double widthMm, double heightMm)
    {
        _widthMm = widthMm;
        _heightMm = heightMm;
        return this;
    }

    public IImageBuilder WithWidth(double widthMm)
    {
        _widthMm = widthMm;
        return this;
    }

    public IImageBuilder WithHeight(double heightMm)
    {
        _heightMm = heightMm;
        return this;
    }

    public IImageBuilder WithAltText(string altText)
    {
        _altText = altText;
        return this;
    }

    public IImageBuilder WithCaption(string caption)
    {
        _caption = caption;
        return this;
    }

    public IImageBuilder MaintainAspectRatio(bool maintain)
    {
        _maintainAspectRatio = maintain;
        return this;
    }

    public IImageBuilder AtCell(string anchorCell)
    {
        _anchorCell = anchorCell;
        return this;
    }

    public IImageBuilder WithOffsetPx(int x, int y)
    {
        _offsetXPx = x;
        _offsetYPx = y;
        return this;
    }

    internal ImageContent Build()
    {
        return new ImageContent
        {
            Data = _data ??
                   throw new InvalidOperationException("Image data is required. Call FromBytes() or FromStream()."),
            Format = _format,
            AltText = _altText,
            Caption = _caption,
            WidthMm = _widthMm,
            HeightMm = _heightMm,
            MaintainAspectRatio = _maintainAspectRatio,
            AnchorCell = _anchorCell,
            OffsetXPx = _offsetXPx,
            OffsetYPx = _offsetYPx
        };
    }
}

internal sealed class BorderSetBuilder : IBorderSetBuilder
{
    private BorderDefinition? _top;
    private BorderDefinition? _right;
    private BorderDefinition? _bottom;
    private BorderDefinition? _left;

    public IBorderSetBuilder All(BorderStyle style, string? color, double? widthPt)
    {
        var border = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        _top = _right = _bottom = _left = border;
        return this;
    }

    public IBorderSetBuilder Top(BorderStyle style, string? color, double? widthPt)
    {
        _top = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    public IBorderSetBuilder Right(BorderStyle style, string? color, double? widthPt)
    {
        _right = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    public IBorderSetBuilder Bottom(BorderStyle style, string? color, double? widthPt)
    {
        _bottom = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    public IBorderSetBuilder Left(BorderStyle style, string? color, double? widthPt)
    {
        _left = new BorderDefinition { Style = style, Color = color, WidthPt = widthPt };
        return this;
    }

    internal BorderSet Build()
    {
        return new BorderSet
        {
            Top = _top,
            Right = _right,
            Bottom = _bottom,
            Left = _left
        };
    }
}
