using Arcade_mania_backend_webAPI.Models.Dtos.Scores;

namespace Arcade_mania_backend_webAPI.Models.Dtos.Users
{
    public class UserLoginResultDto
    {

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        public List<GameScoreDto> Scores { get; set; } = new();
    }
}
