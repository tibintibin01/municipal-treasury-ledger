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
        private CheckBox showPasswordCheck;
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
            Width = 760;
            Height = 500;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5F);
            BackColor = Color.FromArgb(246, 248, 251);

            BuildLayout();
        }

        private void BuildLayout()
        {
            TableLayoutPanel shell = new TableLayoutPanel();
            shell.Dock = DockStyle.Fill;
            shell.ColumnCount = 2;
            shell.RowCount = 1;
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 285));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.BackColor = Color.FromArgb(246, 248, 251);
            Controls.Add(shell);

            Panel brandPanel = new Panel();
            brandPanel.Dock = DockStyle.Fill;
            brandPanel.Padding = new Padding(28, 32, 28, 28);
            brandPanel.BackColor = Color.FromArgb(15, 118, 110);
            brandPanel.Paint += BrandPanel_Paint;
            shell.Controls.Add(brandPanel, 0, 0);

            TableLayoutPanel brandLayout = new TableLayoutPanel();
            brandLayout.Dock = DockStyle.Fill;
            brandLayout.ColumnCount = 1;
            brandLayout.RowCount = 4;
            brandLayout.BackColor = Color.Transparent;
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            brandLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            brandPanel.Controls.Add(brandLayout);

            Label appName = new Label();
            appName.Text = "Business Tax & Permit Collection System";
            appName.Dock = DockStyle.Fill;
            appName.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            appName.ForeColor = Color.White;
            appName.TextAlign = ContentAlignment.TopLeft;
            brandLayout.Controls.Add(appName, 0, 0);

            Label appSubtitle = new Label();
            appSubtitle.Text = "Business registration, renewal assessment, and payment records.";
            appSubtitle.Dock = DockStyle.Fill;
            appSubtitle.Font = new Font("Segoe UI", 9.5F);
            appSubtitle.ForeColor = Color.FromArgb(213, 245, 239);
            brandLayout.Controls.Add(appSubtitle, 0, 1);

            Label securityNote = new Label();
            securityNote.Text = "Authorized municipal treasury staff only. Use your own account for audit tracking.";
            securityNote.Dock = DockStyle.Fill;
            securityNote.Font = new Font("Segoe UI", 9F);
            securityNote.ForeColor = Color.FromArgb(213, 245, 239);
            securityNote.TextAlign = ContentAlignment.BottomLeft;
            brandLayout.Controls.Add(securityNote, 0, 3);

            Panel contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Padding = new Padding(42, 38, 42, 30);
            contentPanel.BackColor = Color.White;
            shell.Controls.Add(contentPanel, 1, 0);

            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.ColumnCount = 1;
            content.RowCount = 7;
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            contentPanel.Controls.Add(content);

            Label title = new Label();
            title.Text = "Sign in";
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI Semibold", 21F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(17, 24, 39);
            title.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = "Enter your assigned account to continue.";
            subtitle.Dock = DockStyle.Fill;
            subtitle.ForeColor = Color.FromArgb(99, 111, 128);
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(subtitle, 0, 1);

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Fill;
            form.ColumnCount = 1;
            form.RowCount = 4;
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            form.Padding = new Padding(0, 12, 0, 0);

            usernameText = MakeTextBox();
            passwordText = MakeTextBox();
            passwordText.UseSystemPasswordChar = true;
            passwordText.KeyDown += PasswordText_KeyDown;

            AddField(form, 0, "Username", usernameText);
            AddField(form, 2, "Password", passwordText);
            content.Controls.Add(form, 0, 2);

            showPasswordCheck = new CheckBox();
            showPasswordCheck.Text = "Show password";
            showPasswordCheck.Dock = DockStyle.Fill;
            showPasswordCheck.ForeColor = Color.FromArgb(75, 85, 99);
            showPasswordCheck.CheckedChanged += ShowPasswordCheck_CheckedChanged;
            content.Controls.Add(showPasswordCheck, 0, 3);

            messageLabel = new Label();
            messageLabel.Dock = DockStyle.Fill;
            messageLabel.ForeColor = Color.FromArgb(185, 28, 28);
            messageLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            messageLabel.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(messageLabel, 0, 4);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;

            Button loginButton = MakeButton("Login");
            loginButton.Click += LoginButton_Click;
            actions.Controls.Add(loginButton);
            AcceptButton = loginButton;

            Button closeButton = MakeSecondaryButton("Close");
            closeButton.Click += CloseButton_Click;
            actions.Controls.Add(closeButton);

            content.Controls.Add(actions, 0, 5);

            Label footer = new Label();
            footer.Text = "Forgot your password? Ask the system Admin to reset it.";
            footer.Dock = DockStyle.Fill;
            footer.ForeColor = Color.FromArgb(107, 114, 128);
            footer.Font = new Font("Segoe UI", 8.7F);
            footer.TextAlign = ContentAlignment.BottomLeft;
            content.Controls.Add(footer, 0, 6);
            usernameText.Focus();
        }

        private TextBox MakeTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.Font = new Font("Segoe UI", 10F);
            textBox.Margin = new Padding(0, 0, 0, 8);
            return textBox;
        }

        private Button MakeButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 118;
            button.Height = 36;
            button.Margin = new Padding(10, 4, 0, 4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(15, 118, 110);
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.FlatAppearance.BorderColor = Color.FromArgb(17, 94, 89);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 94, 89);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private Button MakeSecondaryButton(string text)
        {
            Button button = MakeButton(text);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(31, 41, 55);
            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);
            return button;
        }

        private void AddField(TableLayoutPanel form, int labelRow, string label, Control control)
        {
            Label labelControl = new Label();
            labelControl.Text = label;
            labelControl.Dock = DockStyle.Fill;
            labelControl.TextAlign = ContentAlignment.MiddleLeft;
            labelControl.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            labelControl.ForeColor = Color.FromArgb(55, 65, 81);
            form.Controls.Add(labelControl, 0, labelRow);
            form.Controls.Add(control, 0, labelRow + 1);
        }

        private void BrandPanel_Paint(object sender, PaintEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 184, 166)))
            {
                e.Graphics.FillRectangle(brush, 0, 0, 6, Height);
            }
        }

        private void ShowPasswordCheck_CheckedChanged(object sender, EventArgs e)
        {
            passwordText.UseSystemPasswordChar = !showPasswordCheck.Checked;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
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
