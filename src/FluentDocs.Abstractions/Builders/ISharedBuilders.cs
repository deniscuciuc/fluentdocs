using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Fluent builder for inline text styling.
/// </summary>
public interface ITextStyleBuilder
{
    ITextStyleBuilder FontFamily(string fontFamily);
    ITextStyleBuilder FontSize(double sizePt);
    ITextStyleBuilder Color(string hex);
    ITextStyleBuilder BackgroundColor(string hex);
    ITextStyleBuilder Bold();
    ITextStyleBuilder Italic();
    ITextStyleBuilder Underline();
    ITextStyleBuilder Strikethrough();
    ITextStyleBuilder Superscript();
    ITextStyleBuilder Subscript();
}

/// <summary>
/// Fluent builder for paragraph-level styling.
/// </summary>
public interface IParagraphStyleBuilder
{
    IParagraphStyleBuilder Alignment(TextAlignment alignment);
    IParagraphStyleBuilder LineSpacing(double spacing);
    IParagraphStyleBuilder SpaceBefore(double pt);
    IParagraphStyleBuilder SpaceAfter(double pt);
    IParagraphStyleBuilder Indent(double mm);
}

/// <summary>
/// Fluent builder for an embedded image.
/// </summary>
public interface IImageBuilder
{
    IImageBuilder FromBytes(byte[] data);
    IImageBuilder FromStream(Stream stream);
    IImageBuilder WithFormat(ImageFormat format);
    IImageBuilder WithSize(double widthMm, double heightMm);
    IImageBuilder WithWidth(double widthMm);
    IImageBuilder WithHeight(double heightMm);
    IImageBuilder WithAltText(string altText);
    IImageBuilder WithCaption(string caption);
    IImageBuilder MaintainAspectRatio(bool maintain = true);
    IImageBuilder AtCell(string anchorCell);
    IImageBuilder WithOffsetPx(int x, int y);
}

/// <summary>
/// Fluent builder for an inline table within a document.
/// </summary>
public interface ITableContentBuilder
{
    ITableContentBuilder AddColumn(string? header = null, double? widthMm = null,
        TextAlignment alignment = TextAlignment.Left);

    ITableContentBuilder AddRow(Action<ITableRowBuilder> configure);
    ITableContentBuilder WithBorders(Action<IBorderSetBuilder> configure);
    ITableContentBuilder WithHeaderStyle(Action<ITextStyleBuilder> configure);
    ITableContentBuilder WithCellStyle(Action<ITextStyleBuilder> configure);
}

/// <summary>
/// Fluent builder for a table row within an inline document table.
/// </summary>
public interface ITableRowBuilder
{
    ITableRowBuilder AddCell(string? text);
    ITableRowBuilder AddCell(Action<IParagraphBuilder> configure);
    ITableRowBuilder AddCell(string? text, Action<ITextStyleBuilder> configureStyle);
}

/// <summary>
/// Fluent builder for border sets (four sides).
/// </summary>
public interface IBorderSetBuilder
{
    IBorderSetBuilder All(BorderStyle style, string? color = null, double? widthPt = null);
    IBorderSetBuilder Top(BorderStyle style, string? color = null, double? widthPt = null);
    IBorderSetBuilder Right(BorderStyle style, string? color = null, double? widthPt = null);
    IBorderSetBuilder Bottom(BorderStyle style, string? color = null, double? widthPt = null);
    IBorderSetBuilder Left(BorderStyle style, string? color = null, double? widthPt = null);
}
