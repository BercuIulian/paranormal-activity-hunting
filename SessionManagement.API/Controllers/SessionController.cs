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

        // Creation Endpoints
        [HttpPost("create/quick")]
        public async Task<ActionResult<SessionDetailsDto>> CreateQuickSession(QuickSessionDto dto)
        {
            var session = new Session
            {
                Title = dto.Title,
                Location = dto.Location,
                CreatorId = dto.CreatorId,
                Type = SessionType.Quick,
                Status = SessionStatus.Created,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(MapToSessionDetails(session));
        }

        [HttpPost("create/private")]
        public async Task<ActionResult<SessionDetailsDto>> CreatePrivateSession(PrivateSessionDto dto)
        {
            var session = new Session
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                CreatorId = dto.CreatorId,
                Type = SessionType.Private,
                IsPrivate = true,
                Status = SessionStatus.Created,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(MapToSessionDetails(session));
        }

        [HttpPost("create/schedule")]
        public async Task<ActionResult<SessionDetailsDto>> CreateScheduledSession(ScheduledSessionDto dto)
        {
            var session = new Session
            {
                Title = dto.Title,
                Description = dto.Description,
                Location = dto.Location,
                CreatorId = dto.CreatorId,
                Type = SessionType.Scheduled,
                Status = SessionStatus.Scheduled,
                ScheduledStartTime = dto.ScheduledStartTime,
                ScheduledEndTime = dto.ScheduledEndTime,
                MaxParticipants = dto.MaxParticipants,
                Category = dto.Category,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(MapToSessionDetails(session));
        }

        // Details Endpoints
        [HttpGet("{id}/details")]
        public async Task<ActionResult<SessionDetailsDto>> GetSessionDetails(Guid id)
        {
            var session = await _context.Sessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            return Ok(MapToSessionDetails(session));
        }

        [HttpGet("{id}/participants")]
        public async Task<ActionResult> GetSessionParticipants(Guid id)
        {
            var participants = await _context.SessionParticipants
                .Where(p => p.SessionId == id)
                .Select(p => new SessionParticipantDto
                {
                    UserId = p.UserId,
                    Role = p.Role,
                    JoinedAt = p.JoinedAt
                })
                .ToListAsync();

            return Ok(participants);
        }

        // Activation Endpoints
        [HttpPost("activate/user/{id}")]
        public async Task<ActionResult> AddUserToSession(Guid id, [FromBody] string userId)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session == null)
                return NotFound();

            var participant = new SessionParticipant
            {
                SessionId = id,
                UserId = userId,
                Role = ParticipantRole.Investigator,
                JoinedAt = DateTime.UtcNow,
                Status = ParticipantStatus.Joined
            };

            _context.SessionParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User added to session" });
        }

        // Existing Sessions Endpoints
        [HttpGet("existing/open")]
        public async Task<ActionResult> GetOpenSessions()
        {
            var sessions = await _context.Sessions
                .Where(s => s.Status == SessionStatus.Created || s.Status == SessionStatus.Scheduled)
                .Select(s => MapToSessionDetails(s))
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("existing/nearby")]
        public async Task<ActionResult> GetNearbySessions([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radiusKm = 10)
        {
            var sessions = await _context.Sessions
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
                .Select(s => new
                {
                    Session = s,
                    Distance = CalculateDistance(lat, lon, s.Latitude.Value, s.Longitude.Value)
                })
                .Where(x => x.Distance <= radiusKm)
                .OrderBy(x => x.Distance)
                .Select(x => MapToSessionDetails(x.Session))
                .ToListAsync();

            return Ok(sessions);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Implement distance calculation (Haversine formula)
            return 0; // Placeholder
        }

        private SessionDetailsDto MapToSessionDetails(Session session)
        {
            return new SessionDetailsDto
            {
                Id = session.Id,
                Title = session.Title,
                Description = session.Description,
                Type = session.Type,
                Status = session.Status,
                Location = session.Location,
                CreatedAt = session.CreatedAt,
                CreatorId = session.CreatorId,
                Category = session.Category,
                ParticipantCount = session.Participants?.Count ?? 0
            };
        }
    }
}