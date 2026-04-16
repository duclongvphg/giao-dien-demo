using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using giao_dien_demo.Data;
using giao_dien_demo.Hubs;   // 👈 thêm hub

var builder = WebApplication.CreateBuilder(args);

// ===== ADD SERVICES =====
builder.Services.AddControllersWithViews();

// 🔥 SESSION (GIỮ NGUYÊN)
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔥 DB CONTEXT (GIỮ NGUYÊN)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔥 SIGNALR REALTIME (THÊM MỚI - KHÔNG ẢNH HƯỞNG CODE CŨ)
builder.Services.AddSignalR();

var app = builder.Build();

// ===== ERROR HANDLING =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ===== MIDDLEWARE =====
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔥 SESSION (GIỮ NGUYÊN)
app.UseSession();

app.UseAuthorization();

// ===== ROUTES =====

// ✅ Route riêng cho /Employee (GIỮ NGUYÊN)
app.MapControllerRoute(
    name: "employee",
    pattern: "Employee",
    defaults: new { controller = "Employee", action = "Index" }
);

// ✅ Route mặc định (GIỮ NGUYÊN)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

// 🔥 HUB REALTIME (THÊM MỚI)
app.MapHub<DashboardHub>("/dashboardHub");

app.Run();