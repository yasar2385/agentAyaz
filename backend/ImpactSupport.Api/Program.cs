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
builder.Services.AddSingleton<IGoogleCredentialProvider, GoogleCredentialProvider>();
builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddSingleton<IQaSheetParser, QaSheetParser>();
builder.Services.AddSingleton<IGoogleSheetsService, GoogleSheetsService>();

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

app.UseHttpsRedirection();

app.UseCors("ImpactUiCors");
app.MapHealthChecks("/health");

app.UseAuthorization();

// ---------------------------------------------------------
// Endpoints
// ---------------------------------------------------------

app.MapControllers();

app.Run();
