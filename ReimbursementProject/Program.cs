using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;


var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Increase Kestrel upload limit to 500 MB
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500 MB
});

// ✅ 2. Increase multipart form upload limit (important for file uploads)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500 MB
});

// ✅ 3. Add services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataBaseConnection"))
           .EnableSensitiveDataLogging());
builder.Services.AddDbContext<StoreAppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("secondaryDataBase"))
           .EnableSensitiveDataLogging());

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// ✅ 4. Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/Employee/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddSignalR(); // 👈 Add this before building
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
// ✅ 4.1 Register IHttpContextAccessor


// ✅ 5. Must come *before* UseRouting() to take effect globally
app.Use(async (context, next) =>
{
    var maxRequestBodyFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxRequestBodyFeature != null)
    {
        // allow up to 500 MB
        maxRequestBodyFeature.MaxRequestBodySize = 500 * 1024 * 1024;
    }
    await next();
});



// ✅ 6. Standard pipeline setup
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication(); // ✅ you missed this line earlier
app.UseAuthorization();

// 🔒 NO-CACHE middleware (ADD THIS BLOCK)
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";

    await next();
});

// ✅ Important: Place Hub mapping BEFORE controller routes
app.MapHub<ReimbursementProject.Hubs.NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Dashboard}/{id?}");

app.Run();
