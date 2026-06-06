using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public static class DataProtectionService
    {
        private const string Prefix = "enc:v1:";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MunicipalTreasuryLedger.DataProtection.v1");

        public static string ProtectString(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }

            if (IsProtected(value))
            {
                return value;
            }

            byte[] plainBytes = Encoding.UTF8.GetBytes(value);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(protectedBytes);
        }

        public static string UnprotectString(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }

            if (!IsProtected(value))
            {
                return value;
            }

            try
            {
                string payload = value.Substring(Prefix.Length);
                byte[] protectedBytes = Convert.FromBase64String(payload);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Encrypted ledger data could not be decrypted by this Windows user. Restore it on the original Windows profile or use an app export created before moving machines.",
                    ex);
            }
        }

        public static bool IsProtected(string value)
        {
            return !String.IsNullOrEmpty(value) &&
                value.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public static string ProtectDate(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                return "";
            }

            return ProtectString(value.ToString("o", CultureInfo.InvariantCulture));
        }

        public static string ProtectDecimal(decimal value)
        {
            return ProtectString(value.ToString(CultureInfo.InvariantCulture));
        }

        public static string StableHash(string value)
        {
            string normalized = (value ?? "").Trim().ToLowerInvariant();
            if (String.IsNullOrEmpty(normalized))
            {
                return "";
            }

            byte[] input = Encoding.UTF8.GetBytes(normalized);
            using (SHA256 sha = SHA256.Create())
            {
                return "sha256:" + Convert.ToBase64String(sha.ComputeHash(input));
            }
        }
    }
}
