# Zamfara school portal — ASP.NET Core MVC

An ASP.NET Core 10 MVC website with a school directory at `zamfara.org` and
individual school sites at `{slug}.zamfara.org`. SQLite stores school branding,
contact details, news, calendar events, gallery items, and FAQs. The initial
seed includes Government Science Secondary School, Gusau (`gsss`) and two demo
schools (`demo-one` and `demo-two`).

## Routes and tenants

| Route | School page |
| --- | --- |
| `/` | Home |
| `/about` | About |
| `/academics` | Academics |
| `/admissions` | Admissions |
| `/news` | News |
| `/staff` | Staff |
| `/calendar` | Calendar |
| `/gallery` | Gallery |
| `/faq` | FAQ |

On apex, `www`, and unknown school subdomains, `/` renders the directory.
School-page routes on these portal hosts redirect to `/` with HTTP 302.
Unknown routes return 404. The error action remains available to the production
exception handler.

`localhost` and `127.*` resolve to the first seeded school. In Development,
`?school=slug` selects another school and generated navigation links preserve
that selection. In Production, a valid selection redirects to the school's
canonical HTTPS subdomain, preserving the path and other query parameters.
Invalid production selections are ignored and the hostname resolves normally.

Legacy `.html` and `/school-one|two|three` paths return permanent 301
redirects. The retired contact page redirects to `/`; contact details are in
the school footer.

## Build and run

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet build Zamfara.sln -c Release
cd Zamfara.Web
dotnet run
```

The development profile listens at `http://localhost:5000`. Startup creates
`Zamfara.Web/App_Data/zamfara.db` if needed and seeds content only when the
Schools table is empty. Set `ZAMFARA_DB_PATH` to override the database file. Relative filenames are
resolved under the app's content root; a bare filename such as `school.db`
is supported. Initial school and content inserts commit in one transaction.
There are no schema migrations or content administration UI yet.

## Route regression check

After building, run the smoke test from the repository root with PowerShell 7:

```powershell
dotnet run --project tests/Zamfara.RegressionTests -c Release --no-build
./scripts/smoke-test.ps1
./scripts/smoke-test.ps1 -Environment Development
./scripts/smoke-test.ps1 -DisableProxy
```

The console regression checks cover database filename resolution, a forced
content-save failure followed by successful seed retry, seed idempotency, and
trusted/untrusted proxy addresses.

The smoke script starts the Release build on a temporary loopback port,
uses a temporary SQLite database, then stops its process and removes its
temporary data. It checks 149 requests in Production and 146 in Development,
including portal redirects, school pages, legacy URLs, per-school sitemaps and
robots files, query-selected navigation, rejected POST requests, and HTTPS
redirection. `-DisableProxy` additionally verifies that the old ASP.NET proxy
flag cannot enable unrestricted header handling. GitHub Actions runs all of
these checks before publishing the container image.

Sitemaps and robots files are generated per tenant. The portal sitemap lists
its root; each school's sitemap lists its nine school pages on its canonical
subdomain. Static sitemap/robots files in `legacy-static/` are only snapshots.

## Structure

| Path | Purpose |
| --- | --- |
| `Zamfara.Web/Program.cs` | Startup, security headers, proxy settings, literal routes, legacy redirects |
| `Zamfara.Web/Controllers/HomeController.cs` | Page actions and shared portal route guard |
| `Zamfara.Web/Infrastructure/` | Tenant resolution and branding helpers |
| `Zamfara.Web/Data/` | EF Core SQLite context and initial content seed |
| `Zamfara.Web/Models/` | School/content entities and page view models |
| `Zamfara.Web/Views/` | School pages, shared layout, and portal directory |
| `Zamfara.Web/wwwroot/` | CSS, JavaScript, images, and search-engine files |
| `legacy-static/` | Original static website snapshot |

To change initial school branding, edit `Data/Seeder.cs`. Changes to the seed
will not update an existing database; existing records need to be updated
separately. Phone numbers, email addresses, social links, and much of the
school content are still placeholders.

## Docker deployment

```powershell
docker build -t zamfara-web .
```

The Dockerfile builds and runs on .NET 10. The runtime listens on port 8080,
runs as the non-root `app` user, and probes `/healthz` over plain HTTP.
The compose stack uses the published GHCR image and a Cloudflare Tunnel.
Provide `TUNNEL_TOKEN` before running:

```powershell
docker compose up -d
```

SQLite persists in the `zamfara-data` volume at `/app/App_Data`. The app
container has a read-only root filesystem, a `/tmp` tmpfs, no added Linux
capabilities, and `no-new-privileges`. Back up the existing database volume
before deploying an upgrade.

## Security and proxy configuration

- `AllowedHosts` permits localhost, loopback, `zamfara.org`, and its subdomains.
  Override it for additional deployment domains.
- Only GET and HEAD are accepted. The app sends security headers and a Content
  Security Policy; it has no form submission or email-sending functionality.
- Production enables the generic exception handler, HSTS, and HTTPS redirection.
- The compose stack enables `ZAMFARA_FORWARDEDHEADERS_ENABLED` for its
  TLS-terminating proxy. The app trusts scheme headers only from loopback and
  the Docker bridge range `172.16.0.0/12`. Other proxy networks require updating
  `Infrastructure/ProxyHeaders.cs`. Keep the app inaccessible directly from
  the public network. The old `ASPNETCORE_FORWARDEDHEADERS_ENABLED` automatic
  middleware is explicitly disabled, even if a deployment still sets it.
- `/healthz` returns a simple health response without requiring HTTPS.
