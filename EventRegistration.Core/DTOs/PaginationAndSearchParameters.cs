using System.ComponentModel.DataAnnotations;


namespace EventRegistration.Core.DTOs;

/// <summary>
/// Generic paginated response wrapper
/// Essential for handling 100K+ events efficiently
/// </summary>
public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }

    public string? NextCursor { get; init; }
    public string? PreviousCursor { get; init; }
}

/// <summary>
/// Base pagination parameters
/// </summary>
public record PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int PageNumber { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
    
    public int Skip => (PageNumber - 1) * PageSize;
}

/// <summary>
/// Query parameters for event search with filters
/// Optimized for indexed columns
/// </summary>
public sealed record EventSearchParameters : PaginationParameters
{
    /// <summary>
    /// Search in name and description
    /// </summary>
    [StringLength(100)]
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by location
    /// </summary>
    [StringLength(300)]
    public string? Location { get; init; }

    /// <summary>
    /// Filter events starting after this date
    /// Uses indexed StartTime column for performance
    /// </summary>
    public DateTime? StartTimeFrom { get; init; }

    /// <summary>
    /// Filter events starting before this date
    /// </summary>
    public DateTime? StartTimeTo { get; init; }

    /// <summary>
    /// Filter by creator (Azure Entra ID Object ID)
    /// Uses indexed CreatedBy column
    /// </summary>
    [StringLength(36)]
    public string? CreatedBy { get; init; }

    
    /// <summary>
    /// Sort order
    /// </summary>
    public string? SortBy { get; init; } = "StartTime";

    public bool SortDescending { get; init; }

    // Cursor-based pagination
    public int? LastId { get; init; }  // Last event ID from previous page
    public DateTime? LastStartTime { get; init; }  // Last start time from previous page
}

/// <summary>
/// Query parameters for registration search
/// </summary>
public sealed record RegistrationSearchParameters : PaginationParameters
{
    [Range(1, int.MaxValue)]
    public int EventId { get; init; }

    [StringLength(100)]
    public string? SearchTerm { get; init; }

    public string? SortBy { get; init; } = "RegisteredAt";

    public bool SortDescending { get; init; } = true;

    // Cursor-based pagination
    public int? LastId { get; init; }
    public DateTime? LastRegisteredAt { get; init; }
}

