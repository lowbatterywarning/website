using System.Text;
using Zamfara.Web.Data;

namespace Zamfara.Web.Infrastructure;

/// <summary>
/// Runs before routing and stamps every request with its resolved school.
/// Single-school mode: the apex domain (zamfara.org) is the site; the www
/// subdomain is 301-redirected to it, and any other host gets a 404 with a
/// link back home.
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

        // www.zamfara.org -> canonical apex (301). Path and query are preserved.
        if (host.Equals("www.zamfara.org", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location =
                $"https://zamfara.org{context.Request.Path}{context.Request.QueryString}";
            return;
        }

        // Health probes must be reachable on any host (localhost, container IP,
        // internal hostname), so they bypass tenant resolution entirely.
        if (context.Request.Path.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var school = _resolver.Resolve(host, context.Request.Query["school"]);

        if (school is not null)
        {
            context.Items[TenantKeys.School] = school;
            await _next(context);
            return;
        }

        // Unowned host (school subdomain, unknown host, direct IP): 404 with a
        // self-contained page so it renders even though no static files are
        // served for this host. Inline styles are allowed by the CSP.
        await WriteNotFound(context, "https://zamfara.org");
    }

    private static async Task WriteNotFound(HttpContext context, string homeUrl)
    {
        var html = """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Page not found — Government Science Secondary School, Gusau</title>
              <style>
                :root {
                  --navy: #1B2A4A;
                  --navy-light: #31456f;
                  --gold: #C9A84C;
                }
                * { box-sizing: border-box; margin: 0; padding: 0; }
                body {
                  font-family: Georgia, "Times New Roman", Times, serif;
                  background: linear-gradient(135deg, var(--navy) 0%, var(--navy-light) 100%);
                  color: #fff;
                  min-height: 100vh;
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  justify-content: center;
                  text-align: center;
                  padding: 2rem 1.25rem;
                }
                .status {
                  font-size: 5rem;
                  font-weight: 700;
                  color: var(--gold);
                  line-height: 1;
                  margin-bottom: 1rem;
                }
                h1 { font-size: 1.6rem; margin-bottom: 0.75rem; }
                p { font-size: 1.05rem; max-width: 480px; opacity: 0.92; margin-bottom: 1.75rem; }
                a {
                  display: inline-block;
                  background: var(--gold);
                  color: #1B2A4A;
                  font-weight: 700;
                  text-decoration: none;
                  padding: 0.8rem 1.6rem;
                  border-radius: 999px;
                }
                a:hover, a:focus-visible { filter: brightness(1.08); outline: none; }
              </style>
            </head>
            <body>
              <div class="status">404</div>
              <h1>This page doesn't exist</h1>
              <p>Government Science Secondary School, Gusau — this address isn't part of our website.</p>
              <a href="__HOME__">Go to the home page</a>
            </body>
            </html>
            """.Replace("__HOME__", homeUrl);
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, Encoding.UTF8);
    }
}
