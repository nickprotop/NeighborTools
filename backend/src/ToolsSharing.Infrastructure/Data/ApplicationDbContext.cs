using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToolsSharing.Core.Entities;
using ToolsSharing.Core.Entities.GDPR;

namespace ToolsSharing.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tool> Tools { get; set; }
    public DbSet<ToolImage> ToolImages { get; set; }
    public DbSet<Rental> Rentals { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    
    // GDPR entities
    public DbSet<UserConsent> UserConsents { get; set; }
    public DbSet<DataProcessingLog> DataProcessingLogs { get; set; }
    public DbSet<DataSubjectRequest> DataSubjectRequests { get; set; }
    public DbSet<CookieConsent> CookieConsents { get; set; }
    public DbSet<PrivacyPolicyVersion> PrivacyPolicyVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure soft delete
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filterMethod = typeof(ApplicationDbContext).GetMethod(nameof(GetSoftDeleteFilter))!
                    .MakeGenericMethod(entityType.ClrType);
                var filter = (LambdaExpression)filterMethod.Invoke(this, new object[] { })!;
                builder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : BaseEntity
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}