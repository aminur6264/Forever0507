using Forever0507App.Data;
using Forever0507App.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// The "Event" section seeds the DB once; afterwards appsettings is only the fallback.
var configEvent = builder.Configuration.GetSection(EventOptions.SectionName).Get<EventOptions>() ?? new();
var eventHolder = new EventOptionsHolder(configEvent);
builder.Services.AddSingleton(eventHolder);
// Resolved per request so an admin save (which swaps holder.Current) takes effect immediately.
builder.Services.AddScoped(sp => eventHolder.Current);

builder.Services.AddDbContext<AlumniDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Demo site: create the database straight from the model (no migrations), then seed lookup data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AlumniDbContext>();
    db.Database.EnsureCreated();

    // EnsureCreated never alters an existing database — back-fill the FullName column
    // (added after first release) so older databases keep working without migrations.
    var hasFullName = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'AppUsers') AND name = N'FullName'")
        .SingleAsync();
    if (hasFullName == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE AppUsers ADD FullName nvarchar(120) NOT NULL CONSTRAINT DF_AppUsers_FullName DEFAULT N''");
    }

    await DbSeeder.SeedAsync(db, configEvent);

    // From here on the app reads event text from the database, not appsettings.
    var settings = await db.EventSettings.AsNoTracking()
        .SingleAsync(s => s.Id == EventSettings.SingleRowId);
    eventHolder.Current = settings.ToOptions();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Users flagged MustChangePassword (first login / admin reset) may only reach the change-password
// page (and the static assets it needs) until they set a new password.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isAllowedPath =
        path.StartsWithSegments("/Account") ||
        path.StartsWithSegments("/css") || path.StartsWithSegments("/js") || path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/images") || path.StartsWithSegments("/favicon") ||
        path.StartsWithSegments("/Forever0507App.styles.css");

    if (context.User.Identity?.IsAuthenticated == true
        && context.User.HasClaim(AuthConstants.MustChangePasswordClaim, "true")
        && !isAllowedPath)
    {
        context.Response.Redirect("/Account/ChangePassword");
        return;
    }

    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
