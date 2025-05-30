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

var builder = WebApplication.CreateBuilder(args);

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

    // Configure services
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        try
        {
            Log.Information("Seeding database...");

            await DataSeeder.SeedTestDataAsync(context, userManager, roleManager, services);

            // Seed test users for role testing
            await TestUserSeeder.SeedTestUsersAsync(userManager, roleManager);

            Log.Information("Database seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database");
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

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // CHANGED: Set the default root route to point directly to EventsForToday
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Events}/{action=EventsForToday}/{id?}");
    app.MapRazorPages();

    // Add a redirect from the root to EventsForToday
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/Events/EventsForToday");
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