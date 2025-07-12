using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mapster;
using MapsterMapper;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Common.Models;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Features.Auth;
using ToolsSharing.Infrastructure.Features.Tools;
using ToolsSharing.Infrastructure.Features.Rentals;
using ToolsSharing.Infrastructure.Features.Users;
using ToolsSharing.Infrastructure.Features.Settings;
using ToolsSharing.Infrastructure.Repositories;
using ToolsSharing.Infrastructure.Services;
using ToolsSharing.Infrastructure.Services.Email;
using ToolsSharing.Infrastructure.Services.Email.Providers;
using ToolsSharing.Infrastructure.Mappings;

namespace ToolsSharing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection")),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

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
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IToolsService, ToolsService>();
        services.AddScoped<IRentalsService, RentalsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPublicProfileService, PublicProfileService>();

        // Email Notification System
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        
        // Register email template engine
        services.AddSingleton<IEmailTemplateEngine, RazorEmailTemplateEngine>();
        
        // Register email providers based on configuration
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>();
        switch (emailSettings?.Provider ?? EmailProvider.Smtp)
        {
            case EmailProvider.Smtp:
                services.AddScoped<IEmailProvider, SmtpEmailProvider>();
                break;
            case EmailProvider.SendGrid:
                services.AddScoped<IEmailProvider, SendGridEmailProvider>();
                break;
            case EmailProvider.Mailgun:
                // TODO: Implement MailgunEmailProvider
                services.AddScoped<IEmailProvider, SmtpEmailProvider>(); // Fallback to SMTP
                break;
            case EmailProvider.AmazonSes:
                // TODO: Implement AmazonSesEmailProvider
                services.AddScoped<IEmailProvider, SmtpEmailProvider>(); // Fallback to SMTP
                break;
            default:
                services.AddScoped<IEmailProvider, SmtpEmailProvider>();
                break;
        }
        
        // Register email notification service
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        
        // Register email queue processor as a hosted service if queue is enabled
        if (emailSettings?.EnableQueue ?? true)
        {
            services.AddHostedService<EmailQueueProcessor>();
            services.AddSingleton<IEmailQueueProcessor>(provider => 
                provider.GetServices<IHostedService>()
                    .OfType<EmailQueueProcessor>()
                    .FirstOrDefault()!);
        }

        return services;
    }
}