using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public static class EncryptedDatabaseContainerService
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("MTOLEDGER1");
        private const int Version = 1;
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int KeySize = 32;
        private const int HmacSize = 32;
        private const int Pbkdf2Iterations = 200000;

        public static bool IsEncryptedContainer(string filePath)
        {
            if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            using (FileStream stream = File.OpenRead(filePath))
            {
                if (stream.Length < Magic.Length)
                {
                    return false;
                }

                byte[] buffer = new byte[Magic.Length];
                stream.Read(buffer, 0, buffer.Length);
                return BytesEqual(buffer, Magic);
            }
        }

        public static void EncryptDatabaseFile(string sourceDbPath, string encryptedContainerPath, string password)
        {
            ValidatePassword(password);
            if (String.IsNullOrEmpty(sourceDbPath) || !File.Exists(sourceDbPath))
            {
                throw new FileNotFoundException("Source database was not found.", sourceDbPath);
            }

            string directory = Path.GetDirectoryName(encryptedContainerPath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[] plainBytes = File.ReadAllBytes(sourceDbPath);
            byte[] salt = RandomBytes(SaltSize);
            byte[] iv = RandomBytes(IvSize);
            byte[] keyMaterial = DeriveKeyMaterial(password, salt);
            byte[] encryptionKey = Slice(keyMaterial, 0, KeySize);
            byte[] hmacKey = Slice(keyMaterial, KeySize, KeySize);
            byte[] cipherBytes = Encrypt(plainBytes, encryptionKey, iv);

            byte[] headerAndCipher = BuildHeaderAndCipher(salt, iv, cipherBytes);
            byte[] hmac = Hmac(headerAndCipher, hmacKey);
            string tempPath = encryptedContainerPath + ".tmp";

            using (FileStream output = File.Create(tempPath))
            {
                output.Write(headerAndCipher, 0, headerAndCipher.Length);
                output.Write(hmac, 0, hmac.Length);
            }

            if (File.Exists(encryptedContainerPath))
            {
                File.Delete(encryptedContainerPath);
            }

            File.Move(tempPath, encryptedContainerPath);
        }

        public static void DecryptDatabaseToFile(string encryptedContainerPath, string destinationDbPath, string password)
        {
            ValidatePassword(password);
            byte[] fileBytes = File.ReadAllBytes(encryptedContainerPath);
            int minimumLength = Magic.Length + 4 + SaltSize + IvSize + HmacSize + 1;
            if (fileBytes.Length < minimumLength)
            {
                throw new InvalidDataException("Encrypted database container is invalid or incomplete.");
            }

            int offset = 0;
            byte[] magic = Slice(fileBytes, offset, Magic.Length);
            offset += Magic.Length;
            if (!BytesEqual(magic, Magic))
            {
                throw new InvalidDataException("This is not a Municipal Treasury encrypted database container.");
            }

            int version = BitConverter.ToInt32(fileBytes, offset);
            offset += 4;
            if (version != Version)
            {
                throw new InvalidDataException("Encrypted database container version is not supported.");
            }

            byte[] salt = Slice(fileBytes, offset, SaltSize);
            offset += SaltSize;
            byte[] iv = Slice(fileBytes, offset, IvSize);
            offset += IvSize;

            int cipherLength = fileBytes.Length - offset - HmacSize;
            if (cipherLength <= 0)
            {
                throw new InvalidDataException("Encrypted database container has no data.");
            }

            byte[] cipherBytes = Slice(fileBytes, offset, cipherLength);
            offset += cipherLength;
            byte[] storedHmac = Slice(fileBytes, offset, HmacSize);

            byte[] keyMaterial = DeriveKeyMaterial(password, salt);
            byte[] encryptionKey = Slice(keyMaterial, 0, KeySize);
            byte[] hmacKey = Slice(keyMaterial, KeySize, KeySize);
            byte[] headerAndCipher = Slice(fileBytes, 0, fileBytes.Length - HmacSize);
            byte[] computedHmac = Hmac(headerAndCipher, hmacKey);
            if (!BytesEqual(storedHmac, computedHmac))
            {
                throw new CryptographicException("Encrypted database container password is incorrect or the encrypted container was changed.");
            }

            byte[] plainBytes = Decrypt(cipherBytes, encryptionKey, iv);
            string directory = Path.GetDirectoryName(destinationDbPath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(destinationDbPath, plainBytes);
        }

        public static string CreateTempDatabasePath()
        {
            return Path.Combine(Path.GetTempPath(), "municipal-treasury-ledger-" + Guid.NewGuid().ToString("N") + ".db");
        }

        private static byte[] BuildHeaderAndCipher(byte[] salt, byte[] iv, byte[] cipherBytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(Magic, 0, Magic.Length);
                byte[] versionBytes = BitConverter.GetBytes(Version);
                stream.Write(versionBytes, 0, versionBytes.Length);
                stream.Write(salt, 0, salt.Length);
                stream.Write(iv, 0, iv.Length);
                stream.Write(cipherBytes, 0, cipherBytes.Length);
                return stream.ToArray();
            }
        }

        private static void ValidatePassword(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Encrypted database container password is required.");
            }
        }

        private static byte[] DeriveKeyMaterial(string password, byte[] salt)
        {
            using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations))
            {
                return pbkdf2.GetBytes(KeySize * 2);
            }
        }

        private static byte[] Encrypt(byte[] plainBytes, byte[] key, byte[] iv)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (MemoryStream output = new MemoryStream())
                using (CryptoStream crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    crypto.Write(plainBytes, 0, plainBytes.Length);
                    crypto.FlushFinalBlock();
                    return output.ToArray();
                }
            }
        }

        private static byte[] Decrypt(byte[] cipherBytes, byte[] key, byte[] iv)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (MemoryStream input = new MemoryStream(cipherBytes))
                using (CryptoStream crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    crypto.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        private static byte[] Hmac(byte[] bytes, byte[] key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(key))
            {
                return hmac.ComputeHash(bytes);
            }
        }

        private static byte[] RandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private static byte[] Slice(byte[] source, int offset, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }
}
