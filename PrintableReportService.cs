using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public static class PrintableReportService
    {
        public static CollectionSummaryReport BuildCollectionSummary(LedgerDatabase database, int? year)
        {
            CollectionSummaryReport report = new CollectionSummaryReport();
            report.Year = year;

            List<BusinessOwner> owners = database == null || database.Owners == null
                ? new List<BusinessOwner>()
                : database.Owners;

            report.BusinessCount = owners.Count;
            report.ActiveCount = owners.Count(owner => String.Equals(Safe(owner.Status), "Active", StringComparison.OrdinalIgnoreCase));
            report.ClosedCount = owners.Count(owner => String.Equals(Safe(owner.Status), "Closed", StringComparison.OrdinalIgnoreCase));
            report.DelinquentCount = owners.Count(owner => DelinquencyService.OwnerHasDelinquency(owner, year, DateTime.Today));

            Dictionary<string, CollectionScheduleRow> schedules = new Dictionary<string, CollectionScheduleRow>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, CollectionLineRow> lines = new Dictionary<string, CollectionLineRow>(StringComparer.OrdinalIgnoreCase);
            List<CollectionBalanceRow> balances = new List<CollectionBalanceRow>();

            foreach (BusinessOwner owner in owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (year.HasValue && assessment.Year != year.Value)
                    {
                        continue;
                    }

                    report.AssessmentTotal += assessment.TotalAssessment;
                    report.PaidTotal += assessment.TotalPaid;
                    report.BalanceTotal += assessment.Balance;

                    string line = String.IsNullOrEmpty(Safe(owner.LineOfBusiness)) ? "(blank)" : Safe(owner.LineOfBusiness);
                    if (!lines.ContainsKey(line))
                    {
                        lines[line] = new CollectionLineRow { LineOfBusiness = line };
                    }

                    lines[line].Assessment += assessment.TotalAssessment;
                    lines[line].Paid += assessment.TotalPaid;
                    lines[line].Balance += assessment.Balance;

                    if (assessment.Balance > 0m)
                    {
                        balances.Add(new CollectionBalanceRow
                        {
                            Year = assessment.Year,
                            BusinessName = Safe(owner.BusinessName),
                            OwnerName = Safe(owner.OwnerName),
                            Balance = assessment.Balance,
                            Status = DelinquencyService.AssessmentPaymentStatus(assessment, DateTime.Today)
                        });
                    }

                    if (assessment.Payments == null)
                    {
                        continue;
                    }

                    foreach (PaymentRecord payment in assessment.Payments)
                    {
                        string schedule = String.IsNullOrEmpty(Safe(payment.Schedule)) ? "(blank)" : Safe(payment.Schedule);
                        if (!schedules.ContainsKey(schedule))
                        {
                            schedules[schedule] = new CollectionScheduleRow { Schedule = schedule };
                        }

                        schedules[schedule].PaymentCount++;
                        schedules[schedule].Amount += payment.Amount;
                    }
                }
            }

            report.ScheduleRows = schedules.Values.OrderBy(row => ScheduleSort(row.Schedule)).ThenBy(row => row.Schedule).ToList();
            report.LineRows = lines.Values.OrderByDescending(row => row.Paid).ThenByDescending(row => row.Assessment).Take(20).ToList();
            report.TopBalanceRows = balances.OrderByDescending(row => row.Balance).Take(20).ToList();
            return report;
        }

        public static DelinquentListReport BuildDelinquentList(LedgerDatabase database, int? year)
        {
            DelinquentListReport report = new DelinquentListReport();
            report.Year = year;

            List<BusinessOwner> owners = database == null || database.Owners == null
                ? new List<BusinessOwner>()
                : database.Owners;

            foreach (BusinessOwner owner in owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (year.HasValue && assessment.Year != year.Value)
                    {
                        continue;
                    }

                    string reason;
                    if (!DelinquencyService.IsAssessmentDelinquent(assessment, DateTime.Today, out reason))
                    {
                        continue;
                    }

                    DelinquentReportRow row = BuildDelinquentRow(owner, assessment, reason);
                    report.Rows.Add(row);

                    report.AssessmentTotal += row.TotalAssessment;
                    report.PaidTotal += row.Paid;
                    report.BalanceTotal += row.Balance;
                }
            }

            report.Rows = report.Rows
                .OrderByDescending(row => row.Balance)
                .ThenBy(row => row.BusinessName)
                .ToList();
            return report;
        }

        public static DelinquencyNoticeBatch BuildDelinquencyNotices(LedgerDatabase database, int? year)
        {
            DelinquencyNoticeBatch batch = new DelinquencyNoticeBatch();
            batch.Year = year;
            batch.NoticeDate = DateTime.Today;

            List<BusinessOwner> owners = database == null || database.Owners == null
                ? new List<BusinessOwner>()
                : database.Owners;

            foreach (BusinessOwner owner in owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (year.HasValue && assessment.Year != year.Value)
                    {
                        continue;
                    }

                    string reason;
                    if (!DelinquencyService.IsAssessmentDelinquent(assessment, DateTime.Today, out reason))
                    {
                        continue;
                    }

                    DelinquencyNotice notice = new DelinquencyNotice();
                    notice.Year = assessment.Year;
                    notice.OwnerName = Safe(owner.OwnerName);
                    notice.BusinessName = Safe(owner.BusinessName);
                    notice.OwnerAddress = Safe(owner.OwnerAddress);
                    notice.BusinessAddress = Safe(owner.BusinessAddress);
                    notice.LineOfBusiness = Safe(owner.LineOfBusiness);
                    notice.TotalAssessment = assessment.TotalAssessment;
                    notice.Paid = assessment.TotalPaid;
                    notice.Balance = assessment.Balance;
                    notice.Reason = reason;
                    batch.Notices.Add(notice);

                    batch.AssessmentTotal += notice.TotalAssessment;
                    batch.PaidTotal += notice.Paid;
                    batch.BalanceTotal += notice.Balance;
                }
            }

            batch.Notices = batch.Notices
                .OrderByDescending(row => row.Balance)
                .ThenBy(row => row.BusinessName)
                .ThenBy(row => row.OwnerName)
                .ToList();
            return batch;
        }

        private static DelinquentReportRow BuildDelinquentRow(BusinessOwner owner, YearlyAssessment assessment, string reason)
        {
            DelinquentReportRow row = new DelinquentReportRow();
            row.Year = assessment.Year;
            row.BusinessName = Safe(owner.BusinessName);
            row.OwnerName = Safe(owner.OwnerName);
            row.LineOfBusiness = Safe(owner.LineOfBusiness);
            row.TotalAssessment = assessment.TotalAssessment;
            row.Paid = assessment.TotalPaid;
            row.Balance = assessment.Balance;
            row.Reason = reason;
            return row;
        }

        public static void PreviewCollectionSummary(IWin32Window owner, LedgerDatabase database, int? year, UserAccount currentUser)
        {
            CollectionSummaryReport report = BuildCollectionSummary(database, year);
            PreviewReport(owner, BuildCollectionSummaryPrintReport(report, currentUser, database == null ? null : database.Settings), true);
        }

        public static void PreviewDelinquentList(IWin32Window owner, LedgerDatabase database, int? year, UserAccount currentUser)
        {
            DelinquentListReport report = BuildDelinquentList(database, year);
            PreviewReport(owner, BuildDelinquentPrintReport(report, currentUser, database == null ? null : database.Settings), true);
        }

        public static void PreviewDelinquencyNotices(IWin32Window owner, LedgerDatabase database, int? year, UserAccount currentUser)
        {
            DelinquencyNoticeBatch batch = BuildDelinquencyNotices(database, year);
            if (batch.Notices.Count == 0)
            {
                MessageBox.Show(
                    "No delinquent assessments were found for the selected report period.",
                    "No notices to print",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            PreviewNoticeBatch(owner, batch, currentUser, database == null ? null : database.Settings);
        }

        public static void PreviewAuditReport(IWin32Window owner, LedgerDatabase database, UserAccount currentUser)
        {
            PreviewReport(owner, BuildAuditPrintReport(database, currentUser, database == null ? null : database.Settings), true);
        }

        public static PaymentReceipt BuildPaymentReceipt(BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment, UserAccount currentUser, AppSettings settings)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }

            if (assessment == null)
            {
                throw new ArgumentNullException("assessment");
            }

            if (payment == null)
            {
                throw new ArgumentNullException("payment");
            }

            PaymentReceipt receipt = new PaymentReceipt();
            receipt.OwnerName = Safe(owner.OwnerName);
            receipt.BusinessName = Safe(owner.BusinessName);
            receipt.OwnerAddress = Safe(owner.OwnerAddress);
            receipt.BusinessAddress = Safe(owner.BusinessAddress);
            receipt.Tin = Safe(owner.Tin);
            receipt.LineOfBusiness = Safe(owner.LineOfBusiness);
            receipt.AssessmentYear = assessment.Year;
            receipt.TotalAssessment = assessment.TotalAssessment;
            receipt.TotalPaid = assessment.TotalPaid;
            receipt.BalanceAfterPayment = assessment.Balance;
            receipt.DatePaid = payment.DatePaid;
            receipt.OrNumber = Safe(payment.OrNumber);
            receipt.Schedule = Safe(payment.Schedule);
            receipt.Amount = payment.Amount;
            receipt.PaymentRemarks = Safe(payment.Remarks);
            receipt.GeneratedBy = currentUser == null ? "unknown" : Safe(currentUser.Username);
            receipt.CollectorName = settings == null ? "" : Safe(settings.CollectorName);
            receipt.TreasurerName = settings == null ? "" : Safe(settings.TreasurerName);
            receipt.CollectorSignaturePath = settings == null ? "" : Safe(settings.CollectorSignaturePath);
            receipt.TreasurerSignaturePath = settings == null ? "" : Safe(settings.TreasurerSignaturePath);
            receipt.QrPayload = QrCodeService.BuildReceiptPayload(receipt);
            receipt.VerificationCode = QrCodeService.VerificationCode(receipt);
            return receipt;
        }

        public static bool PreviewPaymentReceipt(IWin32Window owner, BusinessOwner businessOwner, YearlyAssessment assessment, PaymentRecord payment, UserAccount currentUser, AppSettings settings)
        {
            if (businessOwner == null || assessment == null || payment == null)
            {
                MessageBox.Show(
                    "Select a saved payment first.",
                    "Payment required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            PaymentReceipt receipt = BuildPaymentReceipt(businessOwner, assessment, payment, currentUser, settings);
            PrintDocument document = new PrintDocument();
            document.DocumentName = "Payment Receipt " + DisplayText(receipt.OrNumber, "No OR");
            document.DefaultPageSettings.Landscape = false;
            document.PrintPage += delegate(object sender, PrintPageEventArgs e) { PrintPaymentReceiptPage(receipt, currentUser, settings, e); };

            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = document;
                preview.Width = 900;
                preview.Height = 760;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.ShowDialog(owner);
            }

            return true;
        }

        private static PrintReport BuildCollectionSummaryPrintReport(CollectionSummaryReport report, UserAccount currentUser, AppSettings settings)
        {
            PrintReport printReport = new PrintReport("Collection Summary", PeriodText(report.Year), currentUser, settings);
            printReport.Lines.Add(PrintLine.Summary("Businesses: " + report.BusinessCount.ToString("N0") +
                "    Active: " + report.ActiveCount.ToString("N0") +
                "    Closed: " + report.ClosedCount.ToString("N0") +
                "    Delinquent: " + report.DelinquentCount.ToString("N0")));
            printReport.Lines.Add(PrintLine.Summary("Assessment: " + Money(report.AssessmentTotal) +
                "    Paid: " + Money(report.PaidTotal) +
                "    Balance: " + Money(report.BalanceTotal) +
                "    Collection Rate: " + report.CollectionRateText));
            printReport.Lines.Add(PrintLine.Spacer());

            AddTable(
                printReport,
                "Payment Schedule Totals",
                new string[] { "Schedule", "Payments", "Amount" },
                report.ScheduleRows.Select(row => new string[] { row.Schedule, row.PaymentCount.ToString("N0"), Money(row.Amount) }),
                new float[] { 0.45f, 0.20f, 0.35f });

            AddTable(
                printReport,
                "Top Unpaid Balances",
                new string[] { "Year", "Business", "Owner", "Balance", "Status" },
                report.TopBalanceRows.Select(row => new string[] { row.Year.ToString(), row.BusinessName, row.OwnerName, Money(row.Balance), row.Status }),
                new float[] { 0.10f, 0.30f, 0.25f, 0.18f, 0.17f });

            AddTable(
                printReport,
                "Line of Business Totals",
                new string[] { "Line of Business", "Assessment", "Paid", "Balance" },
                report.LineRows.Select(row => new string[] { row.LineOfBusiness, Money(row.Assessment), Money(row.Paid), Money(row.Balance) }),
                new float[] { 0.40f, 0.20f, 0.20f, 0.20f });

            return printReport;
        }

        private static PrintReport BuildDelinquentPrintReport(DelinquentListReport report, UserAccount currentUser, AppSettings settings)
        {
            PrintReport printReport = new PrintReport("Delinquent List", PeriodText(report.Year), currentUser, settings);
            printReport.Lines.Add(PrintLine.Summary("Records: " + report.Rows.Count.ToString("N0") +
                "    Assessment: " + Money(report.AssessmentTotal) +
                "    Paid: " + Money(report.PaidTotal) +
                "    Balance: " + Money(report.BalanceTotal)));
            printReport.Lines.Add(PrintLine.Spacer());

            AddTable(
                printReport,
                "Overdue Assessments",
                new string[] { "Year", "Business", "Owner", "Line", "Total", "Paid", "Balance", "Reason" },
                report.Rows.Select(row => new string[]
                {
                    row.Year.ToString(),
                    row.BusinessName,
                    row.OwnerName,
                    row.LineOfBusiness,
                    Money(row.TotalAssessment),
                    Money(row.Paid),
                    Money(row.Balance),
                    row.Reason
                }),
                new float[] { 0.07f, 0.17f, 0.15f, 0.12f, 0.11f, 0.10f, 0.11f, 0.17f });

            return printReport;
        }

        private static PrintReport BuildAuditPrintReport(LedgerDatabase database, UserAccount currentUser, AppSettings settings)
        {
            List<AuditLogEntry> entries = database == null || database.AuditTrail == null
                ? new List<AuditLogEntry>()
                : database.AuditTrail.OrderByDescending(entry => entry.Timestamp).Take(100).ToList();

            int fieldChangeCount = entries.Sum(entry => entry.ChangeDetails == null ? 0 : entry.ChangeDetails.Count);
            AuditHashVerificationResult verification = AuditHashService.Verify(database);
            PrintReport printReport = new PrintReport("Audit Trail Review", "Latest 100 audit entries", currentUser, settings);
            printReport.Lines.Add(PrintLine.Summary("Entries: " + entries.Count.ToString("N0") +
                "    Field changes: " + fieldChangeCount.ToString("N0")));
            printReport.Lines.Add(PrintLine.Summary("Audit chain: " + (verification.IsValid ? "Verified" : "Failed") +
                "    Tip: " + ShortHash(verification.CurrentTipHash)));
            if (!verification.IsValid)
            {
                printReport.Lines.Add(PrintLine.Summary("Verification issue: " + verification.Message));
            }

            printReport.Lines.Add(PrintLine.Summary("For a complete field-level review, use Export Audit CSV."));
            printReport.Lines.Add(PrintLine.Spacer());

            AddTable(
                printReport,
                "Audit Entries",
                new string[] { "Date / Time", "User", "Role", "Action", "Record", "Summary" },
                entries.Select(entry => new string[]
                {
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                    Safe(entry.Username),
                    Safe(entry.Role),
                    Safe(entry.Action),
                    Safe(entry.EntityType),
                    Safe(entry.Details)
                }),
                new float[] { 0.15f, 0.12f, 0.10f, 0.15f, 0.12f, 0.36f });

            List<string[]> detailRows = new List<string[]>();
            foreach (AuditLogEntry entry in entries)
            {
                if (entry.ChangeDetails == null)
                {
                    continue;
                }

                foreach (AuditLogDetail detail in entry.ChangeDetails)
                {
                    detailRows.Add(new string[]
                    {
                        entry.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                        Safe(entry.Action),
                        Safe(detail.FieldName),
                        Safe(detail.OldValue),
                        Safe(detail.NewValue)
                    });
                }
            }

            AddTable(
                printReport,
                "Field-Level Changes",
                new string[] { "Date / Time", "Action", "Field", "Before", "After" },
                detailRows,
                new float[] { 0.16f, 0.17f, 0.18f, 0.24f, 0.25f });

            return printReport;
        }

        private static void AddTable(PrintReport report, string title, string[] headers, IEnumerable<string[]> rows, float[] widths)
        {
            report.Lines.Add(PrintLine.Section(title));
            report.Lines.Add(PrintLine.Header(headers, widths));

            bool hasRows = false;
            foreach (string[] row in rows)
            {
                hasRows = true;
                report.Lines.Add(PrintLine.Row(row, widths));
            }

            if (!hasRows)
            {
                report.Lines.Add(PrintLine.Summary("No records."));
            }

            report.Lines.Add(PrintLine.Spacer());
        }

        private static void PreviewReport(IWin32Window owner, PrintReport report, bool landscape)
        {
            PrintState state = new PrintState(report);
            PrintDocument document = new PrintDocument();
            document.DocumentName = report.Title;
            document.DefaultPageSettings.Landscape = landscape;
            document.BeginPrint += delegate { state.Reset(); };
            document.PrintPage += delegate(object sender, PrintPageEventArgs e) { PrintPage(state, e); };

            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = document;
                preview.Width = 1100;
                preview.Height = 760;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.ShowDialog(owner);
            }
        }

        private static void PreviewNoticeBatch(IWin32Window owner, DelinquencyNoticeBatch batch, UserAccount currentUser, AppSettings settings)
        {
            NoticePrintState state = new NoticePrintState(batch, currentUser, settings);
            PrintDocument document = new PrintDocument();
            document.DocumentName = "Delinquency Notices";
            document.DefaultPageSettings.Landscape = false;
            document.BeginPrint += delegate { state.Reset(); };
            document.PrintPage += delegate(object sender, PrintPageEventArgs e) { PrintNoticePage(state, e); };

            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = document;
                preview.Width = 980;
                preview.Height = 760;
                preview.StartPosition = FormStartPosition.CenterParent;
                preview.ShowDialog(owner);
            }
        }

        private static void PrintPage(PrintState state, PrintPageEventArgs e)
        {
            Rectangle bounds = e.MarginBounds;
            int y = bounds.Top;

            using (Font titleFont = new Font("Segoe UI Semibold", 15F, FontStyle.Bold))
            using (Font subtitleFont = new Font("Segoe UI", 9F))
            using (Font normalFont = new Font("Segoe UI", 8F))
            using (Font boldFont = new Font("Segoe UI Semibold", 8F, FontStyle.Bold))
            using (Pen linePen = new Pen(Color.FromArgb(185, 195, 205)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(99, 111, 128)))
            using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(222, 244, 241)))
            {
                if (state.LineIndex == 0)
                {
                    DrawReportHeader(e.Graphics, state.Report, bounds, ref y, titleFont, subtitleFont, normalFont, textBrush, mutedBrush, linePen);
                }

                while (state.LineIndex < state.Report.Lines.Count)
                {
                    PrintLine line = state.Report.Lines[state.LineIndex];
                    int height = line.Height;
                    if (y + height > bounds.Bottom)
                    {
                        DrawFooter(e, state);
                        e.HasMorePages = true;
                        state.PageNumber++;
                        return;
                    }

                    DrawPrintLine(e.Graphics, line, bounds.Left, y, bounds.Width, height, normalFont, boldFont, textBrush, mutedBrush, headerBrush, linePen);
                    y += height;
                    state.LineIndex++;
                }

                DrawFooter(e, state);
                e.HasMorePages = false;
            }
        }

        private static void PrintNoticePage(NoticePrintState state, PrintPageEventArgs e)
        {
            Rectangle bounds = e.MarginBounds;
            int y = bounds.Top;
            DelinquencyNotice notice = state.CurrentNotice;

            using (Font titleFont = new Font("Segoe UI Semibold", 15F, FontStyle.Bold))
            using (Font subtitleFont = new Font("Segoe UI", 9F))
            using (Font normalFont = new Font("Segoe UI", 9.5F))
            using (Font boldFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold))
            using (Font smallFont = new Font("Segoe UI", 8F))
            using (Pen linePen = new Pen(Color.FromArgb(185, 195, 205)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(99, 111, 128)))
            {
                PrintReport header = new PrintReport("Notice of Delinquency", PeriodText(state.Batch.Year), state.CurrentUser, state.Settings);
                DrawReportHeader(e.Graphics, header, bounds, ref y, titleFont, subtitleFont, smallFont, textBrush, mutedBrush, linePen);

                y += 8;
                DrawText(e.Graphics, "Date: " + state.Batch.NoticeDate.ToString("yyyy-MM-dd"), normalFont, textBrush, bounds.Left, ref y, bounds.Width, 22);
                y += 8;

                DrawText(e.Graphics, "To:", boldFont, textBrush, bounds.Left, ref y, bounds.Width, 20);
                DrawText(e.Graphics, DisplayText(notice.OwnerName, "(owner name not recorded)"), normalFont, textBrush, bounds.Left + 24, ref y, bounds.Width - 24, 20);
                if (!String.IsNullOrEmpty(notice.OwnerAddress))
                {
                    DrawText(e.Graphics, notice.OwnerAddress, normalFont, textBrush, bounds.Left + 24, ref y, bounds.Width - 24, 20);
                }

                y += 16;
                DrawText(e.Graphics, "Subject: Delinquent Business Assessment", boldFont, textBrush, bounds.Left, ref y, bounds.Width, 24);
                y += 8;

                string greetingName = DisplayText(notice.OwnerName, "Business Owner");
                DrawWrappedText(e.Graphics, "Dear " + greetingName + ",", normalFont, textBrush, bounds.Left, ref y, bounds.Width, 22);
                y += 8;

                string body =
                    "This is to inform you that our records show an overdue balance for the business assessment listed below. " +
                    "Please settle the outstanding amount or coordinate with the Municipal Treasurer's Office for verification and proper updating of records.";
                DrawWrappedText(e.Graphics, body, normalFont, textBrush, bounds.Left, ref y, bounds.Width, 24);
                y += 12;

                DrawNoticeDetailBox(e.Graphics, notice, bounds.Left, ref y, bounds.Width, normalFont, boldFont, textBrush, mutedBrush, linePen);
                y += 14;

                DrawWrappedText(e.Graphics, "Basis of delinquency: " + notice.Reason, normalFont, textBrush, bounds.Left, ref y, bounds.Width, 24);
                y += 10;
            DrawWrappedText(e.Graphics, "If payment has already been made, kindly present the official receipt for reconciliation. This notice is generated from the Business Tax & Permit Collection System and is subject to verification against official treasury records.", normalFont, textBrush, bounds.Left, ref y, bounds.Width, 24);
                y += 26;

                DrawWrappedText(e.Graphics, "Respectfully,", normalFont, textBrush, bounds.Left, ref y, bounds.Width, 24);
                y += 8;

                string treasurer = state.Settings == null ? "" : Safe(state.Settings.TreasurerName);
                DrawSignatureImage(e.Graphics, state.Settings == null ? "" : state.Settings.TreasurerSignaturePath, bounds.Left, y, 220, 42);
                y += 42;
                DrawText(e.Graphics, DisplayText(treasurer, "Municipal Treasurer"), boldFont, textBrush, bounds.Left, ref y, bounds.Width, 20);
                DrawText(e.Graphics, "Municipal Treasurer", normalFont, mutedBrush, bounds.Left, ref y, bounds.Width, 20);

                string pageText = "Notice " + (state.NoticeIndex + 1).ToString("N0") + " of " + state.Batch.Notices.Count.ToString("N0");
                SizeF pageSize = e.Graphics.MeasureString(pageText, smallFont);
                e.Graphics.DrawString(pageText, smallFont, mutedBrush, bounds.Right - pageSize.Width, bounds.Bottom + 18);
            }

            state.NoticeIndex++;
            e.HasMorePages = state.NoticeIndex < state.Batch.Notices.Count;
        }

        private static void PrintPaymentReceiptPage(PaymentReceipt receipt, UserAccount currentUser, AppSettings settings, PrintPageEventArgs e)
        {
            Rectangle bounds = e.MarginBounds;
            int y = bounds.Top;

            using (Font titleFont = new Font("Segoe UI Semibold", 15F, FontStyle.Bold))
            using (Font subtitleFont = new Font("Segoe UI", 9F))
            using (Font normalFont = new Font("Segoe UI", 9F))
            using (Font boldFont = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
            using (Font smallFont = new Font("Segoe UI", 8F))
            using (Font amountFont = new Font("Segoe UI Semibold", 16F, FontStyle.Bold))
            using (Pen linePen = new Pen(Color.FromArgb(185, 195, 205)))
            using (SolidBrush textBrush = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (SolidBrush mutedBrush = new SolidBrush(Color.FromArgb(99, 111, 128)))
            using (SolidBrush accentBrush = new SolidBrush(Color.FromArgb(15, 118, 110)))
            using (SolidBrush headerBrush = new SolidBrush(Color.FromArgb(222, 244, 241)))
            {
                PrintReport header = new PrintReport("Payment Receipt", "Assessment Year " + receipt.AssessmentYear, currentUser, settings);
                DrawReportHeader(e.Graphics, header, bounds, ref y, titleFont, subtitleFont, smallFont, textBrush, mutedBrush, linePen);

                Rectangle receiptBox = new Rectangle(bounds.Left, y, bounds.Width, 72);
                e.Graphics.FillRectangle(headerBrush, receiptBox);
                e.Graphics.DrawRectangle(linePen, receiptBox);
                e.Graphics.DrawString("OR Number", smallFont, mutedBrush, receiptBox.Left + 12, receiptBox.Top + 10);
                e.Graphics.DrawString(DisplayText(receipt.OrNumber, "(not recorded)"), titleFont, textBrush, receiptBox.Left + 12, receiptBox.Top + 28);
                e.Graphics.DrawString("Amount Paid", smallFont, mutedBrush, receiptBox.Left + receiptBox.Width - 190, receiptBox.Top + 10);
                e.Graphics.DrawString(Money(receipt.Amount), amountFont, accentBrush, receiptBox.Left + receiptBox.Width - 190, receiptBox.Top + 28);
                y += receiptBox.Height + 18;

                AddReceiptSection(e.Graphics, "Taxpayer / Business", bounds.Left, ref y, bounds.Width, boldFont, normalFont, textBrush, mutedBrush, linePen, new string[,]
                {
                    { "Owner", DisplayText(receipt.OwnerName, "(owner name not recorded)") },
                    { "Business", DisplayText(receipt.BusinessName, "(business name not recorded)") },
                    { "Business address", Safe(receipt.BusinessAddress) },
                    { "Line of business", Safe(receipt.LineOfBusiness) },
                    { "TIN", Safe(receipt.Tin) }
                });

                y += 12;
                AddReceiptSection(e.Graphics, "Payment Details", bounds.Left, ref y, bounds.Width, boldFont, normalFont, textBrush, mutedBrush, linePen, new string[,]
                {
                    { "Date paid", receipt.DatePaid.ToString("yyyy-MM-dd") },
                    { "Payment schedule", DisplayText(receipt.Schedule, "(blank)") },
                    { "Assessment year", receipt.AssessmentYear.ToString() },
                    { "Total assessment", Money(receipt.TotalAssessment) },
                    { "Total paid to date", Money(receipt.TotalPaid) },
                    { "Balance after payment", Money(receipt.BalanceAfterPayment) },
                    { "Remarks", Safe(receipt.PaymentRemarks) }
                });

                y += 16;
                int qrSize = 108;
                int qrX = bounds.Right - qrSize;
                int qrY = y;
                using (Bitmap qr = QrCodeService.RenderQrCode(receipt.QrPayload, 3))
                {
                    e.Graphics.DrawImage(qr, new Rectangle(qrX, qrY, qrSize, qrSize));
                }

                e.Graphics.DrawRectangle(linePen, qrX, qrY, qrSize, qrSize);
                string verifyText = "Verify: " + Safe(receipt.VerificationCode);
                SizeF verifySize = e.Graphics.MeasureString(verifyText, smallFont);
                e.Graphics.DrawString(verifyText, smallFont, mutedBrush, qrX + Math.Max(0, (qrSize - verifySize.Width) / 2), qrY + qrSize + 4);

            DrawWrappedText(e.Graphics, "Scan the QR code to read the receipt verification payload. This receipt is generated from the Business Tax & Permit Collection System for taxpayer reference and reconciliation against official treasury records.", smallFont, mutedBrush, bounds.Left, ref y, bounds.Width - qrSize - 20, 18);
                y = Math.Max(y + 28, qrY + qrSize + 30);

                int signatureWidth = (bounds.Width - 40) / 2;
                DrawSignatureBlock(e.Graphics, bounds.Left, ref y, signatureWidth, "Received by", DisplayText(receipt.CollectorName, receipt.GeneratedBy), receipt.CollectorSignaturePath, normalFont, boldFont, mutedBrush, textBrush, linePen);
                int signatureY = y - 82;
                DrawSignatureBlockAt(e.Graphics, bounds.Left + signatureWidth + 40, signatureY, signatureWidth, "Verified by", DisplayText(receipt.TreasurerName, "Municipal Treasurer"), receipt.TreasurerSignaturePath, normalFont, boldFont, mutedBrush, textBrush, linePen);

                string printText = "Printed " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                SizeF printSize = e.Graphics.MeasureString(printText, smallFont);
                e.Graphics.DrawString(printText, smallFont, mutedBrush, bounds.Right - printSize.Width, bounds.Bottom + 18);
            }

            e.HasMorePages = false;
        }

        private static void AddReceiptSection(
            Graphics graphics,
            string title,
            int x,
            ref int y,
            int width,
            Font boldFont,
            Font normalFont,
            Brush textBrush,
            Brush mutedBrush,
            Pen linePen,
            string[,] rows)
        {
            graphics.DrawString(title, boldFont, textBrush, x, y);
            y += 24;

            int labelWidth = 150;
            int rowHeight = 27;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Rectangle labelRect = new Rectangle(x, y, labelWidth, rowHeight);
                Rectangle valueRect = new Rectangle(x + labelWidth, y, width - labelWidth, rowHeight);
                graphics.DrawRectangle(linePen, labelRect);
                graphics.DrawRectangle(linePen, valueRect);
                graphics.DrawString(rows[i, 0], boldFont, mutedBrush, new Rectangle(x + 7, y + 6, labelWidth - 14, rowHeight - 8));
                graphics.DrawString(FitText(rows[i, 1], width - labelWidth), normalFont, textBrush, new Rectangle(x + labelWidth + 7, y + 6, width - labelWidth - 14, rowHeight - 8));
                y += rowHeight;
            }
        }

        private static void DrawSignatureBlock(
            Graphics graphics,
            int x,
            ref int y,
            int width,
            string label,
            string name,
            string signaturePath,
            Font normalFont,
            Font boldFont,
            Brush mutedBrush,
            Brush textBrush,
            Pen linePen)
        {
            DrawSignatureBlockAt(graphics, x, y, width, label, name, signaturePath, normalFont, boldFont, mutedBrush, textBrush, linePen);
            y += 82;
        }

        private static void DrawSignatureBlockAt(
            Graphics graphics,
            int x,
            int y,
            int width,
            string label,
            string name,
            string signaturePath,
            Font normalFont,
            Font boldFont,
            Brush mutedBrush,
            Brush textBrush,
            Pen linePen)
        {
            graphics.DrawString(label, normalFont, mutedBrush, x, y);
            DrawSignatureImage(graphics, signaturePath, x + 10, y + 16, width - 20, 42);
            graphics.DrawLine(linePen, x, y + 56, x + width, y + 56);
            graphics.DrawString(DisplayText(name, "(name)"), boldFont, textBrush, x, y + 60);
        }

        private static void DrawSignatureImage(Graphics graphics, string signaturePath, int x, int y, int width, int height)
        {
            signaturePath = Safe(signaturePath);
            if (String.IsNullOrEmpty(signaturePath) || !File.Exists(signaturePath))
            {
                return;
            }

            try
            {
                using (Image image = Image.FromFile(signaturePath))
                {
                    Size drawSize = FitImage(image.Size, new Size(width, height));
                    int drawX = x + Math.Max(0, (width - drawSize.Width) / 2);
                    int drawY = y + Math.Max(0, (height - drawSize.Height) / 2);
                    graphics.DrawImage(image, new Rectangle(drawX, drawY, drawSize.Width, drawSize.Height));
                }
            }
            catch
            {
                return;
            }
        }

        private static Size FitImage(Size imageSize, Size bounds)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return Size.Empty;
            }

            float scale = Math.Min((float)bounds.Width / imageSize.Width, (float)bounds.Height / imageSize.Height);
            return new Size(Math.Max(1, (int)(imageSize.Width * scale)), Math.Max(1, (int)(imageSize.Height * scale)));
        }

        private static void DrawNoticeDetailBox(
            Graphics graphics,
            DelinquencyNotice notice,
            int x,
            ref int y,
            int width,
            Font normalFont,
            Font boldFont,
            Brush textBrush,
            Brush mutedBrush,
            Pen linePen)
        {
            int rowHeight = 25;
            int labelWidth = 130;
            int boxTop = y;
            string[,] rows = new string[,]
            {
                { "Business", DisplayText(notice.BusinessName, "(business name not recorded)") },
                { "Business address", Safe(notice.BusinessAddress) },
                { "Line of business", Safe(notice.LineOfBusiness) },
                { "Assessment year", notice.Year.ToString() },
                { "Total assessment", Money(notice.TotalAssessment) },
                { "Amount paid", Money(notice.Paid) },
                { "Outstanding balance", Money(notice.Balance) }
            };

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                Rectangle labelRect = new Rectangle(x, y, labelWidth, rowHeight);
                Rectangle valueRect = new Rectangle(x + labelWidth, y, width - labelWidth, rowHeight);
                graphics.DrawRectangle(linePen, labelRect);
                graphics.DrawRectangle(linePen, valueRect);
                graphics.DrawString(rows[i, 0], boldFont, mutedBrush, new Rectangle(x + 6, y + 5, labelWidth - 12, rowHeight - 8));
                graphics.DrawString(FitText(rows[i, 1], width - labelWidth), normalFont, textBrush, new Rectangle(x + labelWidth + 6, y + 5, width - labelWidth - 12, rowHeight - 8));
                y += rowHeight;
            }

            graphics.DrawRectangle(linePen, x, boxTop, width, y - boxTop);
        }

        private static void DrawText(Graphics graphics, string text, Font font, Brush brush, int x, ref int y, int width, int height)
        {
            graphics.DrawString(Safe(text), font, brush, new Rectangle(x, y, width, height));
            y += height;
        }

        private static void DrawWrappedText(Graphics graphics, string text, Font font, Brush brush, int x, ref int y, int width, int lineHeight)
        {
            string value = Safe(text);
            int estimatedLines = Math.Max(1, (int)Math.Ceiling((double)value.Length / Math.Max(35, width / 8)));
            int height = Math.Max(lineHeight, estimatedLines * lineHeight);
            graphics.DrawString(value, font, brush, new Rectangle(x, y, width, height));
            y += height;
        }

        private static void DrawPrintLine(
            Graphics graphics,
            PrintLine line,
            int x,
            int y,
            int width,
            int height,
            Font normalFont,
            Font boldFont,
            Brush textBrush,
            Brush mutedBrush,
            Brush headerBrush,
            Pen linePen)
        {
            if (line.Type == PrintLineType.Spacer)
            {
                return;
            }

            if (line.Type == PrintLineType.Section)
            {
                graphics.DrawString(line.Text, boldFont, textBrush, x, y + 4);
                return;
            }

            if (line.Type == PrintLineType.Summary)
            {
                graphics.DrawString(line.Text, normalFont, mutedBrush, x, y + 3);
                return;
            }

            DrawCells(graphics, line, x, y, width, height, normalFont, boldFont, textBrush, headerBrush, linePen);
        }

        private static void DrawCells(
            Graphics graphics,
            PrintLine line,
            int x,
            int y,
            int width,
            int height,
            Font normalFont,
            Font boldFont,
            Brush textBrush,
            Brush headerBrush,
            Pen linePen)
        {
            Font font = line.Type == PrintLineType.TableHeader ? boldFont : normalFont;
            if (line.Type == PrintLineType.TableHeader)
            {
                graphics.FillRectangle(headerBrush, x, y, width, height);
            }

            int cursor = x;
            for (int i = 0; i < line.Cells.Length; i++)
            {
                int cellWidth = i == line.Cells.Length - 1
                    ? (x + width) - cursor
                    : Math.Max(28, (int)(width * line.Widths[i]));

                Rectangle cell = new Rectangle(cursor + 3, y + 3, Math.Max(10, cellWidth - 6), height - 6);
                graphics.DrawString(FitText(line.Cells[i], cellWidth), font, textBrush, cell);
                graphics.DrawRectangle(linePen, cursor, y, cellWidth, height);
                cursor += cellWidth;
            }
        }

        private static void DrawFooter(PrintPageEventArgs e, PrintState state)
        {
            using (Font footerFont = new Font("Segoe UI", 8F))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(99, 111, 128)))
            {
                string pageText = "Page " + state.PageNumber;
                SizeF size = e.Graphics.MeasureString(pageText, footerFont);
                e.Graphics.DrawString(pageText, footerFont, brush, e.MarginBounds.Right - size.Width, e.MarginBounds.Bottom + 18);

                string footerNote = state.Report.Settings == null ? "" : Safe(state.Report.Settings.ReportFooterNote);
                if (!String.IsNullOrEmpty(footerNote))
                {
                    e.Graphics.DrawString(FitText(footerNote, e.MarginBounds.Width - 120), footerFont, brush, e.MarginBounds.Left, e.MarginBounds.Bottom + 18);
                }

                string treasurerName = state.Report.Settings == null ? "" : Safe(state.Report.Settings.TreasurerName);
                if (!String.IsNullOrEmpty(treasurerName))
                {
                    e.Graphics.DrawString("Treasurer: " + treasurerName, footerFont, brush, e.MarginBounds.Left, e.MarginBounds.Bottom + 32);
                }
            }
        }

        private static void DrawReportHeader(
            Graphics graphics,
            PrintReport report,
            Rectangle bounds,
            ref int y,
            Font titleFont,
            Font subtitleFont,
            Font normalFont,
            Brush textBrush,
            Brush mutedBrush,
            Pen linePen)
        {
            AppSettings settings = report.Settings;
            int top = y;
            int textLeft = bounds.Left;
            int sealSize = 56;
            bool drewSeal = false;

            string sealPath = settings == null ? "" : Safe(settings.SealImagePath);
            if (!String.IsNullOrEmpty(sealPath) && File.Exists(sealPath))
            {
                try
                {
                    using (Image seal = Image.FromFile(sealPath))
                    {
                        graphics.DrawImage(seal, new Rectangle(bounds.Left, y, sealSize, sealSize));
                        textLeft += sealSize + 12;
                        drewSeal = true;
                    }
                }
                catch
                {
                    drewSeal = false;
                    textLeft = bounds.Left;
                }
            }

            string municipality = DisplayText(settings == null ? "" : settings.MunicipalityName, "Business Tax & Permit Collection System");
            string province = settings == null ? "" : Safe(settings.ProvinceName);
            string office = DisplayText(settings == null ? "" : settings.OfficeName, "Municipal Treasurer's Office");

            graphics.DrawString(municipality, titleFont, textBrush, textLeft, y);
            y += 24;

            if (!String.IsNullOrEmpty(province))
            {
                graphics.DrawString(province, subtitleFont, mutedBrush, textLeft, y);
                y += 17;
            }

            graphics.DrawString(office, subtitleFont, mutedBrush, textLeft, y);
            y += 18;

            if (drewSeal)
            {
                y = Math.Max(y, top + sealSize + 4);
            }

            y += 8;
            graphics.DrawString(report.Title, titleFont, textBrush, bounds.Left, y);
            y += 26;
            graphics.DrawString(report.Subtitle, subtitleFont, mutedBrush, bounds.Left, y);
            y += 18;
            graphics.DrawString(report.GeneratedText, normalFont, mutedBrush, bounds.Left, y);
            y += 24;
            graphics.DrawLine(linePen, bounds.Left, y, bounds.Right, y);
            y += 10;
        }

        private static string FitText(string value, int cellWidth)
        {
            value = Safe(value);
            int maxChars = Math.Max(6, cellWidth / 6);
            if (value.Length <= maxChars)
            {
                return value;
            }

            return value.Substring(0, Math.Max(3, maxChars - 3)) + "...";
        }

        private static int ScheduleSort(string schedule)
        {
            if (String.Equals(schedule, "Annual", StringComparison.OrdinalIgnoreCase)) return 0;
            if (String.Equals(schedule, "1st Qtr", StringComparison.OrdinalIgnoreCase)) return 1;
            if (String.Equals(schedule, "2nd Qtr", StringComparison.OrdinalIgnoreCase)) return 2;
            if (String.Equals(schedule, "3rd Qtr", StringComparison.OrdinalIgnoreCase)) return 3;
            if (String.Equals(schedule, "4th Qtr", StringComparison.OrdinalIgnoreCase)) return 4;
            return 9;
        }

        private static string PeriodText(int? year)
        {
            return year.HasValue ? "Year " + year.Value : "All years";
        }

        private static string Money(decimal value)
        {
            return value.ToString("N2");
        }

        private static string ShortHash(string hash)
        {
            if (String.IsNullOrEmpty(hash))
            {
                return "";
            }

            return hash.Length <= 16 ? hash : hash.Substring(0, 16);
        }

        private static string DisplayText(string value, string fallback)
        {
            value = Safe(value).Trim();
            return String.IsNullOrEmpty(value) ? fallback : value;
        }

        private static string Safe(string value)
        {
            return value == null ? "" : value;
        }

        public class CollectionSummaryReport
        {
            public int? Year { get; set; }
            public int BusinessCount { get; set; }
            public int ActiveCount { get; set; }
            public int ClosedCount { get; set; }
            public int DelinquentCount { get; set; }
            public decimal AssessmentTotal { get; set; }
            public decimal PaidTotal { get; set; }
            public decimal BalanceTotal { get; set; }
            public List<CollectionScheduleRow> ScheduleRows { get; set; }
            public List<CollectionLineRow> LineRows { get; set; }
            public List<CollectionBalanceRow> TopBalanceRows { get; set; }

            public CollectionSummaryReport()
            {
                ScheduleRows = new List<CollectionScheduleRow>();
                LineRows = new List<CollectionLineRow>();
                TopBalanceRows = new List<CollectionBalanceRow>();
            }

            public string CollectionRateText
            {
                get { return AssessmentTotal <= 0m ? "0.00%" : (PaidTotal / AssessmentTotal).ToString("P2"); }
            }
        }

        public class CollectionScheduleRow
        {
            public string Schedule { get; set; }
            public int PaymentCount { get; set; }
            public decimal Amount { get; set; }
        }

        public class CollectionLineRow
        {
            public string LineOfBusiness { get; set; }
            public decimal Assessment { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }
        }

        public class CollectionBalanceRow
        {
            public int Year { get; set; }
            public string BusinessName { get; set; }
            public string OwnerName { get; set; }
            public decimal Balance { get; set; }
            public string Status { get; set; }
        }

        public class DelinquentListReport
        {
            public int? Year { get; set; }
            public decimal AssessmentTotal { get; set; }
            public decimal PaidTotal { get; set; }
            public decimal BalanceTotal { get; set; }
            public List<DelinquentReportRow> Rows { get; set; }

            public DelinquentListReport()
            {
                Rows = new List<DelinquentReportRow>();
            }
        }

        public class DelinquentReportRow
        {
            public int Year { get; set; }
            public string BusinessName { get; set; }
            public string OwnerName { get; set; }
            public string LineOfBusiness { get; set; }
            public decimal TotalAssessment { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }
            public string Reason { get; set; }
        }

        public class DelinquencyNoticeBatch
        {
            public int? Year { get; set; }
            public DateTime NoticeDate { get; set; }
            public decimal AssessmentTotal { get; set; }
            public decimal PaidTotal { get; set; }
            public decimal BalanceTotal { get; set; }
            public List<DelinquencyNotice> Notices { get; set; }

            public DelinquencyNoticeBatch()
            {
                NoticeDate = DateTime.Today;
                Notices = new List<DelinquencyNotice>();
            }
        }

        public class DelinquencyNotice
        {
            public int Year { get; set; }
            public string OwnerName { get; set; }
            public string BusinessName { get; set; }
            public string OwnerAddress { get; set; }
            public string BusinessAddress { get; set; }
            public string LineOfBusiness { get; set; }
            public decimal TotalAssessment { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }
            public string Reason { get; set; }
        }

        public class PaymentReceipt
        {
            public string OwnerName { get; set; }
            public string BusinessName { get; set; }
            public string OwnerAddress { get; set; }
            public string BusinessAddress { get; set; }
            public string Tin { get; set; }
            public string LineOfBusiness { get; set; }
            public int AssessmentYear { get; set; }
            public decimal TotalAssessment { get; set; }
            public decimal TotalPaid { get; set; }
            public decimal BalanceAfterPayment { get; set; }
            public DateTime DatePaid { get; set; }
            public string OrNumber { get; set; }
            public string Schedule { get; set; }
            public decimal Amount { get; set; }
            public string PaymentRemarks { get; set; }
            public string GeneratedBy { get; set; }
            public string CollectorName { get; set; }
            public string TreasurerName { get; set; }
            public string CollectorSignaturePath { get; set; }
            public string TreasurerSignaturePath { get; set; }
            public string QrPayload { get; set; }
            public string VerificationCode { get; set; }
        }

        private class PrintReport
        {
            public string Title { get; private set; }
            public string Subtitle { get; private set; }
            public string GeneratedText { get; private set; }
            public AppSettings Settings { get; private set; }
            public List<PrintLine> Lines { get; private set; }

            public PrintReport(string title, string subtitle, UserAccount currentUser, AppSettings settings)
            {
                Title = title;
                Subtitle = subtitle;
                Settings = settings ?? new AppSettings();
                string username = currentUser == null ? "unknown" : Safe(currentUser.Username);
                GeneratedText = "Generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " by " + username;
                if (!String.IsNullOrEmpty(Safe(Settings.CollectorName)))
                {
                    GeneratedText += " | Collector/Cashier: " + Safe(Settings.CollectorName);
                }

                Lines = new List<PrintLine>();
            }
        }

        private class PrintState
        {
            public PrintReport Report { get; private set; }
            public int LineIndex { get; set; }
            public int PageNumber { get; set; }

            public PrintState(PrintReport report)
            {
                Report = report;
                Reset();
            }

            public void Reset()
            {
                LineIndex = 0;
                PageNumber = 1;
            }
        }

        private class NoticePrintState
        {
            public DelinquencyNoticeBatch Batch { get; private set; }
            public UserAccount CurrentUser { get; private set; }
            public AppSettings Settings { get; private set; }
            public int NoticeIndex { get; set; }

            public NoticePrintState(DelinquencyNoticeBatch batch, UserAccount currentUser, AppSettings settings)
            {
                Batch = batch ?? new DelinquencyNoticeBatch();
                CurrentUser = currentUser;
                Settings = settings ?? new AppSettings();
                Reset();
            }

            public DelinquencyNotice CurrentNotice
            {
                get { return Batch.Notices[NoticeIndex]; }
            }

            public void Reset()
            {
                NoticeIndex = 0;
            }
        }

        private class PrintLine
        {
            public PrintLineType Type { get; private set; }
            public string Text { get; private set; }
            public string[] Cells { get; private set; }
            public float[] Widths { get; private set; }
            public int Height { get; private set; }

            public static PrintLine Summary(string text)
            {
                return new PrintLine { Type = PrintLineType.Summary, Text = text, Height = 18 };
            }

            public static PrintLine Section(string text)
            {
                return new PrintLine { Type = PrintLineType.Section, Text = text, Height = 24 };
            }

            public static PrintLine Header(string[] cells, float[] widths)
            {
                return new PrintLine { Type = PrintLineType.TableHeader, Cells = cells, Widths = widths, Height = 24 };
            }

            public static PrintLine Row(string[] cells, float[] widths)
            {
                return new PrintLine { Type = PrintLineType.TableRow, Cells = cells, Widths = widths, Height = 24 };
            }

            public static PrintLine Spacer()
            {
                return new PrintLine { Type = PrintLineType.Spacer, Height = 10 };
            }
        }

        private enum PrintLineType
        {
            Summary,
            Section,
            TableHeader,
            TableRow,
            Spacer
        }
    }
}
