using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm
    {
        private TabPage BuildFeesTab()
        {
            TabPage tab = new TabPage("Fees");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.SplitterDistance = 280;
            split.SplitterWidth = 1;
            split.BackColor = BorderLine;
            split.Panel1.Padding = new Padding(12);
            split.Panel2.Padding = new Padding(12);
            split.Panel1.BackColor = SurfaceBack;
            split.Panel2.BackColor = SurfaceBack;

            feeCatalogGrid = MakeGrid();
            feeCatalogGrid.Columns.Add(MakeColumn("Id", "Id", false));
            feeCatalogGrid.Columns.Add(MakeColumn("Code", "Code", true));
            feeCatalogGrid.Columns.Add(MakeColumn("Description", "Description", true));
            feeCatalogGrid.Columns.Add(MakeColumn("Amount", "Amount", true));
            feeCatalogGrid.Columns.Add(MakeColumn("Active", "Active", true));
            feeCatalogGrid.Columns["Code"].Width = 120;
            feeCatalogGrid.Columns["Description"].Width = 320;
            feeCatalogGrid.Columns["Amount"].Width = 120;
            feeCatalogGrid.Columns["Active"].Width = 90;
            feeCatalogGrid.SelectionChanged += FeeCatalogGrid_SelectionChanged;
            split.Panel1.Controls.Add(feeCatalogGrid);

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.AutoSize = true;
            form.ColumnCount = 4;
            form.RowCount = 2;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

            feeCodeText = MakeTextBox();
            feeDescriptionText = MakeTextBox();
            feeAmountText = MakeTextBox();
            feeAmountText.TextAlign = HorizontalAlignment.Right;
            feeAmountText.Text = "0.00";
            feeActiveCombo = MakeComboBox(new string[] { "Yes", "No" });

            AddLabeled(form, 0, 0, "Code", feeCodeText);
            AddLabeled(form, 0, 2, "Amount", feeAmountText);
            AddLabeled(form, 1, 0, "Description", feeDescriptionText);
            AddLabeled(form, 1, 2, "Active", feeActiveCombo);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 44;

            Button newButton = MakeButton("New Fee");
            newButton.Width = 110;
            newButton.Click += NewFee_Click;
            actions.Controls.Add(newButton);

            Button saveButton = MakeButton("Save Fee");
            saveButton.Width = 110;
            saveButton.Click += SaveFee_Click;
            actions.Controls.Add(saveButton);

            Button deleteButton = MakeButton("Delete Fee");
            deleteButton.Width = 120;
            deleteButton.Click += DeleteFee_Click;
            actions.Controls.Add(deleteButton);

            Label note = new Label();
            note.Text = CanManageFeeCatalog()
                ? "Standard fees can be applied from the Assessment tab."
                : "Only Admin or Treasurer users can manage fee items.";
            note.Dock = DockStyle.Top;
            note.Height = 34;
            note.ForeColor = CanManageFeeCatalog() ? TextMuted : Danger;
            note.TextAlign = ContentAlignment.MiddleLeft;

            split.Panel2.Controls.Add(actions);
            split.Panel2.Controls.Add(form);
            split.Panel2.Controls.Add(note);

            bool canManage = CanManageFeeCatalog();
            feeCodeText.Enabled = canManage;
            feeDescriptionText.Enabled = canManage;
            feeAmountText.Enabled = canManage;
            feeActiveCombo.Enabled = canManage;
            foreach (Control control in actions.Controls)
            {
                control.Enabled = canManage;
            }

            tab.Controls.Add(split);
            return tab;
        }

        private bool CanManageFeeCatalog()
        {
            return SecurityService.IsAdmin(currentUser) || SecurityService.IsTreasurer(currentUser);
        }

        private void RefreshFeeCatalogGrid(string selectFeeId)
        {
            if (feeCatalogGrid == null)
            {
                return;
            }

            loading = true;
            try
            {
                feeCatalogGrid.Rows.Clear();
                if (database.FeeCatalog != null)
                {
                    foreach (FeeCatalogItem fee in database.FeeCatalog.OrderBy(item => Safe(item.Code)).ThenBy(item => Safe(item.Description)))
                    {
                        int rowIndex = feeCatalogGrid.Rows.Add(new object[]
                        {
                            fee.Id,
                            Safe(fee.Code),
                            Safe(fee.Description),
                            Money(fee.Amount),
                            fee.IsActive ? "Yes" : "No"
                        });
                        feeCatalogGrid.Rows[rowIndex].Tag = fee.Id;
                    }
                }

                feeCatalogGrid.ClearSelection();
                if (!String.IsNullOrEmpty(selectFeeId))
                {
                    for (int i = 0; i < feeCatalogGrid.Rows.Count; i++)
                    {
                        if (Convert.ToString(feeCatalogGrid.Rows[i].Tag) == selectFeeId)
                        {
                            feeCatalogGrid.Rows[i].Selected = true;
                            feeCatalogGrid.CurrentCell = feeCatalogGrid.Rows[i].Cells[1];
                            break;
                        }
                    }
                }
            }
            finally
            {
                loading = false;
            }

            if (String.IsNullOrEmpty(selectFeeId))
            {
                LoadFeeToForm(null);
            }
        }

        private void LoadFeeToForm(FeeCatalogItem fee)
        {
            selectedFeeCatalogItem = fee;
            if (fee == null)
            {
                feeCodeText.Text = "";
                feeDescriptionText.Text = "";
                feeAmountText.Text = "0.00";
                SelectComboValue(feeActiveCombo, "Yes", "Yes");
                return;
            }

            feeCodeText.Text = Safe(fee.Code);
            feeDescriptionText.Text = Safe(fee.Description);
            feeAmountText.Text = Money(fee.Amount);
            SelectComboValue(feeActiveCombo, fee.IsActive ? "Yes" : "No", "Yes");
        }

        private void FeeCatalogGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (loading || feeCatalogGrid.CurrentRow == null || database.FeeCatalog == null)
            {
                return;
            }

            string feeId = Convert.ToString(feeCatalogGrid.CurrentRow.Tag);
            FeeCatalogItem fee = database.FeeCatalog.FirstOrDefault(item => item.Id == feeId);
            LoadFeeToForm(fee);
        }

        private void NewFee_Click(object sender, EventArgs e)
        {
            if (!CanManageFeeCatalog())
            {
                MessageBox.Show("Only Admin or Treasurer users can manage fees.", "Permission required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedFeeCatalogItem = null;
            feeCatalogGrid.ClearSelection();
            LoadFeeToForm(null);
            feeCodeText.Focus();
        }

        private void SaveFee_Click(object sender, EventArgs e)
        {
            decimal amount;
            if (!ValidateMoneyField("Fee amount", feeAmountText, out amount))
            {
                return;
            }

            FeeCatalogItem savedFee = null;
            string message = "";
            bool isNew = false;
            bool saved = false;
            bool isActive = String.Equals(feeActiveCombo.Text, "Yes", StringComparison.OrdinalIgnoreCase);
            RunWithBusy("Saving fee item...", delegate
            {
                saved = CreateFeeCatalogService().SaveFee(
                    selectedFeeCatalogItem,
                    currentUser,
                    feeCodeText.Text,
                    feeDescriptionText.Text,
                    amount,
                    isActive,
                    out savedFee,
                    out message,
                    out isNew);
            });

            if (!saved)
            {
                MessageBox.Show(message, "Fee not saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusFeeValidation(message);
                return;
            }

            selectedFeeCatalogItem = savedFee;
            ClearValidation();
            RefreshFeeCatalogGrid(savedFee.Id);
            RefreshAssessmentFeeCatalogCombo();
            RefreshAuditLog();
            MessageBox.Show("Fee item saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteFee_Click(object sender, EventArgs e)
        {
            if (selectedFeeCatalogItem == null)
            {
                return;
            }

            DialogResult result = MessageBox.Show("Delete this fee item?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            string message = "";
            bool deleted = false;
            RunWithBusy("Deleting fee item...", delegate
            {
                deleted = CreateFeeCatalogService().DeleteFee(selectedFeeCatalogItem, currentUser, out message);
            });

            if (!deleted)
            {
                MessageBox.Show(message, "Fee not deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedFeeCatalogItem = null;
            RefreshFeeCatalogGrid(null);
            RefreshAssessmentFeeCatalogCombo();
            RefreshAuditLog();
        }

        private void FocusFeeValidation(string message)
        {
            string value = (message ?? "").ToLowerInvariant();
            if (value.Contains("amount"))
            {
                ShowValidation(feeAmountText, message);
                return;
            }

            if (value.Contains("description"))
            {
                ShowValidation(feeDescriptionText, message);
                return;
            }

            ShowValidation(feeCodeText, message);
        }
    }
}
