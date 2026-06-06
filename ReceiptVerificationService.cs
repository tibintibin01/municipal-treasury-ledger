using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class ReceiptVerificationResult
    {
        public string Status { get; set; }
        public string OrNumber { get; set; }
        public DateTime DatePaid { get; set; }
        public decimal Amount { get; set; }
        public int AssessmentYear { get; set; }
        public string BusinessName { get; set; }
        public string OwnerName { get; set; }
        public string ExpectedCode { get; set; }
        public string ProvidedCode { get; set; }
        public string Details { get; set; }

        public ReceiptVerificationResult()
        {
            Status = "";
            OrNumber = "";
            BusinessName = "";
            OwnerName = "";
            ExpectedCode = "";
            ProvidedCode = "";
            Details = "";
        }
    }

    public static class ReceiptVerificationService
    {
        public static List<ReceiptVerificationResult> VerifyQrPayload(LedgerDatabase database, string payload)
        {
            List<ReceiptVerificationResult> results = new List<ReceiptVerificationResult>();
            payload = (payload ?? "").Trim();
            if (String.IsNullOrEmpty(payload))
            {
                results.Add(Message("Missing", "Enter or scan a QR verification payload."));
                return results;
            }

            string[] parts = payload.Split('|');
            if (parts.Length != 5 || !String.Equals(parts[0], "MTO", StringComparison.OrdinalIgnoreCase))
            {
                return VerifyManual(database, payload, "");
            }

            string orPrefix = parts[1].Trim();
            DateTime datePaid;
            if (!DateTime.TryParseExact(parts[2].Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out datePaid))
            {
                results.Add(Message("Invalid", "QR date is invalid."));
                return results;
            }

            decimal amount = 0m;
            long cents;
            if (!Int64.TryParse(parts[3].Trim(), out cents))
            {
                results.Add(Message("Invalid", "QR amount is invalid."));
                return results;
            }

            amount = cents / 100m;
            string providedCode = parts[4].Trim().ToUpperInvariant();
            List<ReceiptCandidate> candidates = Candidates(database)
                .Where(candidate =>
                    candidate.Payment.DatePaid.Date == datePaid.Date &&
                    Decimal.Round(candidate.Payment.Amount, 2) == Decimal.Round(amount, 2) &&
                    StartsWith(candidate.Payment.OrNumber, orPrefix))
                .ToList();

            if (candidates.Count == 0)
            {
                results.Add(Message("No Match", "No saved payment matched the QR OR/date/amount."));
                return results;
            }

            foreach (ReceiptCandidate candidate in candidates)
            {
                results.Add(BuildResult(candidate, providedCode));
            }

            return results;
        }

        public static List<ReceiptVerificationResult> VerifyManual(LedgerDatabase database, string orNumber, string verificationCode)
        {
            List<ReceiptVerificationResult> results = new List<ReceiptVerificationResult>();
            orNumber = (orNumber ?? "").Trim();
            verificationCode = (verificationCode ?? "").Trim().ToUpperInvariant();

            if (String.IsNullOrEmpty(orNumber))
            {
                results.Add(Message("Missing", "Enter an OR number or QR payload."));
                return results;
            }

            List<ReceiptCandidate> candidates = Candidates(database)
                .Where(candidate => String.Equals(candidate.Payment.OrNumber ?? "", orNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0)
            {
                results.Add(Message("No Match", "No saved payment matched this OR number."));
                return results;
            }

            foreach (ReceiptCandidate candidate in candidates)
            {
                results.Add(BuildResult(candidate, verificationCode));
            }

            return results;
        }

        private static ReceiptVerificationResult BuildResult(ReceiptCandidate candidate, string providedCode)
        {
            PrintableReportService.PaymentReceipt receipt = PrintableReportService.BuildPaymentReceipt(
                candidate.Owner,
                candidate.Assessment,
                candidate.Payment,
                null,
                null);
            string expectedCode = QrCodeService.VerificationCode(receipt);
            bool hasCode = !String.IsNullOrEmpty(providedCode);
            bool verified = hasCode && String.Equals(expectedCode, providedCode, StringComparison.OrdinalIgnoreCase);

            return new ReceiptVerificationResult
            {
                Status = hasCode ? (verified ? "Verified" : "Mismatch") : "Found",
                OrNumber = candidate.Payment.OrNumber,
                DatePaid = candidate.Payment.DatePaid,
                Amount = candidate.Payment.Amount,
                AssessmentYear = candidate.Assessment.Year,
                BusinessName = candidate.Owner.BusinessName,
                OwnerName = candidate.Owner.OwnerName,
                ExpectedCode = expectedCode,
                ProvidedCode = providedCode,
                Details = hasCode
                    ? (verified ? "Code matches saved ledger payment." : "Code does not match saved ledger payment.")
                    : "OR found. Enter verification code for full verification."
            };
        }

        private static ReceiptVerificationResult Message(string status, string details)
        {
            return new ReceiptVerificationResult
            {
                Status = status,
                Details = details
            };
        }

        private static IEnumerable<ReceiptCandidate> Candidates(LedgerDatabase database)
        {
            if (database == null || database.Owners == null)
            {
                yield break;
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
                        yield return new ReceiptCandidate(owner, assessment, payment);
                    }
                }
            }
        }

        private static bool StartsWith(string value, string prefix)
        {
            value = value ?? "";
            prefix = prefix ?? "";
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private class ReceiptCandidate
        {
            public BusinessOwner Owner { get; private set; }
            public YearlyAssessment Assessment { get; private set; }
            public PaymentRecord Payment { get; private set; }

            public ReceiptCandidate(BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment)
            {
                Owner = owner;
                Assessment = assessment;
                Payment = payment;
            }
        }
    }
}
