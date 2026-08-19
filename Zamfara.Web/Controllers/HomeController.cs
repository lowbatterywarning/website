using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Data;
using Zamfara.Web.Infrastructure;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>
/// Serves the single school site. Which school a request belongs to was
/// resolved from the Host header by TenantMiddleware; unowned hosts are
/// answered with a 404 by the middleware before this controller is reached.
/// </summary>
public sealed class HomeController : Controller
{
    private readonly ZamfaraDbContext _db;

    public HomeController(ZamfaraDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
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

    public IActionResult Gsttp() => TemplatePage(nameof(Gsttp));

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

    // Unknown path on an owned host (e.g. /aboutxyz): a friendly 404 with a
    // link back to the school home page.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NotFoundPage()
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    private IActionResult TemplatePage(string page)
    {
        var school = RequireSchool();
        return View(page, new PageModel(Meta(page, school), school));
    }

    private School RequireSchool() =>
        HttpContext.GetSchool() ?? throw new InvalidOperationException("Tenant middleware did not resolve a school for this request.");

    private static PageMeta Meta(string page, School school) => SchoolPages.Get(page, school.Name);
}
