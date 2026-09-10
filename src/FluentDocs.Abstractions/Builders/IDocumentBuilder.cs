using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models;
using FluentDocs.Abstractions.Models.Documents;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="DocumentDefinition"/>.
/// </summary>
public interface IDocumentBuilder
{
    IDocumentBuilder WithFileName(string fileName);
    IDocumentBuilder ForFormat(GeneratedFileFormat format);
    IDocumentBuilder WithMetadata(Action<IDocumentMetadataBuilder> configure);
    IDocumentBuilder WithPageSetup(Action<IPageSetupBuilder> configure);
    IDocumentBuilder WithDefaultStyles(Action<IDocumentStylesBuilder> configure);
    IDocumentBuilder WithHeader(Action<IHeaderFooterBuilder> configure);
    IDocumentBuilder WithFooter(Action<IHeaderFooterBuilder> configure);
    IDocumentBuilder AddSection(Action<ISectionBuilder> configure);
    DocumentDefinition Build();
}

/// <summary>
/// Builder for document metadata (title, author, keywords, etc.).
/// </summary>
public interface IDocumentMetadataBuilder
{
    IDocumentMetadataBuilder Title(string title);
    IDocumentMetadataBuilder Author(string author);
    IDocumentMetadataBuilder Subject(string subject);
    IDocumentMetadataBuilder Description(string description);
    IDocumentMetadataBuilder AddKeyword(string keyword);
    IDocumentMetadataBuilder Language(string language);
    IDocumentMetadataBuilder CreatedAt(DateTimeOffset createdAt);
}

/// <summary>
/// Builder for page setup (size, orientation, margins).
/// </summary>
public interface IPageSetupBuilder
{
    IPageSetupBuilder WithPageSize(PageSize pageSize);
    IPageSetupBuilder WithOrientation(PageOrientation orientation);
    IPageSetupBuilder WithMargins(Margins margins);
    IPageSetupBuilder WithMargins(double topMm, double rightMm, double bottomMm, double leftMm);
    IPageSetupBuilder WithCustomSize(double widthMm, double heightMm);
}

/// <summary>
/// Builder for default document styles.
/// </summary>
public interface IDocumentStylesBuilder
{
    IDocumentStylesBuilder WithDefaultTextStyle(Action<ITextStyleBuilder> configure);
    IDocumentStylesBuilder WithDefaultParagraphStyle(Action<IParagraphStyleBuilder> configure);
    IDocumentStylesBuilder WithHeadingStyle(HeadingLevel level, Action<ITextStyleBuilder> configure);
}

/// <summary>
/// Builder for a document section containing ordered elements.
/// </summary>
public interface ISectionBuilder
{
    ISectionBuilder AddHeading(string text, HeadingLevel level = HeadingLevel.H1);
    ISectionBuilder AddHeading(string text, HeadingLevel level, Action<ITextStyleBuilder> configureStyle);
    ISectionBuilder AddParagraph(string text);
    ISectionBuilder AddParagraph(Action<IParagraphBuilder> configure);
    ISectionBuilder AddImage(Action<IImageBuilder> configure);
    ISectionBuilder AddTable(Action<ITableContentBuilder> configure);
    ISectionBuilder AddChart(Action<IChartBuilder> configure);
    ISectionBuilder AddPageBreak();
    ISectionBuilder AddHorizontalRule();
    ISectionBuilder AddList(bool ordered, Action<IListBuilder> configure);
    ISectionBuilder AddCodeBlock(string code, string? language = null);
    ISectionBuilder AddBlockQuote(string text);
    ISectionBuilder AddBlockQuote(Action<IParagraphBuilder> configure);
}

/// <summary>
/// Builder for a paragraph with multiple styled text runs.
/// </summary>
public interface IParagraphBuilder
{
    IParagraphBuilder AddText(string text);
    IParagraphBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure);
    IParagraphBuilder AddLineBreak();
    IParagraphBuilder WithStyle(Action<IParagraphStyleBuilder> configure);
}

/// <summary>
/// Builder for header or footer definitions.
/// </summary>
public interface IHeaderFooterBuilder
{
    IHeaderFooterBuilder AddText(string text);
    IHeaderFooterBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure);
    IHeaderFooterBuilder ShowPageNumber(string? format = null);
}

/// <summary>
/// Builder for a list (ordered or unordered).
/// </summary>
public interface IListBuilder
{
    IListBuilder AddItem(string text);
    IListBuilder AddItem(Action<IParagraphBuilder> configure);
    IListBuilder AddItem(string text, Action<IListBuilder> configureNested);
}
