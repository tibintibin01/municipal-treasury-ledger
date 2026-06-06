using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public class LoginForm : Form
    {
        private readonly LedgerDatabase database;
        private readonly TreasuryDataStore dataStore;
        private TextBox usernameText;
        private TextBox passwordText;
        private Label messageLabel;

        public UserAccount AuthenticatedUser { get; private set; }

        public LoginForm(LedgerDatabase database)
            : this(database, null)
        {
        }

        public LoginForm(LedgerDatabase database, TreasuryDataStore dataStore)
        {
            this.database = database;
            this.dataStore = dataStore;
            Text = "Business Tax & Permit Collection System - Login";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 430;
            Height = 310;
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
            title.Text = "Sign in";
            title.Dock = DockStyle.Top;
            title.Height = 38;
            title.Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(31, 41, 55);
            panel.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "Use your assigned business tax collection account.";
            subtitle.Dock = DockStyle.Top;
            subtitle.Height = 28;
            subtitle.ForeColor = Color.FromArgb(99, 111, 128);
            panel.Controls.Add(subtitle);
            subtitle.BringToFront();

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.Height = 118;
            form.ColumnCount = 2;
            form.RowCount = 2;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            form.Padding = new Padding(0, 12, 0, 0);

            usernameText = MakeTextBox();
            passwordText = MakeTextBox();
            passwordText.UseSystemPasswordChar = true;
            passwordText.KeyDown += PasswordText_KeyDown;

            AddRow(form, 0, "Username", usernameText);
            AddRow(form, 1, "Password", passwordText);
            panel.Controls.Add(form);
            form.BringToFront();

            messageLabel = new Label();
            messageLabel.Dock = DockStyle.Top;
            messageLabel.Height = 28;
            messageLabel.ForeColor = Color.FromArgb(185, 28, 28);
            panel.Controls.Add(messageLabel);
            messageLabel.BringToFront();

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 44;
            actions.FlowDirection = FlowDirection.RightToLeft;

            Button loginButton = MakeButton("Login");
            loginButton.Click += LoginButton_Click;
            actions.Controls.Add(loginButton);

            panel.Controls.Add(actions);
            actions.BringToFront();
            usernameText.Focus();
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
            Button button = new Button();
            button.Text = text;
            button.Width = 96;
            button.Height = 32;
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(15, 118, 110);
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.FlatAppearance.BorderColor = Color.FromArgb(17, 94, 89);
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

        private void PasswordText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TryLogin();
                e.Handled = true;
            }
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            TryLogin();
        }

        private void TryLogin()
        {
            string username = usernameText.Text.Trim();
            UserAccount user = SecurityService.FindUser(database, username);

            if (SecurityService.IsLockedOut(user))
            {
                messageLabel.Text = SecurityService.LockoutMessage(user);
                LogLoginAttempt(username, "Failed login - account locked");
                SaveLoginState();
                return;
            }

            if (!SecurityService.VerifyPassword(user, passwordText.Text))
            {
                SecurityService.RegisterFailedLogin(user);
                LogLoginAttempt(username, user == null ? "Failed login - unknown user" : "Failed login - invalid password");
                SaveLoginState();
                messageLabel.Text = "Invalid username or password.";
                passwordText.SelectAll();
                passwordText.Focus();
                return;
            }

            SecurityService.RegisterSuccessfulLogin(user);
            if (SecurityService.NeedsPasswordUpgrade(user))
            {
                SecurityService.SetPassword(user, passwordText.Text);
            }

            AuthenticatedUser = user;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void LogLoginAttempt(string username, string action)
        {
            if (database.AuditTrail == null)
            {
                database.AuditTrail = new System.Collections.Generic.List<AuditLogEntry>();
            }

            database.AuditTrail.Add(new AuditLogEntry
            {
                Username = username,
                Role = "",
                Action = action,
                EntityType = "UserAccount",
                EntityId = "",
                Details = "Login screen"
            });
        }

        private void SaveLoginState()
        {
            if (dataStore != null)
            {
                dataStore.Save(database);
            }
        }
    }
}
