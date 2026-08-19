using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Data;
using Zamfara.Web.Models;

namespace Zamfara.Web.Infrastructure;

/// <summary>
/// Resolves which school (tenant) a request belongs to, from the request Host
/// header. In single-school mode only the apex domain (zamfara.org) and the
/// loopback host are owned, so the whole site is the default school. Every
/// other host resolves to null (404). The Schools table is tiny (a few
/// hundred bytes per row), so it is loaded once and cached for the process
/// lifetime.
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
    /// Resolves the school for a request, or null when the request is unowned
    /// and should be answered with a 404. Owned hosts are the apex domain
    /// (zamfara.org, the single school site) and the loopback address used for
    /// local development and health checks.
    /// </summary>
    public School? Resolve(string host, string? schoolOverride)
    {
        EnsureLoaded();

        // Dev/convenience override: /?school=slug previews that school. Only
        // honored on the loopback host so the live site can never be switched
        // away from the single school.
        if (!string.IsNullOrEmpty(schoolOverride) && IsLoopback(host))
        {
            var slug = schoolOverride.Trim().ToLowerInvariant();
            if (SlugPattern.IsMatch(slug) && _bySlug.TryGetValue(slug, out var overridden))
            {
                return overridden;
            }
            return null;
        }

        // Local development and loopback health checks get the default school.
        if (IsLoopback(host))
        {
            return _defaultSchool;
        }

        // Single-school mode: the apex domain is the site. www.zamfara.org is
        // redirected to the apex by TenantMiddleware; every other host (school
        // subdomains, unknown hosts, direct IPs) is unowned -> 404.
        if (host.Equals("zamfara.org", StringComparison.OrdinalIgnoreCase))
        {
            return _defaultSchool;
        }

        return null;
    }

    private static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.StartsWith("127.", StringComparison.OrdinalIgnoreCase);

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
