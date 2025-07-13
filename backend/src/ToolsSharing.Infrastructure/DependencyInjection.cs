using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mapster;
using MapsterMapper;
using ToolsSharing.Core.Common.Interfaces;
using ToolsSharing.Core.Interfaces;
using ToolsSharing.Infrastructure.Data;
using ToolsSharing.Infrastructure.Features.Auth;
using ToolsSharing.Infrastructure.Features.Tools;
using ToolsSharing.Infrastructure.Features.Rentals;
using ToolsSharing.Infrastructure.Features.Users;
using ToolsSharing.Infrastructure.Features.Settings;
using ToolsSharing.Infrastructure.Repositories;
using ToolsSharing.Infrastructure.Services;
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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IToolsService, ToolsService>();
        services.AddScoped<IRentalsService, RentalsService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPublicProfileService, PublicProfileService>();

        // Email Notification Service
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();

        return services;
    }
}