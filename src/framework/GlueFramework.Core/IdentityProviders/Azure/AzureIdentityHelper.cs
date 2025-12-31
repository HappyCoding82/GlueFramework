using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace GlueFramework.Core.IdentityProviders.Azure
{
    public class AzureIdentityHelper
    {
        private ConfigurationManager<OpenIdConnectConfiguration> _configurationManager;


        private async Task<OpenIdConnectConfiguration> GetOIDCWellknownConfigurationAsync(string tenantId)
        {
            var instance = "https://sts.windows.net/";
            var wellKnownEndpoint = $"{instance}{tenantId}/.well-known/openid-configuration";
            _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                 wellKnownEndpoint, new OpenIdConnectConfigurationRetriever());

            return await _configurationManager.GetConfigurationAsync();
        }

        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token, string audience)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token);
            //var tenantId = jsonToken.Claims.First(x => x.Type == "tid").Value;
            var tenantId = jsonToken.Issuer.Replace("https://sts.windows.net/", "").Replace("/", "");
            string myTenant = jsonToken.Issuer;
            var myAudience = audience;
            //var myIssuer = String.Format(CultureInfo.InvariantCulture, "https://sts.windows.net/{0}/", jsonToken.Issuer);
            //var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidAudience = myAudience,
                ValidIssuer = jsonToken.Issuer,
                IssuerSigningKeys = (await GetOIDCWellknownConfigurationAsync(tenantId)).SigningKeys,
                ValidateLifetime = false,
                //IssuerSigningKey = mySecurityKey
            };

            var validatedToken = (SecurityToken)new JwtSecurityToken();
            // Throws an Exception as the token is invalid (expired, invalid-formatted, etc.)  
            var rs = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            return rs;
        }

        public string PopulateLogonUrl(string callbackUrl,string scopes,string clientId,string nonce,string state,string tenantID = "")
        {
            tenantID = string.IsNullOrEmpty(tenantID) ? "organizations" : tenantID;
            return $"https://login.microsoftonline.com/{tenantID}/oauth2/v2.0/authorize?" +
                $"client_id={clientId}&redirect_uri={callbackUrl}&" +
                $"response_type=code&scope=openid%20profile%20offline_access%20{scopes}" +
                $"&response_mode=form_post&nonce={nonce}&state={state}";
        }

        private AccessTokenData PostAccessToken(string url, string para)
        {
            System.Text.Encoding en = System.Text.Encoding.UTF8;
            byte[] data = en.GetBytes(para);
            using (WebClient client = new WebClient())
            {
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //TLS1.2 Probably remove this is code is updated to .net 3.7
                    client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    data = client.UploadData(url, "POST", data);
                    string resStr = en.GetString(data);
                    var accessToken = JsonConvert.DeserializeObject<AccessTokenData>(resStr);
                    return accessToken;
                }
                catch (WebException ex)
                {
                    System.IO.Stream st = ex.Response.GetResponseStream();
                    System.IO.StreamReader readStream = new System.IO.StreamReader(st, en);
                    string message = string.IsNullOrEmpty(readStream.ReadToEnd()) ? string.Empty : readStream.ReadToEnd();
                    throw new Exception(ex.Message + ": " + message);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        public async Task<AccessTokenData> RefreshToken(string code,string clientId,string clientSecret,string redirectUrl)
        {
            var para = "";
            var responseType = "";
            var url = $"https://login.microsoftonline.com/organizations/oauth2/v2.0/token";
            if (!string.IsNullOrEmpty(code))
            {
                responseType = "grant_type=authorization_code";
                var strCode = $"code={code}";
                para = $"{responseType}&{clientId}&{clientSecret}&{redirectUrl}&{strCode}";
            }
            
            if (!string.IsNullOrEmpty(para))
            {
                AzureIdentityHelper tokenService = new AzureIdentityHelper();
                return tokenService.PostAccessToken(url, para);
            }
            return null;
        }
    }
}
