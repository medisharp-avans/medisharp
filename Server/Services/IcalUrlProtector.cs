using System.Security.Cryptography;
using System.Text;

namespace Server.Services
{
    public class IcalUrlProtector
    {
        private readonly byte[] _key;

        public IcalUrlProtector(IConfiguration configuration)
        {
            var base64Key = configuration["CalendarEncryption:Key"]
                ?? throw new InvalidOperationException("Configuratiesleutel 'CalendarEncryption:Key' ontbreekt.");

            _key = Convert.FromBase64String(base64Key);
            if (_key.Length != 32)
                throw new InvalidOperationException("CalendarEncryption:Key moet exact 32 bytes (256-bit) zijn.");
        }

        public string Protect(string plainUrl)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainUrl);
            var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize); 
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; 

            using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            var combined = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        public string Unprotect(string encryptedBase64)
        {
            var combined = Convert.FromBase64String(encryptedBase64);

            var nonceSize = AesGcm.NonceByteSizes.MaxSize;
            var tagSize = AesGcm.TagByteSizes.MaxSize;

            var nonce = combined[..nonceSize];
            var tag = combined[nonceSize..(nonceSize + tagSize)];
            var cipherBytes = combined[(nonceSize + tagSize)..];
            var plainBytes = new byte[cipherBytes.Length];

            using var aes = new AesGcm(_key, tagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
