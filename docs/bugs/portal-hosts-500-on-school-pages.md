# Bug: portal hosts (apex and `www`) return HTTP 500 on every school page

**Status:** fixed — shared controller action guard redirects portal school-page requests to `/` (HTTP 302)
**Found:** 2026-10-04, while deploying the site behind a Cloudflare Tunnel
**Affects:** any host that resolves to the portal tenant — `zamfara.org`, `www.zamfara.org`
**Not affected:** school subdomains (`gsss.zamfara.org`, `demo-one.zamfara.org`, …) — all nine pages return 200

## Summary

`TenantResolver.Resolve` deliberately resolves the apex domain and `www` to the **portal** (no
school). `HomeController` only handles that case in `Index()`. Every other action calls
`RequireSchool()`, which throws when no school was resolved, so those routes return a **500** error
page instead of the directory — or a 404.

## Steps to reproduce

```bash
curl -o /dev/null -w '%{http_code}\n' https://www.zamfara.org/         # 200  (portal directory)
curl -o /dev/null -w '%{http_code}\n' https://www.zamfara.org/about    # 500  <-- bug
curl -o /dev/null -w '%{http_code}\n' https://zamfara.org/about        # 500  <-- bug
curl -o /dev/null -w '%{http_code}\n' https://gsss.zamfara.org/about   # 200  (sanity: real slug is fine)
```

Failing routes on a portal host: `/about`, `/academics`, `/admissions`, `/news`, `/staff`,
`/calendar`, `/gallery`, `/faq`. All eight. `/` renders correctly.

## Root cause

- `Zamfara.Web/Infrastructure/TenantResolver.cs:62-65` — `www` returns `null; // portal`.
- `Zamfara.Web/Infrastructure/TenantResolver.cs:72-74` — the apex domain, unknown subdomains and
  direct IP access also return `null` ("so unknown hosts never see an unowned site").
- `Zamfara.Web/Controllers/HomeController.cs:27-30` — `Index()` is the **only** action that checks
  the portal case (`if (HttpContext.IsPortal()) return Portal();`).
- `Zamfara.Web/Controllers/HomeController.cs:101-102` — `RequireSchool()` throws
  `InvalidOperationException` when the middleware resolved no school. `TemplatePage()` (line 95)
  and the `News`/`Calendar`/`Gallery`/`Faq` actions all call it unconditionally.

Production log excerpt (.NET 8, container `zamfara-web`):

```
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
      An unhandled exception has occurred while executing the request.
      System.InvalidOperationException: Tenant middleware did not resolve a school for this request.
         at Zamfara.Web.Controllers.HomeController.RequireSchool() in /src/Zamfara.Web/Controllers/HomeController.cs:line 102
```

## Why it went unnoticed

`TenantResolver.cs:52-55` treats `localhost` and `127.*` as **the default school**, so a local
`curl http://localhost:5000/about` returns 200. The bug only appears when the `Host` header is a
real portal host. **Do not use localhost as a proxy for portal behaviour** — test with a Host
header, e.g.:

```bash
curl -H 'Host: www.zamfara.org' -H 'X-Forwarded-Proto: https' http://127.0.0.1:8081/about
```

## Suggested fix

Choose one behaviour for portal hosts on school-page routes:

1. **Redirect to the directory (recommended).** Friendliest, and consistent with the existing 301
   legacy-rewrite style in `Program.cs`. Any school page requested on a portal host returns a
   redirect to `/`.
2. **404.** Strict, and arguably correct — the route does not exist on the portal. Note this would
   also make the legacy rewrites (`/about.html` → `/about`, `/school-two/admissions` →
   `/admissions`) land on a 404 when requested on the apex.

Implement once rather than per action: a small action filter, a shared guard in `TemplatePage()`
plus the data-backed actions, or a middleware branch that rewrites portal school-page requests to
`/` before routing runs.

## Acceptance criteria

- `https://zamfara.org/about` and `https://www.zamfara.org/about` no longer return 500.
- `https://zamfara.org/` still renders the school directory.
- School subdomains unchanged: all nine pages 200 on `gsss.zamfara.org`.
- The legacy 301 rewrites still work on school subdomains.

## Implemented fix and verification

`HomeController.OnActionExecuting` checks the resolved portal flag before
executing school actions and returns a redirect to the directory. `Index` and
`Error` are exempt, preserving the directory and production error handler.
Unknown paths still return 404 because they do not resolve to a controller action.
Legacy redirects retain their existing HTTP 301 behavior.

The solution builds on .NET 10 without warnings. `scripts/smoke-test.ps1` passes
136 HTTP checks against Production using a temporary SQLite database, including
all affected routes on apex, www, and unknown subdomains, school routes for all
three seeded schools, and legacy redirects.