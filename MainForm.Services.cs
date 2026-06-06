using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm
    {

        private AuditService CreateAuditService()
        {
            return new AuditService(database, currentUser);
        }

        private OwnerService CreateOwnerService()
        {
            return new OwnerService(database, dataStore, CreateAuditService());
        }

        private AssessmentService CreateAssessmentService()
        {
            return new AssessmentService(database, dataStore, CreateAuditService());
        }

        private PaymentService CreatePaymentService()
        {
            return new PaymentService(database, dataStore, CreateAuditService());
        }

        private UserAccountService CreateUserAccountService()
        {
            return new UserAccountService(database, dataStore, CreateAuditService());
        }

        private FeeCatalogService CreateFeeCatalogService()
        {
            return new FeeCatalogService(database, dataStore, CreateAuditService());
        }

        private void FocusOwnerValidation(string message)
        {
            string value = (message ?? "").ToLowerInvariant();
            if (value.Contains("contact"))
            {
                ShowValidation(contactNumberText, message);
                return;
            }

            if (value.Contains("tin"))
            {
                ShowValidation(tinText, message);
                return;
            }

            ShowValidation(ownerNameText, message);
        }

        private void FocusPaymentValidation(string message)
        {
            string value = (message ?? "").ToLowerInvariant();
            if (value.Contains("or"))
            {
                ShowValidation(orNumberText, message);
                return;
            }

            if (value.Contains("date"))
            {
                ShowValidation(paymentDatePicker, message);
                return;
            }

            ShowValidation(paymentAmountText, message);
        }

        private void FocusUserValidation(string message)
        {
            string value = (message ?? "").ToLowerInvariant();
            if (value.Contains("password"))
            {
                ShowValidation(userPasswordText, message);
                return;
            }

            if (value.Contains("username"))
            {
                ShowValidation(userUsernameText, message);
                return;
            }

            ShowValidation(userUsernameText, message);
        }

        private void LogAction(string action, string entityType, string entityId, string details)
        {
            CreateAuditService().Log(action, entityType, entityId, details);
        }

        private void LogAction(string action, string entityType, string entityId, string details, IEnumerable<AuditLogDetail> changeDetails)
        {
            CreateAuditService().Log(action, entityType, entityId, details, changeDetails);
        }
    }
}
