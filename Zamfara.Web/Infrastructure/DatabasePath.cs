namespace Zamfara.Web.Infrastructure;

public static class DatabasePath
{
    // Relative filenames, including a bare "school.db", are rooted in the
    // content directory so their parent is always suitable for CreateDirectory.
    public static string Resolve(string? configuredPath, string contentRoot) =>
        Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("App_Data", "zamfara.db")
            : configuredPath, contentRoot);
}
