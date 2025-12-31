namespace GlueFramework.Core.ConfigurationOptions
{
    public class MailSettings
    {
        public string contact { get; set; } = string.Empty;
        public string from { get; set; } = string.Empty;
        public string smtp { get; set; } = string.Empty;
        public int port { get; set; }
        public string username { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;

        public bool enableSSL = true;
    }
}
