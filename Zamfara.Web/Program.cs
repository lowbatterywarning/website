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

builder.Services.AddControllersWithViews();
builder.Services.AddRouting(options =>
    options.ConstraintMap.Add("schoolSite", typeof(SchoolSiteConstraint)));

var app = builder.Build();

// Security headers on every response: informational site, no framing or MIME sniffing.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

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
