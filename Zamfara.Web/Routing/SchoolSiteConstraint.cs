using Zamfara.Web.Models;

namespace Zamfara.Web.Routing;

/// <summary>
/// Matches only slugs registered in <see cref="SchoolSites"/>, so unknown
/// site segments 404 instead of rendering a school page.
/// </summary>
public sealed class SchoolSiteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
        => values.TryGetValue(routeKey, out var value)
           && value is string slug
           && SchoolSites.TryGet(slug) is not null;
}
