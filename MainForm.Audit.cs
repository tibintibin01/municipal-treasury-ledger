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

        private TabPage BuildAuditTab()
        {
            TabPage tab = new TabPage("Audit Log");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(12);
            host.BackColor = SurfaceBack;

            Label title = new Label();
            title.Text = "Recent create, update, delete, backup, restore, and export actions";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.MiddleLeft;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Right;
            actions.Width = 380;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;

            Button verifyButton = MakeButton("Verify Chain");
            verifyButton.Width = 112;
            verifyButton.Click += VerifyAuditChain_Click;
            actions.Controls.Add(verifyButton);

            Button printButton = MakeButton("Print Audit");
            printButton.Width = 104;
            printButton.Click += PrintAudit_Click;
            actions.Controls.Add(printButton);

            Button exportButton = MakeButton("Export Audit");
            exportButton.Width = 112;
            exportButton.Click += ExportAudit_Click;
            actions.Controls.Add(exportButton);

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 42;
            header.BackColor = SurfaceBack;
            header.Controls.Add(title);
            header.Controls.Add(actions);

            auditChainStatusLabel = new Label();
            auditChainStatusLabel.Dock = DockStyle.Bottom;
            auditChainStatusLabel.Height = 24;
            auditChainStatusLabel.ForeColor = TextMuted;
            auditChainStatusLabel.Font = new Font("Segoe UI", 8.6F);
            auditChainStatusLabel.TextAlign = ContentAlignment.MiddleLeft;

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.SplitterDistance = 330;
            split.SplitterWidth = 6;

            auditGrid = MakeGrid();
            auditGrid.SelectionChanged += AuditGrid_SelectionChanged;
            auditGrid.Columns.Add(MakeColumn("Id", "Id", false));
            auditGrid.Columns.Add(MakeColumn("Timestamp", "Date / Time", true));
            auditGrid.Columns.Add(MakeColumn("Username", "User", true));
            auditGrid.Columns.Add(MakeColumn("Role", "Role", true));
            auditGrid.Columns.Add(MakeColumn("Action", "Action", true));
            auditGrid.Columns.Add(MakeColumn("EntityType", "Record Type", true));
            auditGrid.Columns.Add(MakeColumn("Details", "Details", true));
            auditGrid.Dock = DockStyle.Fill;

            Panel detailPanel = new Panel();
            detailPanel.Dock = DockStyle.Fill;
            detailPanel.Padding = new Padding(0, 10, 0, 0);
            detailPanel.BackColor = SurfaceBack;

            Label detailTitle = new Label();
            detailTitle.Dock = DockStyle.Top;
            detailTitle.Height = 30;
            detailTitle.Text = "Selected entry field changes";
            detailTitle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            detailTitle.ForeColor = TextMain;
            detailTitle.TextAlign = ContentAlignment.MiddleLeft;

            auditDetailsGrid = MakeGrid();
            auditDetailsGrid.Columns.Add(MakeColumn("FieldName", "Field", true));
            auditDetailsGrid.Columns.Add(MakeColumn("OldValue", "Before", true));
            auditDetailsGrid.Columns.Add(MakeColumn("NewValue", "After", true));
            auditDetailsGrid.Dock = DockStyle.Fill;

            detailPanel.Controls.Add(auditDetailsGrid);
            detailPanel.Controls.Add(detailTitle);
            split.Panel1.Controls.Add(auditGrid);
            split.Panel2.Controls.Add(detailPanel);

            host.Controls.Add(split);
            host.Controls.Add(auditChainStatusLabel);
            host.Controls.Add(header);
            tab.Controls.Add(host);
            return tab;
        }

        private void RefreshAuditLog()
        {
            if (auditGrid == null)
            {
                return;
            }

            auditGrid.Rows.Clear();
            if (database.AuditTrail == null)
            {
                return;
            }

            foreach (AuditLogEntry entry in database.AuditTrail.OrderByDescending(item => item.Timestamp).Take(500))
            {
                auditGrid.Rows.Add(new object[]
                {
                    entry.Id,
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    Safe(entry.Username),
                    Safe(entry.Role),
                    Safe(entry.Action),
                    Safe(entry.EntityType),
                    Safe(entry.Details)
                });
            }

            RefreshAuditDetails();
            RefreshAuditChainStatus(false);
        }

        private void AuditGrid_SelectionChanged(object sender, EventArgs e)
        {
            RefreshAuditDetails();
        }

        private void RefreshAuditDetails()
        {
            if (auditDetailsGrid == null)
            {
                return;
            }

            auditDetailsGrid.Rows.Clear();
            if (auditGrid == null || auditGrid.CurrentRow == null || database == null || database.AuditTrail == null)
            {
                return;
            }

            string auditId = Convert.ToString(auditGrid.CurrentRow.Cells["Id"].Value);
            AuditLogEntry entry = database.AuditTrail.FirstOrDefault(item => item.Id == auditId);
            if (entry == null || entry.ChangeDetails == null || entry.ChangeDetails.Count == 0)
            {
                return;
            }

            foreach (AuditLogDetail detail in entry.ChangeDetails)
            {
                auditDetailsGrid.Rows.Add(new object[]
                {
                    Safe(detail.FieldName),
                    Safe(detail.OldValue),
                    Safe(detail.NewValue)
                });
            }
        }

        private void PrintAudit_Click(object sender, EventArgs e)
        {
            PrintableReportService.PreviewAuditReport(this, database, currentUser);
            LogAction("Print Audit Report", "AuditLog", "", "Printed audit trail review");
            dataStore.Save(database);
            RefreshAuditLog();
        }

        private void VerifyAuditChain_Click(object sender, EventArgs e)
        {
            AuditHashVerificationResult result = AuditHashService.Verify(database);
            RefreshAuditChainStatus(true);
            MessageBox.Show(
                result.Message + "\n\nEntries checked: " + result.EntryCount.ToString("N0") +
                "\nStored tip: " + ShortHash(result.StoredTipHash) +
                "\nComputed tip: " + ShortHash(result.CurrentTipHash),
                result.IsValid ? "Audit chain verified" : "Audit chain warning",
                MessageBoxButtons.OK,
                result.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void ExportAudit_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Export Audit CSV";
            dialog.Filter = "CSV files (*.csv)|*.csv";
            dialog.FileName = "treasury-audit-log-" + DateTime.Today.ToString("yyyyMMdd") + ".csv";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            dataStore.ExportAuditCsv(database, dialog.FileName);
            LogAction("Export Audit CSV", "AuditLog", "", dialog.FileName);
            dataStore.Save(database);
            RefreshAuditLog();
            MessageBox.Show("Exported audit CSV report.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RefreshAuditChainStatus(bool fullMessage)
        {
            if (auditChainStatusLabel == null)
            {
                return;
            }

            AuditHashVerificationResult result = AuditHashService.Verify(database);
            auditChainStatusLabel.ForeColor = result.IsValid ? TextMuted : Danger;
            string tip = ShortHash(result.CurrentTipHash);
            auditChainStatusLabel.Text = result.IsValid
                ? "Audit chain verified: " + result.EntryCount.ToString("N0") + " entries" + (String.IsNullOrEmpty(tip) ? "" : " | Tip " + tip)
                : "Audit chain warning: " + (fullMessage ? result.Message : "click Verify Chain");
        }

        private string ShortHash(string hash)
        {
            if (String.IsNullOrEmpty(hash))
            {
                return "";
            }

            return hash.Length <= 16 ? hash : hash.Substring(0, 16);
        }
    }
}
