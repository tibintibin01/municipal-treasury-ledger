using System;
using System.Globalization;
using System.IO;

namespace MunicipalTreasuryLedger
{
    public class BackupService
    {
        public const int DefaultRetentionDays = 30;

        public static string GetEffectiveBackupFolder(TreasuryDataStore dataStore, LedgerDatabase database)
        {
            if (database != null &&
                database.Settings != null &&
                !String.IsNullOrWhiteSpace(database.Settings.BackupFolderPath))
            {
                return database.Settings.BackupFolderPath.Trim();
            }

            string dataDirectory = "";
            if (dataStore != null && dataStore.UsesEncryptedContainer && !String.IsNullOrEmpty(dataStore.EncryptedContainerPath))
            {
                dataDirectory = Path.GetDirectoryName(dataStore.EncryptedContainerPath);
            }
            else if (dataStore != null && !String.IsNullOrEmpty(dataStore.FilePath))
            {
                dataDirectory = Path.GetDirectoryName(dataStore.FilePath);
            }

            if (String.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(dataDirectory, "Backups");
        }

        public static bool HasAutoBackupToday(LedgerDatabase database)
        {
            return database != null &&
                database.Settings != null &&
                database.Settings.LastAutoBackupDate.Date >= DateTime.Today;
        }

        public static string RunDailyAutoBackup(TreasuryDataStore dataStore, LedgerDatabase database, UserAccount currentUser)
        {
            EnsureSettings(database);
            if (HasAutoBackupToday(database))
            {
                return "";
            }

            string folder = GetEffectiveBackupFolder(dataStore, database);
            Directory.CreateDirectory(folder);

            string extension = dataStore != null && dataStore.UsesEncryptedContainer ? ".mtdb" : ".db";
            string fileName = "municipal-treasury-auto-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + extension;
            string backupPath = Path.Combine(folder, fileName);

            database.Settings.LastAutoBackupDate = DateTime.Today;
            new AuditService(database, currentUser).Log("Auto Backup", "Backup", "", backupPath);
            dataStore.CreateBackup(database, backupPath);
            BackupVerificationResult verification = dataStore != null && dataStore.UsesEncryptedContainer
                ? dataStore.VerifyEncryptedContainerBackup(backupPath)
                : BackupVerificationService.VerifyPlainSqlite(backupPath);
            if (verification == null || !verification.IsValid)
            {
                string message = verification == null ? "No verification result." : verification.Message;
                new AuditService(database, currentUser).Log("Auto Backup Verification Failed", "Backup", "", backupPath + " | " + message);
                dataStore.Save(database);
                throw new InvalidOperationException("Automatic backup verification failed. " + message);
            }

            new AuditService(database, currentUser).Log("Auto Backup Verified", "Backup", "", backupPath + " | " + verification.Message);
            dataStore.Save(database);
            DeleteExpiredAutoBackups(folder, database.Settings.AutoBackupRetentionDays);

            return backupPath;
        }

        public static void SetBackupFolder(TreasuryDataStore dataStore, LedgerDatabase database, UserAccount currentUser, string folderPath)
        {
            EnsureSettings(database);
            if (String.IsNullOrWhiteSpace(folderPath))
            {
                throw new ArgumentException("Backup folder is required.", "folderPath");
            }

            Directory.CreateDirectory(folderPath);
            database.Settings.BackupFolderPath = folderPath.Trim();
            new AuditService(database, currentUser).Log("Change Backup Folder", "Settings", "", database.Settings.BackupFolderPath);
            dataStore.Save(database);
        }

        private static void EnsureSettings(LedgerDatabase database)
        {
            if (database.Settings == null)
            {
                database.Settings = new AppSettings();
            }

            if (database.Settings.AutoBackupRetentionDays <= 0)
            {
                database.Settings.AutoBackupRetentionDays = DefaultRetentionDays;
            }
        }

        private static void DeleteExpiredAutoBackups(string folder, int retentionDays)
        {
            if (retentionDays <= 0)
            {
                retentionDays = DefaultRetentionDays;
            }

            DateTime cutoff = DateTime.Today.AddDays(-retentionDays);
            foreach (string path in Directory.GetFiles(folder, "municipal-treasury-auto-*.*"))
            {
                try
                {
                    string extension = Path.GetExtension(path);
                    if (!String.Equals(extension, ".db", StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(extension, ".mtdb", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    FileInfo file = new FileInfo(path);
                    if (file.LastWriteTime.Date < cutoff)
                    {
                        file.Delete();
                    }
                }
                catch
                {
                    // Backup cleanup should never block opening the ledger.
                }
            }
        }
    }
}
