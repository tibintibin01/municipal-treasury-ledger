using System;
using System.IO;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string dataPath = Path.Combine(Application.StartupPath, "business-ledger.db");
            string legacyXmlPath = Path.Combine(Application.StartupPath, "business-ledger-data.xml");
            bool isFirstRun = !File.Exists(dataPath) && !File.Exists(legacyXmlPath);
            TreasuryDataStore dataStore;
            LedgerDatabase database;

            try
            {
                dataStore = new TreasuryDataStore(dataPath);
                database = dataStore.Load();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "The ledger database could not be opened.\n\n" + ex.Message,
                    "Database open failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (isFirstRun)
                {
                    SecurityService.EnsureDefaultAdmin(database);
                    dataStore.Save(database);
                    MessageBox.Show(
                        "First-run admin account created.\n\nUsername: admin\nTemporary password: admin123\n\nCreate named user accounts and change this password before real use.",
                        "First-run setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else if (!SecurityService.HasAnyActiveUser(database))
                {
                    MessageBox.Show(
                        "No active user accounts were found. The app will not recreate the default admin account on an existing data file.\n\nRestore a backup or contact the system administrator.",
                        "No active users",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                using (LoginForm login = new LoginForm(database, dataStore))
                {
                    if (login.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    if (database.AuditTrail == null)
                    {
                        database.AuditTrail = new System.Collections.Generic.List<AuditLogEntry>();
                    }

                    database.AuditTrail.Add(new AuditLogEntry
                    {
                        Username = login.AuthenticatedUser.Username,
                        Role = login.AuthenticatedUser.Role,
                        Action = "Login",
                        EntityType = "UserAccount",
                        EntityId = login.AuthenticatedUser.Id,
                        Details = "Successful login"
                    });
                    dataStore.Save(database);

                    if (SecurityService.IsUsingDefaultAdminPassword(login.AuthenticatedUser))
                    {
                        using (ChangePasswordForm changePassword = new ChangePasswordForm(database, dataStore, login.AuthenticatedUser, true))
                        {
                            if (changePassword.ShowDialog() != DialogResult.OK)
                            {
                                return;
                            }
                        }
                    }

                    try
                    {
                        BackupService.RunDailyAutoBackup(dataStore, database, login.AuthenticatedUser);
                    }
                    catch (Exception ex)
                    {
                        new AuditService(database, login.AuthenticatedUser).Log("Auto Backup Failed", "Backup", "", ex.Message);
                        dataStore.Save(database);
                        MessageBox.Show(
                            "Automatic backup could not be created.\n\nCheck the backup folder setting, disk space, or network drive availability.",
                            "Auto backup failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    DelinquencyResult delinquencyResult = DelinquencyService.RunCheck(database, login.AuthenticatedUser);
                    if (delinquencyResult.ChangedOwners > 0)
                    {
                        dataStore.Save(database);
                    }

                    Application.Run(new MainForm(dataStore, database, login.AuthenticatedUser));
                }
            }
            finally
            {
                dataStore.CleanupTemporaryDatabase();
            }
        }
    }
}
