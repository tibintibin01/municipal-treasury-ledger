using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm
    {
        private TabPage BuildDashboardTab()
        {
            TabPage tab = new TabPage("Dashboard");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            TableLayoutPanel host = new TableLayoutPanel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(12);
            host.BackColor = WindowBack;
            host.ColumnCount = 1;
            host.RowCount = 4;
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
            host.RowStyles.Add(new RowStyle(SizeType.Absolute, 320));
            host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            TableLayoutPanel filterBar = new TableLayoutPanel();
            filterBar.Dock = DockStyle.Fill;
            filterBar.BackColor = WindowBack;
            filterBar.Margin = new Padding(0, 0, 12, 8);
            filterBar.Padding = new Padding(0, 0, 0, 0);
            filterBar.ColumnCount = 5;
            filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
            filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));

            Label title = new Label();
            title.Text = "Collection Dashboard";
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            title.ForeColor = TextMain;

            Label yearLabel = MakeLabel("Year");
            dashboardYearFilter = MakeComboBox(new string[] { "All" });
            dashboardYearFilter.SelectedIndexChanged += DashboardFilter_Changed;

            Button refreshButton = MakeButton("Refresh");
            refreshButton.Width = 86;
            refreshButton.Dock = DockStyle.Fill;
            refreshButton.Click += DashboardFilter_Changed;

            Button delinquencyButton = MakeButton("Run Check");
            delinquencyButton.Width = 106;
            delinquencyButton.Dock = DockStyle.Fill;
            delinquencyButton.Click += RunDelinquencyCheck_Click;

            filterBar.Controls.Add(title, 0, 0);
            filterBar.Controls.Add(yearLabel, 1, 0);
            filterBar.Controls.Add(dashboardYearFilter, 2, 0);
            filterBar.Controls.Add(delinquencyButton, 3, 0);
            filterBar.Controls.Add(refreshButton, 4, 0);

            TableLayoutPanel metrics = new TableLayoutPanel();
            metrics.Dock = DockStyle.Fill;
            metrics.BackColor = Color.Transparent;
            metrics.Margin = new Padding(0);
            metrics.ColumnCount = 4;
            metrics.RowCount = 2;
            for (int i = 0; i < 4; i++)
            {
                metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            metrics.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            metrics.Controls.Add(MakeDashboardMetric("Businesses", out dashboardBusinessCountLabel), 0, 0);
            metrics.Controls.Add(MakeDashboardMetric("Active", out dashboardActiveCountLabel), 1, 0);
            metrics.Controls.Add(MakeDashboardMetric("Closed", out dashboardClosedCountLabel), 2, 0);
            metrics.Controls.Add(MakeDashboardMetric("Delinquent", out dashboardDelinquentCountLabel), 3, 0);
            metrics.Controls.Add(MakeDashboardMetric("Assessment", out dashboardAssessmentLabel), 0, 1);
            metrics.Controls.Add(MakeDashboardMetric("Paid", out dashboardPaidLabel), 1, 1);
            metrics.Controls.Add(MakeDashboardMetric("Balance", out dashboardBalanceLabel), 2, 1);
            metrics.Controls.Add(MakeDashboardMetric("Collection Rate", out dashboardCollectionRateLabel), 3, 1);

            TableLayoutPanel charts = new TableLayoutPanel();
            charts.Dock = DockStyle.Fill;
            charts.BackColor = Color.Transparent;
            charts.Margin = new Padding(0);
            charts.ColumnCount = 2;
            charts.RowCount = 1;
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            charts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            dashboardMonthlyCollectionsChart = MakeDashboardChart("Monthly Collections");
            dashboardYearComparisonChart = MakeDashboardChart("Year-over-Year Revenue");
            
            dashboardMonthlyCollectionsChart.Margin = new Padding(0, 0, 12, 12);
            dashboardYearComparisonChart.Margin = new Padding(0, 0, 12, 12);

            charts.Controls.Add(dashboardMonthlyCollectionsChart, 0, 0);
            charts.Controls.Add(dashboardYearComparisonChart, 1, 0);

            TableLayoutPanel grids = new TableLayoutPanel();
            grids.Dock = DockStyle.Fill;
            grids.BackColor = Color.Transparent;
            grids.Margin = new Padding(0);
            grids.ColumnCount = 2;
            grids.RowCount = 2;
            grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grids.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grids.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            dashboardTopBalancesGrid = MakeGrid();
            dashboardTopBalancesGrid.Columns.Add(MakeColumn("Year", "Year", true));
            dashboardTopBalancesGrid.Columns.Add(MakeColumn("Business", "Business", true));
            dashboardTopBalancesGrid.Columns.Add(MakeColumn("Owner", "Owner", true));
            dashboardTopBalancesGrid.Columns.Add(MakeColumn("Balance", "Balance", true));
            dashboardTopBalancesGrid.Columns.Add(MakeColumn("Status", "Status", true));
            ConfigureDashboardGridColumns(dashboardTopBalancesGrid, new string[] { "Year", "Business", "Owner", "Balance", "Status" }, new int[] { 64, 130, 130, 96, 90 }, "Business");

            dashboardRecentPaymentsGrid = MakeGrid();
            dashboardRecentPaymentsGrid.Columns.Add(MakeColumn("Date", "Date", true));
            dashboardRecentPaymentsGrid.Columns.Add(MakeColumn("OR", "OR Number", true));
            dashboardRecentPaymentsGrid.Columns.Add(MakeColumn("Business", "Business", true));
            dashboardRecentPaymentsGrid.Columns.Add(MakeColumn("Schedule", "Schedule", true));
            dashboardRecentPaymentsGrid.Columns.Add(MakeColumn("Amount", "Amount", true));
            ConfigureDashboardGridColumns(dashboardRecentPaymentsGrid, new string[] { "Date", "OR", "Business", "Schedule", "Amount" }, new int[] { 92, 112, 140, 96, 96 }, "Business");

            dashboardQuarterGrid = MakeGrid();
            dashboardQuarterGrid.Columns.Add(MakeColumn("Schedule", "Schedule", true));
            dashboardQuarterGrid.Columns.Add(MakeColumn("Payments", "Payments", true));
            dashboardQuarterGrid.Columns.Add(MakeColumn("Amount", "Amount", true));
            ConfigureDashboardGridColumns(dashboardQuarterGrid, new string[] { "Schedule", "Payments", "Amount" }, new int[] { 150, 110, 130 }, "Schedule");

            dashboardLineBusinessGrid = MakeGrid();
            dashboardLineBusinessGrid.Columns.Add(MakeColumn("Line", "Line of Business", true));
            dashboardLineBusinessGrid.Columns.Add(MakeColumn("Assessment", "Assessment", true));
            dashboardLineBusinessGrid.Columns.Add(MakeColumn("Paid", "Paid", true));
            dashboardLineBusinessGrid.Columns.Add(MakeColumn("Balance", "Balance", true));
            ConfigureDashboardGridColumns(dashboardLineBusinessGrid, new string[] { "Line", "Assessment", "Paid", "Balance" }, new int[] { 190, 120, 100, 110 }, "Line");

            grids.Controls.Add(MakeDashboardSection("Top Unpaid Balances", dashboardTopBalancesGrid), 0, 0);
            grids.Controls.Add(MakeDashboardSection("Recent Payments", dashboardRecentPaymentsGrid), 1, 0);
            grids.Controls.Add(MakeDashboardSection("Payment Schedule Totals", dashboardQuarterGrid), 0, 1);
            grids.Controls.Add(MakeDashboardSection("Line of Business Totals", dashboardLineBusinessGrid), 1, 1);

            host.Controls.Add(filterBar, 0, 0);
            host.Controls.Add(metrics, 0, 1);
            host.Controls.Add(charts, 0, 2);
            host.Controls.Add(grids, 0, 3);
            tab.Controls.Add(host);
            return tab;
        }

        private Chart MakeDashboardChart(string title)
        {
            Chart chart = new Chart();
            chart.Dock = DockStyle.Fill;
            chart.Margin = new Padding(0, 0, 12, 12);
            chart.BackColor = SurfaceBack;
            chart.BorderlineColor = BorderLine;
            chart.BorderlineDashStyle = ChartDashStyle.Solid;

            ChartArea area = new ChartArea("Main");
            area.BackColor = SurfaceBack;
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8F);
            area.AxisX.LabelStyle.ForeColor = TextMuted;
            area.AxisX.LineColor = BorderLine;
            area.AxisX.MajorTickMark.LineColor = BorderLine;
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8F);
            area.AxisY.LabelStyle.Format = "N0";
            area.AxisY.LabelStyle.ForeColor = TextMuted;
            area.AxisY.LineColor = BorderLine;
            area.AxisY.MajorGrid.LineColor = ChartGridLine;
            area.AxisY.MajorTickMark.LineColor = BorderLine;
            chart.ChartAreas.Add(area);

            Title chartTitle = new Title(title);
            chartTitle.Alignment = ContentAlignment.TopLeft;
            chartTitle.Docking = Docking.Top;
            chartTitle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            chartTitle.ForeColor = TextMain;
            chart.Titles.Add(chartTitle);

            Legend legend = new Legend("Legend");
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            legend.Font = new Font("Segoe UI", 8F);
            legend.ForeColor = TextMuted;
            legend.BackColor = Color.Transparent;
            chart.Legends.Add(legend);

            return chart;
        }

        private void ConfigureDashboardGridColumns(DataGridView grid, string[] names, int[] widths, string fillColumnName = "")
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.ScrollBars = ScrollBars.Both;

            for (int i = 0; i < names.Length && i < widths.Length; i++)
            {
                if (!grid.Columns.Contains(names[i]))
                {
                    continue;
                }

                grid.Columns[names[i]].Width = widths[i];
                grid.Columns[names[i]].MinimumWidth = Math.Min(widths[i], 60);
                if (names[i] == fillColumnName)
                {
                    grid.Columns[names[i]].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
                else
                {
                    grid.Columns[names[i]].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }
        }

        private Panel MakeDashboardMetric(string title, out Label valueLabel)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, 12, 12);
            panel.Padding = new Padding(18, 10, 12, 8);
            panel.BackColor = SurfaceBack;

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 22;
            titleLabel.ForeColor = TextMuted;
            titleLabel.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.AutoEllipsis = true;

            valueLabel = new Label();
            valueLabel.Text = "0";
            valueLabel.Dock = DockStyle.Top;
            valueLabel.Height = 38;
            valueLabel.ForeColor = TextMain;
            valueLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            valueLabel.AutoEllipsis = true;

            panel.Controls.Add(valueLabel);
            panel.Controls.Add(titleLabel);

            panel.Paint += (s, e) => {
                // Draw thin card border
                using (Pen pen = new Pen(BorderLine, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }

                // Draw left colored accent bar
                Color accentColor = GetMetricAccentColor(title);
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(brush, 1, 1, 4, panel.Height - 2);
                }
            };

            return panel;
        }

        private Color GetMetricAccentColor(string title)
        {
            string t = (title ?? "").ToLowerInvariant();
            if (t.Contains("delinquent") || t.Contains("balance"))
            {
                return Danger;
            }
            if (t.Contains("assessment"))
            {
                return DarkThemeEnabled ? Color.FromArgb(96, 165, 250) : Color.FromArgb(37, 99, 235);
            }
            if (t.Contains("paid") || t.Contains("active") || t.Contains("rate") || t.Contains("business"))
            {
                return Accent;
            }
            if (t.Contains("closed"))
            {
                return TextMuted;
            }
            return Accent;
        }

        private Control MakeDashboardSection(string title, DataGridView grid)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, 12, 12);
            panel.BackColor = SurfaceBack;
            panel.Padding = new Padding(12, 8, 12, 8);
            panel.ColumnCount = 1;
            panel.RowCount = 2;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.ForeColor = TextMain;
            titleLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            panel.Controls.Add(titleLabel, 0, 0);
            panel.Controls.Add(grid, 0, 1);

            panel.Paint += (s, e) => {
                using (Pen pen = new Pen(BorderLine, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            return panel;
        }

        private void RefreshDashboard()
        {
            if (dashboardYearFilter == null)
            {
                return;
            }

            RefreshDashboardYearOptions();

            List<BusinessOwner> owners = database == null || database.Owners == null
                ? new List<BusinessOwner>()
                : database.Owners;

            int selectedYear;
            bool hasYearFilter = Int32.TryParse(dashboardYearFilter.Text, out selectedYear);

            int activeCount = owners.Count(owner => String.Equals(Safe(owner.Status), "Active", StringComparison.OrdinalIgnoreCase));
            int closedCount = owners.Count(owner => String.Equals(Safe(owner.Status), "Closed", StringComparison.OrdinalIgnoreCase));
            int? delinquencyYear = hasYearFilter ? (int?)selectedYear : null;
            int delinquentCount = owners.Count(owner => DelinquencyService.OwnerHasDelinquency(owner, delinquencyYear, DateTime.Today));

            decimal totalAssessment = 0m;
            decimal totalPaid = 0m;
            decimal totalBalance = 0m;

            Dictionary<string, DashboardScheduleTotal> scheduleTotals = new Dictionary<string, DashboardScheduleTotal>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, DashboardLineTotal> lineTotals = new Dictionary<string, DashboardLineTotal>(StringComparer.OrdinalIgnoreCase);
            List<DashboardPaymentItem> recentPayments = new List<DashboardPaymentItem>();
            List<DashboardBalanceItem> balanceItems = new List<DashboardBalanceItem>();
            decimal[] monthlyCollections = new decimal[12];
            Dictionary<int, DashboardYearTotal> yearTotals = new Dictionary<int, DashboardYearTotal>();
            int chartYear = hasYearFilter
                ? selectedYear
                : (database.Settings != null && database.Settings.DefaultReportYear > 0 ? database.Settings.DefaultReportYear : DateTime.Today.Year);

            foreach (BusinessOwner owner in owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (!yearTotals.ContainsKey(assessment.Year))
                    {
                        yearTotals[assessment.Year] = new DashboardYearTotal(assessment.Year);
                    }

                    yearTotals[assessment.Year].Assessment += assessment.TotalAssessment;
                    yearTotals[assessment.Year].Paid += assessment.TotalPaid;
                    yearTotals[assessment.Year].Balance += assessment.Balance;

                    if (assessment.Payments != null)
                    {
                        foreach (PaymentRecord payment in assessment.Payments)
                        {
                            if (payment.DatePaid.Year == chartYear)
                            {
                                int monthIndex = Math.Max(0, Math.Min(11, payment.DatePaid.Month - 1));
                                monthlyCollections[monthIndex] += payment.Amount;
                            }
                        }
                    }

                    if (hasYearFilter && assessment.Year != selectedYear)
                    {
                        continue;
                    }

                    totalAssessment += assessment.TotalAssessment;
                    totalPaid += assessment.TotalPaid;
                    totalBalance += assessment.Balance;

                    if (assessment.Balance > 0m)
                    {
                        balanceItems.Add(new DashboardBalanceItem(owner, assessment));
                    }

                    string line = String.IsNullOrEmpty(Safe(owner.LineOfBusiness)) ? "(blank)" : Safe(owner.LineOfBusiness);
                    if (!lineTotals.ContainsKey(line))
                    {
                        lineTotals[line] = new DashboardLineTotal(line);
                    }

                    lineTotals[line].Assessment += assessment.TotalAssessment;
                    lineTotals[line].Paid += assessment.TotalPaid;
                    lineTotals[line].Balance += assessment.Balance;

                    if (assessment.Payments == null)
                    {
                        continue;
                    }

                    foreach (PaymentRecord payment in assessment.Payments)
                    {
                        string schedule = String.IsNullOrEmpty(Safe(payment.Schedule)) ? "(blank)" : Safe(payment.Schedule);
                        if (!scheduleTotals.ContainsKey(schedule))
                        {
                            scheduleTotals[schedule] = new DashboardScheduleTotal(schedule);
                        }

                        scheduleTotals[schedule].PaymentCount++;
                        scheduleTotals[schedule].Amount += payment.Amount;
                        recentPayments.Add(new DashboardPaymentItem(owner, assessment, payment));
                    }
                }
            }

            dashboardBusinessCountLabel.Text = owners.Count.ToString("N0");
            dashboardActiveCountLabel.Text = activeCount.ToString("N0");
            dashboardClosedCountLabel.Text = closedCount.ToString("N0");
            dashboardDelinquentCountLabel.Text = delinquentCount.ToString("N0");
            dashboardAssessmentLabel.Text = Money(totalAssessment);
            dashboardPaidLabel.Text = Money(totalPaid);
            dashboardBalanceLabel.Text = Money(totalBalance);
            dashboardCollectionRateLabel.Text = totalAssessment <= 0m ? "0.00%" : (totalPaid / totalAssessment).ToString("P2");

            RefreshDashboardCharts(monthlyCollections, yearTotals, chartYear);
            RefreshDashboardGrids(scheduleTotals, lineTotals, recentPayments, balanceItems);
        }

        private void RefreshDashboardCharts(decimal[] monthlyCollections, Dictionary<int, DashboardYearTotal> yearTotals, int chartYear)
        {
            RefreshMonthlyCollectionsChart(monthlyCollections, chartYear);
            RefreshYearComparisonChart(yearTotals);
        }

        private void RefreshMonthlyCollectionsChart(decimal[] monthlyCollections, int chartYear)
        {
            if (dashboardMonthlyCollectionsChart == null)
            {
                return;
            }

            dashboardMonthlyCollectionsChart.Series.Clear();
            dashboardMonthlyCollectionsChart.Titles[0].Text = "Monthly Collections - " + chartYear.ToString();

            Series paidSeries = new Series("Paid");
            paidSeries.ChartType = SeriesChartType.Column;
            paidSeries.Color = Accent;
            paidSeries.BorderWidth = 0;
            paidSeries.IsValueShownAsLabel = false;

            string[] months = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            for (int i = 0; i < months.Length; i++)
            {
                paidSeries.Points.AddXY(months[i], monthlyCollections == null || i >= monthlyCollections.Length ? 0m : monthlyCollections[i]);
            }

            dashboardMonthlyCollectionsChart.Series.Add(paidSeries);
            dashboardMonthlyCollectionsChart.ChartAreas["Main"].AxisX.Interval = 1;
            dashboardMonthlyCollectionsChart.ChartAreas["Main"].AxisY.Minimum = 0;
            dashboardMonthlyCollectionsChart.ChartAreas["Main"].RecalculateAxesScale();
        }

        private void RefreshYearComparisonChart(Dictionary<int, DashboardYearTotal> yearTotals)
        {
            if (dashboardYearComparisonChart == null)
            {
                return;
            }

            dashboardYearComparisonChart.Series.Clear();
            dashboardYearComparisonChart.Titles[0].Text = "Year-over-Year Revenue";

            Series assessmentSeries = MakeRevenueSeries("Assessment", Color.FromArgb(37, 99, 235));
            Series paidSeries = MakeRevenueSeries("Paid", Accent);
            Series balanceSeries = MakeRevenueSeries("Balance", Color.FromArgb(185, 28, 28));

            List<DashboardYearTotal> rows = yearTotals == null
                ? new List<DashboardYearTotal>()
                : yearTotals.Values.OrderBy(item => item.Year).ToList();

            if (rows.Count == 0)
            {
                int year = DateTime.Today.Year;
                assessmentSeries.Points.AddXY(year.ToString(), 0m);
                paidSeries.Points.AddXY(year.ToString(), 0m);
                balanceSeries.Points.AddXY(year.ToString(), 0m);
            }
            else
            {
                foreach (DashboardYearTotal row in rows)
                {
                    string label = row.Year.ToString();
                    assessmentSeries.Points.AddXY(label, row.Assessment);
                    paidSeries.Points.AddXY(label, row.Paid);
                    balanceSeries.Points.AddXY(label, row.Balance);
                }
            }

            dashboardYearComparisonChart.Series.Add(assessmentSeries);
            dashboardYearComparisonChart.Series.Add(paidSeries);
            dashboardYearComparisonChart.Series.Add(balanceSeries);
            dashboardYearComparisonChart.ChartAreas["Main"].AxisX.Interval = 1;
            dashboardYearComparisonChart.ChartAreas["Main"].AxisY.Minimum = 0;
            dashboardYearComparisonChart.ChartAreas["Main"].RecalculateAxesScale();
        }

        private Series MakeRevenueSeries(string name, Color color)
        {
            Series series = new Series(name);
            series.ChartType = SeriesChartType.Column;
            series.Color = color;
            series.BorderWidth = 0;
            series.IsValueShownAsLabel = false;
            return series;
        }

        private void RefreshDashboardGrids(
            Dictionary<string, DashboardScheduleTotal> scheduleTotals,
            Dictionary<string, DashboardLineTotal> lineTotals,
            List<DashboardPaymentItem> recentPayments,
            List<DashboardBalanceItem> balanceItems)
        {
            dashboardQuarterGrid.Rows.Clear();
            foreach (DashboardScheduleTotal total in scheduleTotals.Values.OrderBy(item => ScheduleSort(item.Schedule)).ThenBy(item => item.Schedule))
            {
                dashboardQuarterGrid.Rows.Add(new object[] { total.Schedule, total.PaymentCount.ToString("N0"), Money(total.Amount) });
            }

            dashboardRecentPaymentsGrid.Rows.Clear();
            foreach (DashboardPaymentItem item in recentPayments.OrderByDescending(item => item.Payment.DatePaid).Take(10))
            {
                dashboardRecentPaymentsGrid.Rows.Add(new object[]
                {
                    item.Payment.DatePaid.ToString("yyyy-MM-dd"),
                    Safe(item.Payment.OrNumber),
                    Safe(item.Owner.BusinessName),
                    Safe(item.Payment.Schedule),
                    Money(item.Payment.Amount)
                });
            }

            dashboardTopBalancesGrid.Rows.Clear();
            foreach (DashboardBalanceItem item in balanceItems.OrderByDescending(item => item.Assessment.Balance).Take(10))
            {
                dashboardTopBalancesGrid.Rows.Add(new object[]
                {
                    item.Assessment.Year,
                    Safe(item.Owner.BusinessName),
                    Safe(item.Owner.OwnerName),
                    Money(item.Assessment.Balance),
                    DelinquencyService.AssessmentPaymentStatus(item.Assessment, DateTime.Today)
                });
            }

            dashboardLineBusinessGrid.Rows.Clear();
            foreach (DashboardLineTotal item in lineTotals.Values.OrderByDescending(item => item.Paid).ThenByDescending(item => item.Assessment).Take(10))
            {
                dashboardLineBusinessGrid.Rows.Add(new object[] { item.LineOfBusiness, Money(item.Assessment), Money(item.Paid), Money(item.Balance) });
            }
        }

        private void RefreshDashboardYearOptions()
        {
            if (dashboardYearFilter == null || loading)
            {
                return;
            }

            string selectedYear = dashboardYearFilter.Text;
            string currentYear = DateTime.Today.Year.ToString();

            loading = true;
            try
            {
                dashboardYearFilter.Items.Clear();
                dashboardYearFilter.Items.Add("All");

                List<int> years = database.Owners
                    .Where(owner => owner.Assessments != null)
                    .SelectMany(owner => owner.Assessments)
                    .Select(assessment => assessment.Year)
                    .Distinct()
                    .OrderByDescending(year => year)
                    .ToList();

                foreach (int year in years)
                {
                    dashboardYearFilter.Items.Add(year.ToString());
                }

                if (String.IsNullOrEmpty(selectedYear))
                {
                    selectedYear = dashboardYearFilter.Items.Contains(currentYear) ? currentYear : "All";
                }

                SelectComboValue(dashboardYearFilter, selectedYear, "All");
            }
            finally
            {
                loading = false;
            }
        }

        private void DashboardFilter_Changed(object sender, EventArgs e)
        {
            if (!loading)
            {
                RefreshDashboard();
            }
        }

        private void RunDelinquencyCheck_Click(object sender, EventArgs e)
        {
            DelinquencyResult result = DelinquencyService.RunCheck(database, currentUser);
            if (result.ChangedOwners > 0)
            {
                dataStore.Save(database);
            }

            string selectedOwnerId = selectedOwner == null ? null : selectedOwner.Id;
            RefreshOwnerList(selectedOwnerId);
            RefreshReport();
            RefreshAuditLog();

            MessageBox.Show(
                "Delinquency check complete.\n\nReviewed assessments: " + result.ReviewedAssessments.ToString("N0") +
                "\nDelinquent assessments: " + result.DelinquentAssessments.ToString("N0") +
                "\nOwners newly flagged: " + result.ChangedOwners.ToString("N0"),
                "Delinquency check",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private int ScheduleSort(string schedule)
        {
            if (String.Equals(schedule, "Annual", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (String.Equals(schedule, "1st Qtr", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (String.Equals(schedule, "2nd Qtr", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (String.Equals(schedule, "3rd Qtr", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (String.Equals(schedule, "4th Qtr", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            return 9;
        }

        private class DashboardScheduleTotal
        {
            public string Schedule { get; private set; }
            public int PaymentCount { get; set; }
            public decimal Amount { get; set; }

            public DashboardScheduleTotal(string schedule)
            {
                Schedule = schedule;
            }
        }

        private class DashboardLineTotal
        {
            public string LineOfBusiness { get; private set; }
            public decimal Assessment { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }

            public DashboardLineTotal(string lineOfBusiness)
            {
                LineOfBusiness = lineOfBusiness;
            }
        }

        private class DashboardYearTotal
        {
            public int Year { get; private set; }
            public decimal Assessment { get; set; }
            public decimal Paid { get; set; }
            public decimal Balance { get; set; }

            public DashboardYearTotal(int year)
            {
                Year = year;
            }
        }

        private class DashboardPaymentItem
        {
            public BusinessOwner Owner { get; private set; }
            public YearlyAssessment Assessment { get; private set; }
            public PaymentRecord Payment { get; private set; }

            public DashboardPaymentItem(BusinessOwner owner, YearlyAssessment assessment, PaymentRecord payment)
            {
                Owner = owner;
                Assessment = assessment;
                Payment = payment;
            }
        }

        private class DashboardBalanceItem
        {
            public BusinessOwner Owner { get; private set; }
            public YearlyAssessment Assessment { get; private set; }

            public DashboardBalanceItem(BusinessOwner owner, YearlyAssessment assessment)
            {
                Owner = owner;
                Assessment = assessment;
            }
        }
    }
}
