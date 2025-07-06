using ToolsSharing.Core.Entities;

namespace ToolsSharing.Core.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Tool> Tools { get; }
    IRepository<Rental> Rentals { get; }
    IRepository<Review> Reviews { get; }
    IRepository<ToolImage> ToolImages { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}