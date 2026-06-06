using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public class BackupPasswordForm : Form
    {
        private readonly bool confirmPassword;
        private TextBox passwordText;
        private TextBox confirmText;

        public string BackupPassword { get; private set; }

        public BackupPasswordForm(bool confirmPassword)
        {
            this.confirmPassword = confirmPassword;
            Text = confirmPassword ? "Set Backup Password" : "Backup Password";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, confirmPassword ? 220 : 170);
            Font = new Font("Segoe UI", 9.5F);
            BuildControls();
        }

        private void BuildControls()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(18);
            layout.ColumnCount = 2;
            layout.RowCount = confirmPassword ? 5 : 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            if (confirmPassword)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            }

            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            Label note = new Label();
            note.Text = confirmPassword
                ? "This password is required to restore the encrypted backup. Keep it in a safe place."
                : "Enter the password used when this encrypted backup was created.";
            note.Dock = DockStyle.Fill;
            note.ForeColor = Color.FromArgb(99, 111, 128);
            note.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(note, 0, 0);
            layout.SetColumnSpan(note, 2);

            Label passwordLabel = MakeLabel("Password");
            passwordText = MakePasswordTextBox();
            layout.Controls.Add(passwordLabel, 0, 1);
            layout.Controls.Add(passwordText, 1, 1);

            int buttonRow;
            if (confirmPassword)
            {
                Label confirmLabel = MakeLabel("Confirm");
                confirmText = MakePasswordTextBox();
                layout.Controls.Add(confirmLabel, 0, 2);
                layout.Controls.Add(confirmText, 1, 2);
                buttonRow = 4;
            }
            else
            {
                buttonRow = 3;
            }

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Width = 86;
            okButton.Height = 30;
            okButton.Click += OkButton_Click;
            buttons.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Width = 86;
            cancelButton.Height = 30;
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancelButton);

            layout.Controls.Add(buttons, 0, buttonRow);
            layout.SetColumnSpan(buttons, 2);

            AcceptButton = okButton;
            CancelButton = cancelButton;
            Controls.Add(layout);
        }

        private Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            return label;
        }

        private TextBox MakePasswordTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.UseSystemPasswordChar = true;
            return textBox;
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            string password = passwordText.Text;
            if (String.IsNullOrEmpty(password))
            {
                MessageBox.Show("Backup password is required.", "Password required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                passwordText.Focus();
                return;
            }

            if (confirmPassword && password.Length < 8)
            {
                MessageBox.Show("Use at least 8 characters for encrypted backups.", "Weak backup password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                passwordText.Focus();
                return;
            }

            if (confirmPassword && !String.Equals(password, confirmText.Text, StringComparison.Ordinal))
            {
                MessageBox.Show("Backup passwords do not match.", "Password mismatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                confirmText.Focus();
                return;
            }

            BackupPassword = password;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
