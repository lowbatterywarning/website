using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Zamfara.Web.Infrastructure;

namespace Zamfara.Web.Controllers;

public sealed class SearchEngineController : Controller
{
    private static readonly string[] SchoolPaths =
        ["/", "/about", "/academics", "/admissions", "/news", "/staff", "/calendar", "/gallery", "/faq"];

    private string Origin => HttpContext.GetSchool() is { } school
        ? $"https://{school.Slug}.zamfara.org"
        : "https://zamfara.org";

    [HttpGet("/sitemap.xml")]
    public IActionResult Sitemap()
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var paths = HttpContext.IsPortal() ? new[] { "/" } : SchoolPaths;
        var document = new XDocument(new XElement(ns + "urlset",
            paths.Select(path => new XElement(ns + "url",
                new XElement(ns + "loc", Origin + path)))));
        return Content(document.ToString(), "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots() =>
        Content($"User-agent: *\nAllow: /\nSitemap: {Origin}/sitemap.xml\n", "text/plain; charset=utf-8");
}
