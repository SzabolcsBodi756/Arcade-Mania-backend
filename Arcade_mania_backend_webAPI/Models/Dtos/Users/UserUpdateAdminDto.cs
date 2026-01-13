namespace Arcade_mania_backend_webAPI.Models.Dtos.Users
{
    public class UserUpdateAdminDto
    {

        /// <summary>
        /// Új név (ha null vagy üres, akkor nem módosítjuk).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Új plain-text jelszó (ha nem null/üres, új hash-t számolunk).
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Teljes score-lista.
        /// Ha null: a score-okat NEM módosítjuk.
        /// Ha nem null: a meglévő score-kat felülírjuk, a hiányzókat töröljük.
        /// </summary>
        /// 

        public string? Role { get; set; }

        public List<UserUpdateScoreAdminDto>? Scores { get; set; }

    }
}
