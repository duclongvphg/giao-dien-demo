using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// ===== ADD SERVICES =====
builder.Services.AddControllersWithViews();

// 🔥 SESSION
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

// 🔥 SESSION
app.UseSession();

app.UseAuthorization();

// ===== ROUTES =====

// ✅ Route riêng cho /Employee (FIX LỖI 404)
app.MapControllerRoute(
    name: "employee",
    pattern: "Employee",
    defaults: new { controller = "Employee", action = "Index" }
);

// ✅ Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();