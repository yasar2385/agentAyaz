using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer")]
public class TestCaseViewerController : ControllerBase
{
    private readonly IGoogleCredentialProvider _credentialProvider;
    private readonly ILogger<TestCaseViewerController> _logger;
    private readonly IGoogleDriveService _driveService;

    public TestCaseViewerController(
        IGoogleCredentialProvider credentialProvider,
        IGoogleDriveService driveService,
        ILogger<TestCaseViewerController> logger)
    {
        _credentialProvider = credentialProvider;
        _driveService = driveService;
        _logger = logger;
    }

    /// <summary>
    /// Phase 2 verification: confirms the service account credential is valid
    /// and can talk to Google Drive. Remove or gate behind an env check once
    /// Phase 3 endpoints are in place.
    /// </summary>
    [HttpGet("auth-check")]
    public IActionResult AuthCheck()
    {
        try
        {
            var credential = _credentialProvider.GetCredential();

            var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ImpactTestCasesViewer"
            });

            var about = driveService.About.Get();
            about.Fields = "user";
            var result = about.Execute();

            return Ok(new
            {
                authenticated = true,
                serviceAccount = result.User.EmailAddress
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google auth check failed");

            return StatusCode(500, new
            {
                authenticated = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("files")]
    public async Task<IActionResult> GetFiles(
       [FromQuery] string type,
       CancellationToken cancellationToken)
    {
        if (type is not ("master" or "regression"))
        {
            return BadRequest(new
            {
                code = "INVALID_REPORT_TYPE",
                message = "type must be 'master' or 'regression'."
            });
        }

        try
        {
            var files = await _driveService.GetFilesAsync(type, cancellationToken);
            return Ok(files);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Folder not configured for report type {ReportType}", type);
            return StatusCode(500, new { code = "GOOGLE_FILE_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files for report type {ReportType}", type);
            return StatusCode(500, new { code = "GOOGLE_AUTH_FAILED", message = "Unable to retrieve files from Google Drive." });
        }
    }
}