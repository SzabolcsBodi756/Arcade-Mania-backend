using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcade_mania_backend_webAPI.Services
{
    public interface IJwtService
    {

        string GenerateJwtToken(string subjectId, string name, string role);

    }
}
