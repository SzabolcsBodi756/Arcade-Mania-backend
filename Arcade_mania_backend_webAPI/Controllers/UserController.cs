using Arcade_mania_backend_webAPI.Models;
using Arcade_mania_backend_webAPI.Models.Dtos.Auth;
using Arcade_mania_backend_webAPI.Models.Dtos.Users;
using Arcade_mania_backend_webAPI.Models.Dtos.Scores;
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

        public UsersController(ArcadeManiaDatasContext context, IConfiguration config)
        {
            _context = context;
            _config = config;

            _passwordKey = config["Crypto:PasswordKey"]
                ?? throw new InvalidOperationException("Crypto:PasswordKey is not configured.");
        }

        // --------------------------
        //  WPF: AUTOMATA ADMIN TOKEN (DTO NÉLKÜL)
        //  POST: api/users/admin-token
        //  Body: { "serviceKey": "..." }
        // --------------------------
        [AllowAnonymous]
        [HttpPost("admin-token")]
        public ActionResult GetAdminToken([FromBody] JsonElement body)
        {
            try
            {
                var expected = _config["Admin:ServiceKey"];
                if (string.IsNullOrWhiteSpace(expected))
                {
                    return StatusCode(500, new { message = "Admin:ServiceKey is not configured.", result = "" });
                }

                if (!body.TryGetProperty("serviceKey", out var skProp))
                {
                    return Unauthorized(new { message = "Missing serviceKey.", result = "" });
                }

                var provided = skProp.GetString();
                if (string.IsNullOrWhiteSpace(provided) || provided != expected)
                {
                    return Unauthorized(new { message = "Invalid admin service key.", result = "" });
                }

                var token = GenerateJwtToken(
                    subjectId: "WPF_ADMIN",
                    name: "WPF_ADMIN",
                    role: "Admin"
                );

                return Ok(new { message = "Admin token generated.", token, result = "" });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message, result = "" });
            }
        }

        // --------------------------
        //  REGISTER
        // --------------------------
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult> Register(UserRegisterDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.Password))
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
                    PasswordHash = EncryptProvider.AESEncrypt(dto.Password, _passwordKey)
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
                    await _context.SaveChangesAsync();

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

        // --------------------------
        //  LOGIN (JWT + user result)
        // --------------------------
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

                // USER JWT
                var token = GenerateJwtToken(
                    subjectId: user.Id.ToString(),
                    name: user.UserName,
                    role: "User"
                );

                return StatusCode(200, new { message = "Sikeres bejelentkezés", token, result });
            }
            catch (Exception ex)
            {
                return StatusCode(400, new { message = ex.Message, result = "" });
            }
        }

        // --------------------------
        //  PUBLIC
        // --------------------------
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

        // --------------------------
        //  ADMIN (WPF auto-token -> Role=Admin)
        // --------------------------
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
                        .ToList()
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
                if (string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.Password))
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
                    PasswordHash = EncryptProvider.AESEncrypt(dto.Password.Trim(), _passwordKey)
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
                    await _context.SaveChangesAsync();

                var result = new UserDataAdminDto
                {
                    Id = user.Id,
                    Name = user.UserName,
                    Password = dto.Password.Trim(),
                    Scores = games.Select(g => new GameScoreDto
                    {
                        GameId = g.Id,
                        GameName = g.Name,
                        HighScore = 0
                    }).ToList()
                };

                return StatusCode(201, new { message = "Új felhasználó sikeresen létrehozva (admin).", result });
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

        // --------------------------
        // JWT helper (egyetlen helyen)
        // --------------------------
        private string GenerateJwtToken(string subjectId, string name, string role)
        {
            var jwt = _config.GetSection("Jwt");

            var keyString = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
            var audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

            int expireMinutes = 60;
            if (int.TryParse(jwt["ExpireMinutes"], out var parsed))
                expireMinutes = parsed;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, subjectId),
                new Claim(ClaimTypes.Name, name),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
