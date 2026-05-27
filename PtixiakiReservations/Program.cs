using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PtixiakiReservations.Configurations;
using PtixiakiReservations.Data;
using PtixiakiReservations.Models;
using PtixiakiReservations.Seeders;
using PtixiakiReservations.Services;
using Serilog;
using Serilog.Exceptions;
using Serilog.Formatting.Elasticsearch;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Sinks.Elasticsearch;
using System;
using Microsoft.AspNetCore.Authorization;

// Configure Npgsql to handle DateTime as timestamp without time zone
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// EF MODE: Only load DbContext for migrations
if (builder.Environment.IsEnvironment("EF"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    var efApp = builder.Build();
    return 0;
}


// Configure Serilog with Elasticsearch
var elasticUrl = builder.Configuration["ElasticSettings:Url"] ?? "http://elasticsearch:9200";
var indexPrefix = builder.Configuration["ElasticSettings:DefaultIndex"] ?? "events";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console()
    .WriteTo.Debug()
    .WriteTo.Elasticsearch(
        new Serilog.Sinks.Elasticsearch.ElasticsearchSinkOptions(new Uri(elasticUrl))
        {
            IndexFormat = $"{indexPrefix}-logs-{DateTime.UtcNow:yyyy-MM}",
            AutoRegisterTemplate = true,
            OverwriteTemplate = true,
            DetectElasticsearchVersion = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            NumberOfShards = 1,
            NumberOfReplicas = 0,
            ModifyConnectionSettings = x =>
                x.BasicAuthentication("", "").ServerCertificateValidationCallback((o, c, ch, e) => true),
            CustomFormatter = new ElasticsearchJsonFormatter(),
            EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                               EmitEventFailureHandling.WriteToFailureSink |
                               EmitEventFailureHandling.RaiseCallback
        })
    .CreateLogger();

// Use Serilog for logging
builder.Host.UseSerilog();

try
{
    Log.Information("Starting web application");

    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection");

        Log.Information("Using PostgreSQL database");
        options.UseNpgsql(postgresConnection);
    });


    builder.Services.AddSignalR();

    // Add configuration for forwarded headers
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                   ForwardedHeaders.XForwardedProto |
                                   ForwardedHeaders.XForwardedHost;
        // Additional options to help with proxy
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Stores.MaxLengthForKeys = 128;

            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 0;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultUI()
        .AddDefaultTokenProviders();

    builder.Services.AddControllersWithViews().AddXmlSerializerFormatters();
    builder.Services.AddRazorPages();

    builder.Services.Configure<ElasticSettings>(builder.Configuration.GetSection("ElasticSettings"));
    builder.Services.AddSingleton<IElasticSearch, ElasticSearchService>();

    builder.Services.AddTransient<IEmailService, EmailService>();

    // Add Event Generator Service
    builder.Services.AddScoped<IEventGeneratorService, EventGeneratorService>();

    // CHANGE: Remove the global authorization filter and apply it selectively
    builder.Services.AddMvc();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Identity/Account/Login";
        options.LogoutPath = "/Identity/Account/Logout";
        options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        // Set the default return URL after login
        options.ReturnUrlParameter = "returnUrl";
        options.SlidingExpiration = true;

        // Add this to fix the redirect issue
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    });

    // Build the app
    var app = builder.Build();

    // Configure the HTTP Request Pipeline

// ΜΗΝ τρέχεις seeding όταν τρέχει EF Core CLI
if (!builder.Environment.IsEnvironment("EF"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        try{
            await PtixiakiReservations.Seeders.RoleSeeder.SeedRolesAndAdminAsync(services);
            Log.Information("Ensuring database is created...");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Database created successfully");

            Log.Information("Seeding database...");
            DataSeeder.BasicDataSeed(context, userManager, roleManager);
            Log.Information("Database seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database");
        }
    }
}

    // Use forwarded headers - this must come first in the pipeline
    app.UseForwardedHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        // Don't redirect to HTTPS when behind a proxy
        // app.UseHsts();
    }

    // Don't use HTTPS redirection when behind a reverse proxy like Traefik
    // app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.MapStaticAssets();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // CHANGED: Set the default root route to point directly to HomePage
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Events}/{action=HomePage}/{id?}");
    app.MapRazorPages();

    // Add a redirect from the root to HomePage
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/Events/HomePage");
            return;
        }

        await next();
    });

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}