namespace Zamfara.Web.Models;

/// <summary>
/// Base view model for every school page: head metadata plus the resolved
/// tenant. The layout renders branding from <see cref="School"/>.
/// </summary>
public record PageModel(PageMeta Meta, School School);

public sealed record HomeViewModel(PageMeta Meta, School School, IReadOnlyList<NewsPost> News)
    : PageModel(Meta, School);

public sealed record NewsViewModel(PageMeta Meta, School School, IReadOnlyList<NewsPost> News)
    : PageModel(Meta, School);

public sealed record CalendarViewModel(PageMeta Meta, School School,
    IReadOnlyList<CalendarEvent> Events, IReadOnlyList<CalendarEvent> Upcoming)
    : PageModel(Meta, School);

public sealed record GalleryViewModel(PageMeta Meta, School School, IReadOnlyList<GalleryItem> Items)
    : PageModel(Meta, School);

public sealed record FaqViewModel(PageMeta Meta, School School, IReadOnlyList<FaqItem> Faqs)
    : PageModel(Meta, School);
