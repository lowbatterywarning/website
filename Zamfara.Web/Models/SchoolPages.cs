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
        ["Index"] = new("{0} — Home", "{0} — Empowering students for a bright future. A leading institution dedicated to academic excellence and character development.", "{0} — Home", "Welcome to {0} — a leading K-12 institution dedicated to academic excellence, character development, and preparing students to thrive."),
        ["About"] = new("About — {0}", "About {0} — Our mission, history, and core values.", "About {0} — Mission, History & Values", "Learn about {0}'s mission, history, leadership team, and core values. Founded in 1995, we serve 800+ students from K-12."),
        ["Academics"] = new("Academics — {0}", "Academics at {0} — Curriculum, programs, and student support.", "Academics at {0} — Curriculum & Programs", "Explore {0}'s comprehensive K-12 curriculum, including 14 AP courses, STEM, arts, athletics, and personalized student support programs."),
        ["Admissions"] = new("Admissions — {0}", "Admissions at {0} — Enrollment process, tuition, and how to apply.", "Admissions at {0} — Apply & Visit", "Join the {0} family. Learn about our admissions process, tuition and financial aid, important dates, and schedule a campus tour."),
        ["Staff"] = new("Staff — {0}", "Staff & Faculty at {0} — Meet our dedicated team of educators and administrators.", "Staff & Faculty at {0}", "Meet the dedicated faculty and staff at {0}. Our leadership team and passionate teachers are committed to every student's success."),
        ["Calendar"] = new("Calendar — {0}", "{0} Calendar — Upcoming events, holidays, and important dates.", "{0} Academic Calendar 2026–2027", "View {0}'s full academic calendar for 2026–2027. Key dates, holidays, events, exam schedules, and important deadlines."),
        ["Contact"] = new("Contact — {0}", "Contact {0} — Get in touch with our admissions office, faculty, or administration.", "Contact {0} — Get in Touch", "Contact {0} — find our address, phone numbers, office hours, and send us a message. Schedule a campus visit today.")
    };

    /// <summary>Formatted head metadata for a page; defensive fallback for unknown keys.</summary>
    public static PageMeta Get(string page) =>
        Templates.TryGetValue(page, out var meta)
            ? meta.Format(School.Name)
            : new PageMeta($"{School.Name} — {page}", $"{School.Name} school page.",
                $"{School.Name} — {page}", $"Explore {School.Name} on zamfara.org.");
}
