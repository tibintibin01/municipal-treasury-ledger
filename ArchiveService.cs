using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalTreasuryLedger
{
    public class ArchivePurgeResult
    {
        public int AssessmentsArchived { get; set; }
        public int PaymentsArchived { get; set; }
        public int AssessmentsPurged { get; set; }
        public int PaymentsPurged { get; set; }

        public string Summary
        {
            get
            {
                return "Assessments archived: " + AssessmentsArchived.ToString("N0") +
                    " | Payments archived: " + PaymentsArchived.ToString("N0") +
                    " | Assessments purged: " + AssessmentsPurged.ToString("N0") +
                    " | Payments purged: " + PaymentsPurged.ToString("N0");
            }
        }
    }

    public static class ArchiveService
    {
        public static ArchivePurgeResult Preview(LedgerDatabase database, int throughYear)
        {
            ArchivePurgeResult result = new ArchivePurgeResult();
            foreach (YearlyAssessment assessment in MatchingAssessments(database, throughYear))
            {
                result.AssessmentsArchived++;
                result.PaymentsArchived += assessment.Payments == null ? 0 : assessment.Payments.Count;
            }

            return result;
        }

        public static ArchivePurgeResult Purge(LedgerDatabase database, int throughYear)
        {
            ArchivePurgeResult result = Preview(database, throughYear);
            if (database == null || database.Owners == null)
            {
                return result;
            }

            foreach (BusinessOwner owner in database.Owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                List<YearlyAssessment> purged = owner.Assessments
                    .Where(assessment => assessment.Year <= throughYear)
                    .ToList();

                result.AssessmentsPurged += purged.Count;
                result.PaymentsPurged += purged.Sum(assessment => assessment.Payments == null ? 0 : assessment.Payments.Count);
                owner.Assessments.RemoveAll(assessment => assessment.Year <= throughYear);
            }

            return result;
        }

        private static IEnumerable<YearlyAssessment> MatchingAssessments(LedgerDatabase database, int throughYear)
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
                    if (assessment.Year <= throughYear)
                    {
                        yield return assessment;
                    }
                }
            }
        }
    }
}
