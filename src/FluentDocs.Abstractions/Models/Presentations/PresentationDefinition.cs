using FluentDocs.Abstractions.Enums;

namespace FluentDocs.Abstractions.Models.Presentations;

/// <summary>
/// Complete definition of a presentation to generate (PPTX).
/// </summary>
public sealed record PresentationDefinition : FileDefinition
{
    public PresentationMetadata Metadata { get; init; } = new();
    public SlideSize SlideSize { get; init; } = SlideSize.Widescreen;
    public ThemeDefinition? Theme { get; init; }
    public IReadOnlyList<SlideDefinition> Slides { get; init; } = [];
}

/// <summary>
/// Presentation-level metadata.
/// </summary>
public sealed record PresentationMetadata
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Subject { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>
/// Slide dimensions.
/// </summary>
public sealed record SlideSize
{
    public double WidthMm { get; init; }
    public double HeightMm { get; init; }

    /// <summary>Standard 16:9 widescreen (254mm × 190.5mm).</summary>
    public static SlideSize Widescreen => new() { WidthMm = 338.67, HeightMm = 190.5 };

    /// <summary>Standard 4:3 (254mm × 190.5mm).</summary>
    public static SlideSize Standard => new() { WidthMm = 254, HeightMm = 190.5 };
}

/// <summary>
/// Visual theme applied across the presentation.
/// </summary>
public sealed record ThemeDefinition
{
    public string? PrimaryColor { get; init; }
    public string? SecondaryColor { get; init; }
    public IReadOnlyList<string> AccentColors { get; init; } = [];
    public string? DefaultFont { get; init; }
    public string? HeadingFont { get; init; }
    public string? BackgroundColor { get; init; }
}

/// <summary>
/// A single slide in a presentation.
/// </summary>
public sealed record SlideDefinition
{
    public SlideLayout Layout { get; init; } = SlideLayout.Blank;
    public string? Notes { get; init; }
    public IReadOnlyList<ISlideElement> Elements { get; init; } = [];
    public SlideTransition? Transition { get; init; }
    public string? BackgroundColor { get; init; }
    public ImageContent? BackgroundImage { get; init; }
}

/// <summary>
/// Slide transition configuration.
/// </summary>
public sealed record SlideTransition
{
    public TransitionType Type { get; init; } = TransitionType.None;
    public int DurationMs { get; init; } = 500;
    public bool AdvanceOnClick { get; init; } = true;
    public int? AdvanceAfterMs { get; init; }
}
