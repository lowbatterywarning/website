namespace Zamfara.Web.Models;

/// <summary>View model for every school page: which school, which page.</summary>
public sealed class SchoolPageViewModel
{
    public required SchoolSite Site { get; init; }

    /// <summary>Key into <see cref="SchoolSites.Pages"/> ("Index", "About", ...).</summary>
    public required string Page { get; init; }

    public PageMeta Meta => SchoolSites.Pages.TryGetValue(Page, out var meta)
        ? meta.Format(Site.Name)
        : new PageMeta($"{Site.Name} — {Page}", $"{Site.Name} school page.",
            $"{Site.Name} — {Page}", $"Explore {Site.Name} on the zamfara.org school network.");
}
