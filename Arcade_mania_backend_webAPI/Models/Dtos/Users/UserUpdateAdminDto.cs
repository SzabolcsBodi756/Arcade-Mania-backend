namespace Arcade_mania_backend_webAPI.Models.Dtos.Users
{
    public class UserUpdateAdminDto
    {

        public string? Name { get; set; }

        public string? Password { get; set; }

        public string? Role { get; set; }

        public List<UserUpdateScoreAdminDto>? Scores { get; set; }

    }
}
