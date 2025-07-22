using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mapster;
using MapsterMapper;
using Minio;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Features.Auth;
using ToolsSharing.Infrastructure.Features.Tools;
using ToolsSharing.Infrastructure.Features.Rentals;
using ToolsSharing.Infrastructure.Features.Users;
using ToolsSharing.Infrastructure.Features.Settings;
using ToolsSharing.Infrastructure.Features.Messaging;
using ToolsSharing.Infrastructure.Repositories;
using ToolsSharing.Infrastructure.Services;
using ToolsSharing.Infrastructure.Mappings;
using ToolsSharing.Infrastructure.PaymentProviders;
using ToolsSharing.Infrastructure.Security;
using ToolsSharing.Core.Configuration;

namespace ToolsSharing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.Parse("8.0.0-mysql"), // Fixed version instead of AutoDetect to prevent connection issues
                b => {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    b.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null); // Add retry logic for transient failures
                }));

        // Note: Identity is configured in the API project since it requires ASP.NET Core

        // Configure Mapster
        MappingConfig.ConfigureMappings();
        services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        services.AddScoped<IMapper, ServiceMapper>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IToolsService, ToolsService>();
        services.AddScoped<IRentalsService, RentalsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPublicProfileService, PublicProfileService>();
        services.AddScoped<IBundleService, BundleService>();

        // Email Notification Service
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        // Payment Configuration
        services.Configure<PaymentConfiguration>(configuration.GetSection("Payment"));

        // Payment Services
        services.AddHttpClient<PayPalPaymentProvider>();
        services.AddScoped<IPaymentProvider, PayPalPaymentProvider>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Payment Security
        services.AddHttpClient<IPayPalWebhookValidator, PayPalWebhookValidator>();

        // Fraud Detection Configuration
        services.Configure<FraudDetectionConfiguration>(configuration.GetSection("FraudDetection"));
        
        // Fraud Detection Service
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();

        // Payment Status and Communication Services
        services.AddScoped<IPaymentStatusService, PaymentStatusService>();
        services.AddScoped<IPaymentReceiptService, PaymentReceiptService>();

        // Dispute Management Service
        services.AddScoped<IDisputeService, DisputeService>();
        
        // MinIO Configuration and Client
        services.AddSingleton<IMinioClient>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var endpoint = config["MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = config["MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = config["MinIO:SecretKey"] ?? "minioadmin";
            var secure = config.GetValue<bool>("MinIO:Secure", false);
            
            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(secure)
                .Build();
        });
        
        // File Storage Service (now using MinIO)
        services.AddScoped<IFileStorageService, MinIOFileStorageService>();
        
        // MinIO Admin Service
        services.AddScoped<IMinIOAdminService, MinIOAdminService>();
        
        // Dispute Notification Service
        services.AddScoped<IDisputeNotificationService, DisputeNotificationService>();

        // Mobile and SMS Notification Services
        services.AddScoped<IMobileNotificationService, MobileNotificationService>();
        services.AddScoped<ISmsNotificationService, SmsNotificationService>();

        // Content Moderation Configuration
        services.Configure<SightEngineConfiguration>(configuration.GetSection("SightEngine"));
        services.Configure<CascadedModerationConfiguration>(configuration.GetSection("CascadedModeration"));
        
        // Content Moderation Services - Register all services
        services.AddScoped<ContentModerationService>(); // Basic service (always available)
        
        var sightEngineConfig = configuration.GetSection("SightEngine").Get<SightEngineConfiguration>();
        if (sightEngineConfig?.IsConfigured == true)
        {
            services.AddHttpClient<SightEngineService>();
            services.AddScoped<SightEngineService>(); // Register as concrete service
        }
        
        var cascadedConfig = configuration.GetSection("CascadedModeration").Get<CascadedModerationConfiguration>();
        if (cascadedConfig?.EnableCascadedModeration == true && sightEngineConfig?.IsConfigured == true)
        {
            // Use cascaded service (basic + SightEngine)
            services.AddScoped<IContentModerationService, CascadedContentModerationService>();
        }
        else if (sightEngineConfig?.IsConfigured == true)
        {
            // Use SightEngine only
            services.AddScoped<IContentModerationService, SightEngineService>();
        }
        else
        {
            // Use basic service only
            services.AddScoped<IContentModerationService, ContentModerationService>();
        }
        
        services.AddScoped<IMessageService, MessageService>();

        // Favorites Service
        services.AddScoped<IFavoritesService, FavoritesService>();

        // Sample Data Service
        services.AddScoped<ISampleDataService, SampleDataService>();

        // Mutual Dispute Closure Configuration
        services.Configure<MutualClosureConfiguration>(configuration.GetSection("MutualClosure"));

        // Mutual Dispute Closure Services
        services.AddScoped<IMutualClosureConfigurationService, MutualClosureConfigurationService>();
        services.AddScoped<IMutualClosureNotificationService, MutualClosureNotificationService>();
        services.AddScoped<IMutualDisputeClosureService, MutualDisputeClosureService>();

        return services;
    }
}