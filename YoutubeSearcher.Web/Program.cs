using YoutubeSearcher.Web.Services;
using YoutubeSearcher.Web.Hubs;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation(); // This requires the Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation package

// Register SignalR
builder.Services.AddSignalR();

// Register our services
builder.Services.AddSingleton<YoutubeService>();
builder.Services.AddSingleton<DownloadService>();
builder.Services.AddSingleton<SearchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<SearchHub>("/searchHub");

app.Run();
