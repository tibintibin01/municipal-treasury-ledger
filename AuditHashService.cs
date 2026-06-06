using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MunicipalTreasuryLedger
{
    public class AuditHashVerificationResult
    {
        public bool IsValid { get; set; }
        public int EntryCount { get; set; }
        public int HashedEntryCount { get; set; }
        public string CurrentTipHash { get; set; }
        public string StoredTipHash { get; set; }
        public string Message { get; set; }

        public AuditHashVerificationResult()
        {
            CurrentTipHash = "";
            StoredTipHash = "";
            Message = "";
        }
    }

    public static class AuditHashService
    {
        public static void EnsureMissingHashes(LedgerDatabase database)
        {
            if (database == null || database.AuditTrail == null || database.AuditTrail.Count == 0)
            {
                if (database != null && String.IsNullOrEmpty(database.AuditChainTipHash))
                {
                    database.AuditChainTipHash = "";
                }

                return;
            }

            string previousHash = "";
            foreach (AuditLogEntry entry in OrderedEntries(database))
            {
                if (String.IsNullOrEmpty(entry.PreviousHash) && String.IsNullOrEmpty(entry.EntryHash))
                {
                    entry.PreviousHash = previousHash;
                    entry.EntryHash = ComputeEntryHash(entry, previousHash);
                }

                previousHash = entry.EntryHash ?? "";
            }

            if (String.IsNullOrEmpty(database.AuditChainTipHash))
            {
                database.AuditChainTipHash = previousHash;
            }
        }

        public static void SealNewEntry(LedgerDatabase database, AuditLogEntry newEntry)
        {
            if (database == null || newEntry == null)
            {
                return;
            }

            string previousHash = "";
            if (database.AuditTrail != null)
            {
                previousHash = OrderedEntries(database)
                    .Where(entry => entry.Id != newEntry.Id && !String.IsNullOrEmpty(entry.EntryHash))
                    .Select(entry => entry.EntryHash)
                    .LastOrDefault() ?? "";
            }

            newEntry.PreviousHash = previousHash;
            newEntry.EntryHash = ComputeEntryHash(newEntry, previousHash);
            database.AuditChainTipHash = newEntry.EntryHash;
        }

        public static AuditHashVerificationResult Verify(LedgerDatabase database)
        {
            AuditHashVerificationResult result = new AuditHashVerificationResult();
            result.StoredTipHash = database == null ? "" : (database.AuditChainTipHash ?? "");

            if (database == null || database.AuditTrail == null || database.AuditTrail.Count == 0)
            {
                result.IsValid = String.IsNullOrEmpty(result.StoredTipHash);
                result.Message = result.IsValid
                    ? "No audit entries yet."
                    : "Audit chain tip exists, but no audit entries were found.";
                return result;
            }

            string previousHash = "";
            foreach (AuditLogEntry entry in OrderedEntries(database))
            {
                result.EntryCount++;
                if (String.IsNullOrEmpty(entry.EntryHash))
                {
                    result.Message = "Audit entry is missing its stored hash: " + Safe(entry.Id);
                    return result;
                }

                result.HashedEntryCount++;
                if (!String.Equals(entry.PreviousHash ?? "", previousHash, StringComparison.Ordinal))
                {
                    result.Message = "Audit chain link mismatch at " + EntryLabel(entry) + ".";
                    return result;
                }

                string computedHash = ComputeEntryHash(entry, previousHash);
                if (!String.Equals(entry.EntryHash, computedHash, StringComparison.Ordinal))
                {
                    result.Message = "Audit entry content was changed after sealing at " + EntryLabel(entry) + ".";
                    return result;
                }

                previousHash = entry.EntryHash;
            }

            result.CurrentTipHash = previousHash;
            if (!String.Equals(result.StoredTipHash, result.CurrentTipHash, StringComparison.Ordinal))
            {
                result.Message = "Audit chain tip mismatch. An end entry may have been removed or the stored tip was changed.";
                return result;
            }

            result.IsValid = true;
            result.Message = "Audit chain verified. " + result.EntryCount.ToString("N0") + " entries checked.";
            return result;
        }

        public static string ComputeEntryHash(AuditLogEntry entry, string previousHash)
        {
            using (SHA256 sha = SHA256.Create())
            {
                string canonical = CanonicalEntry(entry, previousHash);
                byte[] bytes = Encoding.UTF8.GetBytes(canonical);
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static IEnumerable<AuditLogEntry> OrderedEntries(LedgerDatabase database)
        {
            return database.AuditTrail
                .OrderBy(entry => entry.Timestamp)
                .ThenBy(entry => entry.Id);
        }

        private static string CanonicalEntry(AuditLogEntry entry, string previousHash)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("previous=" + Safe(previousHash));
            builder.AppendLine("id=" + Safe(entry.Id));
            builder.AppendLine("timestamp=" + entry.Timestamp.ToString("o"));
            builder.AppendLine("username=" + Safe(entry.Username));
            builder.AppendLine("role=" + Safe(entry.Role));
            builder.AppendLine("action=" + Safe(entry.Action));
            builder.AppendLine("entity_type=" + Safe(entry.EntityType));
            builder.AppendLine("entity_id=" + Safe(entry.EntityId));
            builder.AppendLine("details=" + Safe(entry.Details));

            if (entry.ChangeDetails != null)
            {
                foreach (AuditLogDetail detail in entry.ChangeDetails.OrderBy(item => item.FieldName).ThenBy(item => item.Id))
                {
                    builder.AppendLine("detail_id=" + Safe(detail.Id));
                    builder.AppendLine("detail_field=" + Safe(detail.FieldName));
                    builder.AppendLine("detail_old=" + Safe(detail.OldValue));
                    builder.AppendLine("detail_new=" + Safe(detail.NewValue));
                }
            }

            return builder.ToString();
        }

        private static string EntryLabel(AuditLogEntry entry)
        {
            return Safe(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")) + " / " + Safe(entry.Action);
        }

        private static string Safe(string value)
        {
            return value ?? "";
        }
    }
}
