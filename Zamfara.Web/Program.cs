using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Zamfara.Web.Routing;

// Resolve the project directory (where wwwroot lives) even when the app is
// launched from elsewhere, e.g. `dotnet bin/Debug/net8.0/Zamfara.Web.dll`
// run from the repository root.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRoot()
});

// Don't advertise the server implementation.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// AutoValidateAntiforgeryToken is inert right now (no form actions exist), but it
// guarantees CSRF protection if forms are added later without explicit opt-in.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddRouting(options =>
    options.ConstraintMap.Add("schoolSite", typeof(SchoolSiteConstraint)));

// HSTS for 365 days (includes *.zamfara.org subdomains) once in production.
builder.Services.Configure<HstsOptions>(options => options.MaxAge = TimeSpan.FromDays(365));

var app = builder.Build();

// Security headers on every response: informational site, no framing, no MIME
// sniffing, and a strict content policy. The only script/style sources are our
// own files; images may be self-hosted or the inline data: favicon; form
// submission, framing, and embedding are all blocked.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'none'; " +
        "media-src 'none'; " +
        "object-src 'none'; " +
        "frame-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'none'; " +
        "form-action 'none'";
    await next();
});

// The site is read-only: accept only GET and HEAD. Every other verb (TRACE,
// PUT, POST, DELETE, OPTIONS, …) is rejected up front so scanners and abuse
// attempts hit a closed door instead of the MVC pipeline.
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
    {
        context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
        context.Response.Headers.Allow = "GET, HEAD";
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});

// Honor X-Forwarded-Proto only when a TLS-terminating reverse proxy is
// explicitly enabled, and only from the loopback or Docker bridge networks.
// When on, the proxy must be the only thing that can reach this port, otherwise
// spoofed headers could defeat the HTTPS redirect below.
if (builder.Configuration.GetValue<bool>("ASPNETCORE_FORWARDEDHEADERS_ENABLED"))
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
        KnownNetworks =
        {
            new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Loopback, 8),
            new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12)
        }
    });
}

// No-detail health probe for Docker HEALTHCHECK / uptime monitors. Terminal
// branch mapped before the production middleware so it stays reachable over
// plain HTTP in containers.
app.Map("/healthz", branch => branch.Run(async context =>
{
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("Healthy");
}));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// 301-redirect the legacy static URLs to their School One equivalents.
// (?i) + optional trailing slash so /Index.html and /about.html/ redirect too.
app.UseRewriter(new RewriteOptions()
    .AddRedirect(@"(?i)^index\.html/?$", "/school-one/", 301)
    .AddRedirect(@"(?i)^(about|academics|admissions|staff|calendar|contact)\.html/?$",
        "/school-one/$1", 301));

app.UseStaticFiles();
app.UseRouting();

app.MapDefaultControllerRoute();

app.Run();

static string ResolveContentRoot()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "wwwroot")))
            {
                return dir.FullName;
            }
        }
    }

    return Directory.GetCurrentDirectory();
}
