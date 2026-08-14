# Zamfara Schools — ASP.NET Core MVC

A port of the original static HTML school-template site into a single ASP.NET
Core MVC project. It serves **one generic school site** at the site root:

| Route | Page |
| --- | --- |
| `/` | Home |
| `/about` | About |
| `/academics` | Academics |
| `/admissions` | Admissions |
| `/staff` | Staff |
| `/calendar` | Calendar |
| `/contact` | Contact |

The school name is the `[SCHOOL]` placeholder carried over from the original
template — rebrand by changing one constant in
[`School.cs`](Zamfara.Web/Models/School.cs) (see
[Rebranding the school](#rebranding-the-school)).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Run

```bash
cd Zamfara.Web
dotnet run
```

Then open <http://localhost:5000>. The app resolves its content root (`wwwroot`)
automatically, so it also works when launched from the repository root or by
running the built DLL directly:

```bash
dotnet Zamfara.Web/bin/Debug/net8.0/Zamfara.Web.dll
```

## Structure

| Path | Purpose |
| --- | --- |
| `Zamfara.Web/Program.cs` | Pipeline, literal route table, legacy 301 rewrites |
| `Zamfara.Web/Controllers/HomeController.cs` | One action per page + error page |
| `Zamfara.Web/Models/School.cs` | The `[SCHOOL]` name placeholder (single rebrand point) |
| `Zamfara.Web/Models/SchoolPages.cs` | Per-page head metadata (port of the original `<head>` sections) |
| `Zamfara.Web/Views/Shared/_Layout.cshtml` | Ported school header/footer shell |
| `Zamfara.Web/Views/Home/*.cshtml` | The 7 school pages |
| `Zamfara.Web/wwwroot/` | CSS, JS, images, robots.txt, sitemap.xml |
| `legacy-static/` | Untouched snapshot of the original static site |

Legacy URLs redirect (301, case-insensitive, trailing slashes allowed):
`index.html` → `/`, `about.html` → `/about`, `school-one` → `/`,
`school-two/admissions` → `/admissions`, etc. Unknown legacy paths 404.

## Rebranding the school

Change `School.Name` in [`School.cs`](Zamfara.Web/Models/School.cs) — every
page, title, meta tag and nav item picks the new name up automatically. (The
original template literally used `[SCHOOL]` as the school name, so that is the
default until you set a real one.)

## Assumptions and known placeholders

This is a structural port, not a redesign — the original design, CSS and
markup are preserved. Items carried over from the original site that still
need real values:

- **Contact page is informational only** — there is no form and nothing
  sends email yet; it shows contact details so visitors can reach the school
  by phone, email, or in person.
- **Contact details** are placeholders (`[Your City]`, `(555) 123-4567`,
  `info@school.edu`, social `href="#"`).
- **`[IMAGE: …]` comments** mark where real photos go; the referenced image
  files were never present in the original site.
- **Placeholder images** were generated (brand navy/gold) for
  `favicon.ico`, `images/apple-touch-icon.png` and
  `images/og-default.jpg` — replace with real artwork when available.
- **Domain** is assumed to be `zamfara.org` in `robots.txt` and `sitemap.xml`.
- **`js/main.js`** nav highlighting was patched to compare full URL paths so
  it works under the MVC root-path structure.

## Security hardening

- **Allowed hosts**: `localhost`, loopback, `zamfara.org` and `*.zamfara.org`
  only (in [`appsettings.json`](Zamfara.Web/appsettings.json)) — edit
  `AllowedHosts` to add real domains. Override at runtime with the
  `AllowedHosts` environment variable if needed.
- **Security headers** on every response: `X-Content-Type-Options: nosniff`,
  `X-Frame-Options: DENY`, a strict `Referrer-Policy`, a minimal
  `Permissions-Policy`, and a `Content-Security-Policy` that only allows
  self-hosted scripts/styles, `data:` favicons, and blocks framing, embedding,
  and form submission entirely (matches the info-only site).
- **No `Server` header** — Kestrel is configured not to advertise itself.
- **GET/HEAD only** — every other HTTP verb (TRACE, PUT, POST, DELETE,
  OPTIONS, …) gets an immediate `405 Method Not Allowed`; the site is purely
  informational.
- **Production only** (i.e. not in the `Development` environment): friendly
  exception handler at `/Home/Error` (no exception details are ever rendered),
  HSTS (365 days, subdomains included), and HTTPS redirection.
- **`/healthz`** — no-detail health probe for uptime monitors and Docker
  `HEALTHCHECK`; reachable over plain HTTP even in Production.
- **CSRF**: `AutoValidateAntiforgeryToken` is applied globally so any future
  form action is protected by default.
- **Behind a reverse proxy** (nginx/Caddy/Traefik terminating TLS), set
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so the HTTPS redirection sees the
  real scheme and does not redirect-loop. Only enable this when the app is not
  directly reachable by the public, since it trusts `X-Forwarded-*` headers
  from the connecting peer.

## Build

```bash
dotnet build Zamfara.sln
```

## Running with Docker

```bash
docker build -t zamfara-web .
docker run -d -p 8080:8080 --name zamfara-web zamfara-web
# or with the compose file (read-only root fs, no capabilities, non-root user):
docker compose up -d
```

Then open <http://localhost:8080> (through your TLS proxy in production). The
container:

- runs as the non-root `app` user (uid 1654);
- listens on HTTP port 8080 only — terminate TLS at your reverse proxy or
  load balancer and point it at 8080. The HTTPS port for redirects is assumed
  to be 443 (`ASPNETCORE_HTTPS_PORT`) so plain-HTTP requests get a proper
  `https://` redirect;
- has a built-in `HEALTHCHECK` against `/healthz`;
- sets `ASPNETCORE_ENVIRONMENT=Production`, so HSTS, HTTPS redirection and the
  error page are active.

Public-deployment notes:

- `AllowedHosts` already includes `zamfara.org` and `*.zamfara.org`; if you
  serve the site under other domains, pass `AllowedHosts: "your.domain;…"`
  via the environment.
- If (and only if) TLS is terminated by a proxy in front of the container,
  enable `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` — the compose file already
  does this. Without a proxy, leave it off so spoofed `X-Forwarded-*` headers
  are ignored. When it is on, restrict direct access to the container port at
  the firewall level so only your proxy can send it requests.
- The compose file uses `read_only: true`, `no-new-privileges:true`,
  `cap_drop: ALL`, and a `/tmp` tmpfs; keep these unless you have a concrete
  reason to relax them.
