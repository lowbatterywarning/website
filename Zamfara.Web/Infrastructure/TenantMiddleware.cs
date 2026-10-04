using Microsoft.AspNetCore.Http.Extensions;

namespace Zamfara.Web.Infrastructure;

/// <summary>Resolves school hosts and keeps production URLs canonical.</summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolver _resolver;
    private readonly IWebHostEnvironment _environment;

    public TenantMiddleware(RequestDelegate next, TenantResolver resolver,
        IWebHostEnvironment environment)
    {
        _next = next;
        _resolver = resolver;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host.Trim().ToLowerInvariant();
        var schoolOverride = context.Request.Query["school"].ToString();

        if (!_environment.IsDevelopment() && !string.IsNullOrEmpty(schoolOverride))
        {
            var selected = _resolver.Resolve(host, schoolOverride);
            if (selected is not null)
            {
                var query = new QueryBuilder();
                foreach (var parameter in context.Request.Query)
                {
                    if (parameter.Key.Equals("school", StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var value in parameter.Value)
                        query.Add(parameter.Key, value ?? "");
                }
                context.Response.Redirect($"https://{selected.Slug}.zamfara.org" +
                    context.Request.PathBase.ToUriComponent() +
                    context.Request.Path.ToUriComponent() + query.ToQueryString());
                return;
            }
        }

        var school = _resolver.Resolve(host,
            _environment.IsDevelopment() ? schoolOverride : null);
        if (school is not null)
            context.Items[TenantKeys.School] = school;
        else
            context.Items[TenantKeys.IsPortal] = true;

        await _next(context);
    }
}
