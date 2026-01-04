using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace CustomSiteSettingsModule.Services
{
    public class AesGcmCryptoService
    {
        private readonly byte[] _key;
        private readonly int _tagSizeInBytes;

        public AesGcmCryptoService(byte[] key, int tagSizeInBytes = 16)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            // 合法 AES key 长度：16/24/32 bytes
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("Key must be 16/24/32 bytes (AES-128/192/256).", nameof(key));

            // 尝试用指定 tagSize 构造一次，确保在当前平台受支持
            try
            {
                using var test = new AesGcm(key, tagSizeInBytes);
            }
            catch (Exception ex)
            {
                throw new PlatformNotSupportedException($"Tag size {tagSizeInBytes} bytes is not supported on this platform.", ex);
            }

            _key = (byte[])key.Clone();
            _tagSizeInBytes = tagSizeInBytes;
        }

        public string EncryptToBase64(string plainText)
        {
            var plain = Encoding.UTF8.GetBytes(plainText);
            var nonce = new byte[12]; // 推荐 12 字节 nonce
            RandomNumberGenerator.Fill(nonce);

            var cipher = new byte[plain.Length];
            var tag = new byte[_tagSizeInBytes];

            // 使用显式 tagSize 的构造函数（.NET 8+）
            using var aes = new AesGcm(_key, _tagSizeInBytes);
            aes.Encrypt(nonce, plain, cipher, tag, associatedData: null);

            // 合并存储： nonce(12) | tag | cipher
            var outBuf = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, outBuf, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, outBuf, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, outBuf, nonce.Length + tag.Length, cipher.Length);

            return Convert.ToBase64String(outBuf);
        }

        public string DecryptFromBase64(string base64)
        {
            var all = Convert.FromBase64String(base64);

            var nonce = new byte[12];
            Buffer.BlockCopy(all, 0, nonce, 0, nonce.Length);

            var tag = new byte[_tagSizeInBytes];
            Buffer.BlockCopy(all, nonce.Length, tag, 0, tag.Length);

            var cipherLen = all.Length - nonce.Length - tag.Length;
            var cipher = new byte[cipherLen];
            Buffer.BlockCopy(all, nonce.Length + tag.Length, cipher, 0, cipherLen);

            var plain = new byte[cipherLen];
            using var aes = new AesGcm(_key, _tagSizeInBytes);
            aes.Decrypt(nonce, cipher, tag, plain, associatedData: null);

            return Encoding.UTF8.GetString(plain);
        }
    }
}
