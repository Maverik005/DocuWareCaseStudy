using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using EventRegistration.Core.ExceptionHandlers;
using EventRegistration.Core.Interfaces;
using EventRegistration.Core.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EventRegistration.Infrastructure.Repositories;
public sealed class RegistrationRepository: IRegistrationRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RegistrationCache _Cache;

    public RegistrationRepository(ApplicationDbContext dbContext, RegistrationCache registrationCache) {  
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _Cache = registrationCache;
    }

    public async Task<PagedResponse<Registration>> GetRegistrationsByEventIdAsync(RegistrationSearchParameters registrationSearchParameters, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Registrations.AsNoTracking()
            .Where(r => r.EventId == registrationSearchParameters.EventId);

        if (!string.IsNullOrWhiteSpace(registrationSearchParameters.SearchTerm))
        {
            var searchTerm = registrationSearchParameters.SearchTerm.Trim().ToLower();
            query = query.Where(
                                r => r.Name.ToLower().Contains(searchTerm) || 
                                r.EmailAddress.ToLower().Contains(searchTerm)
                            );
        }

        query = query.OrderByDescending(r => r.RegisteredAt)
                     .ThenByDescending(r => r.Id);

        if (registrationSearchParameters.LastId.HasValue && registrationSearchParameters.LastRegisteredAt.HasValue)
        {
            // Continue from where previous page ended
            query = query.Where(r =>
                r.RegisteredAt < registrationSearchParameters.LastRegisteredAt.Value ||
                (r.RegisteredAt == registrationSearchParameters.LastRegisteredAt.Value && r.Id < registrationSearchParameters.LastId.Value));
        }

        // Get total count only on first page
        int totalCount;
        if (!registrationSearchParameters.LastId.HasValue)
        {
            if (_Cache.TryGetRegistrationCount(registrationSearchParameters.EventId, out var cachedCount))
            {
                totalCount = cachedCount;
            }
            else
            {
                totalCount = totalCount = await query.CountAsync(cancellationToken);

                _Cache.SetRegistrationCount(registrationSearchParameters.EventId, totalCount);
            }
            
        }
        else
        {
            totalCount = -1;
        }

        query = ApplySorting(query, registrationSearchParameters.SortBy, registrationSearchParameters.SortDescending);

        var eventRegistrations = await query.Take(registrationSearchParameters.PageSize)
                                    .ToListAsync(cancellationToken);

        return new PagedResponse<Registration> { 
            Items = eventRegistrations,
            TotalCount = totalCount,
            PageNumber = registrationSearchParameters.PageNumber,
            PageSize = registrationSearchParameters.PageSize,
            HasNextPage = eventRegistrations.Count == registrationSearchParameters.PageSize,
            HasPreviousPage = registrationSearchParameters.LastId.HasValue
        };
    }

    public async Task<Registration?> GetRegistrationByIdAsync(int registrationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Registrations.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == registrationId); 
    }

    public async Task<Registration> CreateRegistrationAsync(Registration newRegistration, CancellationToken cancellationToken = default)
    {
        var isRegistered = await IsEmailRegisteredAsync(newRegistration.EventId, newRegistration.EmailAddress, cancellationToken);

        if (isRegistered) {
            throw new DuplicateRegistrationException(
                newRegistration.EmailAddress,
                newRegistration.EventId);
        }

        _dbContext.Registrations.Add(newRegistration);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Invalidate count cache
            _Cache.RemoveRegistrationCount(newRegistration.EventId);

            return newRegistration;
        }
        catch(DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Registrations_EventId_Email") == true) {

            // Race condition - another request registered same email simultaneously
            throw new DuplicateRegistrationException(
                newRegistration.EmailAddress,
                newRegistration.EventId);
        }
    }

    public async Task<bool> DeleteRegistrationAsync(int id, CancellationToken cancellationToken = default)
    {
        var registration = await _dbContext.Registrations.FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
            return false;

        registration.IsDeleted = true;
        registration.DeletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Invalidate count cache
        _Cache.RemoveRegistrationCount(registration.EventId);

        return true;
    }

    public async Task<bool> IsEmailRegisteredAsync(
        int eventId,
        string email,
        CancellationToken cancellationToken = default)
    {
        // Optimized query using composite index
        return await _dbContext.Registrations.AsNoTracking().
            AnyAsync(r => r.EventId == eventId && r.EmailAddress == email, cancellationToken);
    }

    /// <summary>
    /// Stream registrations for memory-efficient export of large datasets
    /// </summary>
    public async IAsyncEnumerable<Registration> StreamRegistrationsAsync(
        int eventId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var registrations = _dbContext.Registrations.AsNoTracking()
                            .Where(r => r.EventId == eventId)
                            .OrderBy(r => r.RegisteredAt)
                            .AsAsyncEnumerable()
                            .WithCancellation(cancellationToken);

        await foreach (var registration in registrations)
        {
            yield return registration;
        }
    }

    private static IQueryable<Registration> ApplySorting(
        IQueryable<Registration> query,
        string? sortBy,
        bool descending)
    {
        return sortBy?.ToLower() switch
        {
            "name" => descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),
            "email" => descending
                ? query.OrderByDescending(r => r.EmailAddress)
                : query.OrderBy(r => r.EmailAddress),
            _ => descending
                ? query.OrderByDescending(r => r.RegisteredAt)
                : query.OrderBy(r => r.RegisteredAt)
        };
    }
}
