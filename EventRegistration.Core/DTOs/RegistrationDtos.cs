using System.ComponentModel.DataAnnotations;
namespace EventRegistration.Core.DTOs;


public sealed record CreateRegistrationRequest
{

    [Required(ErrorMessage = "Event ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid event ID")]
    public int EventId { get; init; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public required string Name { get; init; }

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters")]
    public required string PhoneNumber { get; init; }

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(256, ErrorMessage = "Email address must not exceed 256 characters")]
    public required string EmailAddress { get; init; }

    /// <summary>
    /// Optional field to capture where the registration came from (e.g. website, mobile app, etc.)
    /// Useful for analytics
    /// </summary>
    [StringLength(50, ErrorMessage = "Registration source must not exceed 50 characters")]
    public string? RegistrationSource { get; init; }
}

public sealed record UpdateRegistrationRequest
{
    [StringLength(200, MinimumLength = 2)]
    public string? Name { get; init; }
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; init; }
    [EmailAddress]
    [StringLength(256)]
    public string? EmailAddress { get; init; }
    [StringLength(50)]
    public string? RegistrationSource { get; init; }
}

public sealed record RegistrationResponse
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public string Name { get; init; } = default!;
    public string PhoneNumber { get; init; } = default!;
    public string EmailAddress { get; init; } = default!;
    public DateTime RegisteredAt { get; init; }
    public string? RegistrationSource { get; init; }
}

/// <summary>
/// Lightweight summary for registration lists
/// Excludes phone number for privacy in list views
/// </summary>
public sealed record RegistrationSummaryResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string EmailAddress { get; init; }
    public required DateTime RegisteredAt { get; init; }
}
