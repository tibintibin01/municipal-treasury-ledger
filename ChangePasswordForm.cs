using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public class ChangePasswordForm : Form
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private readonly UserAccount user;
        private readonly bool forced;

        private TextBox currentPasswordText;
        private TextBox newPasswordText;
        private TextBox confirmPasswordText;
        private Label messageLabel;

        public ChangePasswordForm(LedgerDatabase database, TreasuryDataStore dataStore, UserAccount user, bool forced)
        {
            this.database = database;
            this.dataStore = dataStore;
            this.user = user;
            this.forced = forced;

            Text = forced ? "Change Temporary Password" : "Change Password";
            StartPosition = FormStartPosition.CenterParent;
            Width = 500;
            Height = forced ? 380 : 350;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5F);
            BackColor = Color.FromArgb(246, 248, 251);

            BuildLayout();
        }

        private void BuildLayout()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(28);
            panel.BackColor = Color.White;
            Controls.Add(panel);

            Label title = new Label();
            title.Text = forced ? "Change temporary admin password" : "Change password";
            title.Dock = DockStyle.Top;
            title.Height = 38;
            title.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 41, 55);
            panel.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = forced
                ? "The default admin password must be changed before using the ledger."
                : "Use a password with at least 8 characters, letters, and numbers.";
            subtitle.Dock = DockStyle.Top;
            subtitle.Height = forced ? 48 : 34;
            subtitle.ForeColor = Color.FromArgb(99, 111, 128);
            panel.Controls.Add(subtitle);
            subtitle.BringToFront();

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.Height = 136;
            form.ColumnCount = 2;
            form.RowCount = 3;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.Padding = new Padding(0, 10, 0, 0);

            currentPasswordText = MakeTextBox();
            currentPasswordText.UseSystemPasswordChar = true;

            newPasswordText = MakeTextBox();
            newPasswordText.UseSystemPasswordChar = true;

            confirmPasswordText = MakeTextBox();
            confirmPasswordText.UseSystemPasswordChar = true;
            confirmPasswordText.KeyDown += ConfirmPasswordText_KeyDown;

            AddRow(form, 0, "Current password", currentPasswordText);
            AddRow(form, 1, "New password", newPasswordText);
            AddRow(form, 2, "Confirm", confirmPasswordText);
            panel.Controls.Add(form);
            form.BringToFront();

            messageLabel = new Label();
            messageLabel.Dock = DockStyle.Top;
            messageLabel.Height = 34;
            messageLabel.ForeColor = Color.FromArgb(185, 28, 28);
            panel.Controls.Add(messageLabel);
            messageLabel.BringToFront();

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 44;
            actions.FlowDirection = FlowDirection.RightToLeft;

            Button saveButton = MakeButton("Save");
            saveButton.Click += SaveButton_Click;
            actions.Controls.Add(saveButton);

            Button cancelButton = MakeSecondaryButton(forced ? "Exit" : "Cancel");
            cancelButton.Click += CancelButton_Click;
            actions.Controls.Add(cancelButton);

            panel.Controls.Add(actions);
            actions.BringToFront();
            currentPasswordText.Focus();
        }

        private TextBox MakeTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F);
            return textBox;
        }

        private Button MakeButton(string text)
        {
            Button button = MakeSecondaryButton(text);
            button.BackColor = Color.FromArgb(15, 118, 110);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(17, 94, 89);
            return button;
        }

        private Button MakeSecondaryButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 96;
            button.Height = 32;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(31, 41, 55);
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 228);
            return button;
        }

        private void AddRow(TableLayoutPanel form, int row, string label, Control control)
        {
            Label labelControl = new Label();
            labelControl.Text = label;
            labelControl.Dock = DockStyle.Fill;
            labelControl.TextAlign = ContentAlignment.MiddleLeft;
            labelControl.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            form.Controls.Add(labelControl, 0, row);
            form.Controls.Add(control, 1, row);
        }

        private void ConfirmPasswordText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TrySave();
                e.Handled = true;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            TrySave();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void TrySave()
        {
            string currentPassword = currentPasswordText.Text;
            string newPassword = newPasswordText.Text;
            string confirmPassword = confirmPasswordText.Text;

            if (!SecurityService.VerifyPassword(user, currentPassword))
            {
                ShowMessage("Current password is incorrect.", currentPasswordText);
                return;
            }

            if (String.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            {
                ShowMessage("New password must be different from the current password.", newPasswordText);
                return;
            }

            if (!String.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                ShowMessage("New password and confirmation do not match.", confirmPasswordText);
                return;
            }

            string passwordMessage;
            if (!LedgerValidation.ValidatePasswordStrength(newPassword, out passwordMessage))
            {
                ShowMessage(passwordMessage, newPasswordText);
                return;
            }

            if (SecurityService.IsDefaultAdminPasswordValue(newPassword))
            {
                ShowMessage("Do not use the temporary default admin password.", newPasswordText);
                return;
            }

            SecurityService.SetPassword(user, newPassword);
            new AuditService(database, user).Log(
                "Change Password",
                "UserAccount",
                user.Id,
                forced ? "Changed temporary admin password" : "Changed own password");

            dataStore.Save(database);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowMessage(string message, Control focusControl)
        {
            messageLabel.Text = message;
            if (focusControl != null)
            {
                focusControl.Focus();
            }
        }
    }
}
