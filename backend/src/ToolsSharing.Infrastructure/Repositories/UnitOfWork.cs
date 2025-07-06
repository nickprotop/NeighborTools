using Microsoft.EntityFrameworkCore.Storage;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Entities;
using ToolsSharing.Infrastructure.Data;

namespace ToolsSharing.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Tools = new Repository<Tool>(_context);
        Rentals = new Repository<Rental>(_context);
        Reviews = new Repository<Review>(_context);
        ToolImages = new Repository<ToolImage>(_context);
    }

    public IRepository<Tool> Tools { get; private set; }
    public IRepository<Rental> Rentals { get; private set; }
    public IRepository<Review> Reviews { get; private set; }
    public IRepository<ToolImage> ToolImages { get; private set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}