using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm
    {
        private TabPage BuildVerificationTab()
        {
            TabPage tab = new TabPage("Verify");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(14);
            host.BackColor = SurfaceBack;

            Label title = new Label();
            title.Text = "Receipt Verification";
            title.Dock = DockStyle.Top;
            title.Height = 38;
            title.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.MiddleLeft;

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.Height = 154;
            form.ColumnCount = 4;
            form.RowCount = 4;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));

            verificationPayloadText = MakeTextBox();
            verificationPayloadText.Multiline = true;
            verificationPayloadText.ScrollBars = ScrollBars.Vertical;
            verificationOrText = MakeTextBox();
            verificationCodeText = MakeTextBox();

            Label payloadLabel = MakeLabel("QR payload");
            payloadLabel.Margin = new Padding(0, 0, 8, 8);
            verificationPayloadText.Margin = new Padding(0, 0, 14, 8);
            form.Controls.Add(payloadLabel, 0, 0);
            form.Controls.Add(verificationPayloadText, 1, 0);
            form.SetColumnSpan(verificationPayloadText, 3);

            AddLabeled(form, 1, 0, "OR number", verificationOrText);
            AddLabeled(form, 1, 2, "Verify code", verificationCodeText);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;

            Button verifyButton = MakeButton("Verify");
            verifyButton.Width = 96;
            verifyButton.Click += VerifyReceipt_Click;
            actions.Controls.Add(verifyButton);

            Button clearButton = MakeButton("Clear");
            clearButton.Width = 86;
            clearButton.Click += ClearVerification_Click;
            actions.Controls.Add(clearButton);

            verificationStatusLabel = new Label();
            verificationStatusLabel.Dock = DockStyle.Fill;
            verificationStatusLabel.Width = 650;
            verificationStatusLabel.ForeColor = TextMuted;
            verificationStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            actions.Controls.Add(verificationStatusLabel);

            form.Controls.Add(actions, 1, 2);
            form.SetColumnSpan(actions, 3);

            verificationGrid = MakeGrid();
            verificationGrid.Columns.Add(MakeColumn("Status", "Status", true));
            verificationGrid.Columns.Add(MakeColumn("OrNumber", "OR Number", true));
            verificationGrid.Columns.Add(MakeColumn("DatePaid", "Date", true));
            verificationGrid.Columns.Add(MakeColumn("Amount", "Amount", true));
            verificationGrid.Columns.Add(MakeColumn("BusinessName", "Business", true));
            verificationGrid.Columns.Add(MakeColumn("OwnerName", "Owner", true));
            verificationGrid.Columns.Add(MakeColumn("ExpectedCode", "Expected", true));
            verificationGrid.Columns.Add(MakeColumn("ProvidedCode", "Provided", true));
            verificationGrid.Columns.Add(MakeColumn("Details", "Details", true));
            verificationGrid.Dock = DockStyle.Fill;
            SetColumnWidth(verificationGrid, "Status", 90);
            SetColumnWidth(verificationGrid, "DatePaid", 90);
            SetColumnWidth(verificationGrid, "Amount", 90);

            host.Controls.Add(verificationGrid);
            host.Controls.Add(form);
            host.Controls.Add(title);
            tab.Controls.Add(host);
            return tab;
        }

        private void VerifyReceipt_Click(object sender, EventArgs e)
        {
            List<ReceiptVerificationResult> results;
            if (!String.IsNullOrWhiteSpace(verificationPayloadText.Text))
            {
                results = ReceiptVerificationService.VerifyQrPayload(database, verificationPayloadText.Text);
            }
            else
            {
                results = ReceiptVerificationService.VerifyManual(database, verificationOrText.Text, verificationCodeText.Text);
            }

            RefreshVerificationResults(results);
            string summary = VerificationSummary(results);
            LogAction("Verify Receipt", "PaymentRecord", "", summary);
            dataStore.Save(database);
            RefreshAuditLog();
        }

        private void ClearVerification_Click(object sender, EventArgs e)
        {
            verificationPayloadText.Text = "";
            verificationOrText.Text = "";
            verificationCodeText.Text = "";
            verificationGrid.Rows.Clear();
            verificationStatusLabel.Text = "";
            verificationPayloadText.Focus();
        }

        private void RefreshVerificationResults(List<ReceiptVerificationResult> results)
        {
            verificationGrid.Rows.Clear();
            if (results == null)
            {
                results = new List<ReceiptVerificationResult>();
            }

            foreach (ReceiptVerificationResult result in results)
            {
                int rowIndex = verificationGrid.Rows.Add(new object[]
                {
                    result.Status,
                    Safe(result.OrNumber),
                    result.DatePaid == DateTime.MinValue ? "" : result.DatePaid.ToString("yyyy-MM-dd"),
                    result.Amount == 0m ? "" : Money(result.Amount),
                    Safe(result.BusinessName),
                    Safe(result.OwnerName),
                    Safe(result.ExpectedCode),
                    Safe(result.ProvidedCode),
                    Safe(result.Details)
                });

                DataGridViewRow row = verificationGrid.Rows[rowIndex];
                if (String.Equals(result.Status, "Verified", StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.ForeColor = AccentDark;
                }
                else if (String.Equals(result.Status, "Mismatch", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(result.Status, "No Match", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(result.Status, "Invalid", StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.ForeColor = Danger;
                }
            }

            verificationStatusLabel.Text = VerificationSummary(results);
            verificationStatusLabel.ForeColor = results.Any(item => String.Equals(item.Status, "Verified", StringComparison.OrdinalIgnoreCase))
                ? AccentDark
                : TextMuted;
        }

        private string VerificationSummary(List<ReceiptVerificationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return "No verification result.";
            }

            int verified = results.Count(item => String.Equals(item.Status, "Verified", StringComparison.OrdinalIgnoreCase));
            int mismatch = results.Count(item => String.Equals(item.Status, "Mismatch", StringComparison.OrdinalIgnoreCase));
            int found = results.Count(item => String.Equals(item.Status, "Found", StringComparison.OrdinalIgnoreCase));
            if (verified > 0)
            {
                return "Verified: " + verified.ToString("N0") + " matching saved payment(s).";
            }

            if (mismatch > 0)
            {
                return "Mismatch: saved payment found, but verification code did not match.";
            }

            if (found > 0)
            {
                return "Found: OR exists. Enter verification code for full verification.";
            }

            return results[0].Status + ": " + results[0].Details;
        }
    }
}
