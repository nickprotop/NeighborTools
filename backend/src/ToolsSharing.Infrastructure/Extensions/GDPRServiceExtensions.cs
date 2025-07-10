using Microsoft.Extensions.DependencyInjection;
using ToolsSharing.Core.Interfaces.GDPR;
using ToolsSharing.Infrastructure.Services.GDPR;

namespace ToolsSharing.Infrastructure.Extensions;

public static class GDPRServiceExtensions
{
    public static IServiceCollection AddGDPRServices(this IServiceCollection services)
    {
        services.AddScoped<IConsentService, ConsentService>();
        services.AddScoped<IDataProcessingLogger, DataProcessingLogger>();
        services.AddScoped<IDataSubjectRightsService, DataSubjectRightsService>();
        services.AddScoped<IDataExportService, DataExportService>();
        services.AddScoped<IPrivacyPolicyService, PrivacyPolicyService>();

        return services;
    }
}