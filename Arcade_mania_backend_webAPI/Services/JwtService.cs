using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Arcade_mania_backend_webAPI.Services
{
    public class JwtService : IJwtService
    {

        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {

            _config = config;

        }

        public string GenerateJwtToken(string subjectId, string name, string role)
        {

            var jwt = _config.GetSection("Jwt");

            var keyString = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");

            var issuer = jwt["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

            var audience = jwt["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

            int expireMinutes = 60;

            if (int.TryParse(jwt["ExpireMinutes"], out var parsed))
            {
                expireMinutes = parsed;
            }
                
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
