using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/master")]
public sealed class MasterReviewController : ControllerBase
{
    private readonly IMasterReviewService _masterReviewService;

    public MasterReviewController(IMasterReviewService masterReviewService)
    {
        _masterReviewService = masterReviewService;
    }

    [HttpGet("modules")]
    public async Task<IActionResult> Modules(CancellationToken cancellationToken)
    {
        return Ok(await _masterReviewService.GetModulesAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? moduleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return Ok(await _masterReviewService.GetListAsync(moduleId, page, pageSize, cancellationToken));
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken cancellationToken)
    {
        return Ok(await _masterReviewService.GetLookupsAsync(cancellationToken));
    }

    [HttpGet("{masterTestId}")]
    public async Task<IActionResult> Detail(string masterTestId, CancellationToken cancellationToken)
    {
        var detail = await _masterReviewService.GetDetailAsync(Uri.UnescapeDataString(masterTestId), cancellationToken);
        return detail == null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create(MasterTemplateCreateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _masterReviewService.CreateAsync(request, ReadUser(), cancellationToken));
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

    [HttpPut("{masterTestId}")]
    public async Task<IActionResult> Update(string masterTestId, MasterTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var detail = await _masterReviewService.UpdateAsync(Uri.UnescapeDataString(masterTestId), request, ReadUser(), cancellationToken);
            return detail == null ? NotFound() : Ok(detail);
        }
        catch (ConcurrencyConflictException ex)
        {
            return StatusCode(409, new { message = ex.Message });
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

    [HttpDelete("{masterTestId}")]
    public async Task<IActionResult> Delete(string masterTestId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _masterReviewService.DeleteAsync(Uri.UnescapeDataString(masterTestId), ReadUser(), cancellationToken);
            return deleted ? Ok(new { deleted = true }) : NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
