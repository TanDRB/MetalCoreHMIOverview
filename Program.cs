using MetalCoreHMIOverview.Data;
using MetalCoreHMIOverview.Models.Options;
using MetalCoreHMIOverview.Repositories;
using MetalCoreHMIOverview.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình riêng của từng máy (mật khẩu SQL Server...), không đưa lên git. Xem appsettings.Local.example.json
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// MVC + API
builder.Services.AddControllersWithViews();

// SQL Server (Entity Framework Core)
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Kepware / OPC UA
builder.Services.Configure<OpcUaOptions>(builder.Configuration.GetSection("OpcUa"));

// Repository
builder.Services.AddScoped<ITagDefinitionRepository, TagDefinitionRepository>();
builder.Services.AddScoped<ITagReadingRepository, TagReadingRepository>();

// Service
builder.Services.AddSingleton<IOpcUaClient, OpcUaClient>();
builder.Services.AddSingleton<ITagCache, TagCache>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddHostedService<OpcUaPollingWorker>();

var app = builder.Build();

// Tạo / cập nhật database MetaCoreHMI và nạp danh sách tag ban đầu.
// Nếu SQL Server chưa sẵn sàng thì chỉ ghi log, ứng dụng vẫn chạy.
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
