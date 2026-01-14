using Aprs.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aprs.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for coordinating database transactions.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AprsDbContext _context;
    private readonly IPacketRepository _packetRepository;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(AprsDbContext context, IPacketRepository packetRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _packetRepository = packetRepository ?? throw new ArgumentNullException(nameof(packetRepository));
    }

    /// <inheritdoc />
    public IPacketRepository Packets => _packetRepository;

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction has been started.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _context.Dispose();
        _disposed = true;
    }
}
