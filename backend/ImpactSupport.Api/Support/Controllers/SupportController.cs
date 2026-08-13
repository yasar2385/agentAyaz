using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.Support.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImpactSupport.Api.Support.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SupportController : ControllerBase
{
    private static readonly string[] ActiveStatuses =
    {
        "OPEN",
        "ASSIGNED",
        "IN_PROGRESS",
        "WAITING_USER"
    };

    private readonly SupportDbContext _db;

    public SupportController(SupportDbContext db)
    {
        _db = db;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(CreateSupportRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.UserRole) ||
            string.IsNullOrWhiteSpace(request.DocumentId) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                message = "UserId, UserRole, DocumentId, and Message are required"
            });
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var existingSession = await _db.SupportSessions
            .Where(x =>
                x.UserId == request.UserId &&
                x.DocumentId == request.DocumentId &&
                x.UserRole == request.UserRole &&
                ActiveStatuses.Contains(x.Status))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync();

        if (existingSession is not null)
        {
            existingSession.UpdatedAtUtc = now;

            _db.SupportMessages.Add(new SupportMessage
            {
                MessageId = CreateMessageId(now),
                SupportSessionId = existingSession.SupportSessionId,
                SenderUserId = request.UserId,
                SenderName = request.UserName,
                SenderRole = request.UserRole,
                MessageText = request.Message.Trim(),
                MessageType = "USER",
                CreatedAtUtc = now
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new CreateSupportRequestResponse
            {
                Created = false,
                SupportSessionId = existingSession.SupportSessionId,
                TicketNo = existingSession.TicketNo,
                Status = existingSession.Status,
                Message = "Existing support session continued"
            });
        }

        var supportSessionId = CreateSupportSessionId(now);
        var ticketNo = CreateTicketNo(now);

        var session = new SupportSession
        {
            SupportSessionId = supportSessionId,
            TicketNo = ticketNo,
            UserId = request.UserId,
            UserName = request.UserName,
            UserRole = request.UserRole,
            DocumentId = request.DocumentId,
            DocumentLink = request.DocumentLink,
            ImpactSessionId = request.ImpactSessionId,
            ModuleName = request.ModuleName,
            ClientName = request.ClientName,
            CurrentUrl = request.CurrentUrl,
            Status = "OPEN",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.SupportSessions.Add(session);
        _db.SupportMessages.Add(new SupportMessage
        {
            MessageId = CreateMessageId(now),
            SupportSessionId = supportSessionId,
            SenderUserId = request.UserId,
            SenderName = request.UserName,
            SenderRole = request.UserRole,
            MessageText = request.Message.Trim(),
            MessageType = "USER",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new CreateSupportRequestResponse
        {
            Created = true,
            SupportSessionId = supportSessionId,
            TicketNo = ticketNo,
            Status = "OPEN",
            Message = "New support session created"
        });
    }

    [HttpPost("sessions/{supportSessionId}/messages")]
    public async Task<IActionResult> SendMessage(
        string supportSessionId,
        SendSupportMessageDto request)
    {
        if (string.IsNullOrWhiteSpace(request.SenderUserId) ||
            string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new
            {
                message = "SenderUserId and Message are required"
            });
        }

        var session = await _db.SupportSessions
            .FirstOrDefaultAsync(x => x.SupportSessionId == supportSessionId);

        if (session is null)
        {
            return NotFound(new
            {
                message = "Support session not found"
            });
        }

        if (session.Status is "RESOLVED" or "CLOSED")
        {
            return Conflict(new
            {
                message = "Support session is closed"
            });
        }

        var now = DateTime.UtcNow;
        session.UpdatedAtUtc = now;

        _db.SupportMessages.Add(new SupportMessage
        {
            MessageId = CreateMessageId(now),
            SupportSessionId = supportSessionId,
            SenderUserId = request.SenderUserId,
            SenderName = request.SenderName,
            SenderRole = request.SenderRole,
            MessageText = request.Message.Trim(),
            MessageType = "USER",
            CreatedAtUtc = now
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            supportSessionId,
            message = "Message added"
        });
    }

    [HttpGet("sessions/{supportSessionId}/messages")]
    public async Task<IActionResult> GetMessages(string supportSessionId)
    {
        var sessionExists = await _db.SupportSessions
            .AnyAsync(x => x.SupportSessionId == supportSessionId);

        if (!sessionExists)
        {
            return NotFound(new
            {
                message = "Support session not found"
            });
        }

        var messages = await _db.SupportMessages
            .Where(x => x.SupportSessionId == supportSessionId)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.MessageId,
                x.SenderUserId,
                x.SenderName,
                x.SenderRole,
                x.MessageText,
                x.MessageType,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(messages);
    }

    private static string CreateSupportSessionId(DateTime now)
    {
        return $"CHAT-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
    }

    private static string CreateTicketNo(DateTime now)
    {
        return $"SUP-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..27].ToUpperInvariant();
    }

    private static string CreateMessageId(DateTime now)
    {
        return $"MSG-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..27].ToUpperInvariant();
    }
}