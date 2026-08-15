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
