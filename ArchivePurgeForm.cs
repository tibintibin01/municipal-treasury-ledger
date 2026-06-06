using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public class ArchivePurgeForm : Form
    {
        private TextBox throughYearText;
        private CheckBox purgeCheck;

        public int ThroughYear { get; private set; }
        public bool PurgeAfterArchive { get; private set; }

        public ArchivePurgeForm()
        {
            ThroughYear = DateTime.Today.Year - 5;
            BuildControls();
        }

        private void BuildControls()
        {
            Text = "Archive Old Year Data";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 250);
            MinimumSize = new Size(520, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5F);
            BackColor = Color.White;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(22);
            layout.ColumnCount = 2;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(layout);

            Label note = new Label();
            note.Text = "Export assessments and payments up to the selected year. Purge is optional and only runs after the archive file is created.";
            note.Dock = DockStyle.Fill;
            note.ForeColor = Color.FromArgb(99, 111, 128);
            note.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(note, 0, 0);
            layout.SetColumnSpan(note, 2);

            Label yearLabel = MakeLabel("Archive through year");
            throughYearText = new TextBox();
            throughYearText.Text = ThroughYear.ToString();
            throughYearText.Dock = DockStyle.Fill;
            layout.Controls.Add(yearLabel, 0, 1);
            layout.Controls.Add(throughYearText, 1, 1);

            purgeCheck = new CheckBox();
            purgeCheck.Text = "Purge archived assessment/payment rows after successful archive export";
            purgeCheck.Dock = DockStyle.Fill;
            purgeCheck.ForeColor = Color.FromArgb(31, 41, 55);
            layout.Controls.Add(purgeCheck, 1, 2);

            Label caution = new Label();
            caution.Text = "Owner profiles are kept. Only yearly assessments and their payment rows for archived years are purged.";
            caution.Dock = DockStyle.Fill;
            caution.ForeColor = Color.FromArgb(185, 28, 28);
            caution.TextAlign = ContentAlignment.TopLeft;
            layout.Controls.Add(caution, 0, 3);
            layout.SetColumnSpan(caution, 2);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);

            Button okButton = MakeButton("Continue", Color.FromArgb(15, 118, 110));
            okButton.Click += OkButton_Click;
            buttons.Controls.Add(okButton);

            Button cancelButton = MakeButton("Cancel", Color.FromArgb(92, 109, 125));
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }

        private static Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            return label;
        }

        private static Button MakeButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 96;
            button.Height = 32;
            button.Margin = new Padding(8, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            return button;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            int year;
            if (!Int32.TryParse((throughYearText.Text ?? "").Trim(), out year) || year < 1900 || year > DateTime.Today.Year)
            {
                MessageBox.Show("Enter a valid archive year from 1900 through the current year.", "Invalid year", MessageBoxButtons.OK, MessageBoxIcon.Information);
                throughYearText.Focus();
                return;
            }

            ThroughYear = year;
            PurgeAfterArchive = purgeCheck.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
