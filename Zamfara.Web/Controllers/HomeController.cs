using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>zamfara.org landing page linking out to the three school sites.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => View(SchoolSites.All);

    // No exception details are passed to the view: the error page is generic by
    // design, so nothing sensitive can leak to a public visitor.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
