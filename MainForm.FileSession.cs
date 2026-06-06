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

        private void ExportMenu_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export Ledger";
            dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv";
            dialog.DefaultExt = "xlsx";
            dialog.FileName = "business-tax-ledger-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            RunWithBusy("Exporting ledger...", delegate
            {
                string extension = Path.GetExtension(dialog.FileName);
                if (String.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                {
                    dataStore.ExportCsv(database, dialog.FileName);
                    LogAction("Export CSV", "Report", "", dialog.FileName);
                }
                else
                {
                    ExcelExportService.ExportLedger(database, dialog.FileName);
                    LogAction("Export Excel", "Report", "", dialog.FileName);
                }

                dataStore.Save(database);
                RefreshAuditLog();
            });
            MessageBox.Show("Exported ledger report.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ArchiveOldYears_Click(object sender, EventArgs e)
        {
            if (!SecurityService.IsAdmin(currentUser) && !SecurityService.IsTreasurer(currentUser))
            {
                MessageBox.Show("Only Admin or Treasurer users can archive or purge old records.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (ArchivePurgeForm archiveForm = new ArchivePurgeForm())
            {
                if (archiveForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ArchivePurgeResult preview = ArchiveService.Preview(database, archiveForm.ThroughYear);
                if (preview.AssessmentsArchived == 0)
                {
                    MessageBox.Show("No assessments were found through year " + archiveForm.ThroughYear + ".", "Nothing to archive", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Title = "Save Archive Excel File";
                dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx";
                dialog.DefaultExt = "xlsx";
                dialog.FileName = "business-tax-archive-through-" + archiveForm.ThroughYear + "-" + DateTime.Today.ToString("yyyyMMdd") + ".xlsx";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (archiveForm.PurgeAfterArchive)
                {
                    DialogResult confirm = MessageBox.Show(
                        "Archive and purge old assessment/payment rows through year " + archiveForm.ThroughYear + "?\n\n" +
                        preview.AssessmentsArchived.ToString("N0") + " assessment(s) and " +
                        preview.PaymentsArchived.ToString("N0") + " payment row(s) will be archived first, then removed from the live ledger.\n\n" +
                        "Owner profiles will remain.",
                        "Confirm archive and purge",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes)
                    {
                        return;
                    }
                }

                try
                {
                    string safetyBackup = "";
                    ArchivePurgeResult result = preview;
                    RunWithBusy(archiveForm.PurgeAfterArchive ? "Archiving and purging old years..." : "Creating archive workbook...", delegate
                    {
                        if (archiveForm.PurgeAfterArchive)
                        {
                            string safetyFolder = BackupService.GetEffectiveBackupFolder(dataStore, database);
                            Directory.CreateDirectory(safetyFolder);
                            safetyBackup = Path.Combine(safetyFolder, "before-archive-purge-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db");
                            dataStore.CreateBackup(database, safetyBackup);
                        }

                        ExcelExportService.ExportArchive(database, dialog.FileName, archiveForm.ThroughYear);
                        if (archiveForm.PurgeAfterArchive)
                        {
                            result = ArchiveService.Purge(database, archiveForm.ThroughYear);
                        }

                        LogAction(
                            archiveForm.PurgeAfterArchive ? "Archive and Purge Years" : "Archive Years",
                            "Archive",
                            "",
                            "Through year " + archiveForm.ThroughYear + " | " + result.Summary +
                                " | Archive: " + dialog.FileName +
                                (String.IsNullOrEmpty(safetyBackup) ? "" : " | Safety backup: " + safetyBackup));

                        dataStore.Save(database);
                        selectedOwner = null;
                        selectedAssessment = null;
                        RefreshOwnerList(null);
                        RefreshAssessmentFeeCatalogCombo();
                        RefreshReportFilterOptions();
                        RefreshReport();
                        RefreshDashboard();
                        RefreshAuditLog();
                        RefreshSystemHealth(false);
                    });

                    MessageBox.Show(
                        "Archive complete.\n\n" + result.Summary +
                            (String.IsNullOrEmpty(safetyBackup) ? "" : "\n\nSafety backup:\n" + safetyBackup),
                        archiveForm.PurgeAfterArchive ? "Archive and purge complete" : "Archive complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Archive failed.\n\n" + ex.Message, "Archive failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ImportCsv_Click(object sender, EventArgs e)
        {
            if (!SecurityService.IsAdmin(currentUser) && !SecurityService.IsTreasurer(currentUser))
            {
                MessageBox.Show("Only Admin or Treasurer users can import records.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Import Business Ledger";
            dialog.Filter = "Excel workbook (*.xlsx)|*.xlsx|CSV files (*.csv)|*.csv|All supported files (*.xlsx;*.csv)|*.xlsx;*.csv";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                CsvImportResult preview = null;
                RunWithBusy("Reading import file for preview...", delegate
                {
                    preview = CsvImportService.PreviewBusinessLedgerFile(database, dialog.FileName);
                });

                using (ImportPreviewForm previewForm = new ImportPreviewForm(preview, dialog.FileName))
                {
                    if (previewForm.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }
                }

                string safetyPath = "";
                CsvImportResult result = null;
                RunWithBusy("Importing ledger records...", delegate
                {
                    string safetyFolder = BackupService.GetEffectiveBackupFolder(dataStore, database);
                    Directory.CreateDirectory(safetyFolder);
                    string safetyExtension = dataStore.UsesEncryptedContainer ? ".mtdb" : ".db";
                    safetyPath = Path.Combine(safetyFolder, "before-import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + safetyExtension);
                    dataStore.CreateBackup(database, safetyPath);

                    result = CsvImportService.ImportBusinessLedgerFile(database, dialog.FileName);
                    LogAction("Import Ledger File", "Import", "", dialog.FileName + " | " + result.Summary + " | Safety backup: " + safetyPath);
                    dataStore.Save(database);

                    selectedOwner = null;
                    selectedAssessment = null;
                    RefreshOwnerList(null);
                    RefreshAssessmentFeeCatalogCombo();
                    RefreshFeeCatalogGrid(null);
                    RefreshReport();
                    RefreshDashboard();
                    RefreshAuditLog();
                    RefreshSystemHealth(false);
                });

                string message = result.Summary;
                if (result.Messages.Count > 0)
                {
                    message += "\n\nNotes:\n" + String.Join("\n", result.Messages.ToArray());
                }

                MessageBox.Show(message, "Import complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Import failed.\n\nNo import changes were saved after the error.\n\n" + ex.Message,
                    "Import failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BackupData_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Backup Treasury Data";
            dialog.Filter = dataStore.UsesEncryptedContainer
                ? "Encrypted database container (*.mtdb)|*.mtdb|Encrypted treasury backup (*.mtobak)|*.mtobak|Plain SQLite backup (*.db)|*.db"
                : "Encrypted treasury backup (*.mtobak)|*.mtobak|Database backup (*.db)|*.db";
            dialog.DefaultExt = dataStore.UsesEncryptedContainer ? "mtdb" : "mtobak";
            dialog.FileName = "municipal-treasury-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + (dataStore.UsesEncryptedContainer ? ".mtdb" : ".mtobak");
            string backupFolder = BackupService.GetEffectiveBackupFolder(dataStore, database);
            try
            {
                Directory.CreateDirectory(backupFolder);
                dialog.InitialDirectory = backupFolder;
            }
            catch
            {
                dialog.InitialDirectory = "";
            }

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string backupExtension = Path.GetExtension(dialog.FileName);
            BackupVerificationResult verification = null;
            string backupPassword = "";
            if (String.Equals(backupExtension, ".mtobak", StringComparison.OrdinalIgnoreCase))
            {
                using (BackupPasswordForm passwordDialog = new BackupPasswordForm(true))
                {
                    if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    backupPassword = passwordDialog.BackupPassword;
                }
            }

            RunWithBusy("Creating and verifying backup...", delegate
            {
                if (String.Equals(backupExtension, ".mtdb", StringComparison.OrdinalIgnoreCase))
                {
                    dataStore.CreateBackup(database, dialog.FileName);
                    verification = dataStore.VerifyEncryptedContainerBackup(dialog.FileName);
                    LogAction("Backup Encrypted Database Container", "Backup", "", dialog.FileName);
                }
                else if (String.Equals(backupExtension, ".mtobak", StringComparison.OrdinalIgnoreCase))
                {
                    dataStore.Save(database);
                    EncryptedBackupService.CreateEncryptedBackup(dataStore.FilePath, dialog.FileName, backupPassword);
                    verification = BackupVerificationService.VerifyEncryptedBackup(dialog.FileName, backupPassword);
                    LogAction("Encrypted Backup Data", "Backup", "", dialog.FileName);
                }
                else
                {
                    dataStore.CreateBackup(database, dialog.FileName);
                    verification = BackupVerificationService.VerifyPlainSqlite(dialog.FileName);
                    LogAction("Backup Data", "Backup", "", dialog.FileName);
                    dataStore.Save(database);
                }
            });

            if (verification == null || !verification.IsValid)
            {
                string message = verification == null ? "Backup verification did not return a result." : verification.Message;
                LogAction("Backup Verification Failed", "Backup", "", dialog.FileName + " | " + message);
                dataStore.Save(database);
                RefreshAuditLog();
                MessageBox.Show(
                    "Backup was created, but verification failed.\n\nDo not rely on this backup until the issue is fixed.\n\n" + message,
                    "Backup verification failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            LogAction("Backup Verified", "Backup", "", dialog.FileName + " | " + verification.Message);
            dataStore.Save(database);
            RefreshAuditLog();
            MessageBox.Show(
                "Backup saved and verified.\n\n" + dialog.FileName + "\n\n" + verification.Message,
                "Backup complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ConfigureBackupFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Choose where automatic daily backups should be saved.";
            dialog.SelectedPath = BackupService.GetEffectiveBackupFolder(dataStore, database);
            dialog.ShowNewFolderButton = true;

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                BackupService.SetBackupFolder(dataStore, database, currentUser, dialog.SelectedPath);
                LoadSettingsToForm();
                RefreshAuditLog();
                MessageBox.Show(
                    "Backup folder saved.\n\nAutomatic daily backups will be saved here:\n" + dialog.SelectedPath,
                    "Backup folder saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Backup folder could not be saved.\n\n" + ex.Message,
                    "Backup folder error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void RestoreBackup_Click(object sender, EventArgs e)
        {
            if (!SecurityService.CanRestoreBackup(currentUser))
            {
                MessageBox.Show("Only Admin or Treasurer users can restore backups.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Restore Treasury Backup";
            dialog.Filter = "Encrypted treasury backup (*.mtobak)|*.mtobak|Treasury backup (*.db)|*.db|Legacy XML backup (*.xml)|*.xml|All files (*.*)|*.*";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            DialogResult result = MessageBox.Show(
                "Restore this backup?\n\nThe current data file will be replaced, but the app will keep a safety copy before restoring.",
                "Confirm restore",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            string restoredFromPath = dialog.FileName;
            string temporaryDecryptedPath = "";
            BackupVerificationResult verification = null;
            bool isEncryptedBackup = EncryptedBackupService.IsEncryptedBackup(dialog.FileName);
            string restoreBackupPassword = "";
            if (isEncryptedBackup)
            {
                using (BackupPasswordForm passwordDialog = new BackupPasswordForm(false))
                {
                    if (passwordDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    restoreBackupPassword = passwordDialog.BackupPassword;
                }
            }

            try
            {
                RunWithBusy("Restoring backup...", delegate
                {
                    if (isEncryptedBackup)
                    {
                        temporaryDecryptedPath = Path.Combine(Path.GetTempPath(), "municipal-treasury-restore-" + Guid.NewGuid().ToString("N") + ".db");
                        EncryptedBackupService.DecryptBackupToFile(dialog.FileName, temporaryDecryptedPath, restoreBackupPassword);
                        verification = BackupVerificationService.VerifyEncryptedBackup(dialog.FileName, restoreBackupPassword);
                        EnsureRestoreVerificationPassed(verification);
                        dataStore.RestoreBackup(temporaryDecryptedPath);
                    }
                    else
                    {
                        if (String.Equals(Path.GetExtension(dialog.FileName), ".xml", StringComparison.OrdinalIgnoreCase))
                        {
                            verification = BackupVerificationService.VerifyLegacyXml(dialog.FileName);
                        }
                        else
                        {
                            verification = BackupVerificationService.VerifyPlainSqlite(dialog.FileName);
                        }

                        EnsureRestoreVerificationPassed(verification);
                        dataStore.RestoreBackup(dialog.FileName);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Backup could not be restored.\n\n" + ex.Message,
                    "Restore failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                if (!String.IsNullOrEmpty(temporaryDecryptedPath) && File.Exists(temporaryDecryptedPath))
                {
                    try
                    {
                        File.Delete(temporaryDecryptedPath);
                    }
                    catch
                    {
                        // A temp restore file should not block the restored ledger from opening.
                    }
                }
            }

            database = dataStore.Load();
            selectedOwner = null;
            selectedAssessment = null;
            string restoreAction = "Restore Backup";
            if (EncryptedBackupService.IsEncryptedBackup(restoredFromPath))
            {
                restoreAction = "Restore Encrypted Backup";
            }

            LogAction(
                restoreAction,
                "Backup",
                "",
                restoredFromPath + (verification == null ? "" : " | Verified: " + verification.Message));
            dataStore.Save(database);
            RefreshOwnerList(null);
            if (reportYearFilter != null)
            {
                reportYearFilter.Tag = "UseDefaultYear";
            }

            RefreshReport();
            LoadSettingsToForm();
            RefreshAuditLog();
            MessageBox.Show(
                "Backup restored after verification.\n\n" + (verification == null ? "" : verification.Message),
                "Restore complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void EnsureRestoreVerificationPassed(BackupVerificationResult verification)
        {
            if (verification == null || !verification.IsValid)
            {
                throw new InvalidOperationException("Backup verification failed before restore.\n\n" + (verification == null ? "No verification result." : verification.Message));
            }
        }

        private void SaveMenu_Click(object sender, EventArgs e)
        {
            RunWithBusy("Saving data...", delegate
            {
                dataStore.Save(database);
            });
            MessageBox.Show("Data saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RestoreLastSave_Click(object sender, EventArgs e)
        {
            if (!SecurityService.IsAdmin(currentUser) && !SecurityService.IsTreasurer(currentUser))
            {
                MessageBox.Show("Only Admin or Treasurer users can restore the previous save.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string lastSavePath = dataStore.FilePath + ".bak";
            if (!File.Exists(lastSavePath))
            {
                MessageBox.Show("No previous-save safety copy was found yet.", "Restore unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                "Restore the ledger to the previous saved version?\n\n" +
                "The app will keep a safety copy of the current file before restoring.",
                "Confirm restore last save",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                RunWithBusy("Restoring previous save...", delegate
                {
                    BackupVerificationResult verification = BackupVerificationService.VerifyPlainSqlite(lastSavePath);
                    EnsureRestoreVerificationPassed(verification);

                    dataStore.RestoreBackup(lastSavePath);
                    database = dataStore.Load();
                    selectedOwner = null;
                    selectedAssessment = null;

                    LogAction("Restore Previous Save", "Backup", "", lastSavePath + " | Verified before restore");
                    dataStore.Save(database);

                    RefreshOwnerList(null);
                    RefreshAssessmentFeeCatalogCombo();
                    RefreshFeeCatalogGrid(null);
                    RefreshReportFilterOptions();
                    RefreshReport();
                    RefreshDashboard();
                    LoadSettingsToForm();
                    RefreshAuditLog();
                    RefreshSystemHealth(false);
                });

                MessageBox.Show("Previous saved ledger restored.", "Restore complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Previous save could not be restored.\n\n" + ex.Message, "Restore failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ChangePassword_Click(object sender, EventArgs e)
        {
            using (ChangePasswordForm dialog = new ChangePasswordForm(database, dataStore, currentUser, false))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            if (userLabel != null)
            {
                userLabel.Text = CurrentUserText();
                userLabel.ForeColor = CurrentUserTextColor();
            }

            RefreshAuditLog();
            MessageBox.Show("Password changed.", "Password updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleTheme_Click(object sender, EventArgs e)
        {
            if (database.Settings == null)
            {
                database.Settings = new AppSettings();
            }

            bool enableDarkMode = !database.Settings.DarkModeEnabled;
            database.Settings.DarkModeEnabled = enableDarkMode;
            RunWithBusy("Saving theme setting...", delegate
            {
                LogAction(
                    "Change Theme",
                    "Settings",
                    "",
                    enableDarkMode ? "Dark mode enabled" : "Dark mode disabled");
                dataStore.Save(database);
            });

            if (settingsDarkModeCheck != null)
            {
                settingsDarkModeCheck.Checked = enableDarkMode;
            }

            RefreshAuditLog();
            MessageBox.Show(
                (enableDarkMode ? "Dark mode enabled." : "Light mode enabled.") +
                "\n\nClose and reopen the app to apply the appearance change.",
                "Theme saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ExitMenu_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void StartInactivityMonitor()
        {
            lastActivityAt = DateTime.Now;
            Application.AddMessageFilter(this);

            inactivityTimer = new Timer();
            inactivityTimer.Interval = 60000;
            inactivityTimer.Tick += InactivityTimer_Tick;
            inactivityTimer.Start();
        }

        public bool PreFilterMessage(ref Message message)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_KEYUP = 0x0101;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x0201;
            const int WM_RBUTTONDOWN = 0x0204;
            const int WM_MBUTTONDOWN = 0x0207;

            if (message.Msg == WM_KEYDOWN ||
                message.Msg == WM_KEYUP ||
                message.Msg == WM_MOUSEMOVE ||
                message.Msg == WM_LBUTTONDOWN ||
                message.Msg == WM_RBUTTONDOWN ||
                message.Msg == WM_MBUTTONDOWN)
            {
                lastActivityAt = DateTime.Now;
            }

            return false;
        }

        private void InactivityTimer_Tick(object sender, EventArgs e)
        {
            if (lockingForInactivity)
            {
                return;
            }

            if ((DateTime.Now - lastActivityAt).TotalMinutes < InactivityLockMinutes)
            {
                return;
            }

            lockingForInactivity = true;
            if (inactivityTimer != null)
            {
                inactivityTimer.Stop();
            }

            LogAction("Auto Lock", "Session", currentUser == null ? "" : currentUser.Id, "Locked after " + InactivityLockMinutes + " minutes of inactivity");
            dataStore.Save(database);
            MessageBox.Show("Session locked due to inactivity. Reopen the app and sign in again.", "Session locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            if (inactivityTimer != null)
            {
                inactivityTimer.Stop();
                inactivityTimer.Dispose();
                inactivityTimer = null;
            }
        }
    }
}
