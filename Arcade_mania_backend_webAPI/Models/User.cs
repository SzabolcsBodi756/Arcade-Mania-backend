using System;
using System.Collections.Generic;

namespace Arcade_mania_backend_webAPI.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = "User";

    public virtual ICollection<UserHighScore> UserHighScores { get; set; } = new List<UserHighScore>();
}
