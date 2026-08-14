using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { authenticated = false, message = "Username and password are required." });
        }

        try
        {
            var user = await _authService.ValidateUserAsync(request.Username.Trim(), request.Password, cancellationToken);
            if (user is null)
            {
                return Unauthorized(new { authenticated = false, message = "Invalid username or password." });
            }

            return Ok(new LoginResponse { Authenticated = true, User = user });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Mongo auth is not configured.");
            return StatusCode(503, new { authenticated = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mongo login failed.");
            return StatusCode(500, new { authenticated = false, message = "Login failed. Please try again." });
        }
    }
}
