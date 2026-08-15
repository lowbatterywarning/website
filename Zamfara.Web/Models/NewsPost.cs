namespace Zamfara.Web.Models;

/// <summary>A news story shown on the home page and the News page.</summary>
public sealed class NewsPost
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    /// <summary>Human display date, e.g. "July 28, 2026".</summary>
    public string DateText { get; set; } = "";

    /// <summary>Label for the image placeholder tile, e.g. "Science lab".</summary>
    public string ImageLabel { get; set; } = "";

    public int SortOrder { get; set; }
}
