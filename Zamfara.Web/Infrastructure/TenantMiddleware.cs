using Zamfara.Web.Data;

namespace Zamfara.Web.Infrastructure;

/// <summary>
/// Runs before routing and stamps every request as either belonging to a
/// school sub-site or to the portal directory (apex zamfara.org).
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolver _resolver;

    public TenantMiddleware(RequestDelegate next, TenantResolver resolver)
    {
        _next = next;
        _resolver = resolver;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host.Trim().ToLowerInvariant();
        var school = _resolver.Resolve(host, context.Request.Query["school"]);

        if (school is not null)
        {
            context.Items[TenantKeys.School] = school;
        }
        else
        {
            context.Items[TenantKeys.IsPortal] = true;
        }

        await _next(context);
    }
}
