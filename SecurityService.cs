using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public static class SecurityService
    {
        public const string AdminRole = "Admin";
        public const string TreasurerRole = "Treasurer";
        public const string CashierRole = "Cashier";
        private const string Pbkdf2Algorithm = "PBKDF2";
        private const string LegacySha256Algorithm = "SHA256";
        private const string DefaultAdminUsername = "admin";
        private const string DefaultAdminPassword = "admin123";
        private const int Pbkdf2Iterations = 100000;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public static void EnsureDefaultAdmin(LedgerDatabase database)
        {
            if (database.Users == null)
            {
                database.Users = new System.Collections.Generic.List<UserAccount>();
            }

            if (database.Users.Count > 0)
            {
                return;
            }

            UserAccount admin = new UserAccount();
            admin.Username = "admin";
            admin.FullName = "System Administrator";
            admin.Role = AdminRole;
            admin.IsActive = true;
            SetPassword(admin, DefaultAdminPassword);
            database.Users.Add(admin);
        }

        public static bool HasAnyActiveUser(LedgerDatabase database)
        {
            return database != null &&
                database.Users != null &&
                database.Users.Any(user => user.IsActive);
        }

        public static UserAccount FindUser(LedgerDatabase database, string username)
        {
            if (database == null || database.Users == null || String.IsNullOrEmpty(username))
            {
                return null;
            }

            return database.Users.FirstOrDefault(user =>
                user.IsActive &&
                String.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool VerifyPassword(UserAccount user, string password)
        {
            if (user == null || String.IsNullOrEmpty(user.PasswordSalt) || String.IsNullOrEmpty(user.PasswordHash))
            {
                return false;
            }

            string computed = HashPassword(password, user.PasswordSalt, user.PasswordAlgorithm);
            return String.Equals(computed, user.PasswordHash, StringComparison.Ordinal);
        }

        public static void SetPassword(UserAccount user, string password)
        {
            user.PasswordAlgorithm = Pbkdf2Algorithm;
            user.PasswordSalt = CreateSalt();
            user.PasswordHash = HashPassword(password, user.PasswordSalt, user.PasswordAlgorithm);
            user.FailedLoginCount = 0;
            user.LockoutUntil = DateTime.MinValue;
        }

        public static bool NeedsPasswordUpgrade(UserAccount user)
        {
            return user != null &&
                !String.Equals(user.PasswordAlgorithm, Pbkdf2Algorithm, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLockedOut(UserAccount user)
        {
            return user != null &&
                user.LockoutUntil != DateTime.MinValue &&
                user.LockoutUntil > DateTime.Now;
        }

        public static string LockoutMessage(UserAccount user)
        {
            if (!IsLockedOut(user))
            {
                return "";
            }

            int minutes = Math.Max(1, (int)Math.Ceiling((user.LockoutUntil - DateTime.Now).TotalMinutes));
            return "Account is locked. Try again in " + minutes + " minute(s).";
        }

        public static void RegisterFailedLogin(UserAccount user)
        {
            if (user == null)
            {
                return;
            }

            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTime.Now.AddMinutes(LockoutMinutes);
            }
        }

        public static void RegisterSuccessfulLogin(UserAccount user)
        {
            if (user == null)
            {
                return;
            }

            user.FailedLoginCount = 0;
            user.LockoutUntil = DateTime.MinValue;
        }

        public static bool CanDelete(UserAccount user)
        {
            return IsAdmin(user) || IsTreasurer(user);
        }

        public static bool CanRestoreBackup(UserAccount user)
        {
            return IsAdmin(user) || IsTreasurer(user);
        }

        public static bool IsUsingDefaultAdminPassword(UserAccount user)
        {
            return user != null &&
                String.Equals(user.Username, DefaultAdminUsername, StringComparison.OrdinalIgnoreCase) &&
                VerifyPassword(user, DefaultAdminPassword);
        }

        public static bool IsDefaultAdminPasswordValue(string password)
        {
            return String.Equals(password, DefaultAdminPassword, StringComparison.Ordinal);
        }

        public static bool IsAdmin(UserAccount user)
        {
            return user != null && String.Equals(user.Role, AdminRole, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTreasurer(UserAccount user)
        {
            return user != null && String.Equals(user.Role, TreasurerRole, StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateSalt()
        {
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        private static string HashPassword(string password, string salt, string algorithm)
        {
            if (password == null)
            {
                password = "";
            }

            if (String.Equals(algorithm, Pbkdf2Algorithm, StringComparison.OrdinalIgnoreCase))
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Pbkdf2Iterations))
                {
                    return Convert.ToBase64String(pbkdf2.GetBytes(32));
                }
            }

            string value = salt + ":" + password;
            byte[] input = Encoding.UTF8.GetBytes(value);
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(input);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
