using HierarchicalTaskApp.Services;
using QuestPDF.Infrastructure; // <-- YENİ EKLENDİ
using System.Globalization;
using Microsoft.AspNetCore.Localization;
var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;
// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ITaskRepository, JsonTaskRepository>();

// 3. Oturum (Session) için gereken hizmetleri ayarla
builder.Services.AddDistributedMemoryCache(); // Oturum verilerini bellekte saklamak için
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum zaman aşımı
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContext'e erişim için (Örn: _Layout.cshtml'de oturumdaki kullanıcıyı almak için)
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 4. Oturum ara katmanını (middleware) etkinleştir
// Bu, UseAuthorization'dan ÖNCE gelmelidir.
app.UseSession();

app.UseAuthorization();

// Başlangıç rotasını Auth/Login olarak ayarla
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();