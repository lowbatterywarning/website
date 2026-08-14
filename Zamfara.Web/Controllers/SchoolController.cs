using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>
/// Serves all three school sub-sites from one shared set of views.
/// /{site}                -> Views/School/Index.cshtml
/// /{site}/about          -> Views/School/About.cshtml
/// etc. — identical by construction across schools.
/// </summary>
[Route("{site:schoolSite}")]
public sealed class SchoolController : Controller
{
    [Route("")]
    public IActionResult Index(string site) => SchoolView(site);

    [Route("about")]
    public IActionResult About(string site) => SchoolView(site);

    [Route("academics")]
    public IActionResult Academics(string site) => SchoolView(site);

    [Route("admissions")]
    public IActionResult Admissions(string site) => SchoolView(site);

    [Route("staff")]
    public IActionResult Staff(string site) => SchoolView(site);

    [Route("calendar")]
    public IActionResult Calendar(string site) => SchoolView(site);

    [Route("contact")]
    public IActionResult Contact(string site) => SchoolView(site);

    private IActionResult SchoolView(string site)
    {
        // The route constraint already guarantees a known slug; this guard is a backstop.
        if (SchoolSites.TryGet(site) is not { } school)
        {
            return NotFound();
        }

        return View(new SchoolPageViewModel
        {
            Site = school,
            Page = ControllerContext.ActionDescriptor.ActionName
        });
    }
}
