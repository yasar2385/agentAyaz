using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/runs")]
public sealed class PlaywrightRunsController : ControllerBase
{
    private readonly IPlaywrightRunService _runService;

    public PlaywrightRunsController(IPlaywrightRunService runService)
    {
        _runService = runService;
    }

    [HttpGet("readiness")]
    public async Task<IActionResult> Readiness(CancellationToken cancellationToken)
    {
        return Ok(await _runService.GetReadinessAsync(cancellationToken));
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> Metadata(CancellationToken cancellationToken)
    {
        return Ok(await _runService.GetMetadataAsync(cancellationToken));
    }

    [HttpGet("configs")]
    public async Task<IActionResult> Configs(CancellationToken cancellationToken)
    {
        return Ok(await _runService.GetConfigsAsync(cancellationToken));
    }

    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] string scope = "mine", [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await _runService.GetRecentRunsAsync(scope, limit, ReadUser(), cancellationToken));
    }

    [HttpGet("{configId:int}/progress")]
    public async Task<IActionResult> Progress(int configId, CancellationToken cancellationToken)
    {
        var progress = await _runService.GetProgressAsync(configId, ReadUser(), cancellationToken);
        return progress == null ? NotFound() : Ok(progress);
    }

    [HttpPost("{configId:int}/continue")]
    public async Task<IActionResult> Continue(int configId, CancellationToken cancellationToken)
    {
        try
        {
            var execution = await _runService.ContinueAsync(configId, ReadUser(), cancellationToken);
            return execution == null ? NotFound() : Ok(execution);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("verify-fix")]
    public async Task<IActionResult> VerifyFix(VerifyFixRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _runService.VerifyFixAsync(request, ReadUser(), cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("configs")]
    public async Task<IActionResult> CreateConfig(TestRunConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _runService.CreateConfigAsync(request, ReadUser(), cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("configs/{configId:int}")]
    public async Task<IActionResult> UpdateConfig(int configId, TestRunConfigRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var config = await _runService.UpdateConfigAsync(configId, request, ReadUser(), cancellationToken);
            return config == null ? NotFound() : Ok(config);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("configs/{configId:int}/trigger")]
    public async Task<IActionResult> Trigger(int configId, CancellationToken cancellationToken)
    {
        try
        {
            var execution = await _runService.TriggerAsync(configId, ReadUser(), cancellationToken);
            return execution == null ? NotFound() : Ok(execution);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("executions/{id:int}")]
    public async Task<IActionResult> Execution(int id, CancellationToken cancellationToken)
    {
        var execution = await _runService.GetExecutionAsync(id, cancellationToken);
        return execution == null ? NotFound() : Ok(execution);
    }

    [HttpPost("executions/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var execution = await _runService.CancelAsync(id, ReadUser(), cancellationToken);
            return execution == null ? NotFound() : Ok(execution);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpGet("executions/{id:int}/report")]
    public async Task<IActionResult> Report(int id, CancellationToken cancellationToken)
    {
        var path = await _runService.GetReportPathAsync(id, cancellationToken);
        return path == null ? NotFound() : PhysicalFile(path, "text/html");
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
