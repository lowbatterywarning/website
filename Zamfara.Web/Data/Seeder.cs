using Zamfara.Web.Models;

namespace Zamfara.Web.Data;

/// <summary>
/// Idempotent seed: runs at startup. On an empty database it inserts the
/// three seed tenants; on an existing database it refreshes the single
/// school's (GSSS) contact details so a redeploy picks up content changes.
/// demo-one/demo-two are placeholder tenants so the multi-tenant plumbing
/// can be exercised end to end.
/// </summary>
public static class Seeder
{
    // Canonical identity and contact details for the single school, re-applied
    // on every startup so deployed databases stay in sync with the code.
    private const string GsssShortName = "GSSS, Gusau";
    private const string GsssAddress = "P.M.B. 1017, Gada Biyu, Sokoto Road, Gusau 632101, Zamfara";
    private const string GsssPhone = "(905) 421 7903";
    private const string GsssEmail = "qelesh@gmail.com";

    public static void Seed(ZamfaraDbContext db)
    {
        // Existing database: keep the single school's identity and contact
        // details in sync.
        if (db.Schools.FirstOrDefault(s => s.Slug == "gsss") is { } gsssRow)
        {
            gsssRow.ShortName = GsssShortName;
            gsssRow.Address = GsssAddress;
            gsssRow.Phone = GsssPhone;
            gsssRow.Email = GsssEmail;
            db.SaveChanges();
            return;
        }

        if (db.Schools.Any())
        {
            return;
        }

        var gsss = new School
        {
            Slug = "gsss",
            Name = "Government Science Secondary School, Gusau",
            ShortName = GsssShortName,
            Tagline = "Where curious minds grow, character takes root, and every student is known, challenged, and supported.",
            Established = "1969",
            Location = "Gusau, Zamfara State, Nigeria",
            Lga = "Gusau",
            Address = GsssAddress,
            Phone = GsssPhone,
            Email = GsssEmail,
            PrimaryColor = "#1B2A4A",
            AccentColor = "#C9A84C",
            LogoEmoji = "🎓",
            CampusPhoto = "images/campus.jpg",
            SortOrder = 1
        };

        var demoOne = new School
        {
            Slug = "demo-one",
            Name = "Demo Science Secondary School",
            ShortName = "Demo One",
            Tagline = "A placeholder school site showing how every zamfara.org subdomain gets its own content and theme.",
            Established = "1975",
            Location = "Bungudu, Zamfara State, Nigeria",
            Lga = "Bungudu",
            Address = "Bungudu, Zamfara State, Nigeria",
            Phone = "(555) 123-4567",
            Email = "info@school.edu",
            PrimaryColor = "#1F4A3A",
            AccentColor = "#7FB069",
            LogoEmoji = "🏫",
            CampusPhoto = "images/campus.jpg",
            SortOrder = 2
        };

        var demoTwo = new School
        {
            Slug = "demo-two",
            Name = "Demo Comprehensive Secondary School",
            ShortName = "Demo Two",
            Tagline = "A second placeholder school site — same template, different theme colors.",
            Established = "1984",
            Location = "Talata Mafara, Zamfara State, Nigeria",
            Lga = "Talata Mafara",
            Address = "Talata Mafara, Zamfara State, Nigeria",
            Phone = "(555) 123-4567",
            Email = "info@school.edu",
            PrimaryColor = "#3B2634",
            AccentColor = "#D8A455",
            LogoEmoji = "📘",
            CampusPhoto = "images/campus.jpg",
            SortOrder = 3
        };

        db.Schools.AddRange(gsss, demoOne, demoTwo);
        db.SaveChanges();

        foreach (var school in new[] { gsss, demoOne, demoTwo })
        {
            db.NewsPosts.AddRange(
                new NewsPost { SchoolId = school.Id, Title = "New STEM Wing Grand Opening", Body = "Our state-of-the-art science and technology wing opens this fall, featuring wet labs, a makerspace, and a rooftop greenhouse.", DateText = "July 28, 2026", ImageLabel = "Science lab", SortOrder = 1 },
                new NewsPost { SchoolId = school.Id, Title = "Debate Team Wins State Championship", Body = "Congratulations to our varsity debate squad for bringing home the state title for the third consecutive year.", DateText = "July 15, 2026", ImageLabel = "Championship trophy", SortOrder = 2 },
                new NewsPost { SchoolId = school.Id, Title = "Fall Open House — November 15", Body = "Prospective families are invited to tour campus, meet teachers, and experience the school firsthand. Registration is now open.", DateText = "June 30, 2026", ImageLabel = "Open house event", SortOrder = 3 });

            db.CalendarEvents.AddRange(
                new CalendarEvent { SchoolId = school.Id, DateText = "Aug 15, 2026", Title = "First Day of School", Category = "Academic", Details = "All grades; early dismissal K-5 at 12:00 PM", DayText = "15", MonthText = "Aug", IsFeatured = true, FeaturedText = "All grades return. Early dismissal at 12:00 PM for K-5. Welcome coffee for new families at 8:15 AM in the Commons.", SortOrder = 1 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Sep 4, 2026", Title = "Labor Day", Category = "Holiday", Details = "Campus closed", DayText = "04", MonthText = "Sep", SortOrder = 2 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Sep 22, 2026", Title = "Back to School Night", Category = "Event", Details = "6:00 PM – 8:00 PM, all divisions", DayText = "22", MonthText = "Sep", IsFeatured = true, FeaturedText = "Parents and guardians meet teachers and learn about this year's curriculum. 6:00 PM – 8:00 PM. Childcare provided.", SortOrder = 3 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Oct 9, 2026", Title = "Fall Festival", Category = "Event", Details = "11:00 AM – 3:00 PM, main field", DayText = "09", MonthText = "Oct", IsFeatured = true, FeaturedText = "Family-friendly activities, food trucks, games, and alumni soccer match. 11:00 AM – 4:00 PM on the main field.", SortOrder = 4 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Oct 20, 2026", Title = "Parent-Teacher Conferences", Category = "Academic", Details = "No classes; scheduled appointments", DayText = "20", MonthText = "Oct", SortOrder = 5 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Nov 15, 2026", Title = "Fall Open House", Category = "Event", Details = "10:00 AM – 1:00 PM, prospective families", DayText = "15", MonthText = "Nov", IsFeatured = true, FeaturedText = "Prospective families tour campus, visit classes, and meet faculty and students. 10:00 AM – 1:00 PM. Registration required.", SortOrder = 6 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Nov 22–26, 2026", Title = "Thanksgiving Break", Category = "Holiday", Details = "No school; classes resume Nov 29", DayText = "22", MonthText = "Nov", SortOrder = 7 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Dec 15–17, 2026", Title = "Upper School Fall Exams", Category = "Exams", Details = "Half-day schedule, Grades 9-12", DayText = "15", MonthText = "Dec", SortOrder = 8 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Dec 20, 2026 – Jan 2, 2027", Title = "Winter Break", Category = "Holiday", Details = "No school; classes resume Jan 3", DayText = "20", MonthText = "Dec", SortOrder = 9 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Jan 16, 2027", Title = "Martin Luther King Jr. Day", Category = "Holiday", Details = "Campus closed", DayText = "16", MonthText = "Jan", SortOrder = 10 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Mar 13–17, 2027", Title = "Spring Break", Category = "Holiday", Details = "No school", DayText = "13", MonthText = "Mar", SortOrder = 11 },
                new CalendarEvent { SchoolId = school.Id, DateText = "May 22–24, 2027", Title = "Upper School Spring Exams", Category = "Exams", Details = "Grades 9-12", DayText = "22", MonthText = "May", SortOrder = 12 },
                new CalendarEvent { SchoolId = school.Id, DateText = "May 26, 2027", Title = "Senior Graduation", Category = "Event", Details = "4:00 PM ceremony, main auditorium", DayText = "26", MonthText = "May", SortOrder = 13 },
                new CalendarEvent { SchoolId = school.Id, DateText = "Jun 1, 2027", Title = "Last Day of School", Category = "Academic", Details = "Half-day; K-8 closing ceremony 10:00 AM", DayText = "01", MonthText = "Jun", SortOrder = 14 });

            db.GalleryItems.AddRange(
                new GalleryItem { SchoolId = school.Id, Title = "School entrance sign", Caption = "The main entrance welcomes students and visitors.", ImagePath = "images/campus.jpg", SortOrder = 1 },
                new GalleryItem { SchoolId = school.Id, Title = "Renovated school campus", Caption = "Classrooms and grounds after the rehabilitation project.", ImagePath = "images/renovated-campus.jpg", SortOrder = 2 });

            db.FaqItems.AddRange(
                new FaqItem { SchoolId = school.Id, Question = "What are the age cutoffs for each grade?", Answer = "For Kindergarten, children must turn 5 by September 1 of the entry year. For all other grades, age-appropriate placement is determined during the admissions assessment.", SortOrder = 1 },
                new FaqItem { SchoolId = school.Id, Question = "Does the school offer transportation?", Answer = "We offer bus service from several surrounding communities. Routes and fees vary by location. Carpool matching is also available for interested families.", SortOrder = 2 },
                new FaqItem { SchoolId = school.Id, Question = "Is before- and after-school care available?", Answer = "Yes. Our Extended Day Program runs from 7:00 AM to 6:00 PM for Lower School students, with supervised homework time, activities, and outdoor play. Middle School students have a supervised study hall until 5:30 PM.", SortOrder = 3 },
                new FaqItem { SchoolId = school.Id, Question = "What is the student-teacher ratio?", Answer = "Our average student-teacher ratio is 12:1, with maximum class sizes of 18 in Lower School and 20 in Middle and Upper School.", SortOrder = 4 },
                new FaqItem { SchoolId = school.Id, Question = "Does the school accept mid-year transfers?", Answer = "We consider mid-year transfers on a case-by-case basis, depending on space availability and the student's academic readiness. Please contact admissions directly to discuss your situation.", SortOrder = 5 });
        }

        db.SaveChanges();
    }
}
