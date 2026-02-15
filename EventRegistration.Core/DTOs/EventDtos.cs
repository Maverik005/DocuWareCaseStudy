using System.ComponentModel.DataAnnotations;

namespace EventRegistration.Core.DTOs;
public sealed record CreateEventRequest
{
    [Required(ErrorMessage = "Event name is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Event name must be between 3 and 200 characters")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public required string Description { get; init; }

    [Required(ErrorMessage = "Location is required")]
    [StringLength(300, ErrorMessage = "Location must not exceed 300 characters")]
    public required string Location { get; init; }

    [Required(ErrorMessage = "Start time is required")]
    public DateTime StartTime { get; init; }

    [Required(ErrorMessage = "End time is required")]
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Custom validation - validates end time is after start time
    /// </summary>
    public bool IsValid(out string? errorMessage)
    {
        if (EndTime <= StartTime)
        {
            errorMessage = "End time must be after start time";
            return false;
        }

        if (StartTime < DateTime.UtcNow)
        {
            errorMessage = "Start time cannot be in the past";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

public sealed record UpdateEventRequest
{
    [StringLength(200, MinimumLength = 3)]
    public string? Name { get; init; }

    [StringLength(2000, MinimumLength = 10)]
    public string? Description { get; init; }

    [StringLength(300)]
    public string? Location { get; init; }

    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
}

public sealed record EventResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Location { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string? CreatedBy { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime CreatedAt { get; init; }
    public int RegistrationCount { get; init; }
}

/// <summary>
/// Lightweight DTO for list views - excludes description for better memory usage
/// When showing 100K events, this saves significant memory
/// </summary>
public sealed record EventSummaryResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Location { get; init; }
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    public int RegistrationCount { get; init; }
}


