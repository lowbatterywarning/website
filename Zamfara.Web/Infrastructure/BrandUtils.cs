using System.Globalization;
using System.Text.RegularExpressions;
using Zamfara.Web.Models;

namespace Zamfara.Web.Infrastructure;

/// <summary>
/// Helpers for per-school theming. Colors stored in the database are validated
/// as strict #RRGGBB before they reach a view (anything else falls back to the
/// default palette), and derived shades are computed by blending.
/// </summary>
public static class BrandUtils
{
    private static readonly Regex HexPattern = new(@"^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public static string SafeHex(string? value, string fallback) =>
        value is not null && HexPattern.IsMatch(value) ? value : fallback;

    /// <summary>Blends hex color <paramref name="amount"/> toward white (0..1).</summary>
    public static string Lighten(string hex, double amount) => Mix(hex, "FFFFFF", amount);

    /// <summary>Blends hex color <paramref name="amount"/> toward black (0..1).</summary>
    public static string Darken(string hex, double amount) => Mix(hex, "000000", amount);

    /// <summary>"r, g, b" triple for use inside rgba(var(--x-rgb), alpha).</summary>
    public static string Rgb(string hex)
    {
        var c = Parse(hex);
        return $"{(int)c.R}, {(int)c.G}, {(int)c.B}";
    }

    private static string Mix(string hex, string toward, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        var c = Parse(hex);
        var t = Parse(toward);
        return "#" +
            ((int)Math.Round(c.R + (t.R - c.R) * amount)).ToString("X2", CultureInfo.InvariantCulture) +
            ((int)Math.Round(c.G + (t.G - c.G) * amount)).ToString("X2", CultureInfo.InvariantCulture) +
            ((int)Math.Round(c.B + (t.B - c.B) * amount)).ToString("X2", CultureInfo.InvariantCulture);
    }

    private static (double R, double G, double B) Parse(string hex)
    {
        var value = hex.TrimStart('#');
        return (
            int.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    /// <summary>WCAG 2.1 relative luminance of a #RRGGBB color (0..1).</summary>
    private static double RelativeLuminance(string hex)
    {
        var c = Parse(hex);
        double Channel(double v)
        {
            v /= 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    /// <summary>WCAG 2.1 contrast ratio between two colors (1..21).</summary>
    public static double ContrastRatio(string a, string b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    /// <summary>
    /// Blends <paramref name="start"/> toward <paramref name="toward"/> in small
    /// steps until its contrast against <paramref name="background"/> meets the
    /// WCAG target, returning the first passing color.
    /// </summary>
    private static string Accessible(string start, string toward, string background, double target = 4.5)
    {
        for (var amount = 0.0; amount <= 1.0; amount += 0.01)
        {
            var candidate = Mix(start, toward, amount);
            if (ContrastRatio(candidate, background) >= target)
                return candidate;
        }
        return Mix(start, toward, 1);
    }

    /// <summary>
    /// Accent text on dark/navy surfaces: blends the accent toward white until
    /// legible against the worst-case nav-hover tint (primary blended 20% toward
    /// the accent).
    /// </summary>
    public static string AccentTextOnDark(string accent, string primary) =>
        Accessible(accent, "FFFFFF", Mix(primary, accent, 0.2));

    /// <summary>
    /// Accent text on light surfaces: blends the accent toward black until
    /// legible against the lightest tinted surface (#F4F5F7).
    /// </summary>
    public static string AccentTextOnLight(string accent) =>
        Accessible(accent, "000000", "#F4F5F7");

    /// <summary>
    /// Text for buttons whose resting background is the accent: blends the
    /// primary toward black until legible against the darkened accent
    /// (gold-dark) used on hover.
    /// </summary>
    public static string ButtonTextOnAccent(string primary, string accent) =>
        Accessible(primary, "000000", Darken(accent, 0.15));

    /// <summary>
    /// Data-URI favicon in the school's colors, with the school's initial.
    /// </summary>
    public static string FavIconUri(School school)
    {
        var primary = SafeHex(school.PrimaryColor, "#1B2A4A").TrimStart('#');
        var accent = SafeHex(school.AccentColor, "#C9A84C").TrimStart('#');
        var initial = Uri.EscapeDataString(
            string.IsNullOrWhiteSpace(school.ShortName) ? "S" : school.ShortName.Substring(0, 1).ToUpperInvariant());
        return "data:image/svg+xml," +
            "%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E" +
            $"%3Crect width='100' height='100' rx='20' fill='%23{primary}'/%3E" +
            $"%3Ctext x='50' y='68' text-anchor='middle' font-family='Georgia,serif' font-weight='bold' font-size='60' fill='%23{accent}'%3E{initial}%3C/text%3E%3C/svg%3E";
    }
}
