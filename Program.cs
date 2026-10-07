using MetalCoreHMIOverview.Data;
using MetalCoreHMIOverview.Models.Options;
using MetalCoreHMIOverview.Repositories;
using MetalCoreHMIOverview.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json (không đưa lên git) chứa mật khẩu SQL Server, xem appsettings.Local.example.json
builder.Configuration.AddJsonFile(
    new Microsoft.Extensions.FileProviders.PhysicalFileProvider(builder.Environment.ContentRootPath),
    "appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<OpcUaOptions>(builder.Configuration.GetSection("OpcUa"));

builder.Services.AddScoped<ITagDefinitionRepository, TagDefinitionRepository>();
builder.Services.AddScoped<ITagReadingRepository, TagReadingRepository>();

builder.Services.AddSingleton<IOpcUaClient, OpcUaClient>();
builder.Services.AddSingleton<ITagCache, TagCache>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddHostedService<OpcUaPollingWorker>();

var app = builder.Build();

// Nếu SQL Server chưa sẵn sàng thì chỉ ghi log, ứng dụng vẫn chạy
using (var scope = app.Services.CreateScope())
{
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Không thể khởi tạo database MetaCoreHMI");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
