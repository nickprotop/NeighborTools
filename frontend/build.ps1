param(
    [string]$ApiUrl = "http://localhost:5002",
    [string]$Environment = "Development",
    [string]$BuildConfig = "Release"
)

Write-Host "=========================================" -ForegroundColor Blue
Write-Host "Building NeighborTools Frontend" -ForegroundColor Blue
Write-Host "=========================================" -ForegroundColor Blue
Write-Host "API URL: $ApiUrl" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Green
Write-Host "Build Configuration: $BuildConfig" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Blue

# Validate API URL format
if ($ApiUrl -notmatch "^https?://") {
    Write-Host "❌ Error: API URL must start with http:// or https://" -ForegroundColor Red
    Write-Host "   Example: .\build.ps1 -ApiUrl `"https://api.yourapp.com`"" -ForegroundColor Yellow
    exit 1
}

# Create config.json with actual values
Write-Host "📝 Creating config.json..." -ForegroundColor Yellow

$enableAnalytics = if ($Environment -eq "Production") { "true" } else { "false" }

$configContent = @"
{
  "ApiSettings": {
    "BaseUrl": "$ApiUrl",
    "TimeoutSeconds": 30,
    "RetryAttempts": 3
  },
  "Environment": "$Environment",
  "Features": {
    "EnableAdvancedSearch": true,
    "EnableNotifications": true,
    "EnablePayments": true,
    "EnableDisputes": true,
    "EnableAnalytics": $enableAnalytics
  }
}
"@

$configContent | Out-File -FilePath "config.json" -Encoding UTF8

# Copy config to wwwroot so it's served with the app
Write-Host "📁 Copying configuration to wwwroot..." -ForegroundColor Yellow
if (!(Test-Path "wwwroot")) {
    New-Item -ItemType Directory -Path "wwwroot" | Out-Null
}
Copy-Item "config.json" "wwwroot/"

# Build the application
Write-Host "🔨 Building Blazor WebAssembly application..." -ForegroundColor Yellow
dotnet build --configuration $BuildConfig

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Frontend built successfully!" -ForegroundColor Green
    Write-Host "   API URL: $ApiUrl" -ForegroundColor Green
    Write-Host "   Environment: $Environment" -ForegroundColor Green
    Write-Host "   Configuration: wwwroot/config.json" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}