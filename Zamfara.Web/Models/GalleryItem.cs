namespace Zamfara.Web.Models;

/// <summary>A photo tile on the Gallery page.</summary>
public sealed class GalleryItem
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    public string Title { get; set; } = "";
    public string Caption { get; set; } = "";

    /// <summary>Path relative to wwwroot, e.g. "images/campus.jpg".</summary>
    public string ImagePath { get; set; } = "";

    public int SortOrder { get; set; }
}
