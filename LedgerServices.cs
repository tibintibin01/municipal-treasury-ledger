using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class OwnerService
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly AuditService auditService;

        public OwnerService(LedgerDatabase database, TreasuryDataStore dataStore, AuditService auditService)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.auditService = auditService;
        }

        public bool SaveOwner(BusinessOwner owner, out string message, out bool isNew)
        {
            return SaveOwner(owner, null, out message, out isNew);
        }

        public bool SaveOwner(BusinessOwner owner, BusinessOwner beforeOwner, out string message, out bool isNew)
        {
            message = "";
            isNew = false;

            if (owner == null)
            {
                message = "Owner record is missing.";
                return false;
            }

            EnsureOwners();
            NormalizeOwner(owner);

            if (String.IsNullOrEmpty(owner.OwnerName) && String.IsNullOrEmpty(owner.BusinessName))
            {
                message = "Enter at least the owner name or business name.";
                return false;
            }

            if (!LedgerValidation.IsValidContactNumber(owner.ContactNumber))
            {
                message = "Enter a valid contact number, or leave it blank.";
                return false;
            }

            if (!LedgerValidation.IsValidTin(owner.Tin))
            {
                message = "Enter TIN as 000-000-000 or 000-000-000-000, or leave it blank.";
                return false;
            }

            isNew = !database.Owners.Any(item => item.Id == owner.Id);
            if (isNew)
            {
                database.Owners.Add(owner);
            }

            string details = AuditChangeFormatter.OwnerDetails(beforeOwner, owner, isNew);
            auditService.Log(
                isNew ? "Create Owner" : "Update Owner",
                "BusinessOwner",
                owner.Id,
                details,
                AuditChangeFormatter.OwnerChangeDetails(beforeOwner, owner, isNew));
            dataStore.Save(database);
            return true;
        }

        public bool DeleteOwner(BusinessOwner owner, UserAccount currentUser, out string message)
        {
            message = "";
            if (!SecurityService.CanDelete(currentUser))
            {
                message = "Only Admin or Treasurer users can delete records.";
                return false;
            }

            EnsureOwners();

            if (owner == null || !database.Owners.Any(item => item.Id == owner.Id))
            {
                message = "Select a business owner first.";
                return false;
            }

            database.Owners.RemoveAll(item => item.Id == owner.Id);
            auditService.Log(
                "Delete Owner",
                "BusinessOwner",
                owner.Id,
                AuditService.OwnerName(owner),
                AuditChangeFormatter.OwnerDeleteDetails(owner));
            dataStore.Save(database);
            return true;
        }

        private void EnsureOwners()
        {
            if (database.Owners == null)
            {
                database.Owners = new List<BusinessOwner>();
            }
        }

        private void NormalizeOwner(BusinessOwner owner)
        {
            owner.OwnerName = (owner.OwnerName ?? "").Trim();
            owner.BusinessName = (owner.BusinessName ?? "").Trim();
            owner.OwnerAddress = (owner.OwnerAddress ?? "").Trim();
            owner.BusinessAddress = (owner.BusinessAddress ?? "").Trim();
            owner.ContactNumber = (owner.ContactNumber ?? "").Trim();
            owner.LineOfBusiness = (owner.LineOfBusiness ?? "").Trim();
            owner.Tin = (owner.Tin ?? "").Trim();
            owner.Status = String.IsNullOrWhiteSpace(owner.Status) ? "Active" : owner.Status.Trim();
            owner.RegistrationType = String.IsNullOrWhiteSpace(owner.RegistrationType) ? "Renewal" : owner.RegistrationType.Trim();
            owner.Remarks = (owner.Remarks ?? "").Trim();
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
        }
    }

    public class AssessmentService
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly AuditService auditService;

        public AssessmentService(LedgerDatabase database, TreasuryDataStore dataStore, AuditService auditService)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.auditService = auditService;
        }

        public bool SaveAssessment(BusinessOwner owner, YearlyAssessment assessment, out string message, out bool isNew)
        {
            return SaveAssessment(owner, assessment, null, out message, out isNew);
        }

        public bool SaveAssessment(BusinessOwner owner, YearlyAssessment assessment, YearlyAssessment beforeAssessment, out string message, out bool isNew)
        {
            message = "";
            isNew = false;

            if (owner == null)
            {
                message = "Save the business owner first.";
                return false;
            }

            if (assessment == null)
            {
                message = "Assessment record is missing.";
                return false;
            }

            if (owner.Assessments == null)
            {
                owner.Assessments = new List<YearlyAssessment>();
            }

            if (!LedgerValidation.IsValidAssessmentYear(assessment.Year))
            {
                message = "Enter a valid assessment year from 1900 up to next year.";
                return false;
            }

            if (HasNegativeAmount(assessment))
            {
                message = "Assessment amounts cannot be negative.";
                return false;
            }

            YearlyAssessment sameYear = owner.Assessments.FirstOrDefault(item => item.Year == assessment.Year && item.Id != assessment.Id);
            if (sameYear != null)
            {
                message = "This business already has an assessment for that year.";
                return false;
            }

            isNew = !owner.Assessments.Any(item => item.Id == assessment.Id);
            if (isNew)
            {
                owner.Assessments.Add(assessment);
            }

            string details = AuditChangeFormatter.AssessmentDetails(owner, beforeAssessment, assessment, isNew);
            auditService.Log(
                isNew ? "Create Assessment" : "Update Assessment",
                "YearlyAssessment",
                assessment.Id,
                details,
                AuditChangeFormatter.AssessmentChangeDetails(beforeAssessment, assessment, isNew));
            dataStore.Save(database);
            return true;
        }

        public bool DeleteAssessment(LedgerDatabase database, BusinessOwner owner, YearlyAssessment assessment, UserAccount currentUser, out string message)
        {
            message = "";
            if (!SecurityService.CanDelete(currentUser))
            {
                message = "Only Admin or Treasurer users can delete records.";
                return false;
            }

            if (owner == null || assessment == null)
            {
                message = "Select an assessment first.";
                return false;
            }

            if (owner.Assessments == null)
            {
                message = "Selected assessment was not found.";
                return false;
            }

            owner.Assessments.RemoveAll(item => item.Id == assessment.Id);
            auditService.Log(
                "Delete Assessment",
                "YearlyAssessment",
                assessment.Id,
                "Year " + assessment.Year + " - " + AuditService.OwnerName(owner),
                AuditChangeFormatter.AssessmentDeleteDetails(assessment));
            dataStore.Save(database);
            return true;
        }

        private bool HasNegativeAmount(YearlyAssessment assessment)
        {
            return assessment.Capital < 0m ||
                assessment.GrossSales < 0m ||
                assessment.BusinessTax < 0m ||
                assessment.MayorsPermit < 0m ||
                assessment.Fees < 0m ||
                assessment.Surcharge < 0m ||
                assessment.Penalty < 0m;
        }

    }

    public class PaymentService
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly AuditService auditService;

        public PaymentService(LedgerDatabase database, TreasuryDataStore dataStore, AuditService auditService)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.auditService = auditService;
        }

        public bool AddPayment(BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment, out string message)
        {
            message = "";
            if (owner == null || assessment == null)
            {
                message = "Select a business owner and assessment year first.";
                return false;
            }

            if (payment == null)
            {
                message = "Payment record is missing.";
                return false;
            }

            if (assessment.Payments == null)
            {
                assessment.Payments = new List<PaymentRecord>();
            }

            if (IsFiscalYearLocked(payment.DatePaid.Year))
            {
                message = "Fiscal year " + payment.DatePaid.Year + " is locked. Payments cannot be added or edited for closed accounting periods.";
                return false;
            }

            if (!LedgerValidation.ValidatePayment(database, assessment, payment.OrNumber, payment.Amount, payment.DatePaid, null, out message))
            {
                return false;
            }

            assessment.Payments.Add(payment);
            auditService.Log(
                "Add Payment",
                "PaymentRecord",
                payment.Id,
                "OR " + payment.OrNumber + " - " + payment.Amount.ToString("N2") + " - " + AuditService.OwnerName(owner),
                AuditChangeFormatter.PaymentCreateDetails(payment));
            dataStore.Save(database);
            return true;
        }

        public bool DeletePayment(BusinessOwner owner, YearlyAssessment assessment, string paymentId, UserAccount currentUser, out string message)
        {
            message = "";
            if (!SecurityService.CanDelete(currentUser))
            {
                message = "Only Admin or Treasurer users can delete records.";
                return false;
            }

            if (assessment == null || String.IsNullOrEmpty(paymentId))
            {
                message = "Select a payment first.";
                return false;
            }

            if (assessment.Payments == null)
            {
                message = "Selected payment was not found.";
                return false;
            }

            PaymentRecord deletedPayment = assessment.Payments.FirstOrDefault(item => item.Id == paymentId);
            if (deletedPayment == null)
            {
                message = "Selected payment was not found.";
                return false;
            }

            if (IsFiscalYearLocked(deletedPayment.DatePaid.Year))
            {
                message = "Fiscal year " + deletedPayment.DatePaid.Year + " is locked. Payments cannot be deleted for closed accounting periods.";
                return false;
            }

            string details = "OR " + (deletedPayment.OrNumber ?? "") + " - " + deletedPayment.Amount.ToString("N2");
            assessment.Payments.RemoveAll(item => item.Id == paymentId);
            auditService.Log(
                "Delete Payment",
                "PaymentRecord",
                paymentId,
                details + " - " + AuditService.OwnerName(owner),
                AuditChangeFormatter.PaymentDeleteDetails(deletedPayment));
            dataStore.Save(database);
            return true;
        }

        private bool IsFiscalYearLocked(int year)
        {
            if (database == null || database.Settings == null || String.IsNullOrWhiteSpace(database.Settings.LockedFiscalYears))
            {
                return false;
            }

            string[] parts = database.Settings.LockedFiscalYears.Split(new char[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int lockedYear;
                if (Int32.TryParse(part.Trim(), out lockedYear) && lockedYear == year)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class UserAccountService
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly AuditService auditService;

        public UserAccountService(LedgerDatabase database, TreasuryDataStore dataStore, AuditService auditService)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.auditService = auditService;
        }

        public bool SaveUser(UserAccount selectedUser, UserAccount currentUser, string username, string fullName, string role, bool isActive, string password, out UserAccount savedUser, out string message, out bool isNew)
        {
            savedUser = null;
            message = "";
            bool creatingUser = selectedUser == null;
            isNew = creatingUser;

            if (!SecurityService.IsAdmin(currentUser))
            {
                message = "Only Admin users can manage accounts.";
                return false;
            }

            if (database.Users == null)
            {
                database.Users = new List<UserAccount>();
            }

            username = (username ?? "").Trim();
            fullName = (fullName ?? "").Trim();
            role = String.IsNullOrWhiteSpace(role) ? SecurityService.CashierRole : role.Trim();

            if (String.IsNullOrEmpty(username))
            {
                message = "Enter a username.";
                return false;
            }

            UserAccount duplicate = database.Users.FirstOrDefault(user =>
                String.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase) &&
                (creatingUser || user.Id != selectedUser.Id));

            if (duplicate != null)
            {
                message = "That username already exists.";
                return false;
            }

            if (creatingUser && String.IsNullOrEmpty(password))
            {
                message = "Enter an initial password for the new user.";
                return false;
            }

            if (!String.IsNullOrEmpty(password))
            {
                string passwordMessage;
                if (!LedgerValidation.ValidatePasswordStrength(password, out passwordMessage))
                {
                    message = passwordMessage;
                    return false;
                }
            }

            if (selectedUser != null &&
                currentUser != null &&
                selectedUser.Id == currentUser.Id &&
                !isActive)
            {
                message = "You cannot deactivate your own account while signed in.";
                return false;
            }

            UserAccount userToSave = selectedUser ?? new UserAccount();
            UserAccount beforeUser = AuditChangeFormatter.CloneUser(selectedUser);
            bool passwordChanged = !String.IsNullOrEmpty(password);
            userToSave.Username = username;
            userToSave.FullName = fullName;
            userToSave.Role = role;
            userToSave.IsActive = isActive;

            if (passwordChanged)
            {
                SecurityService.SetPassword(userToSave, password);
            }

            if (creatingUser)
            {
                database.Users.Add(userToSave);
            }

            string details = AuditChangeFormatter.UserDetails(beforeUser, userToSave, creatingUser, passwordChanged);
            auditService.Log(
                creatingUser ? "Create User" : "Update User",
                "UserAccount",
                userToSave.Id,
                details,
                AuditChangeFormatter.UserChangeDetails(beforeUser, userToSave, creatingUser, passwordChanged));
            dataStore.Save(database);
            savedUser = userToSave;
            return true;
        }
    }

    public class FeeCatalogService
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly AuditService auditService;

        public FeeCatalogService(LedgerDatabase database, TreasuryDataStore dataStore, AuditService auditService)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.auditService = auditService;
        }

        public bool SaveFee(FeeCatalogItem selectedFee, UserAccount currentUser, string code, string description, decimal amount, bool isActive, out FeeCatalogItem savedFee, out string message, out bool isNew)
        {
            savedFee = null;
            message = "";
            isNew = selectedFee == null;

            if (!SecurityService.IsAdmin(currentUser) && !SecurityService.IsTreasurer(currentUser))
            {
                message = "Only Admin or Treasurer users can manage fees.";
                return false;
            }

            EnsureFeeCatalog();

            code = (code ?? "").Trim();
            description = (description ?? "").Trim();
            if (String.IsNullOrEmpty(code) && String.IsNullOrEmpty(description))
            {
                message = "Enter a fee code or description.";
                return false;
            }

            if (amount < 0m)
            {
                message = "Fee amount cannot be negative.";
                return false;
            }

            if (!String.IsNullOrEmpty(code))
            {
                FeeCatalogItem duplicate = database.FeeCatalog.FirstOrDefault(item =>
                    String.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase) &&
                    (selectedFee == null || item.Id != selectedFee.Id));
                if (duplicate != null)
                {
                    message = "That fee code already exists.";
                    return false;
                }
            }

            FeeCatalogItem feeToSave = selectedFee ?? new FeeCatalogItem();
            FeeCatalogItem beforeFee = AuditChangeFormatter.CloneFee(selectedFee);
            string beforeDetails = FeeDetails(feeToSave);
            feeToSave.Code = code;
            feeToSave.Description = description;
            feeToSave.Amount = amount;
            feeToSave.IsActive = isActive;

            if (isNew)
            {
                database.FeeCatalog.Add(feeToSave);
            }

            string details = isNew
                ? FeeDetails(feeToSave)
                : "Before: " + beforeDetails + " | After: " + FeeDetails(feeToSave);
            auditService.Log(
                isNew ? "Create Fee" : "Update Fee",
                "FeeCatalogItem",
                feeToSave.Id,
                details,
                AuditChangeFormatter.FeeChangeDetails(beforeFee, feeToSave, isNew));
            dataStore.Save(database);
            savedFee = feeToSave;
            return true;
        }

        public bool DeleteFee(FeeCatalogItem fee, UserAccount currentUser, out string message)
        {
            message = "";
            if (!SecurityService.IsAdmin(currentUser) && !SecurityService.IsTreasurer(currentUser))
            {
                message = "Only Admin or Treasurer users can manage fees.";
                return false;
            }

            EnsureFeeCatalog();
            if (fee == null || !database.FeeCatalog.Any(item => item.Id == fee.Id))
            {
                message = "Select a fee item first.";
                return false;
            }

            database.FeeCatalog.RemoveAll(item => item.Id == fee.Id);
            auditService.Log(
                "Delete Fee",
                "FeeCatalogItem",
                fee.Id,
                FeeDetails(fee),
                AuditChangeFormatter.FeeDeleteDetails(fee));
            dataStore.Save(database);
            return true;
        }

        private void EnsureFeeCatalog()
        {
            if (database.FeeCatalog == null)
            {
                database.FeeCatalog = new List<FeeCatalogItem>();
            }
        }

        private string FeeDetails(FeeCatalogItem fee)
        {
            if (fee == null)
            {
                return "";
            }

            return (fee.Code ?? "") + " - " + (fee.Description ?? "") + " - " + fee.Amount.ToString("N2") + " - " + (fee.IsActive ? "Active" : "Inactive");
        }
    }
}
