using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Data;
using Zamfara.Web.Models;

namespace Zamfara.Web.Infrastructure;

/// <summary>
/// Resolves which school (tenant) a request belongs to, from the request Host
/// header: the first label of *.zamfara.org is the school slug. The whole
/// Schools table is tiny (a few hundred rows of a few hundred bytes each), so
/// it is loaded once and cached for the process lifetime. Phase 2 (the CMS)
/// will invalidate this cache when school data is edited.
/// </summary>
public sealed class TenantResolver
{
    private static readonly Regex SlugPattern = new(@"^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.Compiled);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _gate = new();
    private IReadOnlyDictionary<string, School> _bySlug = new Dictionary<string, School>(StringComparer.OrdinalIgnoreCase);
    private School? _defaultSchool;
    private bool _loaded;

    public TenantResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Resolves the school for a request, or null when the request should show
    /// the portal directory (apex domain, unknown host, or unknown slug).
    /// </summary>
    public School? Resolve(string host, string? schoolOverride)
    {
        EnsureLoaded();

        // Dev/convenience override: /?school=slug shows that school regardless of
        // the Host header. Harmless: every site is public content.
        if (!string.IsNullOrEmpty(schoolOverride))
        {
            var slug = schoolOverride.Trim().ToLowerInvariant();
            if (SlugPattern.IsMatch(slug) && _bySlug.TryGetValue(slug, out var overridden))
            {
                return overridden;
            }
            return null;
        }

        // Local development and loopback health checks get the first school.
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.StartsWith("127.", StringComparison.OrdinalIgnoreCase))
        {
            return _defaultSchool;
        }

        // *.zamfara.org -> first label is the school slug.
        const string domain = ".zamfara.org";
        if (host.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
        {
            var slug = host[..^domain.Length];
            if (slug.Equals("www", StringComparison.OrdinalIgnoreCase))
            {
                return null; // portal
            }
            if (SlugPattern.IsMatch(slug) && _bySlug.TryGetValue(slug, out var school))
            {
                return school;
            }
        }

        // Apex domain, unknown subdomains, and direct IP access all fall back to
        // the portal directory, so unknown hosts never see an unowned site.
        return null;
    }

    public IReadOnlyList<School> AllSchools()
    {
        EnsureLoaded();
        return _bySlug.Values.OrderBy(s => s.SortOrder).ToList();
    }

    private void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (_gate)
        {
            if (_loaded)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ZamfaraDbContext>();
            var schools = db.Schools.AsNoTracking().OrderBy(s => s.SortOrder).ToList();
            _bySlug = schools.ToDictionary(s => s.Slug, StringComparer.OrdinalIgnoreCase);
            _defaultSchool = schools.FirstOrDefault();
            _loaded = true;
        }
    }
}
