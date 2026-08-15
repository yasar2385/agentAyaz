using Google.Apis.Auth.OAuth2;
using ImpactSupport.Api.Helper;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;


var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Services
// ---------------------------------------------------------

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<TestCaseViewerOptions>(builder.Configuration.GetSection("TestCaseViewer"));
builder.Services.Configure<PlaywrightOptions>(builder.Configuration.GetSection("TestCaseViewer:Playwright"));
builder.Services.Configure<MongoAuthOptions>(builder.Configuration.GetSection("MongoAuth"));
builder.Services.AddSingleton<IGoogleCredentialProvider, GoogleCredentialProvider>();
builder.Services.AddSingleton<IGoogleDriveFileLister, GoogleDriveFileLister>();
builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddSingleton<IQaSheetParser, QaSheetParser>();
builder.Services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();
builder.Services.AddSingleton<IGoogleDriveUrlParser, GoogleDriveUrlParser>();
builder.Services.AddSingleton<IQaTsvRowReader, QaTsvRowReader>();
builder.Services.AddScoped<ITestCaseViewerAccessService, TestCaseViewerAccessService>();
builder.Services.AddScoped<IUserAuthService, SqliteUserAuthService>();
builder.Services.AddScoped<IDashboardCacheService, DashboardCacheService>();
builder.Services.AddScoped<IManualImportService, ManualImportService>();
builder.Services.AddScoped<PlaywrightCommandBuilder>();
builder.Services.AddScoped<IPlaywrightRunService, PlaywrightRunService>();
builder.Services.AddHostedService<SelectedMongoUserImportService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ImpactUiCors", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8080",
                "https://localhost:8080",
                "http://localhost:5173"   // TestCaseViewer React dev server
             )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<SupportDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("SupportDb"));
});

// ---------------------------------------------------------
// Application
// ---------------------------------------------------------

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigrations");
    try
    {
        logger.LogInformation("Applying SQLite migrations for SupportDb.");
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDbContext>();
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("SQLite migrations for SupportDb completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "SQLite migration failed for SupportDb.");
        throw;
    }
}

// ---------------------------------------------------------
// Development
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------------------------------------------
// Middleware
// ---------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ImpactUiCors");
app.MapHealthChecks("/health");

app.UseAuthorization();

// ---------------------------------------------------------
// Endpoints
// ---------------------------------------------------------

app.MapControllers();

app.Run();
