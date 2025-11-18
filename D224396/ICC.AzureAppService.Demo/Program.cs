using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

var builder = WebApplication.CreateBuilder(args);

// ✅ Add services (including the custom route) BEFORE Build()
builder.Services
    .AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AddPageRoute("/Index", "Yoshay-22K4396");
        options.Conventions.AddPageRoute("/Architecture", "Yoshay-22K4396/architecture");
        options.Conventions.AddPageRoute("/Services", "Yoshay-22K4396/services");
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
