using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Data;
using Zamfara.Web.Infrastructure;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>
/// Serves every school sub-site. Which school a request belongs to was
/// resolved from the Host header by TenantMiddleware; portal requests (apex
/// zamfara.org) render the school directory instead.
/// </summary>
public sealed class HomeController : Controller
{
    private readonly ZamfaraDbContext _db;
    private readonly TenantResolver _resolver;

    public HomeController(ZamfaraDbContext db, TenantResolver resolver)
    {
        _db = db;
        _resolver = resolver;
    }

    public IActionResult Index()
    {
        if (HttpContext.IsPortal())
        {
            return Portal();
        }

        var school = RequireSchool();
        var news = _db.NewsPosts.AsNoTracking()
            .Where(n => n.SchoolId == school.Id)
            .OrderBy(n => n.SortOrder)
            .Take(3)
            .ToList();
        return View(new HomeViewModel(Meta(nameof(Index), school), school, news));
    }

    public IActionResult About() => TemplatePage(nameof(About));

    public IActionResult Academics() => TemplatePage(nameof(Academics));

    public IActionResult Admissions() => TemplatePage(nameof(Admissions));

    public IActionResult News()
    {
        var school = RequireSchool();
        var news = _db.NewsPosts.AsNoTracking()
            .Where(n => n.SchoolId == school.Id)
            .OrderBy(n => n.SortOrder)
            .ToList();
        return View(new NewsViewModel(Meta(nameof(News), school), school, news));
    }

    public IActionResult Staff() => TemplatePage(nameof(Staff));

    public IActionResult Calendar()
    {
        var school = RequireSchool();
        var events = _db.CalendarEvents.AsNoTracking()
            .Where(e => e.SchoolId == school.Id)
            .OrderBy(e => e.SortOrder)
            .ToList();
        var upcoming = events.Where(e => e.IsFeatured).Take(4).ToList();
        return View(new CalendarViewModel(Meta(nameof(Calendar), school), school, events, upcoming));
    }

    public IActionResult Gallery()
    {
        var school = RequireSchool();
        var items = _db.GalleryItems.AsNoTracking()
            .Where(g => g.SchoolId == school.Id)
            .OrderBy(g => g.SortOrder)
            .ToList();
        return View(new GalleryViewModel(Meta(nameof(Gallery), school), school, items));
    }

    public IActionResult Faq()
    {
        var school = RequireSchool();
        var faqs = _db.FaqItems.AsNoTracking()
            .Where(f => f.SchoolId == school.Id)
            .OrderBy(f => f.SortOrder)
            .ToList();
        return View(new FaqViewModel(Meta(nameof(Faq), school), school, faqs));
    }

    // No exception details are passed to the view: the error page is generic by
    // design, so nothing sensitive can leak to a public visitor.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    private IActionResult TemplatePage(string page)
    {
        var school = RequireSchool();
        return View(page, new PageModel(Meta(page, school), school));
    }

    private School RequireSchool() =>
        HttpContext.GetSchool() ?? throw new InvalidOperationException("Tenant middleware did not resolve a school for this request.");

    private static PageMeta Meta(string page, School school) => SchoolPages.Get(page, school.Name);

    private IActionResult Portal()
    {
        var host = Request.Host.Host;
        var liveDomain = host.EndsWith(".zamfara.org", StringComparison.OrdinalIgnoreCase)
            || host.Equals("zamfara.org", StringComparison.OrdinalIgnoreCase);
        var schools = _resolver.AllSchools()
            .Select(s => new PortalSchool(
                s,
                liveDomain
                    ? $"https://{s.Slug}.zamfara.org"
                    : $"/?school={s.Slug}"))
            .ToList();
        return View("Portal", new PortalViewModel(schools));
    }
}
