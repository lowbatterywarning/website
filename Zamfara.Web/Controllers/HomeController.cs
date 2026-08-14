using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Models;

namespace Zamfara.Web.Controllers;

/// <summary>zamfara.org landing page linking out to the three school sites.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => View(SchoolSites.All);
}
