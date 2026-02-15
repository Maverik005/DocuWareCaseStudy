using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;

namespace EventRegistration.Core.Interfaces;
public interface IEventRepository
{
    Task<PagedResponse<Event>> GetEventsAsync(EventSearchParameters searchParameters, CancellationToken cancellationToken = default);
    Task<Event?> GetEventByIdAsync(int eventId, bool includeRegistrations = false, CancellationToken cancellationToken = default);
    Task<Event> CreateEventAsync(Event newEvent, CancellationToken cancellationToken = default);
    Task<Event?> UpdateEventAsync(int id, Action<Event> updateAction, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user is creator of the event
    /// </summary>
    Task<bool> IsCreatorAsync(int eventId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get registration count for an event (optimized query)
    /// </summary>
    Task<int> GetRegistrationCountAsync(int eventId, CancellationToken cancellationToken = default);

    Task<int> GetEventsCountByCreatorAsync(string createdBy,CancellationToken cancellationToken = default);
}
