namespace Zamfara.Web.Models;

/// <summary>
/// One school sub-site (tenant). The tenant is resolved from the request's
/// Host header (e.g. gsss.zamfara.org -> slug "gsss") — see
/// Infrastructure/TenantMiddleware.cs. All displayed branding, contact
/// details, and themed colors come from this record.
/// </summary>
public sealed class School
{
    public int Id { get; set; }

    /// <summary>Host subdomain, e.g. "gsss". Lowercase letters, digits, hyphens.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Full display name, e.g. "Government Science Secondary School, Gusau".</summary>
    public string Name { get; set; } = "";

    /// <summary>Compact name for narrow screens, e.g. "GSSS Gusau".</summary>
    public string ShortName { get; set; } = "";

    /// <summary>Hero tagline shown on the home page.</summary>
    public string Tagline { get; set; } = "";

    public string Established { get; set; } = "";

    /// <summary>e.g. "Gusau, Zamfara State, Nigeria".</summary>
    public string Location { get; set; } = "";

    /// <summary>Local Government Area, used by the portal directory search.</summary>
    public string Lga { get; set; } = "";

    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>Primary (dark) brand color, validated hex like #1B2A4A.</summary>
    public string PrimaryColor { get; set; } = "#1B2A4A";

    /// <summary>Accent brand color, validated hex like #C9A84C.</summary>
    public string AccentColor { get; set; } = "#C9A84C";

    /// <summary>Emoji shown next to the school name in the header, e.g. 🎓.</summary>
    public string LogoEmoji { get; set; } = "🎓";

    /// <summary>Path to the campus photo used on the home page, e.g. "images/campus.jpg".</summary>
    public string CampusPhoto { get; set; } = "";

    public int SortOrder { get; set; }

    public List<NewsPost> NewsPosts { get; set; } = new();
    public List<CalendarEvent> CalendarEvents { get; set; } = new();
    public List<GalleryItem> GalleryItems { get; set; } = new();
    public List<FaqItem> FaqItems { get; set; } = new();
}
