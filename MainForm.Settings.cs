using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm
    {
        private TabPage BuildSettingsTab()
        {
            TabPage tab = new TabPage("Settings");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.AutoScroll = true;
            host.Padding = new Padding(16);
            host.BackColor = SurfaceBack;

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.AutoSize = true;
            form.ColumnCount = 4;
            form.RowCount = 12;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 12; i++)
            {
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 7 ? 88 : (i == 10 ? 58 : 40)));
            }

            Label title = new Label();
            title.Text = "LGU Profile and Report Settings";
            title.Dock = DockStyle.Top;
            title.Height = 42;
            title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.MiddleLeft;

            settingsMunicipalityText = MakeTextBox();
            settingsProvinceText = MakeTextBox();
            settingsOfficeText = MakeTextBox();
            settingsTreasurerText = MakeTextBox();
            settingsCollectorText = MakeTextBox();
            settingsDefaultYearText = MakeYearTextBox();
            settingsSealPathText = MakeTextBox();
            settingsTreasurerSignaturePathText = MakeTextBox();
            settingsCollectorSignaturePathText = MakeTextBox();
            settingsFooterNoteText = MakeTextBox();
            settingsFooterNoteText.Multiline = true;
            settingsFooterNoteText.ScrollBars = ScrollBars.Vertical;
            settingsBackupFolderText = MakeTextBox();
            settingsBackupRetentionText = MakeYearTextBox();
            settingsBackupRetentionText.MaxLength = 4;
            settingsLockedFiscalYearsText = MakeTextBox();
            settingsDarkModeCheck = new CheckBox();
            settingsDarkModeCheck.Text = "Use dark mode after restart";
            settingsDarkModeCheck.Dock = DockStyle.Fill;
            settingsDarkModeCheck.ForeColor = TextMain;
            settingsDarkModeCheck.BackColor = SurfaceBack;
            settingsDarkModeCheck.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);

            AddLabeled(form, 0, 0, "Municipality", settingsMunicipalityText);
            AddLabeled(form, 0, 2, "Province", settingsProvinceText);
            AddLabeled(form, 1, 0, "Office", settingsOfficeText);
            AddLabeled(form, 1, 2, "Default year", settingsDefaultYearText);
            AddLabeled(form, 2, 0, "Treasurer", settingsTreasurerText);
            AddLabeled(form, 2, 2, "Collector/Cashier", settingsCollectorText);
            AddLabeled(form, 3, 0, "Backup retention", settingsBackupRetentionText);
            AddLabeled(form, 3, 2, "Locked years", settingsLockedFiscalYearsText);

            Label sealLabel = MakeLabel("Seal/logo path");
            sealLabel.Margin = new Padding(0, 0, 8, 8);
            Control sealPathPanel = MakePathPanel(settingsSealPathText, BrowseSeal_Click);
            form.Controls.Add(sealLabel, 0, 4);
            form.Controls.Add(sealPathPanel, 1, 4);
            form.SetColumnSpan(sealPathPanel, 3);

            Label treasurerSignatureLabel = MakeLabel("Treasurer signature");
            treasurerSignatureLabel.Margin = new Padding(0, 0, 8, 8);
            Control treasurerSignaturePanel = MakePathPanel(settingsTreasurerSignaturePathText, BrowseTreasurerSignature_Click);
            form.Controls.Add(treasurerSignatureLabel, 0, 5);
            form.Controls.Add(treasurerSignaturePanel, 1, 5);
            form.SetColumnSpan(treasurerSignaturePanel, 3);

            Label collectorSignatureLabel = MakeLabel("Collector signature");
            collectorSignatureLabel.Margin = new Padding(0, 0, 8, 8);
            Control collectorSignaturePanel = MakePathPanel(settingsCollectorSignaturePathText, BrowseCollectorSignature_Click);
            form.Controls.Add(collectorSignatureLabel, 0, 6);
            form.Controls.Add(collectorSignaturePanel, 1, 6);
            form.SetColumnSpan(collectorSignaturePanel, 3);

            Label footerLabel = MakeLabel("Report footer");
            footerLabel.Margin = new Padding(0, 0, 8, 8);
            settingsFooterNoteText.Margin = new Padding(0, 0, 14, 8);
            form.Controls.Add(footerLabel, 0, 7);
            form.Controls.Add(settingsFooterNoteText, 1, 7);
            form.SetColumnSpan(settingsFooterNoteText, 3);

            Label backupLabel = MakeLabel("Backup folder");
            backupLabel.Margin = new Padding(0, 0, 8, 8);
            Control backupPathPanel = MakePathPanel(settingsBackupFolderText, BrowseBackupFolder_Click);
            form.Controls.Add(backupLabel, 0, 8);
            form.Controls.Add(backupPathPanel, 1, 8);
            form.SetColumnSpan(backupPathPanel, 3);

            Label themeLabel = MakeLabel("Appearance");
            themeLabel.Margin = new Padding(0, 0, 8, 8);
            form.Controls.Add(themeLabel, 0, 9);
            form.Controls.Add(settingsDarkModeCheck, 1, 9);
            form.SetColumnSpan(settingsDarkModeCheck, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;

            Button saveButton = MakeButton("Save Settings");
            saveButton.Width = 128;
            saveButton.Click += SaveSettings_Click;
            actions.Controls.Add(saveButton);

            Button reloadButton = MakeButton("Reload");
            reloadButton.Width = 92;
            reloadButton.Click += ReloadSettings_Click;
            actions.Controls.Add(reloadButton);

            Button archiveButton = MakeButton("Archive/Purge");
            archiveButton.Width = 124;
            archiveButton.Click += ArchiveOldYears_Click;
            actions.Controls.Add(archiveButton);

            Button restoreLastButton = MakeButton("Restore Last");
            restoreLastButton.Width = 120;
            restoreLastButton.Click += RestoreLastSave_Click;
            actions.Controls.Add(restoreLastButton);

            Label permissionNote = new Label();
            permissionNote.Text = CanManageSettings()
                ? ""
                : "Only Admin or Treasurer users can change settings.";
            permissionNote.Dock = DockStyle.Fill;
            permissionNote.ForeColor = Danger;
            permissionNote.TextAlign = ContentAlignment.MiddleLeft;
            actions.Controls.Add(permissionNote);

            Label protectionNote = new Label();
            protectionNote.Text = "Data protection note: the live ledger is saved as a local SQLite database. Sensitive text/date fields use Windows DPAPI, and routine backups can be encrypted separately. Keep the PC, Windows profile, and backup folders protected; BitLocker is recommended for office use.";
            protectionNote.Dock = DockStyle.Fill;
            protectionNote.ForeColor = TextMuted;
            protectionNote.TextAlign = ContentAlignment.MiddleLeft;
            protectionNote.Padding = new Padding(0, 4, 0, 4);
            form.Controls.Add(protectionNote, 0, 10);
            form.SetColumnSpan(protectionNote, 4);

            form.Controls.Add(actions, 1, 11);
            form.SetColumnSpan(actions, 3);

            Control healthPanel = BuildSystemHealthPanel();
            healthPanel.Dock = DockStyle.Top;
            healthPanel.Height = 300;

            host.Controls.Add(healthPanel);
            host.Controls.Add(form);
            host.Controls.Add(title);
            tab.Controls.Add(host);

            SetSettingsInputEnabled(host, CanManageSettings());
            if (systemHealthGrid != null)
            {
                systemHealthGrid.Enabled = true;
            }

            LoadSettingsToForm();
            RefreshSystemHealth(false);
            return tab;
        }

        private Control BuildSystemHealthPanel()
        {
            Panel panel = new Panel();
            panel.BackColor = SurfaceBack;
            panel.Padding = new Padding(0, 14, 0, 0);

            Label title = new Label();
            title.Text = "System Health";
            title.Dock = DockStyle.Top;
            title.Height = 34;
            title.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.MiddleLeft;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 42;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;

            Button runButton = MakeButton("Run Health Check");
            runButton.Width = 140;
            runButton.Click += RunHealthCheck_Click;
            actions.Controls.Add(runButton);

            Label note = new Label();
            note.Text = "Checks database integrity, encryption, audit chain, backups, locked years, records, and users.";
            note.Dock = DockStyle.Fill;
            note.Width = 720;
            note.ForeColor = TextMuted;
            note.TextAlign = ContentAlignment.MiddleLeft;
            actions.Controls.Add(note);

            systemHealthGrid = MakeGrid();
            systemHealthGrid.Columns.Add(MakeColumn("Area", "Area", true));
            systemHealthGrid.Columns.Add(MakeColumn("Status", "Status", true));
            systemHealthGrid.Columns.Add(MakeColumn("Details", "Details", true));
            systemHealthGrid.Dock = DockStyle.Fill;
            SetColumnWidth(systemHealthGrid, "Area", 170);
            SetColumnWidth(systemHealthGrid, "Status", 90);

            panel.Controls.Add(systemHealthGrid);
            panel.Controls.Add(actions);
            panel.Controls.Add(title);
            return panel;
        }

        private Control MakePathPanel(TextBox textBox, EventHandler browseHandler)
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            textBox.Margin = new Padding(0, 0, 8, 8);

            Button browseButton = MakeButton("Browse");
            browseButton.Width = 86;
            browseButton.Margin = new Padding(0, 0, 14, 8);
            browseButton.Click += browseHandler;

            panel.Controls.Add(textBox, 0, 0);
            panel.Controls.Add(browseButton, 1, 0);
            return panel;
        }

        private bool CanManageSettings()
        {
            return SecurityService.IsAdmin(currentUser) || SecurityService.IsTreasurer(currentUser);
        }

        private void SetSettingsInputEnabled(Control root, bool enabled)
        {
            if (root == null)
            {
                return;
            }

            foreach (Control control in root.Controls)
            {
                if (control is TextBox || control is Button || control is CheckBox)
                {
                    control.Enabled = enabled;
                }

                SetSettingsInputEnabled(control, enabled);
            }
        }

        private void LoadSettingsToForm()
        {
            if (settingsMunicipalityText == null || database == null)
            {
                return;
            }

            if (database.Settings == null)
            {
                database.Settings = new AppSettings();
            }

            settingsMunicipalityText.Text = Safe(database.Settings.MunicipalityName);
            settingsProvinceText.Text = Safe(database.Settings.ProvinceName);
            settingsOfficeText.Text = Safe(database.Settings.OfficeName);
            settingsTreasurerText.Text = Safe(database.Settings.TreasurerName);
            settingsCollectorText.Text = Safe(database.Settings.CollectorName);
            settingsDefaultYearText.Text = database.Settings.DefaultReportYear <= 0
                ? DateTime.Today.Year.ToString()
                : database.Settings.DefaultReportYear.ToString();
            settingsSealPathText.Text = Safe(database.Settings.SealImagePath);
            settingsTreasurerSignaturePathText.Text = Safe(database.Settings.TreasurerSignaturePath);
            settingsCollectorSignaturePathText.Text = Safe(database.Settings.CollectorSignaturePath);
            settingsFooterNoteText.Text = Safe(database.Settings.ReportFooterNote);
            settingsBackupFolderText.Text = Safe(database.Settings.BackupFolderPath);
            settingsBackupRetentionText.Text = database.Settings.AutoBackupRetentionDays <= 0
                ? "30"
                : database.Settings.AutoBackupRetentionDays.ToString();
            settingsLockedFiscalYearsText.Text = Safe(database.Settings.LockedFiscalYears);
            if (settingsDarkModeCheck != null)
            {
                settingsDarkModeCheck.Checked = database.Settings.DarkModeEnabled;
            }
        }

        private void RunHealthCheck_Click(object sender, EventArgs e)
        {
            RunWithBusy("Running system health checks...", delegate
            {
                RefreshSystemHealth(false);
                LogAction("Run Health Check", "SystemHealth", "", "System health checks were run from Settings.");
                dataStore.Save(database);
                RefreshAuditLog();
            });
            MessageBox.Show("System health check complete.", "Health check complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshSystemHealth(bool showMessage)
        {
            if (systemHealthGrid == null)
            {
                return;
            }

            systemHealthGrid.Rows.Clear();
            System.Collections.Generic.List<SystemHealthItem> items = SystemHealthService.RunChecks(dataStore, database);
            int warningCount = 0;
            int errorCount = 0;
            foreach (SystemHealthItem item in items)
            {
                int rowIndex = systemHealthGrid.Rows.Add(new object[]
                {
                    item.Area,
                    item.Status,
                    item.Details
                });

                DataGridViewRow row = systemHealthGrid.Rows[rowIndex];
                if (String.Equals(item.Status, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    errorCount++;
                    row.DefaultCellStyle.ForeColor = Danger;
                }
                else if (String.Equals(item.Status, "Warning", StringComparison.OrdinalIgnoreCase))
                {
                    warningCount++;
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(146, 64, 14);
                }
            }

            if (showMessage)
            {
                MessageBox.Show(
                    "System health check complete.\n\nErrors: " + errorCount.ToString("N0") +
                    "\nWarnings: " + warningCount.ToString("N0"),
                    errorCount > 0 ? "Health check warnings" : "Health check complete",
                    MessageBoxButtons.OK,
                    errorCount > 0 || warningCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            if (!CanManageSettings())
            {
                MessageBox.Show("Only Admin or Treasurer users can change settings.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int defaultYear;
            if (!ReadSettingsNumber(settingsDefaultYearText, "Default report year", 1900, 2100, out defaultYear))
            {
                return;
            }

            int retentionDays;
            if (!ReadSettingsNumber(settingsBackupRetentionText, "Backup retention days", 1, 3650, out retentionDays))
            {
                return;
            }

            string sealPath = settingsSealPathText.Text.Trim();
            if (!String.IsNullOrEmpty(sealPath) && !File.Exists(sealPath))
            {
                string message = "Seal/logo path was not found.";
                MessageBox.Show(message, "Invalid path", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowValidation(settingsSealPathText, message);
                return;
            }

            string treasurerSignaturePath = settingsTreasurerSignaturePathText.Text.Trim();
            if (!ValidateImagePath("Treasurer signature path", treasurerSignaturePath, settingsTreasurerSignaturePathText))
            {
                return;
            }

            string collectorSignaturePath = settingsCollectorSignaturePathText.Text.Trim();
            if (!ValidateImagePath("Collector signature path", collectorSignaturePath, settingsCollectorSignaturePathText))
            {
                return;
            }

            string backupFolder = settingsBackupFolderText.Text.Trim();
            if (!String.IsNullOrEmpty(backupFolder))
            {
                try
                {
                    Directory.CreateDirectory(backupFolder);
                }
                catch (Exception ex)
                {
                    string message = "Backup folder could not be created. " + ex.Message;
                    MessageBox.Show("Backup folder could not be created.\n\n" + ex.Message, "Invalid backup folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ShowValidation(settingsBackupFolderText, message);
                    return;
                }
            }

            string lockedFiscalYears = NormalizeLockedFiscalYearsInput(settingsLockedFiscalYearsText.Text);
            if (!String.IsNullOrWhiteSpace(settingsLockedFiscalYearsText.Text) && String.IsNullOrEmpty(lockedFiscalYears))
            {
                string message = "Locked fiscal years must be valid years between 1900 and 2100, separated by commas or spaces.";
                MessageBox.Show(message, "Invalid fiscal year lock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowValidation(settingsLockedFiscalYearsText, message);
                return;
            }

            if (database.Settings == null)
            {
                database.Settings = new AppSettings();
            }

            AppSettings before = AuditChangeFormatter.CloneSettings(database.Settings);
            bool themeChanged = before != null && before.DarkModeEnabled != settingsDarkModeCheck.Checked;
            database.Settings.MunicipalityName = settingsMunicipalityText.Text.Trim();
            database.Settings.ProvinceName = settingsProvinceText.Text.Trim();
            database.Settings.OfficeName = settingsOfficeText.Text.Trim();
            database.Settings.TreasurerName = settingsTreasurerText.Text.Trim();
            database.Settings.CollectorName = settingsCollectorText.Text.Trim();
            database.Settings.DefaultReportYear = defaultYear;
            database.Settings.SealImagePath = sealPath;
            database.Settings.TreasurerSignaturePath = treasurerSignaturePath;
            database.Settings.CollectorSignaturePath = collectorSignaturePath;
            database.Settings.ReportFooterNote = settingsFooterNoteText.Text.Trim();
            database.Settings.BackupFolderPath = backupFolder;
            database.Settings.AutoBackupRetentionDays = retentionDays;
            database.Settings.LockedFiscalYears = lockedFiscalYears;
            database.Settings.DarkModeEnabled = settingsDarkModeCheck.Checked;
            AppSettings after = AuditChangeFormatter.CloneSettings(database.Settings);

            RunWithBusy("Saving settings...", delegate
            {
                LogAction(
                    "Update LGU Settings",
                    "Settings",
                    "",
                    AuditChangeFormatter.SettingsDetails(before, after),
                    AuditChangeFormatter.SettingsChangeDetails(before, after));
                dataStore.Save(database);
                LoadSettingsToForm();
                RefreshReportFilterOptions();
                if (reportYearFilter != null)
                {
                    SelectComboValue(reportYearFilter, defaultYear.ToString(), "All");
                }

                RefreshReport();
                RefreshAuditLog();
                RefreshSystemHealth(false);
            });
            ClearValidation();
            MessageBox.Show(
                themeChanged
                    ? "Settings saved. Close and reopen the app to apply the appearance change."
                    : "Settings saved.",
                "Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private bool ReadSettingsNumber(TextBox textBox, string fieldName, int minimum, int maximum, out int value)
        {
            value = 0;
            if (textBox == null || !Int32.TryParse(textBox.Text.Trim(), out value) || value < minimum || value > maximum)
            {
                string message = fieldName + " must be between " + minimum + " and " + maximum + ".";
                MessageBox.Show(
                    message,
                    "Invalid setting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (textBox != null)
                {
                    ShowValidation(textBox, message);
                }

                return false;
            }

            return true;
        }

        private bool ValidateImagePath(string fieldName, string path, TextBox textBox)
        {
            if (String.IsNullOrEmpty(path))
            {
                return true;
            }

            if (File.Exists(path))
            {
                return true;
            }

            string message = fieldName + " was not found.";
            MessageBox.Show(message, "Invalid path", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (textBox != null)
            {
                ShowValidation(textBox, message);
            }

            return false;
        }

        private string NormalizeLockedFiscalYearsInput(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "";
            }

            System.Collections.Generic.SortedSet<int> years = new System.Collections.Generic.SortedSet<int>();
            string[] parts = value.Split(new char[] { ',', ';', ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                int year;
                if (!Int32.TryParse(part.Trim(), out year) || year < 1900 || year > 2100)
                {
                    return "";
                }

                years.Add(year);
            }

            return String.Join(", ", years);
        }

        private void ReloadSettings_Click(object sender, EventArgs e)
        {
            LoadSettingsToForm();
        }

        private void BrowseSeal_Click(object sender, EventArgs e)
        {
            BrowseImagePath(settingsSealPathText, "Choose LGU seal or logo");
        }

        private void BrowseTreasurerSignature_Click(object sender, EventArgs e)
        {
            BrowseImagePath(settingsTreasurerSignaturePathText, "Choose treasurer signature image");
        }

        private void BrowseCollectorSignature_Click(object sender, EventArgs e)
        {
            BrowseImagePath(settingsCollectorSignaturePathText, "Choose collector/cashier signature image");
        }

        private void BrowseImagePath(TextBox targetTextBox, string title)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";

            if (targetTextBox != null && !String.IsNullOrEmpty(targetTextBox.Text))
            {
                string directory = Path.GetDirectoryName(targetTextBox.Text);
                if (!String.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    dialog.InitialDirectory = directory;
                }
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                targetTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseBackupFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Choose where automatic daily backups should be saved.";
            dialog.SelectedPath = !String.IsNullOrEmpty(settingsBackupFolderText.Text)
                ? settingsBackupFolderText.Text
                : BackupService.GetEffectiveBackupFolder(dataStore, database);
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                settingsBackupFolderText.Text = dialog.SelectedPath;
            }
        }
    }
}
