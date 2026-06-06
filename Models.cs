using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class LedgerDatabase
    {
        public List<BusinessOwner> Owners { get; set; }
        public List<UserAccount> Users { get; set; }
        public List<AuditLogEntry> AuditTrail { get; set; }
        public List<FeeCatalogItem> FeeCatalog { get; set; }
        public AppSettings Settings { get; set; }
        public string AuditChainTipHash { get; set; }

        public LedgerDatabase()
        {
            Owners = new List<BusinessOwner>();
            Users = new List<UserAccount>();
            AuditTrail = new List<AuditLogEntry>();
            FeeCatalog = new List<FeeCatalogItem>();
            Settings = new AppSettings();
            AuditChainTipHash = "";
        }
    }

    public class FeeCatalogItem
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }

        public FeeCatalogItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Code = "";
            Description = "";
            IsActive = true;
            DateCreated = DateTime.Today;
        }

        public string DisplayName
        {
            get
            {
                string name = String.IsNullOrEmpty(Code) ? Description : Code + " - " + Description;
                if (String.IsNullOrWhiteSpace(name))
                {
                    name = "Fee item";
                }

                return name.Trim() + " (" + Amount.ToString("N2") + ")";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class AppSettings
    {
        public string MunicipalityName { get; set; }
        public string ProvinceName { get; set; }
        public string OfficeName { get; set; }
        public string TreasurerName { get; set; }
        public string CollectorName { get; set; }
        public string SealImagePath { get; set; }
        public string TreasurerSignaturePath { get; set; }
        public string CollectorSignaturePath { get; set; }
        public string ReportFooterNote { get; set; }
        public int DefaultReportYear { get; set; }
        public string BackupFolderPath { get; set; }
        public DateTime LastAutoBackupDate { get; set; }
        public int AutoBackupRetentionDays { get; set; }
        public string LockedFiscalYears { get; set; }
        public bool DarkModeEnabled { get; set; }

        public AppSettings()
        {
            MunicipalityName = "";
            ProvinceName = "";
            OfficeName = "Municipal Treasurer's Office";
            TreasurerName = "";
            CollectorName = "";
            SealImagePath = "";
            TreasurerSignaturePath = "";
            CollectorSignaturePath = "";
            ReportFooterNote = "";
            DefaultReportYear = DateTime.Today.Year;
            BackupFolderPath = "";
            LastAutoBackupDate = DateTime.MinValue;
            AutoBackupRetentionDays = 30;
            LockedFiscalYears = "";
            DarkModeEnabled = false;
        }
    }

    public class UserAccount
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string PasswordAlgorithm { get; set; }
        public string PasswordSalt { get; set; }
        public string PasswordHash { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime LockoutUntil { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }

        public UserAccount()
        {
            Id = Guid.NewGuid().ToString("N");
            Role = "Cashier";
            PasswordAlgorithm = "";
            IsActive = true;
            DateCreated = DateTime.Today;
        }
    }

    public class AuditLogEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Details { get; set; }
        public string PreviousHash { get; set; }
        public string EntryHash { get; set; }
        public List<AuditLogDetail> ChangeDetails { get; set; }

        public AuditLogEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now;
            PreviousHash = "";
            EntryHash = "";
            ChangeDetails = new List<AuditLogDetail>();
        }
    }

    public class AuditLogDetail
    {
        public string Id { get; set; }
        public string AuditLogId { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }

        public AuditLogDetail()
        {
            Id = Guid.NewGuid().ToString("N");
            FieldName = "";
            OldValue = "";
            NewValue = "";
        }
    }

    public class BusinessOwner
    {
        public string Id { get; set; }
        public string OwnerName { get; set; }
        public string BusinessName { get; set; }
        public string OwnerAddress { get; set; }
        public string BusinessAddress { get; set; }
        public string ContactNumber { get; set; }
        public string LineOfBusiness { get; set; }
        public string Tin { get; set; }
        public string Status { get; set; }
        public string RegistrationType { get; set; }
        public string Remarks { get; set; }
        public bool PrivacyConsentGiven { get; set; }
        public DateTime PrivacyConsentDate { get; set; }
        public string PrivacyConsentMethod { get; set; }
        public string PrivacyNoticeVersion { get; set; }
        public DateTime DateCreated { get; set; }
        public List<YearlyAssessment> Assessments { get; set; }

        public BusinessOwner()
        {
            Id = Guid.NewGuid().ToString("N");
            Status = "Active";
            RegistrationType = "Renewal";
            PrivacyConsentGiven = false;
            PrivacyConsentDate = DateTime.Today;
            PrivacyConsentMethod = "";
            PrivacyNoticeVersion = "RA10173-v1";
            DateCreated = DateTime.Today;
            Assessments = new List<YearlyAssessment>();
        }
    }

    public class YearlyAssessment
    {
        public string Id { get; set; }
        public int Year { get; set; }
        public decimal Capital { get; set; }
        public decimal GrossSales { get; set; }
        public decimal BusinessTax { get; set; }
        public decimal MayorsPermit { get; set; }
        public decimal Fees { get; set; }
        public decimal Surcharge { get; set; }
        public decimal Penalty { get; set; }
        public string Remarks { get; set; }
        public DateTime DateCreated { get; set; }
        public List<PaymentRecord> Payments { get; set; }

        public YearlyAssessment()
        {
            Id = Guid.NewGuid().ToString("N");
            Year = DateTime.Today.Year;
            DateCreated = DateTime.Today;
            Payments = new List<PaymentRecord>();
        }

        public decimal TotalAssessment
        {
            get
            {
                return BusinessTax + MayorsPermit + Fees + Surcharge + Penalty;
            }
        }

        public decimal TotalPaid
        {
            get
            {
                if (Payments == null)
                {
                    return 0m;
                }

                return Payments.Sum(payment => payment.Amount);
            }
        }

        public decimal Balance
        {
            get
            {
                return TotalAssessment - TotalPaid;
            }
        }
    }

    public class PaymentRecord
    {
        public string Id { get; set; }
        public DateTime DatePaid { get; set; }
        public string OrNumber { get; set; }
        public string Schedule { get; set; }
        public decimal Amount { get; set; }
        public string Remarks { get; set; }

        public PaymentRecord()
        {
            Id = Guid.NewGuid().ToString("N");
            DatePaid = DateTime.Today;
            Schedule = "1st Qtr";
        }
    }
}
