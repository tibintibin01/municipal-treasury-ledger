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

        private TabPage BuildUsersTab()
        {
            TabPage tab = new TabPage("Users");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.SplitterDistance = 250;
            split.SplitterWidth = 1;
            split.BackColor = BorderLine;
            split.Panel1.Padding = new Padding(12);
            split.Panel2.Padding = new Padding(12);
            split.Panel1.BackColor = SurfaceBack;
            split.Panel2.BackColor = SurfaceBack;

            usersGrid = MakeGrid();
            usersGrid.Columns.Add(MakeColumn("Id", "Id", false));
            usersGrid.Columns.Add(MakeColumn("Username", "Username", true));
            usersGrid.Columns.Add(MakeColumn("FullName", "Full Name", true));
            usersGrid.Columns.Add(MakeColumn("Role", "Role", true));
            usersGrid.Columns.Add(MakeColumn("Active", "Active", true));
            usersGrid.SelectionChanged += UsersGrid_SelectionChanged;
            split.Panel1.Controls.Add(usersGrid);

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.AutoSize = true;
            form.ColumnCount = 4;
            form.RowCount = 3;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            userUsernameText = MakeTextBox();
            userFullNameText = MakeTextBox();
            userPasswordText = MakeTextBox();
            userPasswordText.UseSystemPasswordChar = true;
            userRoleCombo = MakeComboBox(new string[] { SecurityService.CashierRole, SecurityService.TreasurerRole, SecurityService.AdminRole });
            userActiveCombo = MakeComboBox(new string[] { "Yes", "No" });

            AddLabeled(form, 0, 0, "Username", userUsernameText);
            AddLabeled(form, 0, 2, "Full name", userFullNameText);
            AddLabeled(form, 1, 0, "New password", userPasswordText);
            AddLabeled(form, 1, 2, "Role", userRoleCombo);
            AddLabeled(form, 2, 0, "Active", userActiveCombo);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 44;

            Button newButton = MakeButton("New User");
            newButton.Width = 110;
            newButton.Click += NewUser_Click;
            actions.Controls.Add(newButton);

            Button saveButton = MakeButton("Save User");
            saveButton.Width = 110;
            saveButton.Click += SaveUser_Click;
            actions.Controls.Add(saveButton);

            Label note = new Label();
            note.Text = SecurityService.IsAdmin(currentUser)
                ? "Password is optional when editing an existing user."
                : "Only Admin users can manage accounts.";
            note.Dock = DockStyle.Top;
            note.Height = 34;
            note.ForeColor = SecurityService.IsAdmin(currentUser) ? TextMuted : Danger;
            note.TextAlign = ContentAlignment.MiddleLeft;

            split.Panel2.Controls.Add(actions);
            split.Panel2.Controls.Add(form);
            split.Panel2.Controls.Add(note);

            bool canManage = SecurityService.IsAdmin(currentUser);
            userUsernameText.Enabled = canManage;
            userFullNameText.Enabled = canManage;
            userPasswordText.Enabled = canManage;
            userRoleCombo.Enabled = canManage;
            userActiveCombo.Enabled = canManage;
            foreach (Control control in actions.Controls)
            {
                control.Enabled = canManage;
            }

            tab.Controls.Add(split);
            return tab;
        }

        private void RefreshUsersGrid()
        {
            if (usersGrid == null)
            {
                return;
            }

            usersGrid.Rows.Clear();
            if (database.Users == null)
            {
                return;
            }

            foreach (UserAccount user in database.Users.OrderBy(item => Safe(item.Username)))
            {
                int rowIndex = usersGrid.Rows.Add(new object[]
                {
                    user.Id,
                    Safe(user.Username),
                    Safe(user.FullName),
                    Safe(user.Role),
                    user.IsActive ? "Yes" : "No"
                });
                usersGrid.Rows[rowIndex].Tag = user.Id;
            }
        }

        private void LoadUserToForm(UserAccount user)
        {
            selectedUser = user;
            if (user == null)
            {
                userUsernameText.Text = "";
                userFullNameText.Text = "";
                userPasswordText.Text = "";
                SelectComboValue(userRoleCombo, SecurityService.CashierRole, SecurityService.CashierRole);
                SelectComboValue(userActiveCombo, "Yes", "Yes");
                return;
            }

            userUsernameText.Text = Safe(user.Username);
            userFullNameText.Text = Safe(user.FullName);
            userPasswordText.Text = "";
            SelectComboValue(userRoleCombo, Safe(user.Role), SecurityService.CashierRole);
            SelectComboValue(userActiveCombo, user.IsActive ? "Yes" : "No", "Yes");
        }

        private void UsersGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (loading || usersGrid.CurrentRow == null)
            {
                return;
            }

            string userId = Convert.ToString(usersGrid.CurrentRow.Tag);
            UserAccount user = database.Users.FirstOrDefault(item => item.Id == userId);
            LoadUserToForm(user);
        }

        private void NewUser_Click(object sender, EventArgs e)
        {
            if (!SecurityService.IsAdmin(currentUser))
            {
                MessageBox.Show("Only Admin users can manage accounts.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedUser = null;
            usersGrid.ClearSelection();
            LoadUserToForm(null);
            userUsernameText.Focus();
        }

        private void SaveUser_Click(object sender, EventArgs e)
        {
            UserAccount savedUser = null;
            string message = "";
            bool isNew = false;
            bool saved = false;
            bool isActive = String.Equals(userActiveCombo.Text, "Yes", StringComparison.OrdinalIgnoreCase);

            RunWithBusy("Saving user account...", delegate
            {
                saved = CreateUserAccountService().SaveUser(
                    selectedUser,
                    currentUser,
                    userUsernameText.Text,
                    userFullNameText.Text,
                    userRoleCombo.Text,
                    isActive,
                    userPasswordText.Text,
                    out savedUser,
                    out message,
                    out isNew);
            });

            if (!saved)
            {
                MessageBox.Show(message, "User account not saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusUserValidation(message);
                return;
            }

            selectedUser = savedUser;
            RefreshUsersGrid();
            RefreshAuditLog();
            ClearValidation();
            MessageBox.Show("User account saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
