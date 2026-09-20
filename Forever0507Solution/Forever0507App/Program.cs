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
        // Persistent login: the cookie survives browser restarts and the ticket lasts a year.
        // SlidingExpiration re-issues it after ~6 months of use, so active users stay logged in
        // until they log out. (Browsers cap cookie MaxAge at 400 days, hence the 365d values.)
        options.ExpireTimeSpan = TimeSpan.FromDays(365);
        options.SlidingExpiration = true;
        options.Cookie.MaxAge = TimeSpan.FromDays(365);
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

    // Same back-fill for the jersey-name column (added after first release).
    var hasJerseyName = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'Registrations') AND name = N'NameOnJersey'")
        .SingleAsync();
    if (hasJerseyName == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE Registrations ADD NameOnJersey nvarchar(20) NOT NULL CONSTRAINT DF_Registrations_NameOnJersey DEFAULT N''");
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

    // Home-page gallery images — created here for databases that predate the feature;
    // column names/types mirror what EF conventions would have generated.
    await db.Database.ExecuteSqlAsync($"""
        IF OBJECT_ID(N'GalleryImages', N'U') IS NULL
        BEGIN
            CREATE TABLE [GalleryImages] (
                [Id] int IDENTITY NOT NULL,
                [Title] nvarchar(120) NOT NULL,
                [ImageData] varbinary(max) NOT NULL,
                [ImageContentType] nvarchar(50) NOT NULL,
                [IsActive] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UploadedBy] nvarchar(50) NOT NULL,
                [UploadedByName] nvarchar(120) NOT NULL,
                [ApprovalStatus] nvarchar(20) NULL,
                [ApprovalBy] nvarchar(50) NULL,
                [ApprovalAt] datetime2 NULL,
                CONSTRAINT [PK_GalleryImages] PRIMARY KEY ([Id]));
        END
        """);

    // Gallery uploader/approval columns (added after first release) — these back-fills are
    // deliberately one-time (inside the column-missing branch): after the feature ships,
    // ApprovalStatus IS NULL means "genuinely pending", so an every-startup UPDATE would
    // silently approve pending uploads on each app-pool recycle.
    var hasGalleryUploadedBy = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'GalleryImages') AND name = N'UploadedBy'")
        .SingleAsync();
    if (hasGalleryUploadedBy == 0)
    {
        // Pre-feature rows were all admin uploads — stamp them so no real user phone ever matches.
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE GalleryImages ADD UploadedBy nvarchar(50) NOT NULL CONSTRAINT DF_GalleryImages_UploadedBy DEFAULT N''");
        await db.Database.ExecuteSqlAsync(
            $"UPDATE GalleryImages SET UploadedBy = N'সিস্টেম অ্যাডমিন' WHERE UploadedBy = N''");
    }

    var hasGalleryUploadedByName = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'GalleryImages') AND name = N'UploadedByName'")
        .SingleAsync();
    if (hasGalleryUploadedByName == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE GalleryImages ADD UploadedByName nvarchar(120) NOT NULL CONSTRAINT DF_GalleryImages_UploadedByName DEFAULT N''");
        await db.Database.ExecuteSqlAsync(
            $"UPDATE GalleryImages SET UploadedByName = N'সিস্টেম অ্যাডমিন' WHERE UploadedByName = N''");
    }

    var hasGalleryApprovalBy = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'GalleryImages') AND name = N'ApprovalBy'")
        .SingleAsync();
    if (hasGalleryApprovalBy == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE GalleryImages ADD ApprovalBy nvarchar(50) NULL");
    }

    var hasGalleryApprovalAt = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'GalleryImages') AND name = N'ApprovalAt'")
        .SingleAsync();
    if (hasGalleryApprovalAt == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE GalleryImages ADD ApprovalAt datetime2 NULL");
    }

    var hasGalleryApprovalStatus = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'GalleryImages') AND name = N'ApprovalStatus'")
        .SingleAsync();
    if (hasGalleryApprovalStatus == 0)
    {
        // Last of the gallery blocks — ApprovalBy/ApprovalAt already exist, so the one-time
        // legacy back-fill can stamp the whole decision in a single UPDATE. Pre-feature rows
        // were admin uploads and publish immediately, hence approved by the static admin.
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE GalleryImages ADD ApprovalStatus nvarchar(20) NULL");
        await db.Database.ExecuteSqlAsync($"""
            UPDATE GalleryImages
            SET ApprovalStatus = N'অনুমোদিত', ApprovalBy = N'সিস্টেম অ্যাডমিন', ApprovalAt = SYSUTCDATETIME()
            WHERE ApprovalStatus IS NULL
            """);
    }

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

    // Registration IP capture — table created here for databases that predate the feature;
    // column names/types mirror what EF conventions would have generated.
    await db.Database.ExecuteSqlAsync($"""
        IF OBJECT_ID(N'IpAddresses', N'U') IS NULL
        BEGIN
            CREATE TABLE [IpAddresses] (
                [Id] int IDENTITY NOT NULL,
                [Ip] nvarchar(45) NOT NULL,
                [City] nvarchar(80) NULL,
                [State] nvarchar(80) NULL,
                [CountryShortName] nvarchar(10) NULL,
                [CountryFullName] nvarchar(80) NULL,
                [TimeZone] nvarchar(80) NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_IpAddresses] PRIMARY KEY ([Id]));
        END
        """);

    // Registrations.IpAddressId — the FK is added together with the column so the paired
    // constraint only ever runs once, on databases that predate the feature.
    var hasRegIpAddress = await db.Database.SqlQuery<int>(
        $"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'Registrations') AND name = N'IpAddressId'")
        .SingleAsync();
    if (hasRegIpAddress == 0)
    {
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE Registrations ADD IpAddressId int NULL");
        await db.Database.ExecuteSqlAsync(
            $"ALTER TABLE Registrations ADD CONSTRAINT FK_Registrations_IpAddresses_IpAddressId FOREIGN KEY (IpAddressId) REFERENCES IpAddresses(Id)");
    }

    // Page-visit log — created here for databases that predate the feature; column names/types
    // mirror what EF conventions would have generated (IpId FK is part of the table DDL because
    // IpAddresses is guaranteed to exist by the block above).
    await db.Database.ExecuteSqlAsync($"""
        IF OBJECT_ID(N'PageVisits', N'U') IS NULL
        BEGIN
            CREATE TABLE [PageVisits] (
                [Id] bigint IDENTITY NOT NULL,
                [Url] nvarchar(200) NOT NULL,
                [IpId] int NULL,
                [VisitTime] datetime2 NOT NULL,
                CONSTRAINT [PK_PageVisits] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PageVisits_IpAddresses_IpId] FOREIGN KEY ([IpId])
                    REFERENCES [IpAddresses] ([Id]));
        END
        """);

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

