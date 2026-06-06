using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class SystemHealthItem
    {
        public string Area { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }

        public SystemHealthItem()
        {
            Area = "";
            Status = "";
            Details = "";
        }
    }

    public static class SystemHealthService
    {
        public static List<SystemHealthItem> RunChecks(TreasuryDataStore dataStore, LedgerDatabase database)
        {
            List<SystemHealthItem> items = new List<SystemHealthItem>();
            database = database ?? new LedgerDatabase();

            AddDatabaseIntegrity(items, dataStore);
            AddEncryptionStatus(items, dataStore);
            AddAuditChain(items, database);
            AddBackupStatus(items, dataStore, database);
            AddLockedYears(items, database);
            AddRecordCounts(items, database);
            AddUserStatus(items, database);

            return items;
        }

        private static void AddDatabaseIntegrity(List<SystemHealthItem> items, TreasuryDataStore dataStore)
        {
            if (dataStore == null)
            {
                items.Add(Item("SQLite integrity", "Warning", "Data store is not available."));
                return;
            }

            try
            {
                string result = dataStore.CheckDatabaseIntegrity();
                bool ok = String.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
                items.Add(Item("SQLite integrity", ok ? "OK" : "Warning", result));
            }
            catch (Exception ex)
            {
                items.Add(Item("SQLite integrity", "Error", ex.Message));
            }
        }

        private static void AddEncryptionStatus(List<SystemHealthItem> items, TreasuryDataStore dataStore)
        {
            if (dataStore == null)
            {
                items.Add(Item("Database encryption", "Warning", "Data store is not available."));
                return;
            }

            if (dataStore.UsesEncryptedContainer)
            {
                bool exists = File.Exists(dataStore.EncryptedContainerPath);
                items.Add(Item(
                    "Database encryption",
                    exists ? "OK" : "Warning",
                    exists
                        ? "AES encrypted container active: " + Path.GetFileName(dataStore.EncryptedContainerPath)
                        : "Encrypted container is configured but the file was not found."));
            }
            else
            {
                items.Add(Item("Database encryption", "Warning", "Plain SQLite database mode is active."));
            }
        }

        private static void AddAuditChain(List<SystemHealthItem> items, LedgerDatabase database)
        {
            AuditHashVerificationResult result = AuditHashService.Verify(database);
            items.Add(Item(
                "Audit hash chain",
                result.IsValid ? "OK" : "Error",
                result.Message + (String.IsNullOrEmpty(result.CurrentTipHash) ? "" : " Tip: " + ShortHash(result.CurrentTipHash))));
        }

        private static void AddBackupStatus(List<SystemHealthItem> items, TreasuryDataStore dataStore, LedgerDatabase database)
        {
            string folder = BackupService.GetEffectiveBackupFolder(dataStore, database);
            if (!Directory.Exists(folder))
            {
                items.Add(Item("Backup folder", "Warning", "Folder does not exist: " + folder));
                return;
            }

            FileInfo latest = Directory.GetFiles(folder, "municipal-treasury-auto-*.*")
                .Select(path => new FileInfo(path))
                .Where(file => String.Equals(file.Extension, ".db", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(file.Extension, ".mtdb", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(file => file.LastWriteTime)
                .FirstOrDefault();

            if (latest == null)
            {
                items.Add(Item("Automatic backup", "Warning", "No automatic backup was found in " + folder));
                return;
            }

            int ageDays = Math.Max(0, (DateTime.Today - latest.LastWriteTime.Date).Days);
            items.Add(Item(
                "Automatic backup",
                ageDays <= 1 ? "OK" : "Warning",
                latest.Name + " | " + latest.LastWriteTime.ToString("yyyy-MM-dd HH:mm") + " | " + ageDays.ToString() + " day(s) old"));
        }

        private static void AddLockedYears(List<SystemHealthItem> items, LedgerDatabase database)
        {
            string lockedYears = database != null && database.Settings != null ? database.Settings.LockedFiscalYears : "";
            if (String.IsNullOrWhiteSpace(lockedYears))
            {
                items.Add(Item("Fiscal year lock", "Warning", "No fiscal years are currently locked."));
                return;
            }

            items.Add(Item("Fiscal year lock", "OK", "Locked years: " + lockedYears));
        }

        private static void AddRecordCounts(List<SystemHealthItem> items, LedgerDatabase database)
        {
            int owners = database.Owners == null ? 0 : database.Owners.Count;
            int assessments = database.Owners == null ? 0 : database.Owners.Sum(owner => owner.Assessments == null ? 0 : owner.Assessments.Count);
            int payments = database.Owners == null ? 0 : database.Owners.Sum(owner => owner.Assessments == null ? 0 : owner.Assessments.Sum(assessment => assessment.Payments == null ? 0 : assessment.Payments.Count));
            int audits = database.AuditTrail == null ? 0 : database.AuditTrail.Count;
            items.Add(Item(
                "Record counts",
                "OK",
                "Owners: " + owners.ToString("N0") +
                " | Assessments: " + assessments.ToString("N0") +
                " | Payments: " + payments.ToString("N0") +
                " | Audit entries: " + audits.ToString("N0")));
        }

        private static void AddUserStatus(List<SystemHealthItem> items, LedgerDatabase database)
        {
            int activeUsers = database.Users == null ? 0 : database.Users.Count(user => user.IsActive);
            int admins = database.Users == null ? 0 : database.Users.Count(user => user.IsActive && String.Equals(user.Role, SecurityService.AdminRole, StringComparison.OrdinalIgnoreCase));
            if (activeUsers == 0)
            {
                items.Add(Item("User accounts", "Error", "No active users."));
                return;
            }

            items.Add(Item(
                "User accounts",
                admins > 0 ? "OK" : "Warning",
                "Active users: " + activeUsers.ToString("N0") + " | Active admins: " + admins.ToString("N0")));
        }

        private static SystemHealthItem Item(string area, string status, string details)
        {
            return new SystemHealthItem
            {
                Area = area,
                Status = status,
                Details = details
            };
        }

        private static string ShortHash(string hash)
        {
            if (String.IsNullOrEmpty(hash))
            {
                return "";
            }

            return hash.Length <= 16 ? hash : hash.Substring(0, 16);
        }
    }
}
