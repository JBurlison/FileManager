using WebFileExplorer.Server;
using WebFileExplorer.Server.Configuration;
using WebFileExplorer.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureNetworkBindings(builder.Configuration, builder.Environment.IsDevelopment());

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Configure Explorer Options
builder.Services.Configure<ExplorerOptions>(builder.Configuration.GetSection(ExplorerOptions.SectionName));
builder.Services.AddTransient<IFileSystemProvider, FileSystemProvider>();
builder.Services.AddTransient<IArchiveService, ArchiveService>();
builder.Services.AddTransient<IWindowsShellService, WindowsShellService>();
builder.Services.AddTransient<IRecycleBinService, RecycleBinService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

app.UseBlazorFrameworkFiles();
app.MapStaticAssets();

app.UseMiddleware<AllowedIPMiddleware>();

app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
