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

        private TabPage BuildReportTab()
        {
            TabPage tab = new TabPage("Reports");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;
            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(12);
            host.BackColor = SurfaceBack;

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Top;
            top.Height = 118;
            top.BackColor = SurfaceBack;
            top.ColumnCount = 7;
            top.RowCount = 3;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            reportSummaryLabel = new Label();
            reportSummaryLabel.Dock = DockStyle.Fill;
            reportSummaryLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            reportSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            top.Controls.Add(reportSummaryLabel, 0, 0);
            top.SetColumnSpan(reportSummaryLabel, 7);

            Label yearLabel = MakeLabel("Year");
            reportYearFilter = MakeComboBox(new string[] { "All" });
            reportYearFilter.Tag = "UseDefaultYear";
            reportYearFilter.SelectedIndexChanged += ReportFilter_Changed;
            Label statusLabel = MakeLabel("Payment status");
            reportStatusFilter = MakeComboBox(new string[] { "All", "Paid", "With Balance", "Delinquent" });
            reportStatusFilter.SelectedIndexChanged += ReportFilter_Changed;
            Label scheduleLabel = MakeLabel("Schedule");
            reportScheduleFilter = MakeComboBox(new string[] { "All", "Annual", "1st Qtr", "2nd Qtr", "3rd Qtr", "4th Qtr" });
            reportScheduleFilter.SelectedIndexChanged += ReportFilter_Changed;

            top.Controls.Add(yearLabel, 0, 1);
            top.Controls.Add(reportYearFilter, 1, 1);
            top.Controls.Add(statusLabel, 2, 1);
            top.Controls.Add(reportStatusFilter, 3, 1);
            top.Controls.Add(scheduleLabel, 4, 1);
            top.Controls.Add(reportScheduleFilter, 5, 1);

            Label searchLabel = MakeLabel("Search");
            reportSearchText = MakeTextBox();
            reportSearchText.TextChanged += ReportFilter_Changed;
            top.Controls.Add(searchLabel, 0, 2);
            top.Controls.Add(reportSearchText, 1, 2);
            top.SetColumnSpan(reportSearchText, 5);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;

            Button exportButton = MakeButton("Export CSV");
            exportButton.Width = 108;
            exportButton.Click += ExportMenu_Click;
            actions.Controls.Add(exportButton);

            Button delinquentPrintButton = MakeButton("Print Delinq.");
            delinquentPrintButton.Width = 118;
            delinquentPrintButton.Click += PrintDelinquentReport_Click;
            actions.Controls.Add(delinquentPrintButton);

            Button noticePrintButton = MakeButton("Print Notices");
            noticePrintButton.Width = 124;
            noticePrintButton.Click += PrintDelinquencyNotices_Click;
            actions.Controls.Add(noticePrintButton);

            Button summaryPrintButton = MakeButton("Print Summary");
            summaryPrintButton.Width = 124;
            summaryPrintButton.Click += PrintCollectionSummary_Click;
            actions.Controls.Add(summaryPrintButton);

            top.Controls.Add(actions, 6, 1);
            top.SetRowSpan(actions, 2);

            reportGrid = MakeGrid();
            reportGrid.Columns.Add(MakeColumn("Year", "Year", true));
            reportGrid.Columns.Add(MakeColumn("BusinessName", "Business Name", true));
            reportGrid.Columns.Add(MakeColumn("OwnerName", "Owner Name", true));
            reportGrid.Columns.Add(MakeColumn("LineOfBusiness", "Line of Business", true));
            reportGrid.Columns.Add(MakeColumn("Total", "Total Assessment", true));
            reportGrid.Columns.Add(MakeColumn("Paid", "Paid", true));
            reportGrid.Columns.Add(MakeColumn("Balance", "Balance", true));
            reportGrid.Columns.Add(MakeColumn("Status", "Status", true));
            reportGrid.Dock = DockStyle.Fill;

            host.Controls.Add(reportGrid);
            host.Controls.Add(top);
            tab.Controls.Add(host);
            return tab;
        }

        private void RefreshReport()
        {
            if (reportGrid == null)
            {
                return;
            }

            RefreshReportFilterOptions();

            reportGrid.Rows.Clear();
            int visibleRows = 0;
            int ownerCount = 0;
            HashSet<string> visibleOwnerIds = new HashSet<string>();
            decimal totalAssessment = 0m;
            decimal totalPaid = 0m;
            decimal totalBalance = 0m;

            string yearFilter = reportYearFilter == null ? "All" : reportYearFilter.Text;
            string statusFilter = reportStatusFilter == null ? "All" : reportStatusFilter.Text;
            string scheduleFilter = reportScheduleFilter == null ? "All" : reportScheduleFilter.Text;
            string searchTerm = reportSearchText == null ? "" : reportSearchText.Text.Trim().ToLowerInvariant();
            int selectedYear;
            bool hasYearFilter = Int32.TryParse(yearFilter, out selectedYear);

            foreach (BusinessOwner owner in database.Owners.OrderBy(owner => Safe(owner.BusinessName)))
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments.OrderByDescending(item => item.Year))
                {
                    string paymentStatus = DelinquencyService.AssessmentPaymentStatus(assessment, DateTime.Today);
                    if (hasYearFilter && assessment.Year != selectedYear)
                    {
                        continue;
                    }

                    if (!String.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase) &&
                        !String.Equals(statusFilter, paymentStatus, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!String.Equals(scheduleFilter, "All", StringComparison.OrdinalIgnoreCase) &&
                        !AssessmentHasPaymentSchedule(assessment, scheduleFilter))
                    {
                        continue;
                    }

                    if (!MatchesReportSearch(owner, assessment, searchTerm))
                    {
                        continue;
                    }

                    visibleRows++;
                    if (!visibleOwnerIds.Contains(owner.Id))
                    {
                        visibleOwnerIds.Add(owner.Id);
                        ownerCount++;
                    }

                    totalAssessment += assessment.TotalAssessment;
                    totalPaid += assessment.TotalPaid;
                    totalBalance += assessment.Balance;

                    reportGrid.Rows.Add(new object[]
                    {
                        assessment.Year,
                        Safe(owner.BusinessName),
                        Safe(owner.OwnerName),
                        Safe(owner.LineOfBusiness),
                        Money(assessment.TotalAssessment),
                        Money(assessment.TotalPaid),
                        Money(assessment.Balance),
                        paymentStatus
                    });
                }
            }

            reportSummaryLabel.Text = String.Format(
                "Records: {0}   Businesses: {1}   Total Assessment: {2}   Paid: {3}   Balance: {4}",
                visibleRows,
                ownerCount,
                Money(totalAssessment),
                Money(totalPaid),
                Money(totalBalance));

            RefreshDashboard();
        }

        private void RefreshReportFilterOptions()
        {
            if (reportYearFilter == null || reportStatusFilter == null || reportScheduleFilter == null || loading)
            {
                return;
            }

            string selectedYear = reportYearFilter.Text;
            string selectedStatus = reportStatusFilter.Text;
            string selectedSchedule = reportScheduleFilter.Text;
            bool useDefaultYear = String.Equals(Convert.ToString(reportYearFilter.Tag), "UseDefaultYear", StringComparison.Ordinal);

            loading = true;
            try
            {
                reportYearFilter.Items.Clear();
                reportYearFilter.Items.Add("All");

                List<int> years = database.Owners
                    .Where(owner => owner.Assessments != null)
                    .SelectMany(owner => owner.Assessments)
                    .Select(assessment => assessment.Year)
                    .Distinct()
                    .OrderByDescending(year => year)
                    .ToList();

                foreach (int year in years)
                {
                    reportYearFilter.Items.Add(year.ToString());
                }

                if (useDefaultYear && database.Settings != null && database.Settings.DefaultReportYear > 0)
                {
                    SelectComboValue(reportYearFilter, database.Settings.DefaultReportYear.ToString(), "All");
                    reportYearFilter.Tag = "";
                }
                else
                {
                    SelectComboValue(reportYearFilter, selectedYear, "All");
                }

                SelectComboValue(reportStatusFilter, selectedStatus, "All");
                SelectComboValue(reportScheduleFilter, selectedSchedule, "All");
            }
            finally
            {
                loading = false;
            }
        }

        private bool AssessmentHasPaymentSchedule(YearlyAssessment assessment, string schedule)
        {
            if (assessment == null || assessment.Payments == null)
            {
                return false;
            }

            return assessment.Payments.Any(payment => String.Equals(payment.Schedule ?? "", schedule, StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesReportSearch(BusinessOwner owner, YearlyAssessment assessment, string term)
        {
            if (String.IsNullOrEmpty(term))
            {
                return true;
            }

            string ownerText = String.Join(" ", new string[]
            {
                Safe(owner.OwnerName),
                Safe(owner.BusinessName),
                Safe(owner.LineOfBusiness),
                Safe(owner.Tin),
                Safe(owner.ContactNumber),
                Safe(owner.OwnerAddress),
                Safe(owner.BusinessAddress),
                Safe(owner.Status),
                Safe(owner.RegistrationType),
                Safe(owner.Remarks),
                assessment.Year.ToString(),
                Safe(assessment.Remarks)
            }).ToLowerInvariant();

            if (ownerText.Contains(term))
            {
                return true;
            }

            if (assessment.Payments == null)
            {
                return false;
            }

            foreach (PaymentRecord payment in assessment.Payments)
            {
                string paymentText = String.Join(" ", new string[]
                {
                    Safe(payment.OrNumber),
                    Safe(payment.Schedule),
                    Safe(payment.Remarks),
                    payment.DatePaid.ToString("yyyy-MM-dd"),
                    payment.Amount.ToString("0.00")
                }).ToLowerInvariant();

                if (paymentText.Contains(term))
                {
                    return true;
                }
            }

            return false;
        }

        private void ReportFilter_Changed(object sender, EventArgs e)
        {
            if (!loading)
            {
                RefreshReport();
            }
        }

        private int? SelectedReportYear()
        {
            int year;
            if (reportYearFilter != null && Int32.TryParse(reportYearFilter.Text, out year))
            {
                return year;
            }

            return null;
        }

        private void PrintCollectionSummary_Click(object sender, EventArgs e)
        {
            int? year = SelectedReportYear();
            PrintableReportService.PreviewCollectionSummary(this, database, year, currentUser);
            LogAction("Print Collection Summary", "Report", "", year.HasValue ? "Year " + year.Value : "All years");
            dataStore.Save(database);
            RefreshAuditLog();
        }

        private void PrintDelinquentReport_Click(object sender, EventArgs e)
        {
            int? year = SelectedReportYear();
            PrintableReportService.PreviewDelinquentList(this, database, year, currentUser);
            LogAction("Print Delinquent List", "Report", "", year.HasValue ? "Year " + year.Value : "All years");
            dataStore.Save(database);
            RefreshAuditLog();
        }

        private void PrintDelinquencyNotices_Click(object sender, EventArgs e)
        {
            int? year = SelectedReportYear();
            PrintableReportService.DelinquencyNoticeBatch batch = PrintableReportService.BuildDelinquencyNotices(database, year);
            if (batch.Notices.Count == 0)
            {
                MessageBox.Show(
                    "No delinquent assessments were found for the selected report period.",
                    "No notices to print",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            PrintableReportService.PreviewDelinquencyNotices(this, database, year, currentUser);
            LogAction(
                "Print Delinquency Notices",
                "Report",
                "",
                (year.HasValue ? "Year " + year.Value : "All years") + " | Notices: " + batch.Notices.Count.ToString("N0"));
            dataStore.Save(database);
            RefreshAuditLog();
        }
    }
}
