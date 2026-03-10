using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;
using Arcade_mania_backend_webAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NETCore.Encrypt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Arcade_mania_backend_webAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly ArcadeManiaDatasContext _context;

        private readonly string _passwordKey;

        private readonly IConfiguration _config;

        private readonly IJwtService _jwtService;


        public UsersController(ArcadeManiaDatasContext context, IConfiguration config, IJwtService jwtService)
        {

            _context = context;

            _config = config;

            _jwtService = jwtService;

            _passwordKey = config["Crypto:PasswordKey"]
                ?? throw new InvalidOperationException("Crypto:PasswordKey is not configured.");
        }


        // Regisztráció
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(UserRegisterDto dto)
        {

            try
            {

                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Password))

                {
                    return StatusCode(400, new { message = "Név és jelszó megadása kötelező.", result = "" });

                }

                bool exists = await _context.Users.AnyAsync(u => u.UserName == dto.Name);

                if (exists)
                {

                    return StatusCode(409, new { message = "Ez a név már foglalt.", result = "" });

                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = dto.Name,
                    PasswordHash = EncryptProvider.AESEncrypt(dto.Password, _passwordKey),

                    // default role
                    Role = "User"
                };


                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();


                var games = await _context.Games.ToListAsync();

                foreach (var game in games)
                {

                    _context.UserHighScores.Add(new UserHighScore
                    {
                        UserId = user.Id,
                        GameId = game.Id,
                        HighScore = 0u
                    });
                }

                if (games.Count > 0)
                {

                    await _context.SaveChangesAsync();

                }

                var result = new UserDto
                {
                    Id = user.Id,
                    Name = user.UserName
                };

                return StatusCode(201, new { message = "Sikeres regisztráció", result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        //  login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult> Login(UserLoginDto dto)
        {

            try
            {

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.Name);

                if (user == null)
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                string storedPlainPassword;

                try
                {

                    storedPlainPassword = EncryptProvider.AESDecrypt(user.PasswordHash, _passwordKey);

                }
                catch
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                if (storedPlainPassword != dto.Password)
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                var scores = await _context.UserHighScores
                    .Where(s => s.UserId == user.Id)
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new GameScoreDto
                        {
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                var result = new UserLoginResultDto
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Scores = scores
                };

                // JWT, a role a DB-ből jön
                var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;

                var token = _jwtService.GenerateJwtToken(
                    subjectId: user.Id.ToString(),
                    name: user.UserName,
                    role: role
                );

                return StatusCode(200, new { message = "Sikeres bejelentkezés", token, result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        //  Admin login
        [AllowAnonymous]
        [HttpPost("admin/login")]
        public async Task<ActionResult> AdminLogin(UserLoginDto dto)
        {

            try
            {

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.Name);

                if (user == null)
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                string storedPlainPassword;

                try
                {

                    storedPlainPassword = EncryptProvider.AESDecrypt(user.PasswordHash, _passwordKey);

                }
                catch
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                if (storedPlainPassword != dto.Password)
                {

                    return StatusCode(401, new { message = "Hibás felhasználónév vagy jelszó.", result = "" });

                }

                var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;


                if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {

                    return StatusCode(403, new { message = "Nincs admin jogosultság.", result = "" });

                }

                var token = _jwtService.GenerateJwtToken(
                    subjectId: user.Id.ToString(),
                    name: user.UserName,
                    role: "Admin"
                );

                var result = new
                {
                    Id = user.Id,
                    Name = user.UserName
                };

                return StatusCode(200, new { message = "Sikeres admin bejelentkezés", token, result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<ActionResult> GetAllUsersPublic()
        {

            try
            {

                var users = await _context.Users.ToListAsync();

                var allScores = await _context.UserHighScores
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new
                        {
                            s.UserId,
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                var result = users.Select(u => new UserDataPublicDto
                {
                    Name = u.UserName,
                    Scores = allScores
                        .Where(x => x.UserId == u.Id)
                        .Select(x => new GameScoreDto
                        {
                            GameId = x.GameId,
                            GameName = x.GameName,
                            HighScore = x.HighScore
                        })
                        .ToList()
                }).ToList();

                return StatusCode(200, new { message = "Sikeres lekérdezés (public)", result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [AllowAnonymous]
        [HttpGet("public/{id:guid}")]
        public async Task<ActionResult> GetUserPublicById(Guid id)
        {

            try
            {

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {

                    return StatusCode(404, new { message = "Nincs ilyen felhasználó.", result = "" });

                }

                var scores = await _context.UserHighScores
                    .Where(s => s.UserId == id)
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new GameScoreDto
                        {
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                var result = new UserDataPublicDto
                {
                    Name = user.UserName,
                    Scores = scores
                };

                return StatusCode(200, new { message = "Sikeres lekérdezés (public, ID alapján)", result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        // Bejelentkezett user saját score-ja
        [Authorize]
        [HttpGet("me/scores")]
        public async Task<ActionResult> GetMyScores()
        {

            try
            {

                var userId = GetUserIdFromToken();
                if (userId == Guid.Empty)
                    return StatusCode(401, new { message = "Érvénytelen token.", result = "" });

                var scores = await _context.UserHighScores
                    .Where(s => s.UserId == userId)
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new GameScoreDto
                        {
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                return Ok(new { message = "Sikeres lekérdezés", result = scores });
            }
            catch (Exception ex)
            {

                return BadRequest(new { message = ex.Message, result = "" });

            }
        }


        // Bejelentkezett user saját score-jainak frissítése
        [Authorize]
        [HttpPost("me/scores")]
        public async Task<ActionResult> UpdateMyScores([FromBody] List<GameScoreDto> scores)
        {

            try
            {

                var userId = GetUserIdFromToken();

                if (userId == Guid.Empty)
                {
                    return StatusCode(401, new { message = "Érvénytelen token.", result = "" });
                }

                if (scores == null)
                {
                    return StatusCode(400, new { message = "Hiányzó body.", result = "" });
                }
                    
                var existingScores = await _context.UserHighScores
                    .Where(s => s.UserId == userId)
                    .ToListAsync();

                var incomingGameIds = scores.Select(s => s.GameId).ToHashSet();

                // törlés
                var toDelete = existingScores
                    .Where(es => !incomingGameIds.Contains(es.GameId))
                    .ToList();

                if (toDelete.Any())
                    _context.UserHighScores.RemoveRange(toDelete);

                // add/update
                foreach (var dto in scores)
                {
                    var existing = existingScores.FirstOrDefault(x => x.GameId == dto.GameId);

                    if (existing == null)
                    {

                        await _context.UserHighScores.AddAsync(new UserHighScore
                        {
                            UserId = userId,
                            GameId = dto.GameId,
                            HighScore = (uint)dto.HighScore
                        });

                    }
                    else
                    {

                        existing.HighScore = (uint)dto.HighScore;

                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Pontszámok frissítve", result = "" });
            }
            catch (Exception ex)
            {

                return BadRequest(new { message = ex.Message, result = "" });

            }
        }


        private Guid GetUserIdFromToken()
        {

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
                
            return Guid.Empty;
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult> GetAllUsersAdmin()
        {

            try
            {

                var users = await _context.Users.ToListAsync();

                var allScores = await _context.UserHighScores
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new
                        {
                            s.UserId,
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                var result = users.Select(u => new UserDataAdminDto
                {
                    Id = u.Id,
                    Name = u.UserName,
                    Password = EncryptProvider.AESDecrypt(u.PasswordHash, _passwordKey),
                    Scores = allScores
                        .Where(x => x.UserId == u.Id)
                        .Select(x => new GameScoreDto
                        {
                            GameId = x.GameId,
                            GameName = x.GameName,
                            HighScore = x.HighScore
                        })
                        .ToList(),
                    Role = string.IsNullOrWhiteSpace(u.Role) ? "User" : u.Role
                }).ToList();

                return StatusCode(200, new { message = "Sikeres lekérdezés (admin)", result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{id:guid}")]
        public async Task<ActionResult> GetUserAdminById(Guid id)
        {

            try
            {

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return StatusCode(404, new { message = "Nincs ilyen felhasználó.", result = "" });
                }

                var scores = await _context.UserHighScores
                    .Where(s => s.UserId == id)
                    .Join(_context.Games,
                        s => s.GameId,
                        g => g.Id,
                        (s, g) => new GameScoreDto
                        {
                            GameId = g.Id,
                            GameName = g.Name,
                            HighScore = (int)s.HighScore
                        })
                    .ToListAsync();

                var result = new UserDataAdminDto
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Password = EncryptProvider.AESDecrypt(user.PasswordHash, _passwordKey),
                    Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role,
                    Scores = scores
                };

                return StatusCode(200, new { message = "Sikeres lekérdezés (admin, ID alapján)", result });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("admin")]
        public async Task<ActionResult> CreateUserAdmin(UserCreateAdminDto dto)
        {

            try
            {

                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Password))
                {
                    return StatusCode(400, new { message = "Név és jelszó megadása kötelező.", result = "" });
                }

                var cleanName = dto.Name.Trim();
                
                bool exists = await _context.Users.AnyAsync(u => u.UserName == cleanName);

                if (exists)
                {
                    return StatusCode(409, new { message = "Ez a név már foglalt.", result = "" });
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = cleanName,
                    PasswordHash = EncryptProvider.AESEncrypt(dto.Password.Trim(), _passwordKey),

                    Role = NormalizeRole(dto.Role)

                };

                await _context.Users.AddAsync(user);

                await _context.SaveChangesAsync();

                var games = await _context.Games.ToListAsync();

                foreach (var game in games)
                {

                    _context.UserHighScores.Add(new UserHighScore
                    {
                        UserId = user.Id,
                        GameId = game.Id,
                        HighScore = 0u
                    });
                }

                if (games.Count > 0)
                {
                    await _context.SaveChangesAsync();
                }
                    
                var result = new UserDataAdminDto
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Password = dto.Password.Trim(),
                    Role = NormalizeRole(dto.Role),
                    Scores = games.Select(g => new GameScoreDto
                    {
                        GameId = g.Id,
                        GameName = g.Name,
                        HighScore = 0
                    }).ToList()
                };

                return StatusCode(201, new { message = "Új felhasználó sikeresen létrehozva (admin felületről).", result });

            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id:guid}")]
        public async Task<ActionResult> UpdateUserAdmin(Guid id, UserUpdateAdminDto dto)
        {

            try
            {

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return StatusCode(404, new { message = "Nincs ilyen felhasználó.", result = "" });
                }

                if (!string.IsNullOrWhiteSpace(dto.Name))
                {

                    var newName = dto.Name.Trim();

                    if (newName != user.UserName)
                    {

                        bool nameExists = await _context.Users
                            .AnyAsync(u => u.UserName == newName && u.Id != id);

                        if (nameExists)
                        {

                            return StatusCode(409, new { message = "Ez a felhasználónév már foglalt.", result = "" });

                        }

                        user.UserName = newName;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.Password))
                {

                    var newPassword = dto.Password.Trim();

                    user.PasswordHash = EncryptProvider.AESEncrypt(newPassword, _passwordKey);

                }

                if (dto.Scores != null)
                {

                    var existingScores = await _context.UserHighScores
                        .Where(s => s.UserId == id)
                        .ToListAsync();

                    var incomingGameIds = dto.Scores.Select(s => s.GameId).ToHashSet();

                    var toDelete = existingScores.Where(es => !incomingGameIds.Contains(es.GameId)).ToList();
                    if (toDelete.Count > 0)
                        _context.UserHighScores.RemoveRange(toDelete);

                    foreach (var scoreDto in dto.Scores)
                    {

                        var existing = existingScores.FirstOrDefault(es => es.GameId == scoreDto.GameId);

                        if (existing == null)
                        {

                            await _context.UserHighScores.AddAsync(new UserHighScore
                            {
                                UserId = id,
                                GameId = scoreDto.GameId,
                                HighScore = (uint)scoreDto.HighScore
                            });

                        }
                        else
                        {

                            existing.HighScore = (uint)scoreDto.HighScore;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.Role))
                {

                    user.Role = NormalizeRole(dto.Role);

                }

                await _context.SaveChangesAsync();

                return StatusCode(200, new { message = "Felhasználó és pontszámok sikeresen módosítva (admin)", result = "" });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id:guid}")]
        public async Task<ActionResult> DeleteUserAdmin(Guid id)
        {

            try
            {

                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return StatusCode(404, new { message = "Nincs ilyen felhasználó.", result = "" });
                }

                var scores = await _context.UserHighScores
                    .Where(s => s.UserId == id)
                    .ToListAsync();

                if (scores.Count > 0)
                    _context.UserHighScores.RemoveRange(scores);

                _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                return StatusCode(200, new { message = "Felhasználó és pontszámok sikeresen törölve (admin)", result = "" });
            }
            catch (Exception ex)
            {

                return StatusCode(400, new { message = ex.Message, result = "" });

            }
        }


        private static string NormalizeRole(string? role)
        {

            if (string.IsNullOrWhiteSpace(role))
            {
                return "User";
            }

            role = role.Trim();

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return "Admin";
            }

            if (role.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                return "User";
            }

            return "User";
        }
    }
}
