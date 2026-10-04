using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zamfara.Web.Data;
using Zamfara.Web.Infrastructure;

namespace Zamfara.RegressionTests;

internal static class Program
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    public static async Task Main()
    {
        try { await Run(); }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            Environment.ExitCode = 1;
        }
    }

    private static async Task Run()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "zamfara-path-tests"));
        var bare = DatabasePath.Resolve("school.db", root);
        Check(bare == Path.Combine(root, "school.db"), "Bare database filename did not resolve under content root.");
        Check(!string.IsNullOrEmpty(Path.GetDirectoryName(bare)), "Database directory is empty.");
        Check(DatabasePath.Resolve(null, root) == Path.Combine(root, "App_Data", "zamfara.db"), "Default database path changed.");
        Check(DatabasePath.Resolve(bare, root) == bare, "Absolute database filename changed.");
        var special = DatabasePath.Resolve("school;test.db", root);
        var connectionString = new SqliteConnectionStringBuilder { DataSource = special }.ToString();
        Check(new SqliteConnectionStringBuilder(connectionString).DataSource == special, "Database filename broke connection-string parsing.");

        // Force failure after the schools have been saved but before content
        // is saved. The database must be empty afterward and safe to retry.
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var interceptor = new FailSecondSave();
        var options = new DbContextOptionsBuilder<ZamfaraDbContext>()
            .UseSqlite(connection).AddInterceptors(interceptor).Options;
        using var db = new ZamfaraDbContext(options);
        db.Database.EnsureCreated();
        var failed = false;
        try { Seeder.Seed(db); }
        catch (InvalidOperationException ex) when (ex.Message == "Injected content save failure") { failed = true; }
        Check(failed, "Seed failure was not injected.");
        db.ChangeTracker.Clear();
        Check(!db.Schools.Any() && !db.NewsPosts.Any(), "Failed seed left partial data behind.");
        Seeder.Seed(db);
        Check(db.Schools.Count() == 3 && db.NewsPosts.Count() == 9 &&
            db.CalendarEvents.Count() == 42 && db.GalleryItems.Count() == 6 && db.FaqItems.Count() == 15,
            "Retry did not seed complete content.");
        Seeder.Seed(db);
        Check(db.Schools.Count() == 3 && db.NewsPosts.Count() == 9, "Repeated seed duplicated data.");

        foreach (var (address, trusted) in new[]
        {
            ("127.0.0.1", true), ("::1", true), ("172.18.0.2", true),
            ("203.0.113.9", false), ("10.0.0.2", false)
        })
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(address);
            context.Request.Scheme = "http";
            context.Request.Headers["X-Forwarded-Proto"] = "https";
            context.Request.Headers["X-Forwarded-For"] = "192.0.2.1";
            var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
                NullLoggerFactory.Instance, Options.Create(ProxyHeaders.CreateOptions()));
            await middleware.Invoke(context);
            Check(context.Request.Scheme == (trusted ? "https" : "http"), $"Incorrect proxy trust for {address}.");
            Check(context.Connection.RemoteIpAddress.Equals(IPAddress.Parse(address)), "Client IP header was incorrectly trusted.");
        }
        Console.WriteLine("Database-path, seed rollback/retry, and trusted/untrusted proxy checks passed.");
    }

    private sealed class FailSecondSave : SaveChangesInterceptor
    {
        private int _saves;
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (++_saves == 2) throw new InvalidOperationException("Injected content save failure");
            return result;
        }
    }
}
