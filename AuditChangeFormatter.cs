using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public static class AuditChangeFormatter
    {
        public static BusinessOwner CloneOwner(BusinessOwner owner)
        {
            if (owner == null)
            {
                return null;
            }

            return new BusinessOwner
            {
                Id = owner.Id,
                OwnerName = owner.OwnerName,
                BusinessName = owner.BusinessName,
                OwnerAddress = owner.OwnerAddress,
                BusinessAddress = owner.BusinessAddress,
                ContactNumber = owner.ContactNumber,
                LineOfBusiness = owner.LineOfBusiness,
                Tin = owner.Tin,
                Status = owner.Status,
                RegistrationType = owner.RegistrationType,
                Remarks = owner.Remarks,
                PrivacyConsentGiven = owner.PrivacyConsentGiven,
                PrivacyConsentDate = owner.PrivacyConsentDate,
                PrivacyConsentMethod = owner.PrivacyConsentMethod,
                PrivacyNoticeVersion = owner.PrivacyNoticeVersion,
                DateCreated = owner.DateCreated
            };
        }

        public static YearlyAssessment CloneAssessment(YearlyAssessment assessment)
        {
            if (assessment == null)
            {
                return null;
            }

            return new YearlyAssessment
            {
                Id = assessment.Id,
                Year = assessment.Year,
                Capital = assessment.Capital,
                GrossSales = assessment.GrossSales,
                BusinessTax = assessment.BusinessTax,
                MayorsPermit = assessment.MayorsPermit,
                Fees = assessment.Fees,
                Surcharge = assessment.Surcharge,
                Penalty = assessment.Penalty,
                Remarks = assessment.Remarks,
                DateCreated = assessment.DateCreated
            };
        }

        public static UserAccount CloneUser(UserAccount user)
        {
            if (user == null)
            {
                return null;
            }

            return new UserAccount
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                PasswordAlgorithm = user.PasswordAlgorithm,
                PasswordSalt = user.PasswordSalt,
                PasswordHash = user.PasswordHash,
                FailedLoginCount = user.FailedLoginCount,
                LockoutUntil = user.LockoutUntil,
                IsActive = user.IsActive,
                DateCreated = user.DateCreated
            };
        }

        public static AppSettings CloneSettings(AppSettings settings)
        {
            if (settings == null)
            {
                return null;
            }

            return new AppSettings
            {
                MunicipalityName = settings.MunicipalityName,
                ProvinceName = settings.ProvinceName,
                OfficeName = settings.OfficeName,
                TreasurerName = settings.TreasurerName,
                CollectorName = settings.CollectorName,
                SealImagePath = settings.SealImagePath,
                TreasurerSignaturePath = settings.TreasurerSignaturePath,
                CollectorSignaturePath = settings.CollectorSignaturePath,
                ReportFooterNote = settings.ReportFooterNote,
                DefaultReportYear = settings.DefaultReportYear,
                BackupFolderPath = settings.BackupFolderPath,
                LastAutoBackupDate = settings.LastAutoBackupDate,
                AutoBackupRetentionDays = settings.AutoBackupRetentionDays,
                LockedFiscalYears = settings.LockedFiscalYears,
                DarkModeEnabled = settings.DarkModeEnabled
            };
        }

        public static FeeCatalogItem CloneFee(FeeCatalogItem fee)
        {
            if (fee == null)
            {
                return null;
            }

            return new FeeCatalogItem
            {
                Id = fee.Id,
                Code = fee.Code,
                Description = fee.Description,
                Amount = fee.Amount,
                IsActive = fee.IsActive,
                DateCreated = fee.DateCreated
            };
        }

        public static string OwnerDetails(BusinessOwner before, BusinessOwner after, bool isNew)
        {
            string name = AuditService.OwnerName(after);
            if (isNew || before == null)
            {
                return "Created " + ValueOrBlank(name) + " | Status: " + ValueOrBlank(after.Status) + "; Type: " + ValueOrBlank(after.RegistrationType);
            }

            List<string> changes = new List<string>();
            AddTextChange(changes, "Owner name", before.OwnerName, after.OwnerName);
            AddTextChange(changes, "Business name", before.BusinessName, after.BusinessName);
            AddTextChange(changes, "Owner address", before.OwnerAddress, after.OwnerAddress);
            AddTextChange(changes, "Business address", before.BusinessAddress, after.BusinessAddress);
            AddTextChange(changes, "Contact no.", before.ContactNumber, after.ContactNumber);
            AddTextChange(changes, "Line of business", before.LineOfBusiness, after.LineOfBusiness);
            AddTextChange(changes, "TIN", before.Tin, after.Tin);
            AddTextChange(changes, "Status", before.Status, after.Status);
            AddTextChange(changes, "Type", before.RegistrationType, after.RegistrationType);
            AddTextChange(changes, "Remarks", before.Remarks, after.Remarks);
            AddBoolChange(changes, "Privacy consent", before.PrivacyConsentGiven, after.PrivacyConsentGiven);
            AddDateChange(changes, "Privacy consent date", before.PrivacyConsentDate, after.PrivacyConsentDate);
            AddTextChange(changes, "Privacy consent method", before.PrivacyConsentMethod, after.PrivacyConsentMethod);
            AddTextChange(changes, "Privacy notice version", before.PrivacyNoticeVersion, after.PrivacyNoticeVersion);

            return WithPrefix(name, changes);
        }

        public static string AssessmentDetails(BusinessOwner owner, YearlyAssessment before, YearlyAssessment after, bool isNew)
        {
            string prefix = AuditService.OwnerName(owner) + " | Year " + after.Year;
            if (isNew || before == null)
            {
                return prefix + " created | Total: " + Money(after.TotalAssessment);
            }

            List<string> changes = new List<string>();
            AddIntChange(changes, "Year", before.Year, after.Year);
            AddMoneyChange(changes, "Capital", before.Capital, after.Capital);
            AddMoneyChange(changes, "Gross sales", before.GrossSales, after.GrossSales);
            AddMoneyChange(changes, "Business tax", before.BusinessTax, after.BusinessTax);
            AddMoneyChange(changes, "Mayor's permit", before.MayorsPermit, after.MayorsPermit);
            AddMoneyChange(changes, "Fees", before.Fees, after.Fees);
            AddMoneyChange(changes, "Surcharge", before.Surcharge, after.Surcharge);
            AddMoneyChange(changes, "Penalty", before.Penalty, after.Penalty);
            AddTextChange(changes, "Remarks", before.Remarks, after.Remarks);

            return WithPrefix(prefix, changes);
        }

        public static string UserDetails(UserAccount before, UserAccount after, bool isNew, bool passwordChanged)
        {
            string prefix = (after.Username ?? "") + " (" + (after.Role ?? "") + ")";
            if (isNew || before == null)
            {
                return prefix + " created | Active: " + YesNo(after.IsActive) + (passwordChanged ? "; Initial password set" : "");
            }

            List<string> changes = new List<string>();
            AddTextChange(changes, "Username", before.Username, after.Username);
            AddTextChange(changes, "Full name", before.FullName, after.FullName);
            AddTextChange(changes, "Role", before.Role, after.Role);
            AddBoolChange(changes, "Active", before.IsActive, after.IsActive);
            if (passwordChanged)
            {
                changes.Add("Password changed");
            }

            return WithPrefix(prefix, changes);
        }

        public static string SettingsDetails(AppSettings before, AppSettings after)
        {
            if (after == null)
            {
                return "Settings cleared";
            }

            if (before == null)
            {
                before = new AppSettings();
            }

            List<string> changes = new List<string>();
            AddTextChange(changes, "Municipality", before.MunicipalityName, after.MunicipalityName);
            AddTextChange(changes, "Province", before.ProvinceName, after.ProvinceName);
            AddTextChange(changes, "Office", before.OfficeName, after.OfficeName);
            AddTextChange(changes, "Treasurer", before.TreasurerName, after.TreasurerName);
            AddTextChange(changes, "Collector", before.CollectorName, after.CollectorName);
            AddTextChange(changes, "Seal/logo path", before.SealImagePath, after.SealImagePath);
            AddTextChange(changes, "Treasurer signature path", before.TreasurerSignaturePath, after.TreasurerSignaturePath);
            AddTextChange(changes, "Collector signature path", before.CollectorSignaturePath, after.CollectorSignaturePath);
            AddTextChange(changes, "Report footer", before.ReportFooterNote, after.ReportFooterNote);
            AddIntChange(changes, "Default report year", before.DefaultReportYear, after.DefaultReportYear);
            AddTextChange(changes, "Backup folder", before.BackupFolderPath, after.BackupFolderPath);
            AddIntChange(changes, "Backup retention days", before.AutoBackupRetentionDays, after.AutoBackupRetentionDays);
            AddTextChange(changes, "Locked fiscal years", before.LockedFiscalYears, after.LockedFiscalYears);
            AddBoolChange(changes, "Dark mode", before.DarkModeEnabled, after.DarkModeEnabled);

            return WithPrefix("LGU/report settings", changes);
        }

        public static List<AuditLogDetail> OwnerChangeDetails(BusinessOwner before, BusinessOwner after, bool isNew)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (after == null)
            {
                return details;
            }

            if (isNew || before == null)
            {
                AddNewTextDetail(details, "Owner name", after.OwnerName);
                AddNewTextDetail(details, "Business name", after.BusinessName);
                AddNewTextDetail(details, "Contact no.", after.ContactNumber);
                AddNewTextDetail(details, "Line of business", after.LineOfBusiness);
                AddNewTextDetail(details, "TIN", after.Tin);
                AddNewTextDetail(details, "Status", after.Status);
                AddNewTextDetail(details, "Type", after.RegistrationType);
                details.Add(AuditService.Detail("Privacy consent", "", YesNo(after.PrivacyConsentGiven)));
                AddNewTextDetail(details, "Privacy consent method", after.PrivacyConsentMethod);
                AddNewTextDetail(details, "Privacy notice version", after.PrivacyNoticeVersion);
                return details;
            }

            AddTextDetail(details, "Owner name", before.OwnerName, after.OwnerName);
            AddTextDetail(details, "Business name", before.BusinessName, after.BusinessName);
            AddTextDetail(details, "Owner address", before.OwnerAddress, after.OwnerAddress);
            AddTextDetail(details, "Business address", before.BusinessAddress, after.BusinessAddress);
            AddTextDetail(details, "Contact no.", before.ContactNumber, after.ContactNumber);
            AddTextDetail(details, "Line of business", before.LineOfBusiness, after.LineOfBusiness);
            AddTextDetail(details, "TIN", before.Tin, after.Tin);
            AddTextDetail(details, "Status", before.Status, after.Status);
            AddTextDetail(details, "Type", before.RegistrationType, after.RegistrationType);
            AddTextDetail(details, "Remarks", before.Remarks, after.Remarks);
            AddBoolDetail(details, "Privacy consent", before.PrivacyConsentGiven, after.PrivacyConsentGiven);
            AddDateDetail(details, "Privacy consent date", before.PrivacyConsentDate, after.PrivacyConsentDate);
            AddTextDetail(details, "Privacy consent method", before.PrivacyConsentMethod, after.PrivacyConsentMethod);
            AddTextDetail(details, "Privacy notice version", before.PrivacyNoticeVersion, after.PrivacyNoticeVersion);
            return details;
        }

        public static List<AuditLogDetail> AssessmentChangeDetails(YearlyAssessment before, YearlyAssessment after, bool isNew)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (after == null)
            {
                return details;
            }

            if (isNew || before == null)
            {
                details.Add(AuditService.Detail("Year", "", after.Year.ToString()));
                AddNewMoneyDetail(details, "Capital", after.Capital);
                AddNewMoneyDetail(details, "Gross sales", after.GrossSales);
                AddNewMoneyDetail(details, "Business tax", after.BusinessTax);
                AddNewMoneyDetail(details, "Mayor's permit", after.MayorsPermit);
                AddNewMoneyDetail(details, "Fees", after.Fees);
                AddNewMoneyDetail(details, "Surcharge", after.Surcharge);
                AddNewMoneyDetail(details, "Penalty", after.Penalty);
                AddNewTextDetail(details, "Remarks", after.Remarks);
                return details;
            }

            AddIntDetail(details, "Year", before.Year, after.Year);
            AddMoneyDetail(details, "Capital", before.Capital, after.Capital);
            AddMoneyDetail(details, "Gross sales", before.GrossSales, after.GrossSales);
            AddMoneyDetail(details, "Business tax", before.BusinessTax, after.BusinessTax);
            AddMoneyDetail(details, "Mayor's permit", before.MayorsPermit, after.MayorsPermit);
            AddMoneyDetail(details, "Fees", before.Fees, after.Fees);
            AddMoneyDetail(details, "Surcharge", before.Surcharge, after.Surcharge);
            AddMoneyDetail(details, "Penalty", before.Penalty, after.Penalty);
            AddTextDetail(details, "Remarks", before.Remarks, after.Remarks);
            return details;
        }

        public static List<AuditLogDetail> UserChangeDetails(UserAccount before, UserAccount after, bool isNew, bool passwordChanged)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (after == null)
            {
                return details;
            }

            if (isNew || before == null)
            {
                AddNewTextDetail(details, "Username", after.Username);
                AddNewTextDetail(details, "Full name", after.FullName);
                AddNewTextDetail(details, "Role", after.Role);
                details.Add(AuditService.Detail("Active", "", YesNo(after.IsActive)));
                if (passwordChanged)
                {
                    details.Add(AuditService.Detail("Password", "", "Initial password set"));
                }

                return details;
            }

            AddTextDetail(details, "Username", before.Username, after.Username);
            AddTextDetail(details, "Full name", before.FullName, after.FullName);
            AddTextDetail(details, "Role", before.Role, after.Role);
            AddBoolDetail(details, "Active", before.IsActive, after.IsActive);
            if (passwordChanged)
            {
                details.Add(AuditService.Detail("Password", "Existing password", "Changed"));
            }

            return details;
        }

        public static List<AuditLogDetail> SettingsChangeDetails(AppSettings before, AppSettings after)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (after == null)
            {
                return details;
            }

            if (before == null)
            {
                before = new AppSettings();
            }

            AddTextDetail(details, "Municipality", before.MunicipalityName, after.MunicipalityName);
            AddTextDetail(details, "Province", before.ProvinceName, after.ProvinceName);
            AddTextDetail(details, "Office", before.OfficeName, after.OfficeName);
            AddTextDetail(details, "Treasurer", before.TreasurerName, after.TreasurerName);
            AddTextDetail(details, "Collector", before.CollectorName, after.CollectorName);
            AddTextDetail(details, "Seal/logo path", before.SealImagePath, after.SealImagePath);
            AddTextDetail(details, "Treasurer signature path", before.TreasurerSignaturePath, after.TreasurerSignaturePath);
            AddTextDetail(details, "Collector signature path", before.CollectorSignaturePath, after.CollectorSignaturePath);
            AddTextDetail(details, "Report footer", before.ReportFooterNote, after.ReportFooterNote);
            AddIntDetail(details, "Default report year", before.DefaultReportYear, after.DefaultReportYear);
            AddTextDetail(details, "Backup folder", before.BackupFolderPath, after.BackupFolderPath);
            AddIntDetail(details, "Backup retention days", before.AutoBackupRetentionDays, after.AutoBackupRetentionDays);
            AddTextDetail(details, "Locked fiscal years", before.LockedFiscalYears, after.LockedFiscalYears);
            AddBoolDetail(details, "Dark mode", before.DarkModeEnabled, after.DarkModeEnabled);
            return details;
        }

        public static List<AuditLogDetail> FeeChangeDetails(FeeCatalogItem before, FeeCatalogItem after, bool isNew)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (after == null)
            {
                return details;
            }

            if (isNew || before == null)
            {
                AddNewTextDetail(details, "Code", after.Code);
                AddNewTextDetail(details, "Description", after.Description);
                AddNewMoneyDetail(details, "Amount", after.Amount);
                details.Add(AuditService.Detail("Active", "", YesNo(after.IsActive)));
                return details;
            }

            AddTextDetail(details, "Code", before.Code, after.Code);
            AddTextDetail(details, "Description", before.Description, after.Description);
            AddMoneyDetail(details, "Amount", before.Amount, after.Amount);
            AddBoolDetail(details, "Active", before.IsActive, after.IsActive);
            return details;
        }

        public static List<AuditLogDetail> PaymentCreateDetails(PaymentRecord payment)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (payment == null)
            {
                return details;
            }

            details.Add(AuditService.Detail("Date paid", "", payment.DatePaid.ToString("yyyy-MM-dd")));
            AddNewTextDetail(details, "OR number", payment.OrNumber);
            AddNewTextDetail(details, "Schedule", payment.Schedule);
            AddNewMoneyDetail(details, "Amount", payment.Amount);
            AddNewTextDetail(details, "Remarks", payment.Remarks);
            return details;
        }

        public static List<AuditLogDetail> PaymentDeleteDetails(PaymentRecord payment)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (payment == null)
            {
                return details;
            }

            details.Add(AuditService.Detail("Date paid", payment.DatePaid.ToString("yyyy-MM-dd"), ""));
            AddDeletedTextDetail(details, "OR number", payment.OrNumber);
            AddDeletedTextDetail(details, "Schedule", payment.Schedule);
            AddDeletedMoneyDetail(details, "Amount", payment.Amount);
            AddDeletedTextDetail(details, "Remarks", payment.Remarks);
            return details;
        }

        public static List<AuditLogDetail> OwnerDeleteDetails(BusinessOwner owner)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (owner == null)
            {
                return details;
            }

            AddDeletedTextDetail(details, "Owner name", owner.OwnerName);
            AddDeletedTextDetail(details, "Business name", owner.BusinessName);
            AddDeletedTextDetail(details, "Owner address", owner.OwnerAddress);
            AddDeletedTextDetail(details, "Business address", owner.BusinessAddress);
            AddDeletedTextDetail(details, "Contact no.", owner.ContactNumber);
            AddDeletedTextDetail(details, "Line of business", owner.LineOfBusiness);
            AddDeletedTextDetail(details, "TIN", owner.Tin);
            AddDeletedTextDetail(details, "Status", owner.Status);
            AddDeletedTextDetail(details, "Type", owner.RegistrationType);
            AddDeletedTextDetail(details, "Remarks", owner.Remarks);
            details.Add(AuditService.Detail("Privacy consent", YesNo(owner.PrivacyConsentGiven), ""));
            AddDeletedTextDetail(details, "Privacy consent method", owner.PrivacyConsentMethod);
            AddDeletedTextDetail(details, "Privacy notice version", owner.PrivacyNoticeVersion);
            return details;
        }

        public static List<AuditLogDetail> AssessmentDeleteDetails(YearlyAssessment assessment)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (assessment == null)
            {
                return details;
            }

            details.Add(AuditService.Detail("Year", assessment.Year.ToString(), ""));
            AddDeletedMoneyDetail(details, "Capital", assessment.Capital);
            AddDeletedMoneyDetail(details, "Gross sales", assessment.GrossSales);
            AddDeletedMoneyDetail(details, "Business tax", assessment.BusinessTax);
            AddDeletedMoneyDetail(details, "Mayor's permit", assessment.MayorsPermit);
            AddDeletedMoneyDetail(details, "Fees", assessment.Fees);
            AddDeletedMoneyDetail(details, "Surcharge", assessment.Surcharge);
            AddDeletedMoneyDetail(details, "Penalty", assessment.Penalty);
            AddDeletedTextDetail(details, "Remarks", assessment.Remarks);
            return details;
        }

        public static List<AuditLogDetail> FeeDeleteDetails(FeeCatalogItem fee)
        {
            List<AuditLogDetail> details = new List<AuditLogDetail>();
            if (fee == null)
            {
                return details;
            }

            AddDeletedTextDetail(details, "Code", fee.Code);
            AddDeletedTextDetail(details, "Description", fee.Description);
            AddDeletedMoneyDetail(details, "Amount", fee.Amount);
            details.Add(AuditService.Detail("Active", YesNo(fee.IsActive), ""));
            return details;
        }

        private static string WithPrefix(string prefix, List<string> changes)
        {
            if (changes == null || changes.Count == 0)
            {
                return ValueOrBlank(prefix) + " | No field changes";
            }

            return ValueOrBlank(prefix) + " | " + String.Join("; ", changes.ToArray());
        }

        private static void AddTextChange(List<string> changes, string label, string before, string after)
        {
            string oldValue = Normalize(before);
            string newValue = Normalize(after);
            if (!String.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                changes.Add(label + ": " + ValueOrBlank(oldValue) + " -> " + ValueOrBlank(newValue));
            }
        }

        private static void AddMoneyChange(List<string> changes, string label, decimal before, decimal after)
        {
            if (before != after)
            {
                changes.Add(label + ": " + Money(before) + " -> " + Money(after));
            }
        }

        private static void AddIntChange(List<string> changes, string label, int before, int after)
        {
            if (before != after)
            {
                changes.Add(label + ": " + before + " -> " + after);
            }
        }

        private static void AddBoolChange(List<string> changes, string label, bool before, bool after)
        {
            if (before != after)
            {
                changes.Add(label + ": " + YesNo(before) + " -> " + YesNo(after));
            }
        }

        private static void AddDateChange(List<string> changes, string label, DateTime before, DateTime after)
        {
            if (before.Date != after.Date)
            {
                changes.Add(label + ": " + DateText(before) + " -> " + DateText(after));
            }
        }

        private static void AddTextDetail(List<AuditLogDetail> details, string label, string before, string after)
        {
            string oldValue = Normalize(before);
            string newValue = Normalize(after);
            if (!String.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                details.Add(AuditService.Detail(label, ValueOrBlank(oldValue), ValueOrBlank(newValue)));
            }
        }

        private static void AddNewTextDetail(List<AuditLogDetail> details, string label, string value)
        {
            string normalized = Normalize(value);
            if (!String.IsNullOrEmpty(normalized))
            {
                details.Add(AuditService.Detail(label, "", ValueOrBlank(normalized)));
            }
        }

        private static void AddDeletedTextDetail(List<AuditLogDetail> details, string label, string value)
        {
            string normalized = Normalize(value);
            if (!String.IsNullOrEmpty(normalized))
            {
                details.Add(AuditService.Detail(label, ValueOrBlank(normalized), ""));
            }
        }

        private static void AddMoneyDetail(List<AuditLogDetail> details, string label, decimal before, decimal after)
        {
            if (before != after)
            {
                details.Add(AuditService.Detail(label, Money(before), Money(after)));
            }
        }

        private static void AddNewMoneyDetail(List<AuditLogDetail> details, string label, decimal value)
        {
            if (value != 0m)
            {
                details.Add(AuditService.Detail(label, "", Money(value)));
            }
        }

        private static void AddDeletedMoneyDetail(List<AuditLogDetail> details, string label, decimal value)
        {
            if (value != 0m)
            {
                details.Add(AuditService.Detail(label, Money(value), ""));
            }
        }

        private static void AddIntDetail(List<AuditLogDetail> details, string label, int before, int after)
        {
            if (before != after)
            {
                details.Add(AuditService.Detail(label, before.ToString(), after.ToString()));
            }
        }

        private static void AddBoolDetail(List<AuditLogDetail> details, string label, bool before, bool after)
        {
            if (before != after)
            {
                details.Add(AuditService.Detail(label, YesNo(before), YesNo(after)));
            }
        }

        private static void AddDateDetail(List<AuditLogDetail> details, string label, DateTime before, DateTime after)
        {
            if (before.Date != after.Date)
            {
                details.Add(AuditService.Detail(label, DateText(before), DateText(after)));
            }
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Trim();
        }

        private static string ValueOrBlank(string value)
        {
            value = Normalize(value);
            if (String.IsNullOrEmpty(value))
            {
                return "(blank)";
            }

            if (value.Length > 80)
            {
                return value.Substring(0, 77) + "...";
            }

            return value;
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2");
        }

        private static string DateText(DateTime value)
        {
            return value == DateTime.MinValue ? "(blank)" : value.ToString("yyyy-MM-dd");
        }

        private static string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }
    }
}
