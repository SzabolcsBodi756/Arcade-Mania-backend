namespace Arcade_mania_backend_webAPI.Models.Dtos.Scores
{
    public class GameScoreDto
    {

        public Guid GameId { get; set; }

        public string GameName { get; set; } = null!;

        public int HighScore { get; set; }
    }
}
