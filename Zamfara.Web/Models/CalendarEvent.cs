namespace Zamfara.Web.Models;

/// <summary>One row of the school calendar table.</summary>
public sealed class CalendarEvent
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    /// <summary>e.g. "Aug 15, 2026" or "Nov 22–26, 2026".</summary>
    public string DateText { get; set; } = "";

    public string Title { get; set; } = "";

    /// <summary>One of: Academic, Holiday, Event, Exams. Drives the colored tag.</summary>
    public string Category { get; set; } = "Event";

    public string Details { get; set; } = "";

    /// <summary>Day number for the "Upcoming Events" badge, e.g. "15".</summary>
    public string DayText { get; set; } = "";

    /// <summary>Month for the badge, e.g. "Aug".</summary>
    public string MonthText { get; set; } = "";

    /// <summary>Featured events also appear in the "Upcoming Events" list.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Longer description shown in the "Upcoming Events" list (optional).</summary>
    public string? FeaturedText { get; set; }

    public int SortOrder { get; set; }
}
