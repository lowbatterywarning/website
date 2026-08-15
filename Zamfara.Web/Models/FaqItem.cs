namespace Zamfara.Web.Models;

/// <summary>A question/answer pair on the FAQ page (rendered as native &lt;details&gt;).</summary>
public sealed class FaqItem
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
}
