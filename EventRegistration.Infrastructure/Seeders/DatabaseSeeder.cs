using EventRegistration.Infrastructure;
using EventRegistration.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventRegistration.Infrastructure.Seeders;

/// <summary>
/// Seeds database with test data: 100 events and 100 registrations per event
/// </summary>
public static class DatabaseSeeder
{
    private static readonly Random _random = new();

    // User data
    private const string User1Id = "t44XHqDCI7no_Av3f94yXS1k2RIFGz7RI-GjJoklelw";
    private const string User1Name = "One Event";
    private const string User2Id = "TZLu2VWDKJHaDiEnjFjQPe9egHlBStKgDaKtlaNcrNQ";
    private const string User2Name = "eventsadmin";

    // Event categories and locations
    private static readonly string[] EventCategories =
    {
        "Tech Conference", "Workshop", "Meetup", "Seminar", "Hackathon",
        "Training", "Webinar", "Summit", "Forum", "Symposium"
    };

    private static readonly string[] EventTopics =
    {
        ".NET", "Cloud", "AI/ML", "DevOps", "Security", "Frontend",
        "Backend", "Mobile", "Data Science", "Blockchain"
    };

    private static readonly string[] Cities =
    {
        "Berlin", "Munich", "Hamburg", "Frankfurt", "Cologne",
        "Stuttgart", "Düsseldorf", "Dortmund", "Essen", "Leipzig"
    };

    private static readonly string[] Venues =
    {
        "Convention Center", "Tech Hub", "Innovation Lab", "Business Park",
        "Conference Hall", "Community Center", "University Campus", "Hotel Conference Room"
    };

    // Sample names for registrations
    private static readonly string[] FirstNames =
    {
        "Alexander", "Benjamin", "Charlotte", "David", "Emma", "Felix", "Hannah",
        "Isaac", "Julia", "Klaus", "Laura", "Max", "Nina", "Oliver", "Paula",
        "Quinn", "Rebecca", "Stefan", "Tina", "Ulrich", "Vera", "Wilhelm",
        "Xenia", "Yannick", "Zoe", "Anna", "Bruno", "Clara", "Daniel", "Eva"
    };

    private static readonly string[] LastNames =
    {
        "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer", "Wagner",
        "Becker", "Schulz", "Hoffmann", "Koch", "Richter", "Klein", "Wolf",
        "Schröder", "Neumann", "Schwarz", "Zimmermann", "Braun", "Krüger"
    };

    private static readonly string[] EmailDomains =
    {
        "gmail.com", "outlook.com", "yahoo.com", "company.de", "tech.de", "startup.io"
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Check if already seeded
        

        Console.WriteLine("Starting database seeding...");

        var events = GenerateEvents(100);
        await context.Events.AddRangeAsync(events);
        await context.SaveChangesAsync();

        Console.WriteLine($"Created {events.Count} events");

        // Generate registrations for each event
        var allRegistrations = new List<Registration>();
        foreach (var evt in events)
        {
            var registrations = GenerateRegistrations(evt.Id, 100);
            allRegistrations.AddRange(registrations);
        }

        await context.Registrations.AddRangeAsync(allRegistrations);
        await context.SaveChangesAsync();

        Console.WriteLine($"Created {allRegistrations.Count} registrations");
        Console.WriteLine("Database seeding completed!");
    }

    private static List<Event> GenerateEvents(int count)
    {
        var events = new List<Event>();
        var baseDate = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            // Alternate between users (50 each)
            var isUser1 = i < 50;
            var createdBy = isUser1 ? User1Id : User2Id;
            var createdByName = isUser1 ? User1Name : User2Name;

            // Generate different start times
            // Events spread over next 6 months
            var daysOffset = _random.Next(1, 180);
            var startHour = _random.Next(8, 18); // 8 AM to 6 PM
            var startMinute = _random.Next(0, 4) * 15; // 0, 15, 30, 45

            var startTime = baseDate.AddDays(daysOffset)
                .Date
                .AddHours(startHour)
                .AddMinutes(startMinute);

            // Event duration: 1-8 hours
            var durationHours = _random.Next(1, 9);
            var endTime = startTime.AddHours(durationHours);

            // Generate event details
            var category = EventCategories[_random.Next(EventCategories.Length)];
            var topic = EventTopics[_random.Next(EventTopics.Length)];
            var city = Cities[_random.Next(Cities.Length)];
            var venue = Venues[_random.Next(Venues.Length)];

            var evt = new Event
            {
                Name = $"{topic} {category} {i + 1}",
                Description = GenerateDescription(category, topic, city),
                Location = $"{venue}, {city}, Germany",
                StartTime = startTime,
                EndTime = endTime,
                CreatedBy = createdBy,
                CreatedByName = createdByName,
                CreatedAt = baseDate.AddDays(-_random.Next(1, 30)), // Created 1-30 days ago
                IsDeleted = false
            };

            events.Add(evt);
        }

