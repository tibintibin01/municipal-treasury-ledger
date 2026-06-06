using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MunicipalTreasuryLedger
{
    public class TreasuryDataStore
    {
        private const int SchemaVersion = 6;

        public string FilePath { get; private set; }
        public string EncryptedContainerPath { get; private set; }
        public bool UsesEncryptedContainer
        {
            get { return !String.IsNullOrEmpty(EncryptedContainerPath); }
        }

        private string encryptedContainerPassword;

        public TreasuryDataStore(string filePath)
        {
            FilePath = filePath;
        }

        public void EnableEncryptedContainer(string encryptedContainerPath, string password)
        {
            if (String.IsNullOrEmpty(encryptedContainerPath))
            {
                throw new ArgumentException("Encrypted container path is required.");
            }

            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Encrypted database container password is required.");
            }

            EncryptedContainerPath = encryptedContainerPath;
            encryptedContainerPassword = password;
        }

        public LedgerDatabase Load()
        {
            EnsureDatabase();

            if (IsDatabaseEmpty())
            {
                string legacyXmlPath = LegacyXmlPath();
                if (File.Exists(legacyXmlPath))
                {
                    LedgerDatabase legacyDatabase = LoadLegacyXml(legacyXmlPath);
                    Save(legacyDatabase);
                    MarkMeta("legacy_xml_migrated_at", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                }
            }

            LedgerDatabase database = LoadFromSqlite();
            Repair(database);
            return database;
        }

        public void Save(LedgerDatabase database)
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            EnsureDatabase();
            Repair(database);
            AuditHashService.EnsureMissingHashes(database);

            bool encryptionMigration = File.Exists(FilePath) && !IsEncryptionEnabled();
            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, FilePath + ".bak", true);
                if (encryptionMigration)
                {
                    string migrationBackup = FilePath + ".before-encryption-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                    File.Copy(FilePath, migrationBackup, true);
                }
            }

            using (SQLiteConnection connection = OpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, "DELETE FROM payment_records");
                Execute(connection, transaction, "DELETE FROM yearly_assessments");
                Execute(connection, transaction, "DELETE FROM business_owners");
                Execute(connection, transaction, "DELETE FROM user_accounts");
                Execute(connection, transaction, "DELETE FROM audit_log_details");
                Execute(connection, transaction, "DELETE FROM audit_log");
                Execute(connection, transaction, "DELETE FROM fee_catalog");

                foreach (BusinessOwner owner in database.Owners)
                {
                    Execute(
                        connection,
                        transaction,
                        @"INSERT INTO business_owners
                          (id, owner_name, business_name, owner_address, business_address, contact_number, line_of_business, tin, status, registration_type, remarks, privacy_consent_given, privacy_consent_date, privacy_consent_method, privacy_notice_version, date_created)
                          VALUES
                          (@id, @owner_name, @business_name, @owner_address, @business_address, @contact_number, @line_of_business, @tin, @status, @registration_type, @remarks, @privacy_consent_given, @privacy_consent_date, @privacy_consent_method, @privacy_notice_version, @date_created)",
                        P("@id", owner.Id),
                        P("@owner_name", ProtectedText(owner.OwnerName)),
                        P("@business_name", ProtectedText(owner.BusinessName)),
                        P("@owner_address", ProtectedText(owner.OwnerAddress)),
                        P("@business_address", ProtectedText(owner.BusinessAddress)),
                        P("@contact_number", ProtectedText(owner.ContactNumber)),
                        P("@line_of_business", ProtectedText(owner.LineOfBusiness)),
                        P("@tin", ProtectedText(owner.Tin)),
                        P("@status", ProtectedText(owner.Status)),
                        P("@registration_type", ProtectedText(owner.RegistrationType)),
                        P("@remarks", ProtectedText(owner.Remarks)),
                        P("@privacy_consent_given", owner.PrivacyConsentGiven ? 1 : 0),
                        P("@privacy_consent_date", ProtectedDateValue(owner.PrivacyConsentDate)),
                        P("@privacy_consent_method", ProtectedText(owner.PrivacyConsentMethod)),
                        P("@privacy_notice_version", ProtectedText(owner.PrivacyNoticeVersion)),
                        P("@date_created", ProtectedDateValue(owner.DateCreated)));

                    foreach (YearlyAssessment assessment in owner.Assessments)
                    {
                        Execute(
                            connection,
                            transaction,
                            @"INSERT INTO yearly_assessments
                              (id, owner_id, year, capital, gross_sales, business_tax, mayors_permit, fees, surcharge, penalty, remarks, date_created)
                              VALUES
                              (@id, @owner_id, @year, @capital, @gross_sales, @business_tax, @mayors_permit, @fees, @surcharge, @penalty, @remarks, @date_created)",
                            P("@id", assessment.Id),
                            P("@owner_id", owner.Id),
                            P("@year", assessment.Year),
                            P("@capital", MoneyValue(assessment.Capital)),
                            P("@gross_sales", MoneyValue(assessment.GrossSales)),
                            P("@business_tax", MoneyValue(assessment.BusinessTax)),
                            P("@mayors_permit", MoneyValue(assessment.MayorsPermit)),
                            P("@fees", MoneyValue(assessment.Fees)),
                            P("@surcharge", MoneyValue(assessment.Surcharge)),
                            P("@penalty", MoneyValue(assessment.Penalty)),
                            P("@remarks", ProtectedText(assessment.Remarks)),
                            P("@date_created", ProtectedDateValue(assessment.DateCreated)));

                        foreach (PaymentRecord payment in assessment.Payments)
                        {
                            Execute(
                                connection,
                                transaction,
                                @"INSERT INTO payment_records
                                  (id, assessment_id, date_paid, or_number, or_number_hash, schedule, amount, remarks)
                                  VALUES
                                  (@id, @assessment_id, @date_paid, @or_number, @or_number_hash, @schedule, @amount, @remarks)",
                                P("@id", payment.Id),
                                P("@assessment_id", assessment.Id),
                                P("@date_paid", ProtectedDateValue(payment.DatePaid)),
                                P("@or_number", ProtectedText(payment.OrNumber)),
                                P("@or_number_hash", DataProtectionService.StableHash(payment.OrNumber)),
                                P("@schedule", ProtectedText(payment.Schedule)),
                                P("@amount", MoneyValue(payment.Amount)),
                                P("@remarks", ProtectedText(payment.Remarks)));
                        }
                    }
                }

                foreach (UserAccount user in database.Users)
                {
                    Execute(
                        connection,
                        transaction,
                        @"INSERT INTO user_accounts
                          (id, username, username_hash, full_name, role, password_algorithm, password_salt, password_hash, failed_login_count, lockout_until, is_active, date_created)
                          VALUES
                          (@id, @username, @username_hash, @full_name, @role, @password_algorithm, @password_salt, @password_hash, @failed_login_count, @lockout_until, @is_active, @date_created)",
                        P("@id", user.Id),
                        P("@username", ProtectedText(user.Username)),
                        P("@username_hash", DataProtectionService.StableHash(user.Username)),
                        P("@full_name", ProtectedText(user.FullName)),
                        P("@role", ProtectedText(user.Role)),
                        P("@password_algorithm", ProtectedText(user.PasswordAlgorithm)),
                        P("@password_salt", ProtectedText(user.PasswordSalt)),
                        P("@password_hash", ProtectedText(user.PasswordHash)),
                        P("@failed_login_count", user.FailedLoginCount),
                        P("@lockout_until", ProtectedDateValue(user.LockoutUntil)),
                        P("@is_active", user.IsActive ? 1 : 0),
                        P("@date_created", ProtectedDateValue(user.DateCreated)));
                }

                foreach (AuditLogEntry entry in database.AuditTrail)
                {
                    Execute(
                        connection,
                        transaction,
                        @"INSERT INTO audit_log
                          (id, timestamp, username, role, action, entity_type, entity_id, details, previous_hash, entry_hash)
                          VALUES
                          (@id, @timestamp, @username, @role, @action, @entity_type, @entity_id, @details, @previous_hash, @entry_hash)",
                        P("@id", entry.Id),
                        P("@timestamp", DateValue(entry.Timestamp)),
                        P("@username", ProtectedText(entry.Username)),
                        P("@role", ProtectedText(entry.Role)),
                        P("@action", entry.Action),
                        P("@entity_type", entry.EntityType),
                        P("@entity_id", entry.EntityId),
                        P("@details", ProtectedText(entry.Details)),
                        P("@previous_hash", entry.PreviousHash),
                        P("@entry_hash", entry.EntryHash));

                    if (entry.ChangeDetails != null)
                    {
                        foreach (AuditLogDetail detail in entry.ChangeDetails)
                        {
                            Execute(
                                connection,
                                transaction,
                                @"INSERT INTO audit_log_details
                                  (id, audit_log_id, field_name, old_value, new_value)
                                  VALUES
                                  (@id, @audit_log_id, @field_name, @old_value, @new_value)",
                                P("@id", detail.Id),
                                P("@audit_log_id", entry.Id),
                                P("@field_name", ProtectedText(detail.FieldName)),
                                P("@old_value", ProtectedText(detail.OldValue)),
                                P("@new_value", ProtectedText(detail.NewValue)));
                        }
                    }
                }

                foreach (FeeCatalogItem fee in database.FeeCatalog)
                {
                    Execute(
                        connection,
                        transaction,
                        @"INSERT INTO fee_catalog
                          (id, code, code_hash, description, amount, is_active, date_created)
                          VALUES
                          (@id, @code, @code_hash, @description, @amount, @is_active, @date_created)",
                        P("@id", fee.Id),
                        P("@code", ProtectedText(fee.Code)),
                        P("@code_hash", DataProtectionService.StableHash(fee.Code)),
                        P("@description", ProtectedText(fee.Description)),
                        P("@amount", MoneyValue(fee.Amount)),
                        P("@is_active", fee.IsActive ? 1 : 0),
                        P("@date_created", ProtectedDateValue(fee.DateCreated)));
                }

                Execute(
                    connection,
                    transaction,
                    "INSERT OR REPLACE INTO app_meta (key, value) VALUES ('schema_version', @schema_version)",
                    P("@schema_version", SchemaVersion.ToString(CultureInfo.InvariantCulture)));

                Execute(
                    connection,
                    transaction,
                    "INSERT OR REPLACE INTO app_meta (key, value) VALUES ('data_encryption', @data_encryption)",
                    P("@data_encryption", "dpapi-current-user-v1"));

                Execute(
                    connection,
                    transaction,
                    "INSERT OR REPLACE INTO app_meta (key, value) VALUES ('audit_chain_tip_hash', @audit_chain_tip_hash)",
                    P("@audit_chain_tip_hash", database.AuditChainTipHash ?? ""));

                SaveSettings(connection, transaction, database.Settings);

                transaction.Commit();
            }

            if (encryptionMigration && File.Exists(FilePath))
            {
                File.Copy(FilePath, FilePath + ".bak", true);
            }

            PersistEncryptedContainer();
        }

        public void CreateBackup(LedgerDatabase database, string backupFilePath)
        {
            Save(database);

            string directory = Path.GetDirectoryName(backupFilePath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (UsesEncryptedContainer && String.Equals(Path.GetExtension(backupFilePath), ".mtdb", StringComparison.OrdinalIgnoreCase))
            {
                PersistEncryptedContainer();
                File.Copy(EncryptedContainerPath, backupFilePath, true);
                return;
            }

            File.Copy(FilePath, backupFilePath, true);
        }

        public void RestoreBackup(string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
            {
                throw new FileNotFoundException("Backup file was not found.", backupFilePath);
            }

            string directory = Path.GetDirectoryName(FilePath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(FilePath))
            {
                string safetyCopy = FilePath + ".before-restore-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                File.Copy(FilePath, safetyCopy, true);
            }

            if (String.Equals(Path.GetExtension(backupFilePath), ".xml", StringComparison.OrdinalIgnoreCase))
            {
                LedgerDatabase legacyDatabase = LoadLegacyXml(backupFilePath);
                Save(legacyDatabase);
                MarkMeta("legacy_xml_restored_at", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
                return;
            }

            File.Copy(backupFilePath, FilePath, true);
            EnsureDatabase();
            PersistEncryptedContainer();
        }

        public void PersistEncryptedContainer()
        {
            if (!UsesEncryptedContainer)
            {
                return;
            }

            if (String.IsNullOrEmpty(encryptedContainerPassword))
            {
                throw new InvalidOperationException("Encrypted database container password is not available for encrypted container save.");
            }

            EncryptedDatabaseContainerService.EncryptDatabaseFile(FilePath, EncryptedContainerPath, encryptedContainerPassword);
        }

        public BackupVerificationResult VerifyEncryptedContainerBackup(string backupFilePath)
        {
            if (!UsesEncryptedContainer)
            {
                throw new InvalidOperationException("Encrypted database container mode is not active.");
            }

            if (String.IsNullOrEmpty(encryptedContainerPassword))
            {
                throw new InvalidOperationException("Encrypted database container password is not available for backup verification.");
            }

            return BackupVerificationService.VerifyEncryptedContainer(backupFilePath, encryptedContainerPassword);
        }

        public void CleanupTemporaryDatabase()
        {
            if (!UsesEncryptedContainer)
            {
                return;
            }

            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }

                if (File.Exists(FilePath + ".bak"))
                {
                    File.Delete(FilePath + ".bak");
                }
            }
            catch
            {
                // A temp cleanup failure must not block app shutdown.
            }
        }

        public void ExportCsv(LedgerDatabase database, string filePath)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Owner Name,Business Name,Line of Business,Status,Registration Type,Privacy Consent,Consent Date,Consent Method,Privacy Notice Version,Year,Capital,Gross Sales,Business Tax,Mayor's Permit,Fees,Surcharge,Penalty,Total Assessment,Amount Paid,Balance,Payment Date,OR Number,Payment Schedule,Payment Amount,Payment Remarks");

            if (database != null && database.Owners != null)
            {
                foreach (BusinessOwner owner in database.Owners)
                {
                    if (owner.Assessments == null || owner.Assessments.Count == 0)
                    {
                        builder.AppendLine(String.Join(",", new string[]
                        {
                            Csv(owner.OwnerName),
                            Csv(owner.BusinessName),
                            Csv(owner.LineOfBusiness),
                            Csv(owner.Status),
                            Csv(owner.RegistrationType),
                            Csv(owner.PrivacyConsentGiven ? "Yes" : "No"),
                            Csv(owner.PrivacyConsentDate == DateTime.MinValue ? "" : owner.PrivacyConsentDate.ToString("yyyy-MM-dd")),
                            Csv(owner.PrivacyConsentMethod),
                            Csv(owner.PrivacyNoticeVersion),
                            "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""
                        }));
                        continue;
                    }

                    foreach (YearlyAssessment assessment in owner.Assessments)
                    {
                        if (assessment.Payments == null || assessment.Payments.Count == 0)
                        {
                            AppendAssessmentRow(builder, owner, assessment, null);
                            continue;
                        }

                        foreach (PaymentRecord payment in assessment.Payments)
                        {
                            AppendAssessmentRow(builder, owner, assessment, payment);
                        }
                    }
                }
            }

            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        public void ExportAuditCsv(LedgerDatabase database, string filePath)
        {
            AuditHashVerificationResult verification = AuditHashService.Verify(database);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Audit Chain Status," + Csv(verification.IsValid ? "Verified" : "Failed") + ",Message," + Csv(verification.Message) + ",Stored Tip," + Csv(verification.StoredTipHash) + ",Computed Tip," + Csv(verification.CurrentTipHash));
            builder.AppendLine("Audit Id,Timestamp,Username,Role,Action,Record Type,Record Id,Summary,Previous Hash,Entry Hash,Field,Before,After");

            if (database != null && database.AuditTrail != null)
            {
                foreach (AuditLogEntry entry in database.AuditTrail.OrderByDescending(item => item.Timestamp))
                {
                    if (entry.ChangeDetails == null || entry.ChangeDetails.Count == 0)
                    {
                        AppendAuditCsvRow(builder, entry, null);
                        continue;
                    }

                    foreach (AuditLogDetail detail in entry.ChangeDetails)
                    {
                        AppendAuditCsvRow(builder, entry, detail);
                    }
                }
            }

            File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);
        }

        public string CheckDatabaseIntegrity()
        {
            EnsureDatabase();
            using (SQLiteConnection connection = OpenConnection())
            {
                string integrity = "";
                using (SQLiteCommand command = new SQLiteCommand("PRAGMA integrity_check", connection))
                {
                    object value = command.ExecuteScalar();
                    integrity = value == null || value == DBNull.Value ? "" : Convert.ToString(value);
                }

                if (!String.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    return String.IsNullOrEmpty(integrity) ? "SQLite integrity check returned no result." : integrity;
                }

                using (SQLiteCommand command = new SQLiteCommand("PRAGMA foreign_key_check", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return "Foreign key issue: table " + Convert.ToString(reader["table"]) + ", row " + Convert.ToString(reader["rowid"]);
                    }
                }

                return "ok";
            }
        }

        private LedgerDatabase LoadFromSqlite()
        {
            LedgerDatabase database = new LedgerDatabase();
            Dictionary<string, BusinessOwner> ownersById = new Dictionary<string, BusinessOwner>();
            Dictionary<string, YearlyAssessment> assessmentsById = new Dictionary<string, YearlyAssessment>();
            Dictionary<string, AuditLogEntry> auditById = new Dictionary<string, AuditLogEntry>();

            using (SQLiteConnection connection = OpenConnection())
            {
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM business_owners ORDER BY business_name, owner_name", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        BusinessOwner owner = new BusinessOwner();
                        owner.Id = Text(reader, "id");
                        owner.OwnerName = Text(reader, "owner_name");
                        owner.BusinessName = Text(reader, "business_name");
                        owner.OwnerAddress = Text(reader, "owner_address");
                        owner.BusinessAddress = Text(reader, "business_address");
                        owner.ContactNumber = Text(reader, "contact_number");
                        owner.LineOfBusiness = Text(reader, "line_of_business");
                        owner.Tin = Text(reader, "tin");
                        owner.Status = Text(reader, "status");
                        owner.RegistrationType = Text(reader, "registration_type");
                        owner.Remarks = Text(reader, "remarks");
                        owner.PrivacyConsentGiven = Bool(reader, "privacy_consent_given");
                        owner.PrivacyConsentDate = Date(reader, "privacy_consent_date", DateTime.Today);
                        owner.PrivacyConsentMethod = Text(reader, "privacy_consent_method");
                        owner.PrivacyNoticeVersion = Text(reader, "privacy_notice_version");
                        owner.DateCreated = Date(reader, "date_created", DateTime.Today);
                        owner.Assessments = new List<YearlyAssessment>();
                        database.Owners.Add(owner);
                        ownersById[owner.Id] = owner;
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM yearly_assessments ORDER BY year DESC", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ownerId = Text(reader, "owner_id");
                        if (!ownersById.ContainsKey(ownerId))
                        {
                            continue;
                        }

                        YearlyAssessment assessment = new YearlyAssessment();
                        assessment.Id = Text(reader, "id");
                        assessment.Year = Number(reader, "year");
                        assessment.Capital = Money(reader, "capital");
                        assessment.GrossSales = Money(reader, "gross_sales");
                        assessment.BusinessTax = Money(reader, "business_tax");
                        assessment.MayorsPermit = Money(reader, "mayors_permit");
                        assessment.Fees = Money(reader, "fees");
                        assessment.Surcharge = Money(reader, "surcharge");
                        assessment.Penalty = Money(reader, "penalty");
                        assessment.Remarks = Text(reader, "remarks");
                        assessment.DateCreated = Date(reader, "date_created", DateTime.Today);
                        assessment.Payments = new List<PaymentRecord>();
                        ownersById[ownerId].Assessments.Add(assessment);
                        assessmentsById[assessment.Id] = assessment;
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM payment_records ORDER BY date_paid DESC", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string assessmentId = Text(reader, "assessment_id");
                        if (!assessmentsById.ContainsKey(assessmentId))
                        {
                            continue;
                        }

                        PaymentRecord payment = new PaymentRecord();
                        payment.Id = Text(reader, "id");
                        payment.DatePaid = Date(reader, "date_paid", DateTime.Today);
                        payment.OrNumber = Text(reader, "or_number");
                        payment.Schedule = Text(reader, "schedule");
                        payment.Amount = Money(reader, "amount");
                        payment.Remarks = Text(reader, "remarks");
                        assessmentsById[assessmentId].Payments.Add(payment);
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM user_accounts ORDER BY username", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        UserAccount user = new UserAccount();
                        user.Id = Text(reader, "id");
                        user.Username = Text(reader, "username");
                        user.FullName = Text(reader, "full_name");
                        user.Role = Text(reader, "role");
                        user.PasswordAlgorithm = Text(reader, "password_algorithm");
                        user.PasswordSalt = Text(reader, "password_salt");
                        user.PasswordHash = Text(reader, "password_hash");
                        user.FailedLoginCount = Number(reader, "failed_login_count");
                        user.LockoutUntil = Date(reader, "lockout_until", DateTime.MinValue);
                        user.IsActive = Number(reader, "is_active") == 1;
                        user.DateCreated = Date(reader, "date_created", DateTime.Today);
                        database.Users.Add(user);
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM audit_log ORDER BY timestamp", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        AuditLogEntry entry = new AuditLogEntry();
                        entry.Id = Text(reader, "id");
                        entry.Timestamp = Date(reader, "timestamp", DateTime.Now);
                        entry.Username = Text(reader, "username");
                        entry.Role = Text(reader, "role");
                        entry.Action = Text(reader, "action");
                        entry.EntityType = Text(reader, "entity_type");
                        entry.EntityId = Text(reader, "entity_id");
                        entry.Details = Text(reader, "details");
                        entry.PreviousHash = Text(reader, "previous_hash");
                        entry.EntryHash = Text(reader, "entry_hash");
                        entry.ChangeDetails = new List<AuditLogDetail>();
                        database.AuditTrail.Add(entry);
                        auditById[entry.Id] = entry;
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM audit_log_details ORDER BY audit_log_id, field_name", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string auditLogId = Text(reader, "audit_log_id");
                        if (!auditById.ContainsKey(auditLogId))
                        {
                            continue;
                        }

                        AuditLogDetail detail = new AuditLogDetail();
                        detail.Id = Text(reader, "id");
                        detail.AuditLogId = auditLogId;
                        detail.FieldName = Text(reader, "field_name");
                        detail.OldValue = Text(reader, "old_value");
                        detail.NewValue = Text(reader, "new_value");
                        auditById[auditLogId].ChangeDetails.Add(detail);
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM fee_catalog ORDER BY code, description", connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        FeeCatalogItem fee = new FeeCatalogItem();
                        fee.Id = Text(reader, "id");
                        fee.Code = Text(reader, "code");
                        fee.Description = Text(reader, "description");
                        fee.Amount = Money(reader, "amount");
                        fee.IsActive = Number(reader, "is_active") == 1;
                        fee.DateCreated = Date(reader, "date_created", DateTime.Today);
                        database.FeeCatalog.Add(fee);
                    }
                }

                database.Settings = LoadSettings(connection);
                database.AuditChainTipHash = MetaText(connection, "audit_chain_tip_hash");
            }

            return database;
        }

        private void EnsureDatabase()
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (SQLiteConnection connection = OpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, "PRAGMA foreign_keys = ON");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS app_meta (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS business_owners (
                    id TEXT PRIMARY KEY,
                    owner_name TEXT,
                    business_name TEXT,
                    owner_address TEXT,
                    business_address TEXT,
                    contact_number TEXT,
                    line_of_business TEXT,
                    tin TEXT,
                    status TEXT NOT NULL DEFAULT 'Active',
                    registration_type TEXT NOT NULL DEFAULT 'Renewal',
                    remarks TEXT,
                    privacy_consent_given INTEGER NOT NULL DEFAULT 0,
                    privacy_consent_date TEXT,
                    privacy_consent_method TEXT,
                    privacy_notice_version TEXT,
                    date_created TEXT NOT NULL
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS yearly_assessments (
                    id TEXT PRIMARY KEY,
                    owner_id TEXT NOT NULL REFERENCES business_owners(id) ON DELETE CASCADE,
                    year INTEGER NOT NULL,
                    capital NUMERIC(18,2) NOT NULL DEFAULT 0,
                    gross_sales NUMERIC(18,2) NOT NULL DEFAULT 0,
                    business_tax NUMERIC(18,2) NOT NULL DEFAULT 0,
                    mayors_permit NUMERIC(18,2) NOT NULL DEFAULT 0,
                    fees NUMERIC(18,2) NOT NULL DEFAULT 0,
                    surcharge NUMERIC(18,2) NOT NULL DEFAULT 0,
                    penalty NUMERIC(18,2) NOT NULL DEFAULT 0,
                    remarks TEXT,
                    date_created TEXT NOT NULL,
                    UNIQUE(owner_id, year)
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS payment_records (
                    id TEXT PRIMARY KEY,
                    assessment_id TEXT NOT NULL REFERENCES yearly_assessments(id) ON DELETE CASCADE,
                    date_paid TEXT NOT NULL,
                    or_number TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    or_number_hash TEXT,
                    schedule TEXT NOT NULL DEFAULT '1st Qtr',
                    amount NUMERIC(18,2) NOT NULL,
                    remarks TEXT
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS user_accounts (
                    id TEXT PRIMARY KEY,
                    username TEXT NOT NULL UNIQUE COLLATE NOCASE,
                    username_hash TEXT,
                    full_name TEXT,
                    role TEXT NOT NULL DEFAULT 'Cashier',
                    password_algorithm TEXT,
                    password_salt TEXT,
                    password_hash TEXT,
                    failed_login_count INTEGER NOT NULL DEFAULT 0,
                    lockout_until TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    date_created TEXT NOT NULL
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS audit_log (
                    id TEXT PRIMARY KEY,
                    timestamp TEXT NOT NULL,
                    username TEXT,
                    role TEXT,
                    action TEXT NOT NULL,
                    entity_type TEXT,
                    entity_id TEXT,
                    details TEXT,
                    previous_hash TEXT,
                    entry_hash TEXT
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS audit_log_details (
                    id TEXT PRIMARY KEY,
                    audit_log_id TEXT NOT NULL REFERENCES audit_log(id) ON DELETE CASCADE,
                    field_name TEXT NOT NULL,
                    old_value TEXT,
                    new_value TEXT
                )");

                Execute(connection, transaction, @"CREATE TABLE IF NOT EXISTS fee_catalog (
                    id TEXT PRIMARY KEY,
                    code TEXT,
                    code_hash TEXT,
                    description TEXT,
                    amount NUMERIC(18,2) NOT NULL DEFAULT 0,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    date_created TEXT NOT NULL
                )");

                EnsureColumn(connection, transaction, "payment_records", "or_number_hash", "TEXT");
                EnsureColumn(connection, transaction, "business_owners", "privacy_consent_given", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumn(connection, transaction, "business_owners", "privacy_consent_date", "TEXT");
                EnsureColumn(connection, transaction, "business_owners", "privacy_consent_method", "TEXT");
                EnsureColumn(connection, transaction, "business_owners", "privacy_notice_version", "TEXT");
                EnsureColumn(connection, transaction, "user_accounts", "username_hash", "TEXT");
                EnsureColumn(connection, transaction, "audit_log", "previous_hash", "TEXT");
                EnsureColumn(connection, transaction, "audit_log", "entry_hash", "TEXT");
                EnsureMoneyColumnTypes(connection, transaction);

                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_business_owners_name ON business_owners(business_name, owner_name)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_business_owners_tin ON business_owners(tin)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_assessments_owner_year ON yearly_assessments(owner_id, year)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_payments_assessment ON payment_records(assessment_id)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_payments_or_number ON payment_records(or_number)");
                Execute(connection, transaction, "CREATE UNIQUE INDEX IF NOT EXISTS idx_payments_or_number_hash_unique ON payment_records(or_number_hash)");
                Execute(connection, transaction, "CREATE UNIQUE INDEX IF NOT EXISTS idx_users_username_hash_unique ON user_accounts(username_hash)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_audit_timestamp ON audit_log(timestamp)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_audit_details_log ON audit_log_details(audit_log_id)");
                Execute(connection, transaction, "CREATE INDEX IF NOT EXISTS idx_fee_catalog_code_hash ON fee_catalog(code_hash)");

                Execute(
                    connection,
                    transaction,
                    "INSERT OR REPLACE INTO app_meta (key, value) VALUES ('schema_version', @schema_version)",
                    P("@schema_version", SchemaVersion.ToString(CultureInfo.InvariantCulture)));

                transaction.Commit();
            }
        }

        private bool IsDatabaseEmpty()
        {
            using (SQLiteConnection connection = OpenConnection())
            {
                int ownerCount = Count(connection, "business_owners");
                int userCount = Count(connection, "user_accounts");
                int auditCount = Count(connection, "audit_log");
                int feeCount = Count(connection, "fee_catalog");
                int savedSettingCount = CountSavedSettings(connection);
                return ownerCount == 0 && userCount == 0 && auditCount == 0 && feeCount == 0 && savedSettingCount == 0;
            }
        }

        private int Count(SQLiteConnection connection, string tableName)
        {
            using (SQLiteCommand command = new SQLiteCommand("SELECT COUNT(*) FROM " + tableName, connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private int CountSavedSettings(SQLiteConnection connection)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                @"SELECT COUNT(*)
                  FROM app_meta
                    WHERE key NOT IN ('schema_version', 'data_encryption', 'audit_chain_tip_hash', 'legacy_xml_migrated_at', 'legacy_xml_restored_at')
                    AND value IS NOT NULL
                    AND TRIM(value) <> ''",
                connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private bool IsEncryptionEnabled()
        {
            try
            {
                EnsureDatabase();
                using (SQLiteConnection connection = OpenConnection())
                {
                    return String.Equals(MetaText(connection, "data_encryption"), "dpapi-current-user-v1", StringComparison.Ordinal);
                }
            }
            catch
            {
                return false;
            }
        }

        private SQLiteConnection OpenConnection()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=" + FilePath + ";Version=3;Foreign Keys=True;");
            connection.Open();
            return connection;
        }

        private void EnsureColumn(SQLiteConnection connection, SQLiteTransaction transaction, string tableName, string columnName, string columnDefinition)
        {
            if (ColumnExists(connection, tableName, columnName))
            {
                return;
            }

            Execute(connection, transaction, "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + columnDefinition);
        }

        private void EnsureMoneyColumnTypes(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            bool assessmentNeedsMigration =
                !ColumnTypeStartsWith(connection, "yearly_assessments", "capital", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "gross_sales", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "business_tax", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "mayors_permit", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "fees", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "surcharge", "NUMERIC") ||
                !ColumnTypeStartsWith(connection, "yearly_assessments", "penalty", "NUMERIC");

            bool paymentNeedsMigration = !ColumnTypeStartsWith(connection, "payment_records", "amount", "NUMERIC");

            if (assessmentNeedsMigration || paymentNeedsMigration)
            {
                RebuildAssessmentAndPaymentTables(connection, transaction);
            }

            if (!ColumnTypeStartsWith(connection, "fee_catalog", "amount", "NUMERIC"))
            {
                RebuildFeeCatalogTable(connection, transaction);
            }
        }

        private void RebuildAssessmentAndPaymentTables(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            Execute(connection, transaction, @"CREATE TABLE yearly_assessments_new (
                id TEXT PRIMARY KEY,
                owner_id TEXT NOT NULL REFERENCES business_owners(id) ON DELETE CASCADE,
                year INTEGER NOT NULL,
                capital NUMERIC(18,2) NOT NULL DEFAULT 0,
                gross_sales NUMERIC(18,2) NOT NULL DEFAULT 0,
                business_tax NUMERIC(18,2) NOT NULL DEFAULT 0,
                mayors_permit NUMERIC(18,2) NOT NULL DEFAULT 0,
                fees NUMERIC(18,2) NOT NULL DEFAULT 0,
                surcharge NUMERIC(18,2) NOT NULL DEFAULT 0,
                penalty NUMERIC(18,2) NOT NULL DEFAULT 0,
                remarks TEXT,
                date_created TEXT NOT NULL,
                UNIQUE(owner_id, year)
            )");

            Execute(connection, transaction, @"INSERT INTO yearly_assessments_new
                (id, owner_id, year, capital, gross_sales, business_tax, mayors_permit, fees, surcharge, penalty, remarks, date_created)
                SELECT id, owner_id, year, capital, gross_sales, business_tax, mayors_permit, fees, surcharge, penalty, remarks, date_created
                FROM yearly_assessments");

            Execute(connection, transaction, "CREATE TABLE payment_records_backup AS SELECT id, assessment_id, date_paid, or_number, or_number_hash, schedule, amount, remarks FROM payment_records");
            Execute(connection, transaction, "DROP TABLE payment_records");
            Execute(connection, transaction, "DROP TABLE yearly_assessments");
            Execute(connection, transaction, "ALTER TABLE yearly_assessments_new RENAME TO yearly_assessments");

            Execute(connection, transaction, @"CREATE TABLE payment_records (
                id TEXT PRIMARY KEY,
                assessment_id TEXT NOT NULL REFERENCES yearly_assessments(id) ON DELETE CASCADE,
                date_paid TEXT NOT NULL,
                or_number TEXT NOT NULL UNIQUE COLLATE NOCASE,
                or_number_hash TEXT,
                schedule TEXT NOT NULL DEFAULT '1st Qtr',
                amount NUMERIC(18,2) NOT NULL,
                remarks TEXT
            )");

            Execute(connection, transaction, @"INSERT INTO payment_records
                (id, assessment_id, date_paid, or_number, or_number_hash, schedule, amount, remarks)
                SELECT id, assessment_id, date_paid, or_number, or_number_hash, schedule, amount, remarks
                FROM payment_records_backup");
            Execute(connection, transaction, "DROP TABLE payment_records_backup");
        }

        private void RebuildFeeCatalogTable(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            Execute(connection, transaction, @"CREATE TABLE fee_catalog_new (
                id TEXT PRIMARY KEY,
                code TEXT,
                code_hash TEXT,
                description TEXT,
                amount NUMERIC(18,2) NOT NULL DEFAULT 0,
                is_active INTEGER NOT NULL DEFAULT 1,
                date_created TEXT NOT NULL
            )");

            Execute(connection, transaction, @"INSERT INTO fee_catalog_new
                (id, code, code_hash, description, amount, is_active, date_created)
                SELECT id, code, code_hash, description, amount, is_active, date_created
                FROM fee_catalog");
            Execute(connection, transaction, "DROP TABLE fee_catalog");
            Execute(connection, transaction, "ALTER TABLE fee_catalog_new RENAME TO fee_catalog");
        }

        private bool ColumnTypeStartsWith(SQLiteConnection connection, string tableName, string columnName, string expectedTypePrefix)
        {
            using (SQLiteCommand command = new SQLiteCommand("PRAGMA table_info(" + tableName + ")", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (String.Equals(Convert.ToString(reader["name"]), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        string type = Convert.ToString(reader["type"]) ?? "";
                        return type.StartsWith(expectedTypePrefix, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return false;
        }

        private bool ColumnExists(SQLiteConnection connection, string tableName, string columnName)
        {
            using (SQLiteCommand command = new SQLiteCommand("PRAGMA table_info(" + tableName + ")", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (String.Equals(Convert.ToString(reader["name"]), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Execute(SQLiteConnection connection, SQLiteTransaction transaction, string sql, params SQLiteParameter[] parameters)
        {
            using (SQLiteCommand command = new SQLiteCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                command.ExecuteNonQuery();
            }
        }

        private SQLiteParameter P(string name, object value)
        {
            return new SQLiteParameter(name, value ?? "");
        }

        private void MarkMeta(string key, string value)
        {
            using (SQLiteConnection connection = OpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            {
                Execute(
                    connection,
                    transaction,
                    "INSERT OR REPLACE INTO app_meta (key, value) VALUES (@key, @value)",
                    P("@key", key),
                    P("@value", value));
                transaction.Commit();
            }

            PersistEncryptedContainer();
        }

        private AppSettings LoadSettings(SQLiteConnection connection)
        {
            AppSettings settings = new AppSettings();
            settings.MunicipalityName = MetaText(connection, "municipality_name");
            settings.ProvinceName = MetaText(connection, "province_name");
            settings.OfficeName = MetaText(connection, "office_name");
            settings.TreasurerName = MetaText(connection, "treasurer_name");
            settings.CollectorName = MetaText(connection, "collector_name");
            settings.SealImagePath = MetaText(connection, "seal_image_path");
            settings.TreasurerSignaturePath = MetaText(connection, "treasurer_signature_path");
            settings.CollectorSignaturePath = MetaText(connection, "collector_signature_path");
            settings.ReportFooterNote = MetaText(connection, "report_footer_note");
            settings.BackupFolderPath = MetaText(connection, "backup_folder_path");
            settings.LockedFiscalYears = MetaText(connection, "locked_fiscal_years");
            settings.DarkModeEnabled = String.Equals(MetaText(connection, "dark_mode_enabled"), "1", StringComparison.OrdinalIgnoreCase);
            settings.LastAutoBackupDate = ParseDate(MetaText(connection, "last_auto_backup_date"), DateTime.MinValue);

            int defaultReportYear;
            if (Int32.TryParse(MetaText(connection, "default_report_year"), out defaultReportYear) && defaultReportYear > 0)
            {
                settings.DefaultReportYear = defaultReportYear;
            }

            int retentionDays;
            if (Int32.TryParse(MetaText(connection, "auto_backup_retention_days"), out retentionDays) && retentionDays > 0)
            {
                settings.AutoBackupRetentionDays = retentionDays;
            }

            return settings;
        }

        private void SaveSettings(SQLiteConnection connection, SQLiteTransaction transaction, AppSettings settings)
        {
            if (settings == null)
            {
                settings = new AppSettings();
            }

            SaveSetting(connection, transaction, "municipality_name", settings.MunicipalityName ?? "");
            SaveSetting(connection, transaction, "province_name", settings.ProvinceName ?? "");
            SaveSetting(connection, transaction, "office_name", settings.OfficeName ?? "");
            SaveSetting(connection, transaction, "treasurer_name", settings.TreasurerName ?? "");
            SaveSetting(connection, transaction, "collector_name", settings.CollectorName ?? "");
            SaveSetting(connection, transaction, "seal_image_path", settings.SealImagePath ?? "");
            SaveSetting(connection, transaction, "treasurer_signature_path", settings.TreasurerSignaturePath ?? "");
            SaveSetting(connection, transaction, "collector_signature_path", settings.CollectorSignaturePath ?? "");
            SaveSetting(connection, transaction, "report_footer_note", settings.ReportFooterNote ?? "");
            SaveSetting(connection, transaction, "default_report_year", settings.DefaultReportYear.ToString(CultureInfo.InvariantCulture));
            SaveSetting(connection, transaction, "backup_folder_path", settings.BackupFolderPath ?? "");
            SaveSetting(connection, transaction, "locked_fiscal_years", settings.LockedFiscalYears ?? "");
            SaveSetting(connection, transaction, "dark_mode_enabled", settings.DarkModeEnabled ? "1" : "0");
            SaveSetting(connection, transaction, "last_auto_backup_date", DateValue(settings.LastAutoBackupDate));
            SaveSetting(connection, transaction, "auto_backup_retention_days", settings.AutoBackupRetentionDays.ToString(CultureInfo.InvariantCulture));
        }

        private void SaveSetting(SQLiteConnection connection, SQLiteTransaction transaction, string key, string value)
        {
            Execute(
                connection,
                transaction,
                "INSERT OR REPLACE INTO app_meta (key, value) VALUES (@key, @value)",
                P("@key", key),
                P("@value", value ?? ""));
        }

        private string MetaText(SQLiteConnection connection, string key)
        {
            using (SQLiteCommand command = new SQLiteCommand("SELECT value FROM app_meta WHERE key = @key", connection))
            {
                command.Parameters.Add(P("@key", key));
                object value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return "";
                }

                return Convert.ToString(value);
            }
        }

        private LedgerDatabase LoadLegacyXml(string xmlPath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(xmlPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(LedgerDatabase));
                    LedgerDatabase database = serializer.Deserialize(stream) as LedgerDatabase;
                    if (database == null)
                    {
                        return new LedgerDatabase();
                    }

                    Repair(database);
                    return database;
                }
            }
            catch
            {
                string backupPath = xmlPath + ".failed-load-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".bak";
                File.Copy(xmlPath, backupPath, true);
                return new LedgerDatabase();
            }
        }

        private string LegacyXmlPath()
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (String.IsNullOrEmpty(directory))
            {
                directory = ApplicationDirectory();
            }

            return Path.Combine(directory, "business-ledger-data.xml");
        }

        private string ApplicationDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string Text(SQLiteDataReader reader, string columnName)
        {
            object value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return "";
            }

            return DataProtectionService.UnprotectString(Convert.ToString(value));
        }

        private int Number(SQLiteDataReader reader, string columnName)
        {
            object value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            int number;
            if (Int32.TryParse(DataProtectionService.UnprotectString(Convert.ToString(value)), out number))
            {
                return number;
            }

            return 0;
        }

        private decimal Money(SQLiteDataReader reader, string columnName)
        {
            object value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return 0m;
            }

            decimal money;
            if (value is decimal)
            {
                return (decimal)value;
            }

            if (value is int || value is long || value is double || value is float)
            {
                return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            }

            string textValue = Convert.ToString(value);
            if (Decimal.TryParse(textValue, NumberStyles.Number, CultureInfo.InvariantCulture, out money))
            {
                return money;
            }

            if (Decimal.TryParse(DataProtectionService.UnprotectString(textValue), NumberStyles.Number, CultureInfo.InvariantCulture, out money))
            {
                return money;
            }

            return 0m;
        }

        private bool Bool(SQLiteDataReader reader, string columnName)
        {
            object value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }

            int number;
            if (Int32.TryParse(Convert.ToString(value), out number))
            {
                return number != 0;
            }

            string textValue = Convert.ToString(value);
            if (String.Equals(textValue, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(textValue, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            textValue = DataProtectionService.UnprotectString(textValue);
            return String.Equals(textValue, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(textValue, "yes", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(textValue, "1", StringComparison.OrdinalIgnoreCase);
        }

        private DateTime Date(SQLiteDataReader reader, string columnName, DateTime fallback)
        {
            object value = reader[columnName];
            if (value == null || value == DBNull.Value)
            {
                return fallback;
            }

            return ParseDate(DataProtectionService.UnprotectString(Convert.ToString(value)), fallback);
        }

        private DateTime ParseDate(string value, DateTime fallback)
        {
            if (String.IsNullOrEmpty(value))
            {
                return fallback;
            }

            DateTime date;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out date))
            {
                return date;
            }

            return fallback;
        }

        private string NormalizeLockedFiscalYears(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            SortedSet<int> years = new SortedSet<int>();
            string[] parts = value.Split(new char[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int year;
                if (Int32.TryParse(part.Trim(), out year) && year >= 1900 && year <= 2100)
                {
                    years.Add(year);
                }
            }

            return String.Join(", ", years);
        }

        private string DateValue(DateTime value)
        {
            if (value == DateTime.MinValue)
            {
                return "";
            }

            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        private string MoneyValue(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string ProtectedText(string value)
        {
            return DataProtectionService.ProtectString(value ?? "");
        }

        private string ProtectedDateValue(DateTime value)
        {
            return DataProtectionService.ProtectDate(value);
        }

        private string ProtectedMoneyValue(decimal value)
        {
            return DataProtectionService.ProtectDecimal(value);
        }

        private void AppendAssessmentRow(StringBuilder builder, BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment)
        {
            string paymentDate = payment == null ? "" : payment.DatePaid.ToString("yyyy-MM-dd");
            string orNumber = payment == null ? "" : payment.OrNumber;
            string schedule = payment == null ? "" : payment.Schedule;
            string amount = payment == null ? "" : payment.Amount.ToString("0.00");
            string remarks = payment == null ? "" : payment.Remarks;

            builder.AppendLine(String.Join(",", new string[]
            {
                Csv(owner.OwnerName),
                Csv(owner.BusinessName),
                Csv(owner.LineOfBusiness),
                Csv(owner.Status),
                Csv(owner.RegistrationType),
                Csv(owner.PrivacyConsentGiven ? "Yes" : "No"),
                Csv(owner.PrivacyConsentDate == DateTime.MinValue ? "" : owner.PrivacyConsentDate.ToString("yyyy-MM-dd")),
                Csv(owner.PrivacyConsentMethod),
                Csv(owner.PrivacyNoticeVersion),
                assessment.Year.ToString(),
                assessment.Capital.ToString("0.00"),
                assessment.GrossSales.ToString("0.00"),
                assessment.BusinessTax.ToString("0.00"),
                assessment.MayorsPermit.ToString("0.00"),
                assessment.Fees.ToString("0.00"),
                assessment.Surcharge.ToString("0.00"),
                assessment.Penalty.ToString("0.00"),
                assessment.TotalAssessment.ToString("0.00"),
                assessment.TotalPaid.ToString("0.00"),
                assessment.Balance.ToString("0.00"),
                Csv(paymentDate),
                Csv(orNumber),
                Csv(schedule),
                Csv(amount),
                Csv(remarks)
            }));
        }

        private void AppendAuditCsvRow(StringBuilder builder, AuditLogEntry entry, AuditLogDetail detail)
        {
            builder.AppendLine(String.Join(",", new string[]
            {
                Csv(entry == null ? "" : entry.Id),
                Csv(entry == null ? "" : entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(entry == null ? "" : entry.Username),
                Csv(entry == null ? "" : entry.Role),
                Csv(entry == null ? "" : entry.Action),
                Csv(entry == null ? "" : entry.EntityType),
                Csv(entry == null ? "" : entry.EntityId),
                Csv(entry == null ? "" : entry.Details),
                Csv(entry == null ? "" : ShortHash(entry.PreviousHash)),
                Csv(entry == null ? "" : ShortHash(entry.EntryHash)),
                Csv(detail == null ? "" : detail.FieldName),
                Csv(detail == null ? "" : detail.OldValue),
                Csv(detail == null ? "" : detail.NewValue)
            }));
        }

        private string ShortHash(string hash)
        {
            if (String.IsNullOrEmpty(hash))
            {
                return "";
            }

            return hash.Length <= 16 ? hash : hash.Substring(0, 16);
        }

        private string Csv(string value)
        {
            if (value == null)
            {
                value = "";
            }

            value = value.Trim();
            if (value.StartsWith("=") ||
                value.StartsWith("+") ||
                value.StartsWith("-") ||
                value.StartsWith("@"))
            {
                value = "'" + value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void Repair(LedgerDatabase database)
        {
            if (database.Owners == null)
            {
                database.Owners = new List<BusinessOwner>();
            }

            if (database.Users == null)
            {
                database.Users = new List<UserAccount>();
            }

            if (database.AuditTrail == null)
            {
                database.AuditTrail = new List<AuditLogEntry>();
            }

            if (database.FeeCatalog == null)
            {
                database.FeeCatalog = new List<FeeCatalogItem>();
            }

            if (database.Settings == null)
            {
                database.Settings = new AppSettings();
            }

            database.Settings.MunicipalityName = (database.Settings.MunicipalityName ?? "").Trim();
            database.Settings.ProvinceName = (database.Settings.ProvinceName ?? "").Trim();
            database.Settings.OfficeName = (database.Settings.OfficeName ?? "").Trim();
            database.Settings.TreasurerName = (database.Settings.TreasurerName ?? "").Trim();
            database.Settings.CollectorName = (database.Settings.CollectorName ?? "").Trim();
            database.Settings.SealImagePath = (database.Settings.SealImagePath ?? "").Trim();
            database.Settings.TreasurerSignaturePath = (database.Settings.TreasurerSignaturePath ?? "").Trim();
            database.Settings.CollectorSignaturePath = (database.Settings.CollectorSignaturePath ?? "").Trim();
            database.Settings.ReportFooterNote = (database.Settings.ReportFooterNote ?? "").Trim();
            if (String.IsNullOrEmpty(database.Settings.OfficeName))
            {
                database.Settings.OfficeName = "Municipal Treasurer's Office";
            }

            if (database.Settings.DefaultReportYear <= 0)
            {
                database.Settings.DefaultReportYear = DateTime.Today.Year;
            }

            database.Settings.BackupFolderPath = (database.Settings.BackupFolderPath ?? "").Trim();
            database.Settings.LockedFiscalYears = NormalizeLockedFiscalYears(database.Settings.LockedFiscalYears);
            if (database.Settings.AutoBackupRetentionDays <= 0)
            {
                database.Settings.AutoBackupRetentionDays = 30;
            }

            foreach (BusinessOwner owner in database.Owners)
            {
                if (String.IsNullOrEmpty(owner.Id))
                {
                    owner.Id = Guid.NewGuid().ToString("N");
                }

                if (String.IsNullOrEmpty(owner.Status))
                {
                    owner.Status = "Active";
                }

                if (String.IsNullOrEmpty(owner.RegistrationType))
                {
                    owner.RegistrationType = "Renewal";
                }

                owner.PrivacyConsentMethod = (owner.PrivacyConsentMethod ?? "").Trim();
                owner.PrivacyNoticeVersion = String.IsNullOrWhiteSpace(owner.PrivacyNoticeVersion) ? "RA10173-v1" : owner.PrivacyNoticeVersion.Trim();
                if (owner.PrivacyConsentDate == DateTime.MinValue)
                {
                    owner.PrivacyConsentDate = DateTime.Today;
                }

                if (owner.Assessments == null)
                {
                    owner.Assessments = new List<YearlyAssessment>();
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (String.IsNullOrEmpty(assessment.Id))
                    {
                        assessment.Id = Guid.NewGuid().ToString("N");
                    }

                    if (assessment.Year == 0)
                    {
                        assessment.Year = DateTime.Today.Year;
                    }

                    if (assessment.Payments == null)
                    {
                        assessment.Payments = new List<PaymentRecord>();
                    }

                    foreach (PaymentRecord payment in assessment.Payments)
                    {
                        if (String.IsNullOrEmpty(payment.Id))
                        {
                            payment.Id = Guid.NewGuid().ToString("N");
                        }

                        if (String.IsNullOrEmpty(payment.Schedule))
                        {
                            payment.Schedule = "1st Qtr";
                        }
                    }
                }
            }

            foreach (UserAccount user in database.Users)
            {
                if (String.IsNullOrEmpty(user.Id))
                {
                    user.Id = Guid.NewGuid().ToString("N");
                }

                if (String.IsNullOrEmpty(user.Role))
                {
                    user.Role = SecurityService.CashierRole;
                }

                if (String.IsNullOrEmpty(user.Username))
                {
                    user.Username = "user-" + user.Id.Substring(0, 6);
                }
            }

            foreach (AuditLogEntry entry in database.AuditTrail)
            {
                if (String.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = Guid.NewGuid().ToString("N");
                }

                if (entry.Timestamp == DateTime.MinValue)
                {
                    entry.Timestamp = DateTime.Now;
                }
            }

            foreach (FeeCatalogItem fee in database.FeeCatalog)
            {
                if (String.IsNullOrEmpty(fee.Id))
                {
                    fee.Id = Guid.NewGuid().ToString("N");
                }

                fee.Code = (fee.Code ?? "").Trim();
                fee.Description = (fee.Description ?? "").Trim();
                if (fee.Amount < 0m)
                {
                    fee.Amount = 0m;
                }

                if (fee.DateCreated == DateTime.MinValue)
                {
                    fee.DateCreated = DateTime.Today;
                }
            }
        }
    }
}
