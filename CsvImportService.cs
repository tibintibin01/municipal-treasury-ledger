using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class CsvImportResult
    {
        public int RowsRead { get; set; }
        public int OwnersCreated { get; set; }
        public int OwnersUpdated { get; set; }
        public int AssessmentsCreated { get; set; }
        public int AssessmentsUpdated { get; set; }
        public int PaymentsCreated { get; set; }
        public int RowsSkipped { get; set; }
        public List<string> Messages { get; private set; }

        public CsvImportResult()
        {
            Messages = new List<string>();
        }

        public string Summary
        {
            get
            {
                return "Rows: " + RowsRead.ToString("N0") +
                    " | Owners created: " + OwnersCreated.ToString("N0") +
                    " | Owners updated: " + OwnersUpdated.ToString("N0") +
                    " | Assessments created: " + AssessmentsCreated.ToString("N0") +
                    " | Assessments updated: " + AssessmentsUpdated.ToString("N0") +
                    " | Payments created: " + PaymentsCreated.ToString("N0") +
                    " | Skipped: " + RowsSkipped.ToString("N0");
            }
        }
    }

    public static class CsvImportService
    {
        public static CsvImportResult PreviewBusinessLedgerCsv(LedgerDatabase database, string filePath)
        {
            LedgerDatabase previewDatabase = CloneForPreview(database);
            return ImportBusinessLedgerCsv(previewDatabase, filePath);
        }

        public static CsvImportResult ImportBusinessLedgerCsv(LedgerDatabase database, string filePath)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (String.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("CSV import file was not found.", filePath);
            }

            if (database.Owners == null)
            {
                database.Owners = new List<BusinessOwner>();
            }

            CsvImportResult result = new CsvImportResult();
            List<string[]> rows = ReadCsv(filePath);
            if (rows.Count == 0)
            {
                result.Messages.Add("CSV file is empty.");
                return result;
            }

            Dictionary<string, int> headers = BuildHeaderMap(rows[0]);
            if (headers.Count == 0)
            {
                result.Messages.Add("CSV header row was not readable.");
                return result;
            }

            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (IsBlankRow(row))
                {
                    continue;
                }

                result.RowsRead++;
                try
                {
                    ImportRow(database, headers, row, i + 1, result);
                }
                catch (Exception ex)
                {
                    result.RowsSkipped++;
                    AddMessage(result, "Row " + (i + 1).ToString() + " skipped: " + ex.Message);
                }
            }

            return result;
        }

        private static LedgerDatabase CloneForPreview(LedgerDatabase source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            LedgerDatabase clone = new LedgerDatabase();
            clone.Owners = (source.Owners ?? new List<BusinessOwner>()).Select(CloneOwner).ToList();
            clone.Users = (source.Users ?? new List<UserAccount>()).Select(CloneUser).ToList();
            clone.AuditTrail = (source.AuditTrail ?? new List<AuditLogEntry>()).Select(CloneAuditLogEntry).ToList();
            clone.FeeCatalog = (source.FeeCatalog ?? new List<FeeCatalogItem>()).Select(CloneFeeCatalogItem).ToList();
            clone.Settings = CloneSettings(source.Settings);
            clone.AuditChainTipHash = source.AuditChainTipHash ?? "";
            return clone;
        }

        private static BusinessOwner CloneOwner(BusinessOwner source)
        {
            if (source == null)
            {
                return new BusinessOwner();
            }

            return new BusinessOwner
            {
                Id = source.Id,
                OwnerName = source.OwnerName,
                BusinessName = source.BusinessName,
                OwnerAddress = source.OwnerAddress,
                BusinessAddress = source.BusinessAddress,
                ContactNumber = source.ContactNumber,
                LineOfBusiness = source.LineOfBusiness,
                Tin = source.Tin,
                Status = source.Status,
                RegistrationType = source.RegistrationType,
                Remarks = source.Remarks,
                PrivacyConsentGiven = source.PrivacyConsentGiven,
                PrivacyConsentDate = source.PrivacyConsentDate,
                PrivacyConsentMethod = source.PrivacyConsentMethod,
                PrivacyNoticeVersion = source.PrivacyNoticeVersion,
                DateCreated = source.DateCreated,
                Assessments = (source.Assessments ?? new List<YearlyAssessment>()).Select(CloneAssessment).ToList()
            };
        }

        private static YearlyAssessment CloneAssessment(YearlyAssessment source)
        {
            if (source == null)
            {
                return new YearlyAssessment();
            }

            return new YearlyAssessment
            {
                Id = source.Id,
                Year = source.Year,
                Capital = source.Capital,
                GrossSales = source.GrossSales,
                BusinessTax = source.BusinessTax,
                MayorsPermit = source.MayorsPermit,
                Fees = source.Fees,
                Surcharge = source.Surcharge,
                Penalty = source.Penalty,
                Remarks = source.Remarks,
                DateCreated = source.DateCreated,
                Payments = (source.Payments ?? new List<PaymentRecord>()).Select(ClonePayment).ToList()
            };
        }

        private static PaymentRecord ClonePayment(PaymentRecord source)
        {
            if (source == null)
            {
                return new PaymentRecord();
            }

            return new PaymentRecord
            {
                Id = source.Id,
                DatePaid = source.DatePaid,
                OrNumber = source.OrNumber,
                Schedule = source.Schedule,
                Amount = source.Amount,
                Remarks = source.Remarks
            };
        }

        private static UserAccount CloneUser(UserAccount source)
        {
            if (source == null)
            {
                return new UserAccount();
            }

            return new UserAccount
            {
                Id = source.Id,
                Username = source.Username,
                FullName = source.FullName,
                Role = source.Role,
                PasswordAlgorithm = source.PasswordAlgorithm,
                PasswordSalt = source.PasswordSalt,
                PasswordHash = source.PasswordHash,
                FailedLoginCount = source.FailedLoginCount,
                LockoutUntil = source.LockoutUntil,
                IsActive = source.IsActive,
                DateCreated = source.DateCreated
            };
        }

        private static AuditLogEntry CloneAuditLogEntry(AuditLogEntry source)
        {
            if (source == null)
            {
                return new AuditLogEntry();
            }

            return new AuditLogEntry
            {
                Id = source.Id,
                Timestamp = source.Timestamp,
                Username = source.Username,
                Role = source.Role,
                Action = source.Action,
                EntityType = source.EntityType,
                EntityId = source.EntityId,
                Details = source.Details,
                PreviousHash = source.PreviousHash,
                EntryHash = source.EntryHash,
                ChangeDetails = (source.ChangeDetails ?? new List<AuditLogDetail>()).Select(CloneAuditLogDetail).ToList()
            };
        }

        private static AuditLogDetail CloneAuditLogDetail(AuditLogDetail source)
        {
            if (source == null)
            {
                return new AuditLogDetail();
            }

            return new AuditLogDetail
            {
                Id = source.Id,
                AuditLogId = source.AuditLogId,
                FieldName = source.FieldName,
                OldValue = source.OldValue,
                NewValue = source.NewValue
            };
        }

        private static FeeCatalogItem CloneFeeCatalogItem(FeeCatalogItem source)
        {
            if (source == null)
            {
                return new FeeCatalogItem();
            }

            return new FeeCatalogItem
            {
                Id = source.Id,
                Code = source.Code,
                Description = source.Description,
                Amount = source.Amount,
                IsActive = source.IsActive,
                DateCreated = source.DateCreated
            };
        }

        private static AppSettings CloneSettings(AppSettings source)
        {
            if (source == null)
            {
                return new AppSettings();
            }

            return new AppSettings
            {
                MunicipalityName = source.MunicipalityName,
                ProvinceName = source.ProvinceName,
                OfficeName = source.OfficeName,
                TreasurerName = source.TreasurerName,
                CollectorName = source.CollectorName,
                SealImagePath = source.SealImagePath,
                TreasurerSignaturePath = source.TreasurerSignaturePath,
                CollectorSignaturePath = source.CollectorSignaturePath,
                ReportFooterNote = source.ReportFooterNote,
                DefaultReportYear = source.DefaultReportYear,
                BackupFolderPath = source.BackupFolderPath,
                LastAutoBackupDate = source.LastAutoBackupDate,
                AutoBackupRetentionDays = source.AutoBackupRetentionDays,
                LockedFiscalYears = source.LockedFiscalYears
            };
        }

        private static void ImportRow(LedgerDatabase database, Dictionary<string, int> headers, string[] row, int rowNumber, CsvImportResult result)
        {
            string ownerName = Value(headers, row, "ownername", "owner");
            string businessName = Value(headers, row, "businessname", "business");
            string tin = Value(headers, row, "tin");
            if (String.IsNullOrWhiteSpace(ownerName) && String.IsNullOrWhiteSpace(businessName))
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " skipped: owner or business name is required.");
                return;
            }

            if (!LedgerValidation.IsValidTin(tin))
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " skipped: invalid TIN.");
                return;
            }

            BusinessOwner owner = FindOwner(database, ownerName, businessName, tin);
            bool isNewOwner = owner == null;
            if (owner == null)
            {
                owner = new BusinessOwner();
                database.Owners.Add(owner);
                result.OwnersCreated++;
            }
            else
            {
                result.OwnersUpdated++;
            }

            owner.OwnerName = ValueOrExisting(owner.OwnerName, ownerName);
            owner.BusinessName = ValueOrExisting(owner.BusinessName, businessName);
            owner.OwnerAddress = ValueOrExisting(owner.OwnerAddress, Value(headers, row, "owneraddress"));
            owner.BusinessAddress = ValueOrExisting(owner.BusinessAddress, Value(headers, row, "businessaddress"));
            owner.ContactNumber = ValueOrExisting(owner.ContactNumber, Value(headers, row, "contactnumber", "contactno", "contact"));
            owner.LineOfBusiness = ValueOrExisting(owner.LineOfBusiness, Value(headers, row, "lineofbusiness", "line"));
            owner.Tin = ValueOrExisting(owner.Tin, tin);
            owner.Status = ValueOrExisting(owner.Status, Value(headers, row, "status"));
            owner.RegistrationType = ValueOrExisting(owner.RegistrationType, Value(headers, row, "registrationtype", "type"));
            owner.Remarks = ValueOrExisting(owner.Remarks, Value(headers, row, "remarks"));
            string consentValue = Value(headers, row, "privacyconsent", "consent", "dataprivacyconsent");
            if (!String.IsNullOrWhiteSpace(consentValue))
            {
                owner.PrivacyConsentGiven = IsYes(consentValue);
            }

            DateTime consentDate;
            if (TryDate(Value(headers, row, "consentdate", "privacyconsentdate"), out consentDate))
            {
                owner.PrivacyConsentDate = consentDate;
            }

            owner.PrivacyConsentMethod = ValueOrExisting(owner.PrivacyConsentMethod, Value(headers, row, "consentmethod", "privacyconsentmethod"));
            owner.PrivacyNoticeVersion = ValueOrExisting(owner.PrivacyNoticeVersion, Value(headers, row, "privacynoticeversion", "noticeversion"));

            if (String.IsNullOrWhiteSpace(owner.Status))
            {
                owner.Status = "Active";
            }

            if (String.IsNullOrWhiteSpace(owner.RegistrationType))
            {
                owner.RegistrationType = "Renewal";
            }

            if (owner.PrivacyConsentDate == DateTime.MinValue)
            {
                owner.PrivacyConsentDate = DateTime.Today;
            }

            if (String.IsNullOrWhiteSpace(owner.PrivacyNoticeVersion))
            {
                owner.PrivacyNoticeVersion = "RA10173-v1";
            }

            if (owner.Assessments == null)
            {
                owner.Assessments = new List<YearlyAssessment>();
            }

            int year;
            if (!TryInt(Value(headers, row, "year", "assessmentyear"), out year))
            {
                return;
            }

            if (!LedgerValidation.IsValidAssessmentYear(year))
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " skipped assessment: invalid year.");
                return;
            }

            YearlyAssessment assessment = owner.Assessments.FirstOrDefault(item => item.Year == year);
            if (assessment == null)
            {
                assessment = new YearlyAssessment { Year = year };
                owner.Assessments.Add(assessment);
                result.AssessmentsCreated++;
            }
            else
            {
                result.AssessmentsUpdated++;
            }

            assessment.Capital = Money(headers, row, "capital", assessment.Capital);
            assessment.GrossSales = Money(headers, row, "grosssales", assessment.GrossSales);
            assessment.BusinessTax = Money(headers, row, "businesstax", assessment.BusinessTax);
            assessment.MayorsPermit = Money(headers, row, "mayorspermit", "mayorpermit", "mayor'spermit", assessment.MayorsPermit);
            assessment.Fees = Money(headers, row, "fees", assessment.Fees);
            assessment.Surcharge = Money(headers, row, "surcharge", assessment.Surcharge);
            assessment.Penalty = Money(headers, row, "penalty", assessment.Penalty);
            string assessmentRemarks = Value(headers, row, "assessmentremarks");
            if (!String.IsNullOrWhiteSpace(assessmentRemarks))
            {
                assessment.Remarks = assessmentRemarks;
            }

            ImportPayment(database, headers, row, rowNumber, assessment, result);
        }

        private static void ImportPayment(LedgerDatabase database, Dictionary<string, int> headers, string[] row, int rowNumber, YearlyAssessment assessment, CsvImportResult result)
        {
            string orNumber = Value(headers, row, "ornumber", "or");
            decimal amount = Money(headers, row, "paymentamount", "amountpaid", "amount", 0m);
            if (String.IsNullOrWhiteSpace(orNumber) && amount <= 0m)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(orNumber))
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " payment skipped: OR number is required.");
                return;
            }

            if (amount <= 0m)
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " payment skipped: amount must be greater than zero.");
                return;
            }

            if (LedgerValidation.OrNumberExists(database, orNumber, null))
            {
                AddMessage(result, "Row " + rowNumber.ToString() + " payment skipped: duplicate OR " + orNumber + ".");
                return;
            }

            if (amount > assessment.Balance)
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " payment skipped: payment is greater than assessment balance.");
                return;
            }

            DateTime datePaid;
            if (!TryDate(Value(headers, row, "paymentdate", "dateofpayment", "datepaid"), out datePaid))
            {
                datePaid = DateTime.Today;
            }

            if (!LedgerValidation.IsValidPaymentDate(datePaid))
            {
                result.RowsSkipped++;
                AddMessage(result, "Row " + rowNumber.ToString() + " payment skipped: payment date cannot be in the future.");
                return;
            }

            if (assessment.Payments == null)
            {
                assessment.Payments = new List<PaymentRecord>();
            }

            assessment.Payments.Add(new PaymentRecord
            {
                DatePaid = datePaid,
                OrNumber = orNumber.Trim(),
                Schedule = Value(headers, row, "paymentschedule", "schedule"),
                Amount = amount,
                Remarks = Value(headers, row, "paymentremarks")
            });
            if (String.IsNullOrWhiteSpace(assessment.Payments[assessment.Payments.Count - 1].Schedule))
            {
                assessment.Payments[assessment.Payments.Count - 1].Schedule = "Annual";
            }

            result.PaymentsCreated++;
        }

        private static BusinessOwner FindOwner(LedgerDatabase database, string ownerName, string businessName, string tin)
        {
            if (!String.IsNullOrWhiteSpace(tin))
            {
                BusinessOwner byTin = database.Owners.FirstOrDefault(owner => String.Equals(owner.Tin ?? "", tin, StringComparison.OrdinalIgnoreCase));
                if (byTin != null)
                {
                    return byTin;
                }
            }

            return database.Owners.FirstOrDefault(owner =>
                String.Equals(owner.OwnerName ?? "", ownerName ?? "", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(owner.BusinessName ?? "", businessName ?? "", StringComparison.OrdinalIgnoreCase));
        }

        private static List<string[]> ReadCsv(string filePath)
        {
            List<string[]> rows = new List<string[]>();
            foreach (string line in File.ReadAllLines(filePath))
            {
                rows.Add(ParseCsvLine(line).ToArray());
            }

            return rows;
        }

        private static List<string> ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            string value = "";
            bool inQuotes = false;
            for (int i = 0; i < (line ?? "").Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(value.Trim());
                    value = "";
                }
                else
                {
                    value += c;
                }
            }

            values.Add(value.Trim());
            return values;
        }

        private static Dictionary<string, int> BuildHeaderMap(string[] headers)
        {
            Dictionary<string, int> map = new Dictionary<string, int>();
            for (int i = 0; i < headers.Length; i++)
            {
                string key = NormalizeHeader(headers[i]);
                if (!String.IsNullOrEmpty(key) && !map.ContainsKey(key))
                {
                    map[key] = i;
                }
            }

            return map;
        }

        private static string Value(Dictionary<string, int> headers, string[] row, params string[] names)
        {
            foreach (string name in names)
            {
                string key = NormalizeHeader(name);
                if (headers.ContainsKey(key))
                {
                    int index = headers[key];
                    if (index >= 0 && index < row.Length)
                    {
                        return (row[index] ?? "").Trim();
                    }
                }
            }

            return "";
        }

        private static decimal Money(Dictionary<string, int> headers, string[] row, string name, decimal fallback)
        {
            return Money(headers, row, new string[] { name }, fallback);
        }

        private static decimal Money(Dictionary<string, int> headers, string[] row, string name1, string name2, string name3, string name4, decimal fallback)
        {
            return Money(headers, row, new string[] { name1, name2, name3, name4 }, fallback);
        }

        private static decimal Money(Dictionary<string, int> headers, string[] row, string name1, string name2, string name3, decimal fallback)
        {
            return Money(headers, row, new string[] { name1, name2, name3 }, fallback);
        }

        private static decimal Money(Dictionary<string, int> headers, string[] row, string[] names, decimal fallback)
        {
            string value = Value(headers, row, names);
            decimal money;
            if (String.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return LedgerValidation.TryParseMoney(value, out money) ? money : fallback;
        }

        private static bool TryInt(string value, out int number)
        {
            return Int32.TryParse((value ?? "").Trim(), out number);
        }

        private static bool TryDate(string value, out DateTime date)
        {
            value = (value ?? "").Trim();
            string[] formats = new string[] { "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy", "yyyyMMdd" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date) ||
                DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
        }

        private static bool IsYes(string value)
        {
            value = (value ?? "").Trim();
            return String.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "accepted", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBlankRow(string[] row)
        {
            return row == null || row.All(value => String.IsNullOrWhiteSpace(value));
        }

        private static string ValueOrExisting(string existing, string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return existing ?? "";
            }

            return value.Trim();
        }

        private static void AddMessage(CsvImportResult result, string message)
        {
            if (result.Messages.Count < 20)
            {
                result.Messages.Add(message);
            }
        }

        private static string NormalizeHeader(string value)
        {
            value = (value ?? "").Trim().ToLowerInvariant();
            string result = "";
            foreach (char c in value)
            {
                if (Char.IsLetterOrDigit(c))
                {
                    result += c;
                }
            }

            return result;
        }
    }
}
