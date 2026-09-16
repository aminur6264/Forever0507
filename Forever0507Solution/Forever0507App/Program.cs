using Forever0507App.Data;
using Forever0507App.Models;
using Forever0507App.Services;
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

// SMTP for the approval credential email — empty Server in config keeps sending disabled.
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddSingleton<EmailSender>();

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

    // Same back-fill for the EventSettings logo column (added after first release).
    var hasLogo = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'EventSettings') AND name = N'LogoUrl'")
        .SingleAsync();
    if (hasLogo == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE EventSettings ADD LogoUrl nvarchar(200) NULL");
    }

    // Same back-fill for the Registrations email column (added after first release).
    var hasEmail = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'Registrations') AND name = N'Email'")
        .SingleAsync();
    if (hasEmail == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE Registrations ADD Email nvarchar(120) NOT NULL CONSTRAINT DF_Registrations_Email DEFAULT N''");
    }

    // Khoroch (expense invoice) tables — created here for databases that predate the feature;
    // column names/types mirror what EF conventions would have generated.
    await db.Database.ExecuteSqlAsync($"""
        IF OBJECT_ID(N'Khorochs', N'U') IS NULL
        BEGIN
            CREATE TABLE [Khorochs] (
                [Id] int IDENTITY NOT NULL,
                [InvoiceCode] nvarchar(30) NOT NULL,
                [Date] datetime2 NOT NULL,
                [Description] nvarchar(200) NOT NULL,
                [Amount] decimal(12,2) NOT NULL,
                [CreatedBy] nvarchar(50) NOT NULL,
                [CreatedByName] nvarchar(120) NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [Status] nvarchar(20) NULL,
                [ActionBy] nvarchar(50) NULL,
                [ActionAt] datetime2 NULL,
                CONSTRAINT [PK_Khorochs] PRIMARY KEY ([Id]));
        END
        """);
    await db.Database.ExecuteSqlAsync($"""
        IF OBJECT_ID(N'KhorochItems', N'U') IS NULL
        BEGIN
            CREATE TABLE [KhorochItems] (
                [Id] int IDENTITY NOT NULL,
                [KhorochId] int NOT NULL,
                [ProductName] nvarchar(160) NOT NULL,
                [Quantity] decimal(12,2) NOT NULL,
                [UnitPrice] decimal(12,2) NOT NULL,
                CONSTRAINT [PK_KhorochItems] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_KhorochItems_Khorochs_KhorochId] FOREIGN KEY ([KhorochId])
                    REFERENCES [Khorochs] ([Id]) ON DELETE CASCADE);
        END
        """);

    // Receipt-image column on Khorochs (for rows created before the feature).
    var hasKhorochImage = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'Khorochs') AND name = N'ImageUrl'")
        .SingleAsync();
    if (hasKhorochImage == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE Khorochs ADD ImageUrl nvarchar(200) NULL");
    }

    // DB-backed image storage (uploads happen on locked-down shared hosting too) —
    // each table gets the byte payload + content type; served via /Image/... routes.
    var upgrades = new (string Table, string Column, string Sql)[]
    {
        ("EventSettings", "LogoData", "ALTER TABLE EventSettings ADD LogoData varbinary(max) NULL"),
        ("EventSettings", "LogoContentType", "ALTER TABLE EventSettings ADD LogoContentType nvarchar(50) NULL"),
        ("WelcomeNotes", "PhotoData", "ALTER TABLE WelcomeNotes ADD PhotoData varbinary(max) NULL"),
        ("WelcomeNotes", "PhotoContentType", "ALTER TABLE WelcomeNotes ADD PhotoContentType nvarchar(50) NULL"),
        ("Khorochs", "ImageData", "ALTER TABLE Khorochs ADD ImageData varbinary(max) NULL"),
        ("Khorochs", "ImageContentType", "ALTER TABLE Khorochs ADD ImageContentType nvarchar(50) NULL"),
    };
    foreach (var (table, column, sql) in upgrades)
    {
        var exists = await db.Database.SqlQuery<int>(
            $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID({table}) AND name = {column}")
            .SingleAsync();
        if (exists == 0)
            await db.Database.ExecuteSqlAsync($"EXEC({sql})");
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
        path.StartsWithSegments("/images") || path.StartsWithSegments("/Image") ||
        path.StartsWithSegments("/uploads") || path.StartsWithSegments("/favicon") ||
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

// MapStaticAssets serves only build-time manifest files — runtime uploads (logo, welcome
// photos) need the physical-file provider too.
app.UseStaticFiles();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
