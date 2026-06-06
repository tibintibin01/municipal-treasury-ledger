using System;
using System.IO;
using System.Xml.Serialization;

namespace MunicipalTreasuryLedger
{
    public class BackupVerificationResult
    {
        public bool IsValid { get; set; }
        public int OwnerCount { get; set; }
        public int AssessmentCount { get; set; }
        public int PaymentCount { get; set; }
        public int AuditCount { get; set; }
        public string IntegrityStatus { get; set; }
        public string AuditChainStatus { get; set; }
        public string Message { get; set; }

        public BackupVerificationResult()
        {
            IntegrityStatus = "";
            AuditChainStatus = "";
            Message = "";
        }
    }

    public static class BackupVerificationService
    {
        public static BackupVerificationResult VerifyPlainSqlite(string sqlitePath)
        {
            string tempPath = TempDbPath();
            try
            {
                File.Copy(sqlitePath, tempPath, true);
                return VerifyWorkingDatabase(tempPath);
            }
            catch (Exception ex)
            {
                return Failed(ex);
            }
            finally
            {
                DeleteTemp(tempPath);
            }
        }

        public static BackupVerificationResult VerifyEncryptedContainer(string encryptedContainerPath, string password)
        {
            string tempPath = TempDbPath();
            try
            {
                EncryptedDatabaseContainerService.DecryptDatabaseToFile(encryptedContainerPath, tempPath, password);
                return VerifyWorkingDatabase(tempPath);
            }
            catch (Exception ex)
            {
                return Failed(ex);
            }
            finally
            {
                DeleteTemp(tempPath);
            }
        }

        public static BackupVerificationResult VerifyEncryptedBackup(string encryptedBackupPath, string password)
        {
            string tempPath = TempDbPath();
            try
            {
                EncryptedBackupService.DecryptBackupToFile(encryptedBackupPath, tempPath, password);
                return VerifyWorkingDatabase(tempPath);
            }
            catch (Exception ex)
            {
                return Failed(ex);
            }
            finally
            {
                DeleteTemp(tempPath);
            }
        }

        public static BackupVerificationResult VerifyLegacyXml(string xmlPath)
        {
            BackupVerificationResult result = new BackupVerificationResult();
            try
            {
                using (FileStream stream = File.OpenRead(xmlPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LedgerDatabase));
                    LedgerDatabase database = serializer.Deserialize(stream) as LedgerDatabase;
                    if (database == null)
                    {
                        result.Message = "XML backup did not contain a ledger database.";
                        return result;
                    }

                    CountRecords(database, result);
                    result.IntegrityStatus = "XML readable";
                    result.AuditChainStatus = AuditHashService.Verify(database).Message;
                    result.IsValid = true;
                    result.Message = Summary(result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                return result;
            }
        }

        private static BackupVerificationResult VerifyWorkingDatabase(string sqlitePath)
        {
            BackupVerificationResult result = new BackupVerificationResult();
            try
            {
                TreasuryDataStore store = new TreasuryDataStore(sqlitePath);
                result.IntegrityStatus = store.CheckDatabaseIntegrity();
                LedgerDatabase database = store.Load();
                CountRecords(database, result);

                AuditHashVerificationResult audit = AuditHashService.Verify(database);
                result.AuditChainStatus = audit.Message;
                result.IsValid = String.Equals(result.IntegrityStatus, "ok", StringComparison.OrdinalIgnoreCase) && audit.IsValid;
                result.Message = result.IsValid ? Summary(result) : result.IntegrityStatus + " | " + audit.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                return result;
            }
        }

        private static void CountRecords(LedgerDatabase database, BackupVerificationResult result)
        {
            if (database == null)
            {
                return;
            }

            result.OwnerCount = database.Owners == null ? 0 : database.Owners.Count;
            result.AssessmentCount = 0;
            result.PaymentCount = 0;
            if (database.Owners != null)
            {
                foreach (BusinessOwner owner in database.Owners)
                {
                    if (owner.Assessments == null)
                    {
                        continue;
                    }

                    result.AssessmentCount += owner.Assessments.Count;
                    foreach (YearlyAssessment assessment in owner.Assessments)
                    {
                        result.PaymentCount += assessment.Payments == null ? 0 : assessment.Payments.Count;
                    }
                }
            }

            result.AuditCount = database.AuditTrail == null ? 0 : database.AuditTrail.Count;
        }

        private static string Summary(BackupVerificationResult result)
        {
            return "Owners: " + result.OwnerCount.ToString("N0") +
                " | Assessments: " + result.AssessmentCount.ToString("N0") +
                " | Payments: " + result.PaymentCount.ToString("N0") +
                " | Audit entries: " + result.AuditCount.ToString("N0");
        }

        private static BackupVerificationResult Failed(Exception ex)
        {
            return new BackupVerificationResult
            {
                IsValid = false,
                Message = ex == null ? "Backup verification failed." : ex.Message
            };
        }

        private static string TempDbPath()
        {
            return Path.Combine(Path.GetTempPath(), "municipal-treasury-backup-check-" + Guid.NewGuid().ToString("N") + ".db");
        }

        private static void DeleteTemp(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(path + ".bak"))
                {
                    File.Delete(path + ".bak");
                }
            }
            catch
            {
                // Temp cleanup should not hide the verification result.
            }
        }
    }
}
