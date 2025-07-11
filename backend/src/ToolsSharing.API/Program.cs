using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ToolsSharing.Infrastructure;
using ToolsSharing.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs to bind to all interfaces
builder.WebHost.UseUrls("https://0.0.0.0:5003", "http://0.0.0.0:5002");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddInfrastructure(builder.Configuration);

// Add GDPR services
builder.Services.AddGDPRServices();

// Add Identity
builder.Services.AddIdentity<ToolsSharing.Core.Entities.User, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ToolsSharing.Infrastructure.Data.ApplicationDbContext>();

// Add Controllers
builder.Services.AddControllers();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Tools Sharing API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ToolsSharing.Infrastructure.Data.ApplicationDbContext>();

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tools Sharing API v1"));
}

app.UseCors("AllowFrontend");
// DEVELOPMENT ONLY: HTTPS redirection disabled for local development with self-signed certificates
// PRODUCTION WARNING: Enable HTTPS redirection in production or when behind a proxy that handles SSL
// app.UseHttpsRedirection();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapReverseProxy();

// Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            Success = false,
            Message = "An error occurred while processing your request.",
            Data = (object?)null,
            Errors = new List<string>()
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    });
});

try
{
    // Check for --seed-only argument
    if (args.Contains("--seed-only"))
    {
        Log.Information("Running in seed-only mode");
        
        // Run migrations and seed the database, then exit
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ToolsSharing.Infrastructure.Data.ApplicationDbContext>();
            
            // Apply any pending migrations
            Log.Information("Applying database migrations...");
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
            
            // Seed the database
            await ToolsSharing.Infrastructure.Data.DataSeeder.SeedAsync(scope.ServiceProvider);
        }
        
        Log.Information("Database seeding completed. Exiting.");
        return;
    }
    
    Log.Information("Starting Tools Sharing API");
    
    // Run database migrations and seed the database
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ToolsSharing.Infrastructure.Data.ApplicationDbContext>();
        
        // Apply any pending migrations
        Log.Information("Applying database migrations...");
        await context.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
        
        // Seed the database
        await ToolsSharing.Infrastructure.Data.DataSeeder.SeedAsync(scope.ServiceProvider);
    }
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}