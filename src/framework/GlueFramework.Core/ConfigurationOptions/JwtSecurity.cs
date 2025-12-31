namespace GlueFramework.Core.ConfigurationOptions
{
    public class JwtSecurity
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecurityKey { get; set; }
        public long? ExpireMinutes { get; set; }
    }
}
