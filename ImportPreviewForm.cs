using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public class ImportPreviewForm : Form
    {
        private readonly CsvImportResult result;
        private readonly string filePath;

        public ImportPreviewForm(CsvImportResult result, string filePath)
        {
            this.result = result ?? new CsvImportResult();
            this.filePath = filePath ?? "";
            BuildForm();
        }

        private void BuildForm()
        {
            Text = "Review CSV Import";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(780, 560);
            MinimumSize = new Size(720, 500);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.Padding = new Padding(22);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            Controls.Add(root);

            Label header = new Label();
            header.Dock = DockStyle.Fill;
            header.Text = "CSV Import Preview";
            header.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            header.ForeColor = Color.FromArgb(15, 42, 71);
            header.TextAlign = ContentAlignment.TopLeft;

            Label source = new Label();
            source.Dock = DockStyle.Fill;
            source.Padding = new Padding(0, 36, 0, 0);
            source.Text = "Source: " + (String.IsNullOrEmpty(filePath) ? "(unknown file)" : Path.GetFileName(filePath));
            source.ForeColor = Color.FromArgb(72, 90, 112);

            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Controls.Add(source);
            headerPanel.Controls.Add(header);
            root.Controls.Add(headerPanel, 0, 0);

            TableLayoutPanel summary = new TableLayoutPanel();
            summary.Dock = DockStyle.Fill;
            summary.ColumnCount = 4;
            summary.RowCount = 2;
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            summary.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            root.Controls.Add(summary, 0, 1);

            AddMetric(summary, 0, 0, "Rows read", result.RowsRead);
            AddMetric(summary, 1, 0, "Owners created", result.OwnersCreated);
            AddMetric(summary, 2, 0, "Owners updated", result.OwnersUpdated);
            AddMetric(summary, 3, 0, "Skipped rows", result.RowsSkipped);
            AddMetric(summary, 0, 1, "Assessments created", result.AssessmentsCreated);
            AddMetric(summary, 1, 1, "Assessments updated", result.AssessmentsUpdated);
            AddMetric(summary, 2, 1, "Payments created", result.PaymentsCreated);
            AddMetric(summary, 3, 1, "Validation notes", result.Messages.Count);

            Label notesTitle = new Label();
            notesTitle.Dock = DockStyle.Fill;
            notesTitle.Text = "Validation Notes";
            notesTitle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            notesTitle.ForeColor = Color.FromArgb(15, 42, 71);
            notesTitle.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(notesTitle, 0, 2);

            DataGridView notesGrid = new DataGridView();
            notesGrid.Dock = DockStyle.Fill;
            notesGrid.AllowUserToAddRows = false;
            notesGrid.AllowUserToDeleteRows = false;
            notesGrid.ReadOnly = true;
            notesGrid.RowHeadersVisible = false;
            notesGrid.MultiSelect = false;
            notesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            notesGrid.BackgroundColor = Color.White;
            notesGrid.BorderStyle = BorderStyle.FixedSingle;
            notesGrid.EnableHeadersVisualStyles = false;
            notesGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(218, 242, 239);
            notesGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(15, 42, 71);
            notesGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            notesGrid.GridColor = Color.FromArgb(198, 216, 225);
            notesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            notesGrid.Columns.Add("type", "Type");
            notesGrid.Columns.Add("message", "Message");
            notesGrid.Columns[0].Width = 120;
            notesGrid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            if (result.Messages.Count == 0)
            {
                notesGrid.Rows.Add("Info", "No validation warnings. Review the summary, then continue if the counts look correct.");
            }
            else
            {
                foreach (string message in result.Messages)
                {
                    notesGrid.Rows.Add(MessageType(message), message);
                }
            }

            root.Controls.Add(notesGrid, 0, 3);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(0, 14, 0, 0);
            root.Controls.Add(buttons, 0, 4);

            Button continueButton = MakeButton("Continue Import", Color.FromArgb(14, 121, 112));
            continueButton.DialogResult = DialogResult.OK;
            buttons.Controls.Add(continueButton);

            Button cancelButton = MakeButton("Cancel", Color.FromArgb(92, 109, 125));
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancelButton);

            AcceptButton = continueButton;
            CancelButton = cancelButton;
        }

        private static void AddMetric(TableLayoutPanel parent, int column, int row, string label, int value)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, column == parent.ColumnCount - 1 ? 0 : 10, 10);
            panel.BackColor = Color.FromArgb(247, 249, 252);

            Label title = new Label();
            title.Text = label;
            title.ForeColor = Color.FromArgb(72, 90, 112);
            title.Location = new Point(12, 9);
            title.Size = new Size(160, 18);

            Label number = new Label();
            number.Text = value.ToString("N0");
            number.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold);
            number.ForeColor = Color.FromArgb(15, 42, 71);
            number.Location = new Point(12, 29);
            number.Size = new Size(160, 26);

            panel.Controls.Add(title);
            panel.Controls.Add(number);
            parent.Controls.Add(panel, column, row);
        }

        private static Button MakeButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 138;
            button.Height = 34;
            button.Margin = new Padding(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
            return button;
        }

        private static string MessageType(string message)
        {
            message = message ?? "";
            if (message.IndexOf("skipped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Warning";
            }

            return "Note";
        }
    }
}
