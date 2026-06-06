using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class DelinquencyResult
    {
        public int ReviewedAssessments { get; set; }
        public int DelinquentAssessments { get; set; }
        public int ChangedOwners { get; set; }
    }

    public static class DelinquencyService
    {
        public static DelinquencyResult RunCheck(LedgerDatabase database, UserAccount currentUser)
        {
            return RunCheck(database, currentUser, DateTime.Today);
        }

        public static DelinquencyResult RunCheck(LedgerDatabase database, UserAccount currentUser, DateTime asOfDate)
        {
            DelinquencyResult result = new DelinquencyResult();
            if (database == null || database.Owners == null)
            {
                return result;
            }

            AuditService auditService = new AuditService(database, currentUser);
            foreach (BusinessOwner owner in database.Owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                List<string> reasons = new List<string>();
                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    result.ReviewedAssessments++;
                    string reason;
                    if (IsAssessmentDelinquent(assessment, asOfDate, out reason))
                    {
                        result.DelinquentAssessments++;
                        reasons.Add(reason);
                    }
                }

                if (reasons.Count == 0 || IsProtectedStatus(owner.Status))
                {
                    continue;
                }

                if (!String.Equals(owner.Status, "Delinquent", StringComparison.OrdinalIgnoreCase))
                {
                    string oldStatus = owner.Status;
                    owner.Status = "Delinquent";
                    result.ChangedOwners++;
                    auditService.Log(
                        "Auto Flag Delinquent",
                        "BusinessOwner",
                        owner.Id,
                        AuditService.OwnerName(owner) + " | Status: " + Blank(oldStatus) + " -> Delinquent; " + String.Join("; ", reasons.Take(3).ToArray()));
                }
            }

            return result;
        }

        public static bool OwnerHasDelinquency(BusinessOwner owner, int? year, DateTime asOfDate)
        {
            if (owner == null || owner.Assessments == null)
            {
                return false;
            }

            return owner.Assessments.Any(assessment =>
                (!year.HasValue || assessment.Year == year.Value) &&
                IsAssessmentDelinquent(assessment, asOfDate));
        }

        public static bool IsAssessmentDelinquent(YearlyAssessment assessment, DateTime asOfDate)
        {
            string reason;
            return IsAssessmentDelinquent(assessment, asOfDate, out reason);
        }

        public static bool IsAssessmentDelinquent(YearlyAssessment assessment, DateTime asOfDate, out string reason)
        {
            reason = "";
            if (assessment == null || assessment.TotalAssessment <= 0m || assessment.Balance <= 0m)
            {
                return false;
            }

            DelinquencyCheckpoint checkpoint = CurrentCheckpoint(assessment.Year, asOfDate);
            if (checkpoint == null)
            {
                return false;
            }

            decimal requiredPaid = Math.Round(assessment.TotalAssessment * checkpoint.RequiredFraction, 2);
            if (assessment.TotalPaid >= requiredPaid)
            {
                return false;
            }

            decimal overdueAmount = requiredPaid - assessment.TotalPaid;
            reason = "Year " + assessment.Year +
                " overdue after " + checkpoint.DueDate.ToString("yyyy-MM-dd") +
                " (" + checkpoint.Label + "), required paid " + requiredPaid.ToString("N2") +
                ", actual paid " + assessment.TotalPaid.ToString("N2") +
                ", overdue " + overdueAmount.ToString("N2");
            return true;
        }

        public static string AssessmentPaymentStatus(YearlyAssessment assessment, DateTime asOfDate)
        {
            if (IsAssessmentDelinquent(assessment, asOfDate))
            {
                return "Delinquent";
            }

            if (assessment != null && assessment.Balance > 0m)
            {
                return "With Balance";
            }

            return "Paid";
        }

        private static DelinquencyCheckpoint CurrentCheckpoint(int assessmentYear, DateTime asOfDate)
        {
            DateTime date = asOfDate.Date;
            DelinquencyCheckpoint[] checkpoints = new DelinquencyCheckpoint[]
            {
                new DelinquencyCheckpoint("1st Qtr", new DateTime(assessmentYear, 3, 31), 0.25m),
                new DelinquencyCheckpoint("2nd Qtr", new DateTime(assessmentYear, 6, 30), 0.50m),
                new DelinquencyCheckpoint("3rd Qtr", new DateTime(assessmentYear, 9, 30), 0.75m),
                new DelinquencyCheckpoint("4th Qtr", new DateTime(assessmentYear, 12, 31), 1.00m)
            };

            DelinquencyCheckpoint current = null;
            foreach (DelinquencyCheckpoint checkpoint in checkpoints)
            {
                if (date > checkpoint.DueDate)
                {
                    current = checkpoint;
                }
            }

            return current;
        }

        private static bool IsProtectedStatus(string status)
        {
            return String.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(status, "Transferred", StringComparison.OrdinalIgnoreCase);
        }

        private static string Blank(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "(blank)" : value.Trim();
        }

        private class DelinquencyCheckpoint
        {
            public string Label { get; private set; }
            public DateTime DueDate { get; private set; }
            public decimal RequiredFraction { get; private set; }

            public DelinquencyCheckpoint(string label, DateTime dueDate, decimal requiredFraction)
            {
                Label = label;
                DueDate = dueDate;
                RequiredFraction = requiredFraction;
            }
        }
    }
}
