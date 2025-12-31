using GlueFramework.Core.ConfigurationOptions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GlueFramework.Core.Security
{
    public class JwtTokenValidater
    {
        private readonly JwtSecurity _jwtSecurity;
        public JwtTokenValidater(IOptions<JwtSecurity> jwtSecurity)
        {
            _jwtSecurity = jwtSecurity.Value;
        }

        public bool ValidateJwtToken(string jwtToken, out System.Security.Claims.ClaimsPrincipal? claimsPrincipal)
        {
            try
            {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecurity.SecurityKey));
                var validateParameter = new TokenValidationParameters()
                {
                    ValidateLifetime = true,
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _jwtSecurity.Issuer,
                    ValidAudience = _jwtSecurity.Audience,
                    IssuerSigningKey = securityKey,
                };

                claimsPrincipal = new JwtSecurityTokenHandler().ValidateToken(jwtToken, validateParameter, out SecurityToken validatedToken);
                return true;
            }
            catch (Exception ex)
            {
                claimsPrincipal = null;
            }

            return false;
        }
    }
}
