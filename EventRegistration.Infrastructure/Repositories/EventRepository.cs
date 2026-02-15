using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using EventRegistration.Core.Interfaces;
using EventRegistration.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace EventRegistration.Infrastructure.Repositories;
public sealed class EventRepository: IEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PagedResponse<Event>> GetEventsAsync(EventSearchParameters searchParameters, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Events.AsNoTracking();

        // Apply filters using indexed columns
        query = ApplyFilters(query, searchParameters);

        
        // Apply sorting
        query = ApplySorting(query, searchParameters.SortBy, searchParameters.SortDescending);

        if (searchParameters.LastId.HasValue && searchParameters.LastStartTime.HasValue)
        {
            // Seek to position after last item from previous page
            query = query.Where(e =>
                e.StartTime > searchParameters.LastStartTime.Value ||
                (e.StartTime == searchParameters.LastStartTime.Value && e.Id > searchParameters.LastId.Value));
        }

        int totalCount = 0;
        // Get total count (only if first page)
        if (!searchParameters.LastId.HasValue)
        {
            totalCount = await query.CountAsync(cancellationToken);
        }
        else
        {
            totalCount = -1;
        }

        var items = await query
                        .Take(searchParameters.PageSize)
                        .ToListAsync(cancellationToken);

        return new PagedResponse<Event> { 
            Items = items,
            TotalCount = totalCount,
            PageNumber = searchParameters.PageNumber,
            PageSize = searchParameters.PageSize,
            HasNextPage = items.Count == searchParameters.PageSize,
            HasPreviousPage = searchParameters.LastId.HasValue
        };
    }

    public async Task<Event?> GetEventByIdAsync(int eventId, bool includeRegistrations = false, CancellationToken cancellationToken = default)
    {
        if (!includeRegistrations)
        {
            return await CompiledQueries.GetEventById(_dbContext, eventId);
        }

        return await _dbContext.Events.AsNoTracking()
                        .Include(e => e.Registrations)
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<Event> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.Events.Add(newEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return newEvent;
    }

    public async Task<Event?> UpdateEventAsync(int id, Action<Event> updateAction, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (eventEntity == null)
            return null;
        updateAction(eventEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return eventEntity;
    }

    public async Task<bool> DeleteEventAsync(int id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if(eventEntity == null)
            return false;
        eventEntity.IsDeleted = true;
        eventEntity.DeletedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsCreatorAsync(int eventId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events.AsNoTracking().AnyAsync(e => e.Id == eventId && e.CreatedBy == userId, cancellationToken);
    }

    public async Task<int> GetRegistrationCountAsync(int eventId, CancellationToken cancellationToken = default)
    {
        return await CompiledQueries.GetRegistrationCount(_dbContext, eventId);
    }

    private static IQueryable<Event> ApplyFilters(
        IQueryable<Event> query,
        EventSearchParameters parameters)
    {
        query = parameters switch
        {
            { SearchTerm: not null and not "" } p => FilterBySearchTerm(query, p.SearchTerm),
            _ => query
        };

        // Filter by location
        if (!string.IsNullOrWhiteSpace(parameters.Location))
        {
            var location = parameters.Location.Trim();
            query = query.Where(e => EF.Functions.Like(e.Location, $"%{location}%"));
        }

        // Date range filters - uses indexed StartTime column
        if (parameters.StartTimeFrom.HasValue)
        {
            query = query.Where(e => e.StartTime >= parameters.StartTimeFrom.Value);
        }

        if (parameters.StartTimeTo.HasValue)
        {
            query = query.Where(e => e.StartTime <= parameters.StartTimeTo.Value);
        }

        // Filter by creator - uses indexed CreatedBy column
        if (!string.IsNullOrWhiteSpace(parameters.CreatedBy))
        {
            query = query.Where(e => e.CreatedBy == parameters.CreatedBy);
        }

        return query;
    }

    private static IQueryable<Event> FilterBySearchTerm(IQueryable<Event> query, string searchTerm)
    {
        var term = searchTerm.Trim();
        return query.Where(e =>
            EF.Functions.Like(e.Name, $"%{term}%") ||
            EF.Functions.Like(e.Description, $"%{term}%"));
    }

    private static IQueryable<Event> ApplySorting(
        IQueryable<Event> query,
        string? sortBy,
        bool descending)
    {
        return (sortBy?.ToLower(), descending) switch
        {
            ("name", true) => query.OrderByDescending(e => e.Name),
            ("name", false) => query.OrderBy(e => e.Name),
            ("location", true) => query.OrderByDescending(e => e.Location),
            ("location", false) => query.OrderBy(e => e.Location),
            ("endtime", true) => query.OrderByDescending(e => e.EndTime),
            ("endtime", false) => query.OrderBy(e => e.EndTime),
            ("createdat", true) => query.OrderByDescending(e => e.CreatedAt),
            ("createdat", false) => query.OrderBy(e => e.CreatedAt),
            (_, true) => query.OrderByDescending(e => e.StartTime),
            _ => query.OrderBy(e => e.StartTime)
        };
    }

    public async Task<int> GetEventsCountByCreatorAsync(string createdBy, CancellationToken cancellationToken = default)
    {
        return await CompiledQueries.GetEventCountByCreator(_dbContext, createdBy);
    }
}
