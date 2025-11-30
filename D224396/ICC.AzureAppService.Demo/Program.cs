using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using ICC.AzureAppService.Demo.Services;


var builder = WebApplication.CreateBuilder(args);

// ✅ Register CosmosDbService as a singleton
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
string account = cosmosConfig["Account"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:Account' is required.");
string key = cosmosConfig["Key"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:Key' is required.");
string databaseName = cosmosConfig["DatabaseName"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:DatabaseName' is required.");
string usersContainer = cosmosConfig["UsersContainer"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:UsersContainer' is required.");
string videosContainer = cosmosConfig["VideosContainer"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:VideosContainer' is required.");
string commentsContainer = cosmosConfig["CommentsContainer"] ?? throw new InvalidOperationException("Configuration value 'CosmosDb:CommentsContainer' is required.");

try
{
    builder.Services.AddSingleton(new CosmosDbService(
        account,
        key,
        databaseName,
        usersContainer,
        videosContainer,
        commentsContainer
    ));
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: CosmosDb not available - {ex.Message}");
    builder.Services.AddSingleton<CosmosDbService>(x => null);
}

// ✅ Register BlobStorageService as a singleton
var blobConfig = builder.Configuration.GetSection("BlobStorage");
string blobConnectionString = blobConfig["ConnectionString"] ?? throw new InvalidOperationException("Blob connection string missing");
string blobContainerName = blobConfig["ContainerName"] ?? "videos";

try
{
    builder.Services.AddSingleton(new BlobStorageService(blobConnectionString, blobContainerName));
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: BlobStorage not available - {ex.Message}");
    builder.Services.AddSingleton<BlobStorageService>(x => null);
}

// ✅ Add services (including the custom route) BEFORE Build()
builder.Services
    .AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AddPageRoute("/Index", "videomania");
        options.Conventions.AddPageRoute("/Architecture", "videomania/architecture");
        options.Conventions.AddPageRoute("/Services", "videomania/services");
    });



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

app.Run();
