using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UserAPI.Settings
{
    public class AuthOptions
    {
        public const string ISSUER = "UserService";
        public const string AUDIENCE = "FlightService";
        const string KEY = "YwLMG7gvyAc4iZaMmhPegTH4wDE9N21k";
        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
    }
}
