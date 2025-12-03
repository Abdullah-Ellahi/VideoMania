using Company.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Get connection string from environment or settings
var cosmosConnectionString = builder.Configuration["CosmosDb:ConnectionString"] 
    ?? Environment.GetEnvironmentVariable("COSMOS_DB_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Cosmos DB connection string not found");

var blobConnectionString = builder.Configuration["BlobStorage:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Blob Storage connection string not found");

// Register custom services
builder.Services.AddSingleton<VideoProcessingService>();
builder.Services.AddSingleton<CosmosDbService>(sp => 
    new CosmosDbService(cosmosConnectionString, sp.GetRequiredService<ILogger<CosmosDbService>>()));
builder.Services.AddSingleton<BlobStorageServiceForFunctions>(sp => 
    new BlobStorageServiceForFunctions(blobConnectionString, sp.GetRequiredService<ILogger<BlobStorageServiceForFunctions>>()));

// Configure Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Configure logging
builder.Logging.AddApplicationInsights();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// Log Application Insights configuration status
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (string.IsNullOrEmpty(appInsightsConnectionString))
{
    logger.LogWarning("⚠️ Application Insights connection string is NOT configured. Telemetry will not be sent.");
}
else
{
    logger.LogInformation("✓ Application Insights is configured and ready.");
}

host.Run();
