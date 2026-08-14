using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>zamfara.org landing page linking out to the three school sites.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => View(SchoolSites.All);

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        return View(feature?.Error);
    }
}
