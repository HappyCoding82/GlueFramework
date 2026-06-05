using GlueFramework.CustomSysSettingsModule.Abstrations;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace GlueFramework.CustomSysSettingsModule.Services
{
    public class KeyVaultService: IKeyVaultService
    {
        private readonly IHostEnvironment _env;
        AesGcmCryptoService? _crypto = null;

        public KeyVaultService(IHostEnvironment env)
        {
            _env = env;
            var keyVaultsFolder = GetKeyVaultDirectory();
            Directory.CreateDirectory(keyVaultsFolder);
            InitCrypto();
        }

        private void InitCrypto()
        {
            if (_crypto == null)
            {
                var secret = Environment.GetEnvironmentVariable("KEYVAULT_SECRET");
                secret = string.IsNullOrEmpty(secret) ? "abcdef1234567890" :secret;
                _crypto = new AesGcmCryptoService(System.Text.Encoding.UTF8.GetBytes(secret), 16);
            }
        }

        public async Task<string> SaveData(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            {
                throw new Exception("Key or value is empty");
            }

            var encrypted = _crypto!.EncryptToBase64(value);

            var filePath = GetKeyVaultFullPath(key);

            await System.IO.File.WriteAllTextAsync(filePath, encrypted);

            return filePath;
        }

        private string GetSafeFileName(string key) {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            var base64 = Convert.ToBase64String(hash);

            // 转成 Base64Url，去掉非法字符
            var safeName = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
            // 可选截短，防止文件名太长
            return safeName.Substring(0, 16);
        }

        private string GetKeyVaultDirectory() => Path.Combine(_env.ContentRootPath, "App_Data", "KeyVaults");
        public string GetKeyVaultFullPath(string key) => Path.Combine(GetKeyVaultDirectory(), $"keyvalut-{GetSafeFileName(key)}.txt");


        async Task<string> IKeyVaultService.GetData(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception("Key is required");
            }

            var filePath = GetKeyVaultFullPath(key);

            if (!System.IO.File.Exists(filePath))
            {
               throw new Exception( "Key not found" );
            }

            var encrypted = await System.IO.File.ReadAllTextAsync(filePath);

            string? plainText;
            try
            {
                plainText = _crypto!.DecryptFromBase64(encrypted);
            }
            catch (Exception ex)
            {
                throw new Exception($"Decrypt failed: {ex.Message}" );
            }

            return  plainText ;
        }
    }
}
