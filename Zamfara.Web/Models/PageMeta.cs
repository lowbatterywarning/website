namespace Zamfara.Web.Models;

/// <summary>
/// Per-page &lt;head&gt; metadata templates ported from the original static HTML.
/// Each template is a composite format string with {0} = school display name.
/// </summary>
public sealed record PageMeta(string Title, string Description, string OgTitle, string OgDescription)
{
    public PageMeta Format(string schoolName) => new(
        string.Format(Title, schoolName),
        string.Format(Description, schoolName),
        string.Format(OgTitle, schoolName),
        string.Format(OgDescription, schoolName));
}
