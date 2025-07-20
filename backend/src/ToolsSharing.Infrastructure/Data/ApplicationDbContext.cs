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
    
    // Payment entities
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Payout> Payouts { get; set; }
    public DbSet<PaymentSettings> PaymentSettings { get; set; }
    
    // GDPR entities
    public DbSet<UserConsent> UserConsents { get; set; }
    public DbSet<DataProcessingLog> DataProcessingLogs { get; set; }
    public DbSet<DataSubjectRequest> DataSubjectRequests { get; set; }
    public DbSet<CookieConsent> CookieConsents { get; set; }
    public DbSet<PrivacyPolicyVersion> PrivacyPolicyVersions { get; set; }
    
    // Fraud detection entities
    public DbSet<FraudCheck> FraudChecks { get; set; }
    public DbSet<SuspiciousActivity> SuspiciousActivities { get; set; }
    public DbSet<VelocityLimit> VelocityLimits { get; set; }
    
    // Dispute management entities
    public DbSet<Dispute> Disputes { get; set; }
    public DbSet<DisputeMessage> DisputeMessages { get; set; }
    public DbSet<DisputeEvidence> DisputeEvidence { get; set; }
    
    // Rental notification entities
    public DbSet<RentalNotification> RentalNotifications { get; set; }
    public DbSet<UserDeviceToken> UserDeviceTokens { get; set; }
    
    // Messaging entities
    public DbSet<Message> Messages { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<MessageAttachment> MessageAttachments { get; set; }
    
    // Favorites entities
    public DbSet<Favorite> Favorites { get; set; }
    
    // Mutual dispute closure entities
    public DbSet<MutualDisputeClosure> MutualDisputeClosures { get; set; }
    public DbSet<MutualClosureAuditLog> MutualClosureAuditLogs { get; set; }

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