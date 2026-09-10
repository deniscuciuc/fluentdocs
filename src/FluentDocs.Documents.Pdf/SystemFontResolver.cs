using PdfSharp.Fonts;

namespace FluentDocs.Documents.Pdf;

/// <summary>
/// Resolves fonts from the Linux system font directories.
/// Falls back to DejaVu Sans / Liberation Sans if the requested family is unavailable.
/// </summary>
internal sealed class SystemFontResolver : IFontResolver
{
    private static readonly string[] FontDirs =
    [
        "/usr/share/fonts",
        "/usr/local/share/fonts",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".fonts"),
        // macOS
        "/System/Library/Fonts",
        "/Library/Fonts",
        // Windows
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts))
    ];

    private static readonly Dictionary<string, string> FontFileCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _cacheBuilt;
    private static readonly object Lock = new();

    /// <summary>
    /// Ensures fonts are scanned once.
    /// </summary>
    private static void EnsureCache()
    {
        if (_cacheBuilt) return;
        lock (Lock)
        {
            if (_cacheBuilt) return;
            foreach (var dir in FontDirs)
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.EnumerateFiles(dir, "*.ttf", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    FontFileCache.TryAdd(name, file);
                }

                foreach (var file in Directory.EnumerateFiles(dir, "*.otf", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    FontFileCache.TryAdd(name, file);
                }
            }

            _cacheBuilt = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        EnsureCache();

        // Build candidate key: "Arial-Bold", "Arial-BoldItalic", "Arial-Italic", "Arial"
        var suffix = (isBold, isItalic) switch
        {
            (true, true) => "-BoldItalic",
            (true, false) => "-Bold",
            (false, true) => "-Italic",
            _ => ""
        };

        // Try exact match first
        var candidates = new[]
        {
            familyName + suffix,
            familyName.Replace(" ", "") + suffix,
            // Common Linux naming patterns
            familyName + (suffix == "" ? "-Regular" : suffix),
            familyName.Replace(" ", "") + (suffix == "" ? "-Regular" : suffix)
        };

        foreach (var c in candidates)
            if (FontFileCache.ContainsKey(c))
                return new FontResolverInfo(c);

        // Fallback chain
        string[] fallbacks = isBold switch
        {
            true when isItalic => ["DejaVuSans-BoldOblique", "LiberationSans-BoldItalic"],
            true => ["DejaVuSans-Bold", "LiberationSans-Bold"],
            _ when isItalic => ["DejaVuSans-Oblique", "LiberationSans-Italic"],
            _ => ["DejaVuSans", "LiberationSans-Regular"]
        };

        foreach (var fb in fallbacks)
            if (FontFileCache.ContainsKey(fb))
                return new FontResolverInfo(fb);

        // Last resort: return the first font in cache
        if (FontFileCache.Count > 0)
            return new FontResolverInfo(FontFileCache.Keys.First());

        return null;
    }

    public byte[]? GetFont(string faceName)
    {
        EnsureCache();
        return FontFileCache.TryGetValue(faceName, out var path) ? File.ReadAllBytes(path) : null;
    }
}