// Page-visit logging — one PageVisits row per page open: URL, visitor IP (reusing the
// distinct-address IpAddresses rows) and Bangladesh time (UTC+6). Only GETs count as visits;
// static assets and image bytes are excluded. A logging failure must never break the page,
// hence the swallow-and-continue. Rows are kept after successful responses only, so 404
// probes don't flood the table.
app.Use(async (context, next) =>
{
    await next();

    var path = context.Request.Path;
    var isStatic =
        path.StartsWithSegments("/css") || path.StartsWithSegments("/js") || path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/images") || path.StartsWithSegments("/Image") || path.StartsWithSegments("/uploads") ||
        path.StartsWithSegments("/favicon") || path.StartsWithSegments("/Forever0507App.styles.css");

    if (!HttpMethods.IsGet(context.Request.Method) || isStatic) return;
    if (context.Response.StatusCode >= 400) return;

    try
    {
        var db = context.RequestServices.GetRequiredService<AlumniDbContext>();
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        int? ipId = null;
        if (!string.IsNullOrWhiteSpace(remoteIp))
        {
            ipId = await db.IpAddresses.Where(i => i.Ip == remoteIp)
                .Select(i => (int?)i.Id).FirstOrDefaultAsync();
            if (ipId is null)
            {
                var ip = new IpAddress { Ip = remoteIp };
                db.IpAddresses.Add(ip);
                await db.SaveChangesAsync();
                ipId = ip.Id;
            }
        }

        var url = path + context.Request.QueryString;
        db.PageVisits.Add(new PageVisit
        {
            Url = url.Length > 200 ? url[..200] : url,
            IpId = ipId,
            VisitTime = DateTime.UtcNow.AddHours(6), // Bangladesh time, fixed offset (no DST)
        });
        await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not record page visit for {Path}", context.Request.Path);
    }
});

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
