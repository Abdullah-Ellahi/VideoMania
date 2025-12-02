using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddControllers(); // ✅ You have this

// Configure form options for large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000; // 500MB
});

// ✅ Register CosmosDbService as a singleton
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
string account = cosmosConfig["Account"] ?? "";
string key = cosmosConfig["Key"] ?? "";
string databaseName = cosmosConfig["DatabaseName"] ?? "videomania";
string usersContainer = cosmosConfig["UsersContainer"] ?? "Users";
string videosContainer = cosmosConfig["VideosContainer"] ?? "Videos";
string commentsContainer = cosmosConfig["CommentsContainer"] ?? "Comments";

if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(key))
{
    Console.WriteLine("Warning: CosmosDb credentials not configured - using null service");
    builder.Services.AddSingleton<CosmosDbService>(sp => null!);
}
else
{
    try
    {
        // Register CosmosDbService with logger
        builder.Services.AddSingleton<CosmosDbService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<CosmosDbService>>();

            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(key))
            {
                Console.WriteLine("Warning: CosmosDb credentials not configured - using null service");
                return null!;
            }

            try
            {
                return new CosmosDbService(
                    account,
                    key,
                    databaseName,
                    usersContainer,
                    videosContainer,
                    commentsContainer,
                    logger   // 👈 THIS IS THE MISSING PARAMETER
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: CosmosDb not available - {ex.Message}");
                return null!;
            }
        });

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: CosmosDb not available - {ex.Message}");
        builder.Services.AddSingleton<CosmosDbService>(sp => null!);
    }
}

// ✅ Register BlobStorageService as a singleton
var blobConfig = builder.Configuration.GetSection("BlobStorage");
string blobConnectionString = blobConfig["ConnectionString"] ?? "";
string blobContainerName = blobConfig["ContainerName"] ?? "videos";

if (string.IsNullOrEmpty(blobConnectionString))
{
    Console.WriteLine("Warning: BlobStorage connection string not configured - using null service");
    builder.Services.AddSingleton<BlobStorageService>(sp => null!);
}
else
{
    try
    {
        builder.Services.AddSingleton(new BlobStorageService(blobConnectionString, blobContainerName));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: BlobStorage not available - {ex.Message}");
        builder.Services.AddSingleton<BlobStorageService>(sp => null!);
    }
}

// ✅ Add Razor Pages with custom routes
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

app.UseAuthorization(); // Good to have this for future auth

app.MapRazorPages();
app.MapControllers(); // ⚠️ THIS WAS MISSING - ADD THIS LINE!

app.Run();