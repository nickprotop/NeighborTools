using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToolsSharing.Core.Entities;

namespace ToolsSharing.Infrastructure.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.HasKey(us => us.Id);
        
        builder.Property(us => us.UserId)
            .IsRequired()
            .HasMaxLength(450);
        
        builder.Property(us => us.Theme)
            .HasMaxLength(20)
            .HasDefaultValue("system");
            
        builder.Property(us => us.Language)
            .HasMaxLength(10)
            .HasDefaultValue("en");
            
        builder.Property(us => us.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("USD");
            
        builder.Property(us => us.TimeZone)
            .HasMaxLength(100)
            .HasDefaultValue("UTC");
            
        builder.Property(us => us.DefaultDepositPercentage)
            .HasColumnType("decimal(5,4)")
            .HasDefaultValue(0.20m);
            
        builder.Property(us => us.RentalLeadTime)
            .HasDefaultValue(24);
            
        builder.Property(us => us.SessionTimeoutMinutes)
            .HasDefaultValue(480);
        
        // Set default values for boolean properties
        builder.Property(us => us.ShowProfilePicture).HasDefaultValue(true);
        builder.Property(us => us.ShowRealName).HasDefaultValue(true);
        builder.Property(us => us.ShowLocation).HasDefaultValue(true);
        builder.Property(us => us.ShowPhoneNumber).HasDefaultValue(false);
        builder.Property(us => us.ShowEmail).HasDefaultValue(false);
        builder.Property(us => us.ShowStatistics).HasDefaultValue(true);
        
        builder.Property(us => us.EmailRentalRequests).HasDefaultValue(true);
        builder.Property(us => us.EmailRentalUpdates).HasDefaultValue(true);
        builder.Property(us => us.EmailMessages).HasDefaultValue(true);
        builder.Property(us => us.EmailMarketing).HasDefaultValue(false);
        builder.Property(us => us.EmailSecurityAlerts).HasDefaultValue(true);
        builder.Property(us => us.PushMessages).HasDefaultValue(true);
        builder.Property(us => us.PushReminders).HasDefaultValue(true);
        builder.Property(us => us.PushRentalRequests).HasDefaultValue(true);
        builder.Property(us => us.PushRentalUpdates).HasDefaultValue(true);
        
        builder.Property(us => us.AutoApproveRentals).HasDefaultValue(false);
        builder.Property(us => us.RequireDeposit).HasDefaultValue(true);
        builder.Property(us => us.TwoFactorEnabled).HasDefaultValue(false);
        builder.Property(us => us.LoginAlertsEnabled).HasDefaultValue(true);
        
        builder.Property(us => us.AllowDirectMessages).HasDefaultValue(true);
        builder.Property(us => us.AllowRentalInquiries).HasDefaultValue(true);
        builder.Property(us => us.ShowOnlineStatus).HasDefaultValue(true);
        
        // Unique constraint on UserId
        builder.HasIndex(us => us.UserId)
            .IsUnique();
        
        // Foreign key relationship
        builder.HasOne(us => us.User)
            .WithOne(u => u.Settings)
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}