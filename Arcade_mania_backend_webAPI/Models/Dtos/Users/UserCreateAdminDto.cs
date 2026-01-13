namespace Arcade_mania_backend_webAPI.Models.Dtos.Users
{
    public class UserCreateAdminDto
    {

        public string Name { get; set; } = null!;
        public string Password { get; set; } = null!;

        public string Role { get; set; } = "User";

    }
}
