using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Настройка HTTPS через Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, opts => opts.UseHttps());
});

// Добавляем сессию для хранения токена
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

// HttpClient для общения с API
builder.Services.AddHttpClient("NotesApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7275");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();