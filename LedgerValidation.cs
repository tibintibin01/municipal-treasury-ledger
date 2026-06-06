using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MunicipalTreasuryLedger
{
    public static class LedgerValidation
    {
        private static readonly Regex TinPattern = new Regex(@"^\d{3}-\d{3}-\d{3}(-\d{3})?$", RegexOptions.Compiled);
        private static readonly Regex ContactPattern = new Regex(@"^[0-9+\-\s().]+$", RegexOptions.Compiled);

        public static bool IsValidTin(string tin)
        {
            if (String.IsNullOrWhiteSpace(tin))
            {
                return true;
            }

            return TinPattern.IsMatch(tin.Trim());
        }

        public static bool IsValidContactNumber(string contactNumber)
        {
            if (String.IsNullOrWhiteSpace(contactNumber))
            {
                return true;
            }

            string value = contactNumber.Trim();
            if (!ContactPattern.IsMatch(value))
            {
                return false;
            }

            int digitCount = value.Count(Char.IsDigit);
            return digitCount >= 7 && digitCount <= 15;
        }

        public static bool IsValidAssessmentYear(int year)
        {
            return year >= 1900 && year <= DateTime.Today.Year + 1;
        }

        public static bool IsValidPaymentDate(DateTime datePaid)
        {
            return datePaid.Date <= DateTime.Today;
        }

        public static bool TryParseMoney(string text, out decimal value)
        {
            value = 0m;
            if (String.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (Decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out value))
            {
                return true;
            }

            return Decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public static bool ValidatePayment(
            LedgerDatabase database,
            YearlyAssessment assessment,
            string orNumber,
            decimal amount,
            DateTime datePaid,
            string ignorePaymentId,
            out string message)
        {
            message = "";

            if (assessment == null)
            {
                message = "Select an assessment year first.";
                return false;
            }

            if (amount <= 0m)
            {
                message = "Payment amount must be greater than zero.";
                return false;
            }

            if (String.IsNullOrWhiteSpace(orNumber))
            {
                message = "OR number is required.";
                return false;
            }

            if (!IsValidPaymentDate(datePaid))
            {
                message = "Payment date cannot be in the future.";
                return false;
            }

            if (OrNumberExists(database, orNumber.Trim(), ignorePaymentId))
            {
                message = "This OR number is already recorded.";
                return false;
            }

            if (amount > assessment.Balance)
            {
                message = "Payment amount cannot be greater than the current balance.";
                return false;
            }

            return true;
        }

        public static bool ValidatePasswordStrength(string password, out string message)
        {
            message = "";
            if (String.IsNullOrEmpty(password) || password.Length < 8)
            {
                message = "Password must be at least 8 characters.";
                return false;
            }

            bool hasLetter = password.Any(Char.IsLetter);
            bool hasDigit = password.Any(Char.IsDigit);
            if (!hasLetter || !hasDigit)
            {
                message = "Password must contain both letters and numbers.";
                return false;
            }

            return true;
        }

        public static bool OrNumberExists(LedgerDatabase database, string orNumber, string ignorePaymentId)
        {
            if (database == null || database.Owners == null || String.IsNullOrWhiteSpace(orNumber))
            {
                return false;
            }

            foreach (BusinessOwner owner in database.Owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (assessment.Payments == null)
                    {
                        continue;
                    }

                    foreach (PaymentRecord payment in assessment.Payments)
                    {
                        if (!String.IsNullOrEmpty(ignorePaymentId) && payment.Id == ignorePaymentId)
                        {
                            continue;
                        }

                        if (String.Equals(payment.OrNumber, orNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
