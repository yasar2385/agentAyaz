using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/import")]
public sealed class ManualImportController : ControllerBase
{
    private readonly IManualImportService _importService;

    public ManualImportController(IManualImportService importService)
    {
        _importService = importService;
    }

    [HttpPost("inspect")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Inspect(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null) return BadRequest("file must be provided");
        return Ok(await _importService.InspectAsync(file, cancellationToken));
    }

    [HttpPost("master/parse")]
    public async Task<IActionResult> ParseMaster(ParseMasterImportRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _importService.ParseMasterAsync(request, ReadUser(), cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("master/upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> UploadMaster(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null) return BadRequest("file must be provided");
        return Ok(await _importService.UploadMasterAsync(file, ReadUser(), cancellationToken));
    }

    [HttpPost("results/upload")]
    [RequestSizeLimit(100_000_000)]
    public async Task<IActionResult> UploadResults([FromForm] List<IFormFile> files, [FromForm] string resultMode = "single", CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) return BadRequest("at least one file must be provided");
        return Ok(await _importService.UploadResultsAsync(files, resultMode, ReadUser(), cancellationToken));
    }

    [HttpGet("{batchId:int}")]
    public async Task<IActionResult> GetBatch(int batchId, CancellationToken cancellationToken)
    {
        var batch = await _importService.GetBatchAsync(batchId, cancellationToken);
        return batch == null ? NotFound() : Ok(batch);
    }

    [HttpGet("{batchId:int}/errors")]
    public async Task<IActionResult> GetErrors(int batchId, CancellationToken cancellationToken)
    {
        return Ok(await _importService.GetErrorsAsync(batchId, cancellationToken));
    }

    [HttpPost("master/{batchId:int}/sheet-actions")]
    public async Task<IActionResult> SaveSheetActions(int batchId, SheetActionRequest request, CancellationToken cancellationToken)
    {
        var batch = await _importService.SaveSheetActionsAsync(batchId, request, cancellationToken);
        return batch == null ? NotFound() : Ok(batch);
    }

    [HttpPost("master/{batchId:int}/manual-edit-actions")]
    public async Task<IActionResult> SaveManualEditActions(int batchId, ManualEditActionRequest request, CancellationToken cancellationToken)
    {
        var batch = await _importService.SaveManualEditActionsAsync(batchId, request, cancellationToken);
        return batch == null ? NotFound() : Ok(batch);
    }

    [HttpPost("{batchId:int}/commit")]
    public async Task<IActionResult> Commit(int batchId, CancellationToken cancellationToken)
    {
        try
        {
            var batch = await _importService.CommitAsync(batchId, cancellationToken);
            return batch == null ? NotFound() : Ok(batch);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private AuthUser? ReadUser()
    {
        var username = Request.Headers["X-TestCaseViewer-Username"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return new AuthUser
        {
            Id = Request.Headers["X-TestCaseViewer-UserId"].FirstOrDefault() ?? string.Empty,
            Username = username,
            DisplayName = Request.Headers["X-TestCaseViewer-DisplayName"].FirstOrDefault() ?? username,
            Role = Request.Headers["X-TestCaseViewer-Role"].FirstOrDefault() ?? string.Empty,
            Email = Request.Headers["X-TestCaseViewer-Email"].FirstOrDefault() ?? string.Empty
        };
    }
}
