using EventRegistration.Core.Entities;
using EventRegistration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventRegistrationTests.Seeders;

public static class TestDataSeeder
{
    private static readonly Random _random = new(42); // Fixed seed for reproducibility

    // User data
    private const string User1Id = "t44XHqDCI7no_Av3f94yXS1k2RIFGz7RI-GjJoklelw";
    private const string User1Name = "One Event";
    private const string User2Id = "TZLu2VWDKJHaDiEnjFjQPe9egHlBStKgDaKtlaNcrNQ";
    private const string User2Name = "eventsadmin";

    // Event data
    private static readonly string[] EventCategories =
    {
        "Conference", "Workshop", "Meetup", "Seminar", "Hackathon",
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

    // Registration data
    private static readonly string[] FirstNames =
    {
        "Alex", "Sam", "Jordan", "Taylor", "Morgan", "Casey", "Drew", "Jamie",
        "Max", "Charlie", "Riley", "Quinn", "Avery", "Sage", "River", "Dakota",
        "Anna", "Bruno", "Clara", "Daniel", "Eva", "Felix", "Hannah", "Isaac"
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller",
        "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Wilson", "Anderson",
        "Müller", "Schmidt", "Schneider", "Fischer", "Weber", "Meyer"
    };

    private static readonly string[] EmailDomains =
    {
        "gmail.com", "outlook.com", "yahoo.com", "email.com", "test.com", "example.org"
    };

    /// <summary>
    /// Seeds events into the database
    /// </summary>
    public static async Task SeedLargeEventDatasetAsync(
        ApplicationDbContext context,
        int eventCount = 10000)
    {
        // Check if already seeded
        if (await context.Events.AnyAsync())
        {
            Console.WriteLine("Events already seeded, skipping...");
            return;
        }

        Console.WriteLine($"Seeding {eventCount:N0} events...");

        var events = new List<Event>();
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var users = new[]
        {
            (User1Id, User1Name),
            (User2Id, User2Name),
            ("user3-oid", "User Three"),
            ("user4-oid", "User Four")
        };

        for (int i = 0; i < eventCount; i++)
        {
            var user = users[i % users.Length];
            var category = EventCategories[_random.Next(EventCategories.Length)];
            var topic = EventTopics[_random.Next(EventTopics.Length)];
            var city = Cities[_random.Next(Cities.Length)];
            var venue = Venues[_random.Next(Venues.Length)];

            // Spread events over a year
            var daysOffset = i % 365;
            var startTime = baseDate.AddDays(daysOffset).AddHours(_random.Next(8, 18));

            events.Add(new Event
            {
                Name = $"{topic} {category} {i + 1}",
                Description = GenerateDescription(category, topic, city),
                Location = $"{venue}, {city}, Germany",
                StartTime = startTime,
                EndTime = startTime.AddHours(_random.Next(2, 8)),
                CreatedBy = user.Item1,
                CreatedByName = user.Item2,
                CreatedAt = baseDate.AddDays(-_random.Next(1, 30)),
                IsDeleted = false
            });

            // Progress indicator
            if ((i + 1) % 1000 == 0)
            {
                Console.WriteLine($"  Generated {i + 1:N0} events...");
            }
        }

        // Save in batches to avoid memory issues
        const int batchSize = 1000;
        for (int i = 0; i < events.Count; i += batchSize)
        {
            var batch = events.Skip(i).Take(batchSize).ToList();
            await context.Events.AddRangeAsync(batch);
            await context.SaveChangesAsync();
            Console.WriteLine($"  Saved batch {(i / batchSize) + 1}/{(events.Count / batchSize)}");
        }

        Console.WriteLine($"✓ Seeded {eventCount:N0} events\n");
    }

    /// <summary>
    /// Seeds registrations for a specific event
    /// </summary>
    public static async Task SeedLargeRegistrationDatasetAsync(
        ApplicationDbContext context,
        int eventId,
        int registrationCount = 100000)
    {
        Console.WriteLine($"Seeding {registrationCount:N0} registrations for event {eventId}...");

        var baseDate = DateTime.UtcNow.AddDays(-30);
        const int batchSize = 10000;

        for (int batch = 0; batch < registrationCount / batchSize; batch++)
        {
            var registrations = new List<Registration>();

            for (int i = 0; i < batchSize; i++)
            {
                var recordNumber = (batch * batchSize) + i;
                var firstName = FirstNames[_random.Next(FirstNames.Length)];
                var lastName = LastNames[_random.Next(LastNames.Length)];
                var domain = EmailDomains[_random.Next(EmailDomains.Length)];

                registrations.Add(new Registration
                {
                    EventId = eventId,
                    Name = $"{firstName} {lastName}",
                    EmailAddress = $"{firstName.ToLower()}.{lastName.ToLower()}.{recordNumber}@{domain}",
                    PhoneNumber = GeneratePhoneNumber(),
                    RegisteredAt = baseDate.AddMinutes(recordNumber),
                    RegistrationSource = "Website",
                    IsDeleted = false
                });
            }

            await context.Registrations.AddRangeAsync(registrations);
            await context.SaveChangesAsync();

            Console.WriteLine($"  Saved batch {batch + 1}/{registrationCount / batchSize} " +
                            $"({(batch + 1) * batchSize:N0} registrations)");
        }

        Console.WriteLine($"✓ Seeded {registrationCount:N0} registrations\n");
    }

    // Helper methods
    private static string GenerateDescription(string category, string topic, string city)
    {
        var templates = new[]
        {
            $"Join us for an exciting {category.ToLower()} focused on {topic}. " +
            $"This event will bring together experts and enthusiasts to discuss the latest trends. " +
            $"Located in {city}, this is a great networking opportunity. " +
            $"Agenda includes keynote speeches, hands-on workshops, and panel discussions.",

            $"Experience the future of {topic} at our {category.ToLower()} in {city}. " +
            $"We've assembled top speakers to share their expertise. " +
            $"This immersive event features practical demonstrations and case studies. " +
            $"Perfect for developers and tech enthusiasts.",

            $"Discover cutting-edge {topic} technologies at this comprehensive {category.ToLower()}. " +
            $"Expert instructors will guide you through real-world scenarios. " +
            $"Held in {city}, this event offers both learning and networking. " +
            $"Continental breakfast and lunch provided."
        };

        return templates[_random.Next(templates.Length)];
    }

    private static string GeneratePhoneNumber()
    {
        return $"+49 {_random.Next(100, 999)} {_random.Next(1000000, 9999999)}";
    }
}
