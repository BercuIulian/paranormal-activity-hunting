using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SessionManagement.API.Data;
using SessionManagement.API.Models;
using SessionManagement.API.DTOs;

namespace SessionManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly SessionDbContext _context;

        public SessionController(SessionDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<ActionResult<SessionResponseDto>> CreateSession(CreateSessionDto createDto)
        {
            var session = new Session
            {
                Title = createDto.Title,
                Description = createDto.Description,
                Location = createDto.Location,
                ScheduledTime = createDto.ScheduledTime,
                Status = SessionStatus.Scheduled,
                CreatorId = createDto.CreatorId,
                ParticipantIds = new List<string> { createDto.CreatorId },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(MapToSessionResponse(session));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SessionResponseDto>> GetSession(Guid id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
                return NotFound();

            return Ok(MapToSessionResponse(session));
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult<SessionResponseDto>> ActivateSession(Guid id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
                return NotFound();

            if (session.Status != SessionStatus.Scheduled)
                return BadRequest("Session can only be activated if it's in Scheduled status");

            session.Status = SessionStatus.Active;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToSessionResponse(session));
        }

        [HttpGet("existing")]
        public async Task<ActionResult<List<SessionResponseDto>>> GetExistingSessions()
        {
            var sessions = await _context.Sessions
                .Where(s => s.Status != SessionStatus.Cancelled)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions.Select(MapToSessionResponse));
        }

        [HttpGet("status")]
        public ActionResult GetStatus()
        {
            return Ok(new { status = "healthy" });
        }

        private SessionResponseDto MapToSessionResponse(Session session)
        {
            return new SessionResponseDto
            {
                Id = session.Id,
                Title = session.Title,
                Description = session.Description,
                Location = session.Location,
                ScheduledTime = session.ScheduledTime,
                Status = session.Status.ToString(),
                ParticipantIds = session.ParticipantIds,
                CreatorId = session.CreatorId,
                CreatedAt = session.CreatedAt
            };
        }
    }
}