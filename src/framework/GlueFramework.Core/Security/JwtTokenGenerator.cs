using GlueFramework.Core.ConfigurationOptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GlueFramework.Core.Security
{
    public class JwtTokenGenerator
    {
        private readonly JwtSecurity _jwtSecurity;

        public JwtTokenGenerator(IOptions<JwtSecurity> jwtSecurity)
        {
            _jwtSecurity = jwtSecurity.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public async Task<string> GenerateToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecurity.SecurityKey));
            var authTime = DateTime.UtcNow;
            
            DateTime? expiresAt = _jwtSecurity.ExpireMinutes == null ? authTime.AddMonths(11) :  
                authTime.AddSeconds(Convert.ToDouble(_jwtSecurity.ExpireMinutes) * 60);

            var jwtToken = new JwtSecurityToken(
                 issuer: _jwtSecurity.Issuer,
                 audience: _jwtSecurity.Audience,
                 claims: claims,
                 expires: expiresAt,
                 signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
             );

            return new JwtSecurityTokenHandler().WriteToken(jwtToken);

        }

    }
}
