using Arcade_mania_backend_webAPI.Models.Dtos.Scores;

namespace Arcade_mania_backend_webAPI.Models.Dtos.Users
{
    public class UserDataAdminDto
    {

        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string Role { get; set; } = "User";

        public List<GameScoreDto> Scores { get; set; } = new();

    }
}
