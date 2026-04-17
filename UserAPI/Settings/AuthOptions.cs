using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UserAPI.Settings
{
    public class AuthOptions
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }
}
