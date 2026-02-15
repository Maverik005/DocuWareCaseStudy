using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;

namespace EventRegistration.Core.Interfaces;
public interface IRegistrationRepository
{
    Task<PagedResponse<Registration>> GetRegistrationsByEventIdAsync(RegistrationSearchParameters registrationSearchParameters, CancellationToken cancellationToken = default);
    Task<Registration?> GetRegistrationByIdAsync(int registrationId, CancellationToken cancellationToken = default);
    Task<Registration> CreateRegistrationAsync(Registration newRegistration, CancellationToken cancellationToken = default);
    Task<bool> DeleteRegistrationAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email already registered for event
    /// Uses composite index for fast lookup
    /// </summary>
    Task<bool> IsEmailRegisteredAsync(
        int eventId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch registration export for an event
    /// Uses streaming for memory efficiency with large datasets
    /// </summary>
    IAsyncEnumerable<Registration> StreamRegistrationsAsync(
        int eventId,
        CancellationToken cancellationToken = default);
}
