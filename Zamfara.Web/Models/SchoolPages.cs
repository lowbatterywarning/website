namespace Zamfara.Web.Models;

/// <summary>
/// Per-page &lt;head&gt; metadata, keyed by controller action name.
/// The meta strings were ported verbatim from the original static HTML
/// &lt;head&gt; sections ({0} = school display name).
/// </summary>
public static class SchoolPages
{
    private static readonly IReadOnlyDictionary<string, PageMeta> Templates =
        new Dictionary<string, PageMeta>
    {
        ["Index"] = new("{0} — Home", "{0} — A public secondary school in Gusau, Zamfara State, Nigeria, established in 1969. Dedicated to STEM education, academic excellence, and character development.", "{0} — Home", "Welcome to {0} — a public secondary school in Gusau, Zamfara State, Nigeria, founded in 1969 and dedicated to STEM education, academic excellence, and preparing students to thrive."),
        ["About"] = new("About — {0}", "About {0} — A public secondary school in Gusau, Zamfara State, Nigeria, established in 1969.", "About {0} — History & Development", "Learn about {0}, founded in 1969 in Gusau, Zamfara State — its history, STEM curriculum, and the recent rehabilitation under Governor Dauda Lawal."),
        ["Academics"] = new("Academics — {0}", "Academics at {0} — Curriculum, programs, and student support.", "Academics at {0} — Curriculum & Programs", "Explore {0}'s comprehensive curriculum, including STEM, arts, athletics, and personalized student support programs."),
        ["Admissions"] = new("Admissions — {0}", "Admissions at {0} — Enrollment process and how to apply.", "Admissions at {0} — Apply & Visit", "Join the {0} family. Learn about our admissions process, important dates, and how to enroll."),
        ["News"] = new("News — {0}", "News and announcements from {0}.", "News — {0}", "Latest news, announcements, and happenings at {0}."),
        ["Gsttp"] = new("GSTTP News — {0}", "Zamfara commences an 18-day Global Standard Teacher Transformation Programme for 250 education personnel.", "Zamfara Commences GSTTP for 250 Education Personnel", "The Zamfara State Ministry of Education, Science and Technology has commenced an 18-day Global Standard Teacher Transformation Programme for 250 teachers, evaluators, and administrators."),
        ["Staff"] = new("Staff — {0}", "Staff & Faculty at {0} — Meet our dedicated team of educators and administrators.", "Staff & Faculty at {0}", "Meet the dedicated faculty and staff at {0}. Our leadership team and passionate teachers are committed to every student's success."),
        ["Calendar"] = new("Calendar — {0}", "{0} Calendar — Upcoming events, holidays, and important dates.", "{0} Academic Calendar", "View {0}'s full academic calendar. Key dates, holidays, events, exam schedules, and important deadlines."),
        ["Gallery"] = new("Gallery — {0}", "Photos of the {0} campus, facilities, and school life.", "Gallery — {0}", "Browse photos of {0} — the campus, classrooms, events, and everyday school life."),
        ["Faq"] = new("FAQ — {0}", "Frequently asked questions about {0}.", "FAQ — {0}", "Answers to frequently asked questions about {0}.")
    };

    /// <summary>Formatted head metadata for a page; defensive fallback for unknown keys.</summary>
    public static PageMeta Get(string page, string schoolName) =>
        Templates.TryGetValue(page, out var meta)
            ? meta.Format(schoolName)
            : new PageMeta($"{schoolName} — {page}", $"{schoolName} school page.",
                $"{schoolName} — {page}", $"Explore {schoolName} on zamfara.org.");
}
