using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Data;
using Zamfara.Web.Infrastructure;

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

// SQLite keeps the whole school directory + content in one file next to the
// app — ideal for the homelab server (no DB process, trivial backup by copying).
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "zamfara.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<ZamfaraDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Singleton tenant resolver: caches the (tiny) Schools table for the process
// lifetime and resolves the school slug from the request Host.
builder.Services.AddSingleton<TenantResolver>();

// HSTS for 365 days (includes *.zamfara.org subdomains) once in production.
builder.Services.Configure<HstsOptions>(options => options.MaxAge = TimeSpan.FromDays(365));

var app = builder.Build();

// Create the database and seed the three schools on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ZamfaraDbContext>();
    db.Database.EnsureCreated();
    Seeder.Seed(db);
}

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

// Stamp every request as belonging to a school sub-site (Host:
// {slug}.zamfara.org) or to the portal directory (apex/unknown host).
app.UseMiddleware<TenantMiddleware>();

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

// 301-redirect legacy URLs to the current equivalents: the original static
// .html paths and the retired /school-one|two|three areas. Targets are
// canonicalized to lowercase without a trailing slash; unknown legacy paths are
// left alone so routing answers 404. contact.* now points at the home page:
// the Contact page was retired and contact details live in the footer.
app.UseRewriter(new RewriteOptions()
    .Add(context =>
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        string? target = null;

        var htmlMatch = Regex.Match(
            path,
            @"^/(?<page>index|about|academics|admissions|staff|calendar|contact)\.html/?$",
            RegexOptions.IgnoreCase);
        if (htmlMatch.Success)
        {
            var page = htmlMatch.Groups["page"].Value;
            target = page.Equals("index", StringComparison.OrdinalIgnoreCase)
                ? "/"
                : page.Equals("contact", StringComparison.OrdinalIgnoreCase)
                    ? "/"
                    : "/" + page.ToLowerInvariant();
        }
        else
        {
            var schoolMatch = Regex.Match(
                path,
                @"^/school-(one|two|three)(?:/(?<page>about|academics|admissions|staff|calendar|contact))?/?$",
                RegexOptions.IgnoreCase);
            if (schoolMatch.Success)
            {
                target = schoolMatch.Groups["page"].Success &&
                    !schoolMatch.Groups["page"].Value.Equals("contact", StringComparison.OrdinalIgnoreCase)
                        ? "/" + schoolMatch.Groups["page"].Value.ToLowerInvariant()
                        : "/";
            }
        }

        if (target is null)
        {
            return;
        }

        context.HttpContext.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.HttpContext.Response.Headers.Location = target;
        context.Result = RuleResult.EndResponse;
    }));

app.UseStaticFiles(new StaticFileOptions
{
    // CSS/JS must always revalidate so a redeploy is picked up immediately
    // (ETag-backed conditional requests keep this cheap). Images get a short
    // public cache: the site is small and photos get swapped by hand.
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
        else
        {
            ctx.Context.Response.Headers.CacheControl = "public, max-age=3600";
        }
    }
});
app.UseRouting();

// Literal route table: the eight template pages plus the error handler.
// Anything else (e.g. /Home/About) falls through to 404.
app.MapControllerRoute(name: "home", pattern: "", defaults: new { controller = "Home", action = "Index" });
app.MapControllerRoute(name: "about", pattern: "about", defaults: new { controller = "Home", action = "About" });
app.MapControllerRoute(name: "academics", pattern: "academics", defaults: new { controller = "Home", action = "Academics" });
app.MapControllerRoute(name: "admissions", pattern: "admissions", defaults: new { controller = "Home", action = "Admissions" });
app.MapControllerRoute(name: "news", pattern: "news", defaults: new { controller = "Home", action = "News" });
app.MapControllerRoute(name: "staff", pattern: "staff", defaults: new { controller = "Home", action = "Staff" });
app.MapControllerRoute(name: "calendar", pattern: "calendar", defaults: new { controller = "Home", action = "Calendar" });
app.MapControllerRoute(name: "gallery", pattern: "gallery", defaults: new { controller = "Home", action = "Gallery" });
app.MapControllerRoute(name: "faq", pattern: "faq", defaults: new { controller = "Home", action = "Faq" });
app.MapControllerRoute(name: "error", pattern: "Home/Error", defaults: new { controller = "Home", action = "Error" });

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
