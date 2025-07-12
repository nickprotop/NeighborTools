using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RazorLight;
using ToolsSharing.Core.Common.Interfaces;

namespace ToolsSharing.Infrastructure.Services.Email;

public class RazorEmailTemplateEngine : IEmailTemplateEngine
{
    private readonly ILogger<RazorEmailTemplateEngine> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;
    private readonly RazorLightEngine _razorEngine;
    private readonly string _templatesPath;

    public RazorEmailTemplateEngine(
        ILogger<RazorEmailTemplateEngine> logger,
        IWebHostEnvironment environment,
        IMemoryCache cache)
    {
        _logger = logger;
        _environment = environment;
        _cache = cache;
        _templatesPath = Path.Combine(_environment.ContentRootPath, "EmailTemplates");
        
        // Initialize RazorLight engine
        _razorEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(_templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"email_template_{templateName}_{model.GetType().Name}";
            
            // Try to get from cache first
            if (_cache.TryGetValue<string>(cacheKey, out var cachedResult))
            {
                _logger.LogDebug("Using cached template for {TemplateName}", templateName);
                return await _razorEngine.CompileRenderStringAsync(cacheKey, cachedResult, model);
            }

            // Load template
            var templatePath = GetTemplatePath(templateName);
            if (!File.Exists(templatePath))
            {
                _logger.LogError("Email template not found: {TemplatePath}", templatePath);
                throw new FileNotFoundException($"Email template '{templateName}' not found");
            }

            var template = await File.ReadAllTextAsync(templatePath, cancellationToken);
            
            // Cache the template
            _cache.Set(cacheKey, template, TimeSpan.FromHours(1));
            
            // Render the template
            var result = await _razorEngine.CompileRenderStringAsync($"template_{templateName}", template, model);
            
            _logger.LogDebug("Rendered email template {TemplateName}", templateName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering email template {TemplateName}", templateName);
            throw;
        }
    }

    public async Task<(string html, string plainText)> RenderWithPlainTextAsync(string templateName, object model, CancellationToken cancellationToken = default)
    {
        var html = await RenderAsync(templateName, model, cancellationToken);
        var plainText = await RenderAsync($"{templateName}.txt", model, cancellationToken);
        
        return (html, plainText);
    }

    public Task<bool> TemplateExistsAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var htmlPath = GetTemplatePath(templateName);
        return Task.FromResult(File.Exists(htmlPath));
    }

    public void ClearCache()
    {
        _cache.Remove("email_template_*");
        _logger.LogInformation("Email template cache cleared");
    }

    private string GetTemplatePath(string templateName)
    {
        // Handle both .cshtml and .txt extensions
        if (templateName.EndsWith(".txt"))
        {
            return Path.Combine(_templatesPath, "PlainText", templateName);
        }
        
        if (!templateName.EndsWith(".cshtml"))
        {
            templateName += ".cshtml";
        }
        
        return Path.Combine(_templatesPath, templateName);
    }
}