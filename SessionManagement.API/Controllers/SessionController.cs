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

        private static SessionDetailsDto MapToSessionDetails(Session session)
        {
            return new SessionDetailsDto
            {
                Id = session.Id,
                Title = session.Title,
                Description = session.Description,
                Location = session.Location,
                CreatorId = session.CreatorId,
                Type = session.Type,
                Status = session.Status,
                Category = session.Category,
                MaxParticipants = session.MaxParticipants,
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        // Creation Endpoints
        [HttpPost("create/quick")]
        public async Task<ActionResult<SessionDetailsDto>> CreateQuickSession([FromBody] QuickSessionDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("create/private")]
        public async Task<ActionResult<SessionDetailsDto>> CreatePrivateSession([FromBody] PrivateSessionDto dto)
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
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

        [HttpPost("create/test")]
        public async Task<ActionResult> CreateTestSession([FromBody] TestSessionDto dto)
        {
            try
            {
                var session = new Session
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    CreatorId = dto.CreatorId,
                    Type = SessionType.Test,
                    Status = SessionStatus.Created,
                    Category = ParanormalCategory.Other, // Default for test sessions
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                return Ok(MapToSessionDetails(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("create/group")]
        public async Task<ActionResult> CreateGroupSession([FromBody] GroupSessionDto dto)
        {
            try
            {
                var session = new Session
                {
                    Title = dto.Title,
                    Description = dto.Description,
                    Location = dto.Location,
                    CreatorId = dto.CreatorId,
                    Type = SessionType.Group,
                    MaxParticipants = dto.MaxParticipants,
                    Status = SessionStatus.Created,
                    Category = dto.Category,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                if (dto.RequiredEquipment != null)
                {
                    foreach (var equipment in dto.RequiredEquipment)
                    {
                        _context.RequiredEquipment.Add(new RequiredEquipment
                        {
                            SessionId = session.Id,
                            EquipmentName = equipment.EquipmentName,
                            Description = equipment.Description,
                            IsMandatory = equipment.IsMandatory,
                            Quantity = equipment.Quantity
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(MapToSessionDetails(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("create/set-challenges")]
        public async Task<ActionResult> SetSessionChallenges([FromBody] SessionChallengesDto dto)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(dto.SessionId);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                foreach (var challenge in dto.Challenges)
                {
                    _context.SessionChallenges.Add(new SessionChallenge
                    {
                        SessionId = session.Id,
                        ChallengeId = challenge.Id,
                        AssignedAt = DateTime.UtcNow,
                        Status = ChallengeStatus.Assigned
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Challenges added to session" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("create/set-rules")]
        public async Task<ActionResult> SetSessionRules([FromBody] SessionRulesDto dto)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(dto.SessionId);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                foreach (var rule in dto.Rules)
                {
                    // Parse the category string to enum
                    if (!Enum.TryParse<RuleCategory>(rule.Category, true, out var category))
                    {
                        category = RuleCategory.Other; // Default to Other if parsing fails
                    }

                    _context.SessionRules.Add(new SessionRule
                    {
                        SessionId = session.Id,
                        Title = rule.Title,
                        Description = rule.Description,
                        IsMandatory = rule.IsMandatory,
                        Category = category
                    });
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Rules added to session" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
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
                    Role = p.Role
                })
                .ToListAsync();

            return Ok(participants);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetSession(Guid id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                return Ok(MapToSessionDetails(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/logs")]
        public async Task<ActionResult> GetSessionLogs(Guid id)
        {
            try
            {
                var logs = await _context.SessionLogs
                    .Where(l => l.SessionId == id)
                    .OrderByDescending(l => l.Timestamp)
                    .ToListAsync();

                return Ok(new { logs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/challenges")]
        public async Task<ActionResult> GetSessionChallenges(Guid id)
        {
            try
            {
                var challenges = await _context.SessionChallenges
                    .Where(c => c.SessionId == id)
                    .ToListAsync();

                return Ok(new { challenges });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/location")]
        public async Task<ActionResult> GetSessionLocation(Guid id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                return Ok(new SessionLocationDto
                {
                    Location = session.Location,
                    Latitude = session.Latitude,
                    Longitude = session.Longitude
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/owner")]
        public async Task<ActionResult> GetSessionOwner(Guid id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                return Ok(new { userId = session.CreatorId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/rules")]
        public async Task<ActionResult> GetSessionRulesBySessionId(Guid id)
        {
            try
            {
                var rules = await _context.SessionRules
                    .Where(r => r.SessionId == id)
                    .ToListAsync();

                return Ok(new { rules });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/created")]
        public async Task<ActionResult> GetSessionCreationInfo(Guid id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                return Ok(new
                {
                    createdAt = session.CreatedAt,
                    creatorId = session.CreatorId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Activation Endpoints
        [HttpPost("{id}/activate")]
        public async Task<ActionResult> ActivateSession(Guid id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                session.Status = SessionStatus.Active;
                session.ActualStartTime = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Session activated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("activate/user/{id}")]
        public async Task<ActionResult> AddUserToSession(Guid id, [FromBody] SessionParticipantDto participantDto)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                var participant = new SessionParticipant
                {
                    SessionId = id,
                    UserId = participantDto.UserId,
                    Role = participantDto.Role,
                    JoinedAt = DateTime.UtcNow,
                    Status = ParticipantStatus.Joined
                };

                _context.SessionParticipants.Add(participant);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User added to session" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("activate/challenge/{id}")]
        public async Task<ActionResult> AddChallengeToSession(Guid id, [FromBody] SessionChallengeDto dto)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                var sessionChallenge = new SessionChallenge
                {
                    SessionId = id,
                    ChallengeId = dto.ChallengeId,
                    AssignedAt = DateTime.UtcNow,
                    Status = ChallengeStatus.Assigned
                };

                _context.SessionChallenges.Add(sessionChallenge);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Challenge added to session" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("activate/recent/{id}")]
        public async Task<ActionResult> GetRecentSessionActions(Guid id)
        {
            try
            {
                var logs = await _context.SessionLogs
                    .Where(l => l.SessionId == id)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(10)
                    .ToListAsync();

                return Ok(new { recentActions = logs });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("activate/rule/{id}")]
        public async Task<ActionResult> GetSessionRules(Guid id)
        {
            try
            {
                var rules = await _context.SessionRules
                    .Where(r => r.SessionId == id)
                    .ToListAsync();

                return Ok(new { activeRules = rules });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("activate/time/{id}")]
        public async Task<ActionResult> SetSessionTime(Guid id, [FromBody] SessionTimeDto timeDto)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);
                if (session == null)
                    return NotFound(new { message = "Session not found" });

                session.ScheduledStartTime = timeDto.StartTime;
                session.ScheduledEndTime = timeDto.StartTime.AddMinutes(timeDto.Duration);
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    startTime = session.ScheduledStartTime,
                    endTime = session.ScheduledEndTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Existing Sessions Endpoints
        [HttpGet("existing")]
        public async Task<ActionResult> GetAllSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var sessionDtos = sessions.Select(MapToSessionDetails).ToList();
                return Ok(new { sessions = sessionDtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/open")]
        public async Task<ActionResult> GetOpenSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .Where(s => s.Status == SessionStatus.Created || s.Status == SessionStatus.Scheduled)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var sessionDtos = sessions.Select(MapToSessionDetails).ToList();
                return Ok(new { sessions = sessionDtos });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/nearby")]
        public async Task<ActionResult> GetNearbySessions([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radiusKm = 10)
        {
            try
            {
                var sessions = await _context.Sessions
                    .Where(s => s.Latitude != null && s.Longitude != null)
                    .ToListAsync();

                // Calculate distance and filter
                var nearbyResults = sessions
                    .Select(s => new
                    {
                        Session = s,
                        Distance = CalculateDistance(lat, lon, s.Latitude.Value, s.Longitude.Value)
                    })
                    .Where(x => x.Distance <= radiusKm)
                    .OrderBy(x => x.Distance)
                    .Select(x => new
                    {
                        Id = x.Session.Id,
                        Title = x.Session.Title,
                        Location = x.Session.Location,
                        Distance = Math.Round(x.Distance, 2),
                        Status = x.Session.Status
                    })
                    .ToList();

                return Ok(new { sessions = nearbyResults });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/private")]
        public async Task<ActionResult> GetPrivateSessions([FromQuery] string userId)
        {
            try
            {
                var sessions = await _context.Sessions
                    .Where(s => s.IsPrivate &&
                           (s.CreatorId == userId ||
                            _context.SessionParticipants.Any(p => p.SessionId == s.Id && p.UserId == userId)))
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => MapToSessionDetails(s))
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/completed")]
        public async Task<ActionResult> GetCompletedSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .Where(s => s.Status == SessionStatus.Completed)
                    .OrderByDescending(s => s.ActualEndTime)
                    .Select(s => MapToSessionDetails(s))
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/popular")]
        public async Task<ActionResult> GetPopularSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .OrderByDescending(s => s.ViewCount)
                    .ThenByDescending(s => _context.SessionParticipants.Count(p => p.SessionId == s.Id))
                    .Take(10)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Title = s.Title,
                        ParticipantCount = _context.SessionParticipants.Count(p => p.SessionId == s.Id),
                        ViewCount = s.ViewCount,
                        Rating = s.Rating
                    })
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/recently-updated")]
        public async Task<ActionResult> GetRecentlyUpdatedSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .OrderByDescending(s => s.UpdatedAt)
                    .Take(10)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Title = s.Title,
                        LastUpdate = s.UpdatedAt,
                        Status = s.Status
                    })
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/joinable")]
        public async Task<ActionResult> GetJoinableSessions()
        {
            try
            {
                var sessions = await _context.Sessions
                    .Where(s => !s.IsPrivate &&
                           (s.Status == SessionStatus.Created || s.Status == SessionStatus.Scheduled) &&
                           (_context.SessionParticipants.Count(p => p.SessionId == s.Id) < s.MaxParticipants))
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Title = s.Title,
                        SpotsAvailable = s.MaxParticipants - _context.SessionParticipants.Count(p => p.SessionId == s.Id),
                        Requirements = _context.RequiredEquipment
                            .Where(e => e.SessionId == s.Id && e.IsMandatory)
                            .Select(e => e.EquipmentName)
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("existing/category/{type}")]
        public async Task<ActionResult> GetSessionsByCategory(string type)
        {
            try
            {
                if (!Enum.TryParse<ParanormalCategory>(type, true, out var category))
                    return BadRequest(new { message = "Invalid category type" });

                var sessions = await _context.Sessions
                    .Where(s => s.Category == category)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => MapToSessionDetails(s))
                    .ToListAsync();

                return Ok(new { sessions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("status")]
        public ActionResult GetStatus()
        {
            return Ok(new { status = "healthy" });
        }

        // Helper method for distance calculation
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula
            const double R = 6371; // Earth radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }       
    }
}