        return events;
    }

    private static string GenerateDescription(string category, string topic, string city)
    {
        var descriptions = new[]
        {
            $"Join us for an exciting {category.ToLower()} focused on {topic}. " +
            $"This event will bring together experts and enthusiasts to discuss the latest trends and best practices. " +
            $"Located in {city}, this is a great opportunity to network with professionals in the field. " +
            $"Agenda includes keynote speeches, hands-on workshops, and panel discussions. " +
            $"Whether you're a beginner or an experienced professional, you'll find valuable insights and practical knowledge. " +
            $"Registration includes access to all sessions, lunch, and networking breaks. " +
            $"Don't miss this opportunity to learn from industry leaders and connect with your peers!",

            $"Experience the future of {topic} at our {category.ToLower()} in {city}. " +
            $"We've assembled top speakers and thought leaders to share their expertise. " +
            $"This immersive event features practical demonstrations, case studies, and interactive sessions. " +
            $"Perfect for developers, architects, and tech enthusiasts looking to expand their knowledge. " +
            $"Attendees will receive certification of participation and exclusive access to presentation materials. " +
            $"Network with like-minded professionals during dedicated networking sessions. " +
            $"Limited seats available - register early to secure your spot!",

            $"Discover cutting-edge {topic} technologies at this comprehensive {category.ToLower()}. " +
            $"Our expert instructors will guide you through real-world scenarios and best practices. " +
            $"Held in the vibrant city of {city}, this event offers both learning and networking opportunities. " +
            $"Topics covered include advanced techniques, tools, and frameworks that industry leaders are using. " +
            $"All skill levels welcome - from beginners to advanced practitioners. " +
            $"Continental breakfast and lunch provided for all participants. " +
            $"Join us and take your skills to the next level!"
        };

        return descriptions[_random.Next(descriptions.Length)];
    }

    private static List<Registration> GenerateRegistrations(int eventId, int count)
    {
        var registrations = new List<Registration>();
        var usedEmails = new HashSet<string>(); // Ensure unique emails per event
        var baseDate = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            string email;
            int attempts = 0;
            string emailFirstName, emailLastName;

            // Generate unique email for this event
            do
            {
                emailFirstName = FirstNames[_random.Next(FirstNames.Length)];
                emailLastName = LastNames[_random.Next(LastNames.Length)];
                var domain = EmailDomains[_random.Next(EmailDomains.Length)];
                var emailPrefix = $"{emailFirstName.ToLower()}.{emailLastName.ToLower()}{_random.Next(1, 1000)}";
                email = $"{emailPrefix}@{domain}";
                attempts++;
            }
            while (usedEmails.Contains(email) && attempts < 10);

            usedEmails.Add(email);

            var regFirstName = FirstNames[_random.Next(FirstNames.Length)];
            var regLastName = LastNames[_random.Next(LastNames.Length)];

            var registration = new Registration
            {
                EventId = eventId,
                Name = $"{regFirstName} {regLastName}",
                EmailAddress = email,
                PhoneNumber = GeneratePhoneNumber(),
                RegisteredAt = baseDate.AddDays(-_random.Next(1, 25)), // Registered 1-25 days ago
                RegistrationSource = GetRandomSource(),
                IsDeleted = false
            };

            registrations.Add(registration);
        }

        return registrations;
    }

    private static string GeneratePhoneNumber()
    {
        // German phone number format
        var areaCode = _random.Next(30, 999);
        var number = _random.Next(1000000, 9999999);
        return $"+49 {areaCode} {number}";
    }

    private static string GetRandomSource()
    {
        var sources = new[] { "Website", "Email", "Social Media", "Direct", "Referral" };
        return sources[_random.Next(sources.Length)];
    }
}