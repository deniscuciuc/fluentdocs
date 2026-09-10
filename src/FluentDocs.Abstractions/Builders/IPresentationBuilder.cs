using FluentDocs.Abstractions.Enums;
using FluentDocs.Abstractions.Models.Presentations;

namespace FluentDocs.Abstractions.Builders;

/// <summary>
/// Fluent builder for constructing a <see cref="PresentationDefinition"/>.
/// </summary>
public interface IPresentationBuilder
{
    IPresentationBuilder WithFileName(string fileName);
    IPresentationBuilder WithMetadata(Action<IPresentationMetadataBuilder> configure);
    IPresentationBuilder WithSlideSize(SlideSize size);
    IPresentationBuilder WithTheme(Action<IThemeBuilder> configure);
    IPresentationBuilder AddSlide(Action<ISlideBuilder> configure);
    PresentationDefinition Build();
}

/// <summary>
/// Builder for presentation metadata.
/// </summary>
public interface IPresentationMetadataBuilder
{
    IPresentationMetadataBuilder Title(string title);
    IPresentationMetadataBuilder Author(string author);
    IPresentationMetadataBuilder Subject(string subject);
    IPresentationMetadataBuilder Description(string description);
    IPresentationMetadataBuilder CreatedAt(DateTimeOffset createdAt);
}

/// <summary>
/// Builder for the presentation visual theme.
/// </summary>
public interface IThemeBuilder
{
    IThemeBuilder PrimaryColor(string hex);
    IThemeBuilder SecondaryColor(string hex);
    IThemeBuilder AddAccentColor(string hex);
    IThemeBuilder DefaultFont(string fontFamily);
    IThemeBuilder HeadingFont(string fontFamily);
    IThemeBuilder BackgroundColor(string hex);
}

/// <summary>
/// Builder for a single slide.
/// </summary>
public interface ISlideBuilder
{
    ISlideBuilder WithLayout(SlideLayout layout);
    ISlideBuilder WithNotes(string notes);
    ISlideBuilder WithTransition(Action<ITransitionBuilder> configure);
    ISlideBuilder WithBackground(string colorHex);
    ISlideBuilder WithBackgroundImage(Action<IImageBuilder> configure);
    ISlideBuilder AddTextBox(Action<ITextBoxBuilder> configure);
    ISlideBuilder AddImage(Action<ISlideImageBuilder> configure);
    ISlideBuilder AddChart(Action<ISlideChartBuilder> configure);
    ISlideBuilder AddTable(Action<ISlideTableBuilder> configure);
    ISlideBuilder AddShape(Action<IShapeBuilder> configure);
}

/// <summary>
/// Builder for slide transitions.
/// </summary>
public interface ITransitionBuilder
{
    ITransitionBuilder OfType(TransitionType type);
    ITransitionBuilder Fade();
    ITransitionBuilder Push();
    ITransitionBuilder Wipe();
    ITransitionBuilder Dissolve();
    ITransitionBuilder WithDuration(int durationMs);
    ITransitionBuilder AdvanceOnClick(bool advance = true);
    ITransitionBuilder AdvanceAfter(int delayMs);
}

/// <summary>
/// Builder for a text box on a slide.
/// </summary>
public interface ITextBoxBuilder
{
    ITextBoxBuilder AtPosition(double xMm, double yMm);
    ITextBoxBuilder WithSize(double widthMm, double heightMm);
    ITextBoxBuilder AddText(string text);
    ITextBoxBuilder AddFormattedText(string text, Action<ITextStyleBuilder> configure);
    ITextBoxBuilder WithBackgroundColor(string hex);
    ITextBoxBuilder WithBorder(BorderStyle style, string? color = null, double? widthPt = null);
    ITextBoxBuilder WithPadding(double allSidesMm);
    ITextBoxBuilder WithPadding(double topMm, double rightMm, double bottomMm, double leftMm);
    ITextBoxBuilder WithTextAlignment(TextAlignment alignment);
    ITextBoxBuilder WithVerticalAlignment(VerticalAlignment alignment);
    ITextBoxBuilder WithAnimation(Action<IAnimationBuilder> configure);
}

/// <summary>
/// Builder for an image placed on a slide.
/// </summary>
public interface ISlideImageBuilder
{
    ISlideImageBuilder AtPosition(double xMm, double yMm);
    ISlideImageBuilder WithSize(double widthMm, double heightMm);
    ISlideImageBuilder FromBytes(byte[] data);
    ISlideImageBuilder FromStream(Stream stream);
    ISlideImageBuilder WithFormat(ImageFormat format);
    ISlideImageBuilder WithAltText(string altText);
    ISlideImageBuilder WithAnimation(Action<IAnimationBuilder> configure);
}

/// <summary>
/// Builder for a chart placed on a slide.
/// </summary>
public interface ISlideChartBuilder
{
    ISlideChartBuilder AtPosition(double xMm, double yMm);
    ISlideChartBuilder WithSize(double widthMm, double heightMm);
    ISlideChartBuilder ConfigureChart(Action<IChartBuilder> configure);
    ISlideChartBuilder WithAnimation(Action<IAnimationBuilder> configure);
}

/// <summary>
/// Builder for a table placed on a slide.
/// </summary>
public interface ISlideTableBuilder
{
    ISlideTableBuilder AtPosition(double xMm, double yMm);
    ISlideTableBuilder WithSize(double widthMm, double heightMm);

    ISlideTableBuilder AddColumn(string? header = null, double? widthMm = null,
        TextAlignment alignment = TextAlignment.Left);

    ISlideTableBuilder AddRow(Action<ITableRowBuilder> configure);
    ISlideTableBuilder WithBorders(Action<IBorderSetBuilder> configure);
    ISlideTableBuilder WithHeaderStyle(Action<ITextStyleBuilder> configure);
    ISlideTableBuilder WithCellStyle(Action<ITextStyleBuilder> configure);
    ISlideTableBuilder WithAnimation(Action<IAnimationBuilder> configure);
}

/// <summary>
/// Builder for a geometric shape on a slide.
/// </summary>
public interface IShapeBuilder
{
    IShapeBuilder OfType(ShapeType type);
    IShapeBuilder AtPosition(double xMm, double yMm);
    IShapeBuilder WithSize(double widthMm, double heightMm);
    IShapeBuilder WithFillColor(string hex);
    IShapeBuilder WithBorder(BorderStyle style, string? color = null, double? widthPt = null);
    IShapeBuilder WithOpacity(double opacity);
    IShapeBuilder WithRotation(double degrees);
    IShapeBuilder WithText(string text, Action<ITextStyleBuilder>? configureStyle = null);
    IShapeBuilder WithAnimation(Action<IAnimationBuilder> configure);
}

/// <summary>
/// Builder for element entrance/emphasis animations.
/// </summary>
public interface IAnimationBuilder
{
    IAnimationBuilder OfType(AnimationType type);
    IAnimationBuilder FadeIn();
    IAnimationBuilder SlideIn();
    IAnimationBuilder ZoomIn();
    IAnimationBuilder Appear();
    IAnimationBuilder WithDelay(int delayMs);
    IAnimationBuilder WithDuration(int durationMs);
    IAnimationBuilder OnClick();
    IAnimationBuilder WithPrevious();
    IAnimationBuilder AfterPrevious();
}
