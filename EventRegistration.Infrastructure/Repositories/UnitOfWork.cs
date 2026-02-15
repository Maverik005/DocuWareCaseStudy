using EventRegistration.Core.Interfaces;
using EventRegistration.Core.Utilities;
using Microsoft.EntityFrameworkCore.Storage;

namespace EventRegistration.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for repository operations as ransactions
/// </summary>
public sealed class UnitOfWork: IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private IDbContextTransaction? _transaction;
    private bool _disposed;
    private RegistrationCache _registrationCache;

    private IEventRepository? _eventRepository;
    private IRegistrationRepository? _registrationRepository;

    public UnitOfWork(ApplicationDbContext dbContext, RegistrationCache regCache)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _registrationCache = regCache;
    }

    public IEventRepository EventRepository
    {
        get
        {
            _eventRepository ??= new EventRepository(_dbContext);
            return _eventRepository;
        }
    }

    public IRegistrationRepository RegistrationRepository
    {
        get
        {
            _registrationRepository ??= new RegistrationRepository(_dbContext, _registrationCache); 
            return _registrationRepository;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) 
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to commit!");
        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null; 
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction to rollback!");
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
        {
            _transaction?.Dispose();
            _dbContext?.Dispose();
        }
        _disposed = true;
    }
}
