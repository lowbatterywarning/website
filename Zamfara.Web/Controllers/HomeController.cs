using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>
/// The whole site: a single generic school at the root paths
/// /, /about, /academics, /admissions, /staff, /calendar, /contact.
/// </summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => Page(nameof(Index));

    public IActionResult About() => Page(nameof(About));

    public IActionResult Academics() => Page(nameof(Academics));

    public IActionResult Admissions() => Page(nameof(Admissions));

    public IActionResult Staff() => Page(nameof(Staff));

    public IActionResult Calendar() => Page(nameof(Calendar));

    public IActionResult Contact() => Page(nameof(Contact));

    // No exception details are passed to the view: the error page is generic by
    // design, so nothing sensitive can leak to a public visitor.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    private ViewResult Page(string page) => View(SchoolPages.Get(page));
}
