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

        private Control BuildOwnerPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = SidebarBack;
            panel.Padding = new Padding(12);
            panel.ColumnCount = 1;
            panel.RowCount = 7;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

            Label title = new Label();
            title.Text = "Business Records";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.MiddleLeft;
            panel.Controls.Add(title, 0, 0);

            Label searchLabel = MakeLabel("Search");
            searchLabel.ForeColor = TextMuted;
            panel.Controls.Add(searchLabel, 0, 1);

            searchBox = MakeTextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.TextChanged += SearchBox_TextChanged;
            panel.Controls.Add(searchBox, 0, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;

            Button newButton = MakeButton("New");
            newButton.Click += NewOwner_Click;
            buttons.Controls.Add(newButton);

            Button saveButton = MakeButton("Save");
            saveButton.Click += SaveOwner_Click;
            buttons.Controls.Add(saveButton);

            Button deleteButton = MakeButton("Delete");
            deleteButton.Click += DeleteOwner_Click;
            buttons.Controls.Add(deleteButton);

            panel.Controls.Add(buttons, 0, 3);

            ownerList = MakeGrid();
            ownerList.Dock = DockStyle.Fill;
            ownerList.BorderStyle = BorderStyle.None;
            ownerList.BackColor = SidebarBack;
            ownerList.ColumnHeadersVisible = true;
            ownerList.RowTemplate.Height = 34;
            ownerList.DefaultCellStyle.Font = new Font("Segoe UI", 8.6F);
            ownerList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.2F, FontStyle.Bold);
            ownerList.Columns.Add(MakeColumn("business", "Business", true));
            ownerList.Columns.Add(MakeColumn("owner", "Owner", true));
            ownerList.Columns.Add(MakeColumn("status", "Status", true));
            ownerList.Columns.Add(MakeColumn("year", "Year", true));
            ownerList.Columns.Add(MakeColumn("balance", "Balance", true));
            ownerList.Columns.Add(MakeColumn("id", "Id", false));
            ownerList.Columns["balance"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            ownerList.Columns["year"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn column in ownerList.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Programmatic;
            }

            ownerList.ScrollBars = ScrollBars.Vertical;
            ownerList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            SetColumnFill(ownerList, "business", 32, 72);
            SetColumnFill(ownerList, "owner", 24, 64);
            SetColumnFill(ownerList, "status", 18, 58);
            SetColumnFill(ownerList, "year", 12, 46);
            SetColumnFill(ownerList, "balance", 18, 64);
            ownerList.SelectionChanged += OwnerList_SelectedIndexChanged;
            ownerList.ColumnHeaderMouseClick += OwnerList_ColumnHeaderMouseClick;
            panel.Controls.Add(ownerList, 0, 4);

            ownerSearchCountLabel = new Label();
            ownerSearchCountLabel.Dock = DockStyle.Fill;
            ownerSearchCountLabel.ForeColor = TextMuted;
            ownerSearchCountLabel.Font = new Font("Segoe UI", 8.6F);
            ownerSearchCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            ownerSearchCountLabel.Text = "0 records";
            panel.Controls.Add(ownerSearchCountLabel, 0, 5);

            dataFileLabel = new Label();
            dataFileLabel.Dock = DockStyle.Fill;
            dataFileLabel.ForeColor = TextMuted;
            dataFileLabel.Font = new Font("Segoe UI", 8.5F);
            dataFileLabel.Text = dataStore.UsesEncryptedContainer
                ? "Data file: " + Path.GetFileName(dataStore.EncryptedContainerPath)
                : "Data file: " + Path.GetFileName(dataStore.FilePath);
            dataFileLabel.TextAlign = ContentAlignment.BottomLeft;
            panel.Controls.Add(dataFileLabel, 0, 6);

            return panel;
        }

        private TabPage BuildProfileTab()
        {
            TabPage tab = new TabPage("Owner");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;
            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(24);
            host.BackColor = SurfaceBack;

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Top;
            grid.AutoSize = true;
            grid.ColumnCount = 4;
            grid.RowCount = 9;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 9; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 76 : 36));
            }

            ownerNameText = MakeTextBox();
            businessNameText = MakeTextBox();
            ownerAddressText = MakeTextBox();
            businessAddressText = MakeTextBox();
            contactNumberText = MakeTextBox();
            lineOfBusinessText = MakeTextBox();
            tinText = MakeTextBox();
            statusCombo = MakeComboBox(new string[] { "Active", "Closed", "Delinquent", "Transferred" });
            registrationTypeCombo = MakeComboBox(new string[] { "Renewal", "New Registration" });
            ownerRemarksText = MakeTextBox();
            ownerRemarksText.Multiline = true;
            privacyConsentCheck = new CheckBox();
            privacyConsentCheck.Dock = DockStyle.Fill;
            privacyConsentCheck.Text = "Privacy notice accepted";
            privacyConsentCheck.ForeColor = TextMain;
            privacyConsentCheck.BackColor = SurfaceBack;
            privacyConsentDatePicker = new DateTimePicker();
            privacyConsentDatePicker.Dock = DockStyle.Fill;
            privacyConsentDatePicker.Format = DateTimePickerFormat.Short;
            privacyConsentMethodCombo = MakeComboBox(new string[] { "", "Written form", "Verbal notice", "Online form", "Imported legacy record" });
            privacyNoticeVersionText = MakeTextBox();
            privacyNoticeVersionText.Text = "RA10173-v1";

            AddLabeled(grid, 0, 0, "Owner name", ownerNameText);
            AddLabeled(grid, 0, 2, "Business name", businessNameText);
            AddLabeled(grid, 1, 0, "Owner address", ownerAddressText);
            AddLabeled(grid, 1, 2, "Business address", businessAddressText);
            AddLabeled(grid, 2, 0, "Contact no.", contactNumberText);
            AddLabeled(grid, 2, 2, "Line of business", lineOfBusinessText);
            AddLabeled(grid, 3, 0, "TIN", tinText);
            AddLabeled(grid, 3, 2, "Status", statusCombo);
            AddLabeled(grid, 4, 0, "Type", registrationTypeCombo);
            AddLabeled(grid, 5, 0, "Remarks", ownerRemarksText);
            grid.SetColumnSpan(ownerRemarksText, 3);
            Label privacySectionLabel = new Label();
            privacySectionLabel.Text = "Data Privacy";
            privacySectionLabel.Dock = DockStyle.Fill;
            privacySectionLabel.TextAlign = ContentAlignment.MiddleLeft;
            privacySectionLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            privacySectionLabel.ForeColor = Accent;
            grid.Controls.Add(privacySectionLabel, 0, 6);
            grid.SetColumnSpan(privacySectionLabel, 4);

            AddLabeled(grid, 7, 0, "Consent", privacyConsentCheck);
            AddLabeled(grid, 7, 2, "Consent date", privacyConsentDatePicker);
            AddLabeled(grid, 8, 0, "Method", privacyConsentMethodCombo);
            AddLabeled(grid, 8, 2, "Notice version", privacyNoticeVersionText);

            Label privacyNotice = new Label();
            privacyNotice.Text = "Privacy notice: personal and business information is collected and processed for business registration, renewal assessment, collection, reporting, audit, and legally required LGU records under RA 10173.";
            privacyNotice.Dock = DockStyle.Top;
            privacyNotice.Height = 46;
            privacyNotice.Padding = new Padding(0, 8, 0, 0);
            privacyNotice.ForeColor = TextMuted;
            privacyNotice.Font = new Font("Segoe UI", 8.7F);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 44;
            actions.Padding = new Padding(0, 10, 0, 0);

            Button saveButton = MakeButton("Save Owner");
            saveButton.Width = 120;
            saveButton.Click += SaveOwner_Click;
            actions.Controls.Add(saveButton);

            Button newButton = MakeButton("New Owner");
            newButton.Width = 120;
            newButton.Click += NewOwner_Click;
            actions.Controls.Add(newButton);

            host.Controls.Add(actions);
            host.Controls.Add(privacyNotice);
            host.Controls.Add(grid);
            tab.Controls.Add(host);
            return tab;
        }

        private void RefreshOwnerList(string selectOwnerId)
        {
            if (loading)
            {
                return;
            }

            loading = true;
            try
            {
                string term = (searchBox == null || searchBox.Text == null) ? "" : searchBox.Text.Trim().ToLowerInvariant();
                List<BusinessOwner> owners = SortOwners(database.Owners
                    .Where(owner => MatchesOwner(owner, term)))
                    .ToList();

                ownerList.Rows.Clear();
                foreach (BusinessOwner owner in owners)
                {
                    YearlyAssessment latestAssessment = LatestAssessment(owner);
                    int rowIndex = ownerList.Rows.Add(
                        String.IsNullOrWhiteSpace(owner.BusinessName) ? "(No business)" : owner.BusinessName,
                        owner.OwnerName,
                        owner.Status,
                        latestAssessment == null ? "" : latestAssessment.Year.ToString(),
                        latestAssessment == null ? "" : latestAssessment.Balance.ToString("N2"),
                        owner.Id);
                    ownerList.Rows[rowIndex].Tag = owner;
                }

                int selectedIndex = -1;
                if (!String.IsNullOrEmpty(selectOwnerId))
                {
                    for (int i = 0; i < ownerList.Rows.Count; i++)
                    {
                        BusinessOwner item = ownerList.Rows[i].Tag as BusinessOwner;
                        if (item != null && item.Id == selectOwnerId)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }

                ownerList.ClearSelection();
                if (selectedIndex >= 0 && selectedIndex < ownerList.Rows.Count)
                {
                    ownerList.Rows[selectedIndex].Selected = true;
                    ownerList.CurrentCell = ownerList.Rows[selectedIndex].Cells["business"];
                }

                RefreshOwnerSortGlyph();
                RefreshOwnerSearchCount(owners.Count, term);
            }
            finally
            {
                loading = false;
            }

            LoadOwnerToForm(SelectedOwnerFromGrid());
        }

        private void RefreshOwnerSearchCount(int count, string term)
        {
            if (ownerSearchCountLabel == null)
            {
                return;
            }

            string noun = count == 1 ? "record" : "records";
            if (String.IsNullOrWhiteSpace(term))
            {
                ownerSearchCountLabel.Text = count.ToString("N0") + " " + noun;
            }
            else
            {
                ownerSearchCountLabel.Text = count.ToString("N0") + " " + noun + " found";
            }
        }

        private IEnumerable<BusinessOwner> SortOwners(IEnumerable<BusinessOwner> owners)
        {
            bool ascending = ownerSortAscending;
            string column = ownerSortColumn ?? "business";

            if (column == "owner")
            {
                return ascending
                    ? owners.OrderBy(owner => Safe(owner.OwnerName)).ThenBy(owner => Safe(owner.BusinessName))
                    : owners.OrderByDescending(owner => Safe(owner.OwnerName)).ThenByDescending(owner => Safe(owner.BusinessName));
            }

            if (column == "status")
            {
                return ascending
                    ? owners.OrderBy(owner => Safe(owner.Status)).ThenBy(owner => Safe(owner.BusinessName))
                    : owners.OrderByDescending(owner => Safe(owner.Status)).ThenByDescending(owner => Safe(owner.BusinessName));
            }

            if (column == "year")
            {
                return ascending
                    ? owners.OrderBy(owner => LatestYear(owner)).ThenBy(owner => Safe(owner.BusinessName))
                    : owners.OrderByDescending(owner => LatestYear(owner)).ThenBy(owner => Safe(owner.BusinessName));
            }

            if (column == "balance")
            {
                return ascending
                    ? owners.OrderBy(owner => LatestBalance(owner)).ThenBy(owner => Safe(owner.BusinessName))
                    : owners.OrderByDescending(owner => LatestBalance(owner)).ThenBy(owner => Safe(owner.BusinessName));
            }

            return ascending
                ? owners.OrderBy(owner => Safe(owner.BusinessName)).ThenBy(owner => Safe(owner.OwnerName))
                : owners.OrderByDescending(owner => Safe(owner.BusinessName)).ThenByDescending(owner => Safe(owner.OwnerName));
        }

        private YearlyAssessment LatestAssessment(BusinessOwner owner)
        {
            if (owner == null || owner.Assessments == null || owner.Assessments.Count == 0)
            {
                return null;
            }

            return owner.Assessments.OrderByDescending(assessment => assessment.Year).FirstOrDefault();
        }

        private int LatestYear(BusinessOwner owner)
        {
            YearlyAssessment assessment = LatestAssessment(owner);
            return assessment == null ? 0 : assessment.Year;
        }

        private decimal LatestBalance(BusinessOwner owner)
        {
            YearlyAssessment assessment = LatestAssessment(owner);
            return assessment == null ? 0m : assessment.Balance;
        }

        private BusinessOwner SelectedOwnerFromGrid()
        {
            if (ownerList == null || ownerList.SelectedRows.Count == 0)
            {
                return null;
            }

            return ownerList.SelectedRows[0].Tag as BusinessOwner;
        }

        private void RefreshOwnerSortGlyph()
        {
            if (ownerList == null)
            {
                return;
            }

            foreach (DataGridViewColumn column in ownerList.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (ownerList.Columns.Contains(ownerSortColumn))
            {
                ownerList.Columns[ownerSortColumn].HeaderCell.SortGlyphDirection = ownerSortAscending ? SortOrder.Ascending : SortOrder.Descending;
            }
        }

        private bool MatchesOwner(BusinessOwner owner, string term)
        {
            if (String.IsNullOrEmpty(term))
            {
                return true;
            }

            string combined = String.Join(" ", new string[]
            {
                Safe(owner.OwnerName),
                Safe(owner.BusinessName),
                Safe(owner.LineOfBusiness),
                Safe(owner.ContactNumber),
                Safe(owner.Tin),
                Safe(owner.Status),
                Safe(owner.RegistrationType),
                owner.PrivacyConsentGiven ? "privacy consent accepted data privacy ra10173" : "privacy consent missing data privacy ra10173",
                Safe(owner.PrivacyConsentMethod),
                Safe(owner.PrivacyNoticeVersion),
                owner.PrivacyConsentDate.ToString("yyyy-MM-dd"),
                Safe(owner.OwnerAddress),
                Safe(owner.BusinessAddress),
                Safe(owner.Remarks)
            }).ToLowerInvariant();

            if (combined.Contains(term))
            {
                return true;
            }

            if (owner.Assessments == null)
            {
                return false;
            }

            foreach (YearlyAssessment assessment in owner.Assessments)
            {
                if (assessment.Year.ToString().Contains(term) ||
                    Safe(assessment.Remarks).ToLowerInvariant().Contains(term))
                {
                    return true;
                }

                if (assessment.Payments == null)
                {
                    continue;
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
            }

            return false;
        }

        private void LoadOwnerToForm(BusinessOwner owner)
        {
            loading = true;
            try
            {
                selectedOwner = owner;
                if (owner == null)
                {
                    ownerNameText.Text = "";
                    businessNameText.Text = "";
                    ownerAddressText.Text = "";
                    businessAddressText.Text = "";
                    contactNumberText.Text = "";
                    lineOfBusinessText.Text = "";
                    tinText.Text = "";
                    statusCombo.SelectedIndex = 0;
                    registrationTypeCombo.SelectedIndex = 0;
                    ownerRemarksText.Text = "";
                    privacyConsentCheck.Checked = false;
                    privacyConsentDatePicker.Value = DateTime.Today;
                    SelectComboValue(privacyConsentMethodCombo, "", "");
                    privacyNoticeVersionText.Text = "RA10173-v1";
                    selectedAssessment = null;
                }
                else
                {
                    ownerNameText.Text = Safe(owner.OwnerName);
                    businessNameText.Text = Safe(owner.BusinessName);
                    ownerAddressText.Text = Safe(owner.OwnerAddress);
                    businessAddressText.Text = Safe(owner.BusinessAddress);
                    contactNumberText.Text = Safe(owner.ContactNumber);
                    lineOfBusinessText.Text = Safe(owner.LineOfBusiness);
                    tinText.Text = Safe(owner.Tin);
                    SelectComboValue(statusCombo, Safe(owner.Status), "Active");
                    SelectComboValue(registrationTypeCombo, Safe(owner.RegistrationType), "Renewal");
                    ownerRemarksText.Text = Safe(owner.Remarks);
                    privacyConsentCheck.Checked = owner.PrivacyConsentGiven;
                    privacyConsentDatePicker.Value = owner.PrivacyConsentDate == DateTime.MinValue ? DateTime.Today : owner.PrivacyConsentDate;
                    SelectComboValue(privacyConsentMethodCombo, Safe(owner.PrivacyConsentMethod), "");
                    privacyNoticeVersionText.Text = String.IsNullOrWhiteSpace(owner.PrivacyNoticeVersion) ? "RA10173-v1" : owner.PrivacyNoticeVersion;
                    selectedAssessment = null;
                }
            }
            finally
            {
                loading = false;
            }

            RefreshAssessmentsGrid(null);
            RefreshPaymentAssessmentCombo(null);
            RefreshReport();
        }

        private void UpdateOwnerFromFields(BusinessOwner owner)
        {
            owner.OwnerName = ownerNameText.Text.Trim();
            owner.BusinessName = businessNameText.Text.Trim();
            owner.OwnerAddress = ownerAddressText.Text.Trim();
            owner.BusinessAddress = businessAddressText.Text.Trim();
            owner.ContactNumber = contactNumberText.Text.Trim();
            owner.LineOfBusiness = lineOfBusinessText.Text.Trim();
            owner.Tin = tinText.Text.Trim();
            owner.Status = statusCombo.Text.Trim();
            owner.RegistrationType = registrationTypeCombo.Text.Trim();
            owner.Remarks = ownerRemarksText.Text.Trim();
            owner.PrivacyConsentGiven = privacyConsentCheck.Checked;
            owner.PrivacyConsentDate = privacyConsentDatePicker.Value.Date;
            owner.PrivacyConsentMethod = privacyConsentMethodCombo.Text.Trim();
            owner.PrivacyNoticeVersion = privacyNoticeVersionText.Text.Trim();
        }

        private void NewOwner_Click(object sender, EventArgs e)
        {
            selectedOwner = new BusinessOwner();
            ownerList.ClearSelection();
            LoadOwnerToForm(selectedOwner);
            ownerNameText.Focus();
        }

        private void SaveOwner_Click(object sender, EventArgs e)
        {
            SaveSelectedOwner(true);
        }

        private bool SaveSelectedOwner(bool showMessage)
        {
            BusinessOwner beforeOwner = null;
            if (selectedOwner != null && database.Owners.Any(owner => owner.Id == selectedOwner.Id))
            {
                beforeOwner = AuditChangeFormatter.CloneOwner(selectedOwner);
            }

            if (selectedOwner == null)
            {
                selectedOwner = new BusinessOwner();
            }

            UpdateOwnerFromFields(selectedOwner);

            string message = "";
            bool isNew = false;
            bool saved = false;
            RunWithBusy("Saving owner record...", delegate
            {
                saved = CreateOwnerService().SaveOwner(selectedOwner, beforeOwner, out message, out isNew);
            });

            if (!saved)
            {
                MessageBox.Show(message, "Owner not saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusOwnerValidation(message);
                return false;
            }

            RefreshOwnerList(selectedOwner.Id);
            RefreshReport();
            RefreshAuditLog();
            ClearValidation();

            if (showMessage)
            {
                MessageBox.Show("Business owner saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            return true;
        }

        private void DeleteOwner_Click(object sender, EventArgs e)
        {
            if (selectedOwner == null || !database.Owners.Any(owner => owner.Id == selectedOwner.Id))
            {
                return;
            }

            int assessmentCount = selectedOwner.Assessments == null ? 0 : selectedOwner.Assessments.Count;
            int paymentCount = selectedOwner.Assessments == null
                ? 0
                : selectedOwner.Assessments.Sum(assessment => assessment.Payments == null ? 0 : assessment.Payments.Count);
            string ownerName = String.IsNullOrWhiteSpace(selectedOwner.BusinessName)
                ? selectedOwner.OwnerName
                : selectedOwner.BusinessName;

            DialogResult result = MessageBox.Show(
                "Delete this business owner?\n\n" +
                Safe(ownerName) + "\n\n" +
                "This will permanently remove:\n" +
                "- " + assessmentCount.ToString("N0") + " assessment record(s)\n" +
                "- " + paymentCount.ToString("N0") + " payment record(s)\n\n" +
                "This cannot be undone except by restoring a backup.",
                "Confirm owner delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            string message = "";
            bool deleted = false;
            RunWithBusy("Deleting owner record...", delegate
            {
                deleted = CreateOwnerService().DeleteOwner(selectedOwner, currentUser, out message);
            });

            if (!deleted)
            {
                MessageBox.Show(message, "Owner not deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedOwner = null;
            selectedAssessment = null;
            RefreshOwnerList(null);
            RefreshReport();
            RefreshAuditLog();
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            string currentId = selectedOwner == null ? null : selectedOwner.Id;
            RefreshOwnerList(currentId);
        }

        private void OwnerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loading)
            {
                return;
            }

            LoadOwnerToForm(SelectedOwnerFromGrid());
        }

        private void OwnerList_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= ownerList.Columns.Count)
            {
                return;
            }

            string columnName = ownerList.Columns[e.ColumnIndex].Name;
            if (columnName == "id")
            {
                return;
            }

            if (String.Equals(ownerSortColumn, columnName, StringComparison.OrdinalIgnoreCase))
            {
                ownerSortAscending = !ownerSortAscending;
            }
            else
            {
                ownerSortColumn = columnName;
                ownerSortAscending = true;
            }

            string currentId = selectedOwner == null ? null : selectedOwner.Id;
            RefreshOwnerList(currentId);
        }
    }
}
