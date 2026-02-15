

namespace EventRegistration.Core.Interfaces;
public interface IUnitOfWork : IDisposable
{
    IEventRepository EventRepository { get; }
    IRegistrationRepository RegistrationRepository { get; }

    /// <summary>
    /// Save all changes in a single transaction
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin explicit transaction for complex operations
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
