using System.Collections.Frozen;

namespace EventRegistration.Core.Utilities;
public static class EventValidationUtility
{
    private static readonly FrozenSet<string> ValidLocationPrefixes = new[]
    {
        "Convention Center", "Tech Hub", "Innovation Lab", "Business Park",
        "Conference Hall", "Community Center", "University Campus", "Hotel",
        "Online", "Virtual", "Remote"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static (bool IsValid, string? ErrorMessage) IsValidLocation(string location)
    {
        if(string.IsNullOrWhiteSpace(location)) return (false, "Event location cannot be empty");

        // Check if location starts with valid prefix
        var hasValidPrefix = ValidLocationPrefixes.Any(prefix =>
            location.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (!hasValidPrefix && !location.Contains(','))
            return (false, "Event location should include a recognizable venue or address");

        return (true,null);
    }

    /// <summary>
    /// Validates user hasn't exceeded event creation limit
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateEventCreationLimit(
        int currentEventCount,
        int maxEventsPerUser)
    {
        if (currentEventCount >= maxEventsPerUser)
            return (false, $"You have reached the maximum limit of {maxEventsPerUser} events per user");

        return (true, null);
    }

    /// <summary>
    /// Validates event hasn't exceeded registration limit
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateRegistrationLimit(
        int currentRegistrationCount,
        int maxRegistrationsPerEvent)
    {
        if (currentRegistrationCount >= maxRegistrationsPerEvent)
            return (false, $"This event has reached the maximum capacity of {maxRegistrationsPerEvent} registrations");

        return (true, null);
    }
}
