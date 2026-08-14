# Zamfara Schools — ASP.NET Core MVC

A port of the original static HTML school-template site into a single ASP.NET
Core MVC project that serves:

- **`/`** — the zamfara.org landing page linking out to three school sites
- **`/school-one`, `/school-two`, `/school-three`** — three school sub-sites,
  each a full copy of the original site's pages (Home, About, Academics,
  Admissions, Staff, Calendar, Contact)

The three schools are identical by construction: they all render the same
shared view set under [`Views/School/`](Zamfara.Web/Views/School), with the
school name substituted from a registry in
[`SchoolSites.cs`](Zamfara.Web/Models/SchoolSites.cs).

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
| `Zamfara.Web/Program.cs` | Pipeline, `schoolSite` route constraint, legacy 301 rewrites |
| `Zamfara.Web/Controllers/HomeController.cs` | zamfara.org landing page |
| `Zamfara.Web/Controllers/SchoolController.cs` | `{site}` routes serving all three schools |
| `Zamfara.Web/Models/SchoolSites.cs` | School registry + per-page head metadata (port of the original `<head>` sections) |
| `Zamfara.Web/Views/Shared/_HomeLayout.cshtml` | Landing-page shell |
| `Zamfara.Web/Views/Shared/_SchoolLayout.cshtml` | Ported school header/footer shell |
| `Zamfara.Web/Views/School/*.cshtml` | The 7 school pages |
| `Zamfara.Web/wwwroot/` | CSS, JS, images, robots.txt, sitemap.xml |
| `legacy-static/` | Untouched snapshot of the original static site |

Legacy URLs redirect (301): `index.html` → `/school-one/`,
`about.html` → `/school-one/about`, etc.

## Adding or renaming schools

Edit the `All` array in [`SchoolSites.cs`](Zamfara.Web/Models/SchoolSites.cs).
Unknown slugs 404 automatically via the `schoolSite` route constraint.

## Assumptions and known placeholders

This is a structural port, not a redesign — the original design, CSS and
markup are preserved. Items carried over from the original site that still
need real values:

- **Contact form** posts to a Formspree placeholder
  (`https://formspree.io/f/YOUR_FORM_ID`); replace `YOUR_FORM_ID` with a real
  Formspree form ID. The AJAX submit handler is in `wwwroot/js/main.js`.
- **Contact details** are placeholders (`[Your City]`, `(555) 123-4567`,
  `info@school.edu`, social `href="#"`).
- **`[IMAGE: …]` comments** mark where real photos go; the referenced image
  files were never present in the original site.
- **Placeholder images** were generated (brand navy/gold) for
  `images/favicon.ico`, `images/apple-touch-icon.png` and
  `images/og-default.jpg` — replace with real artwork when available.
- **Domain** is assumed to be `zamfara.org` in `robots.txt` and `sitemap.xml`.
- **`js/main.js`** nav highlighting was patched to compare full URL paths so
  it works under the `/school-one/...` sub-route structure.

## Build

```bash
dotnet build Zamfara.sln
```
