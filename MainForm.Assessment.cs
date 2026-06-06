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

        private TabPage BuildAssessmentTab()
        {
            TabPage tab = new TabPage("Assessment");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;
            TableLayoutPanel screen = new TableLayoutPanel();
            screen.Dock = DockStyle.Fill;
            screen.ColumnCount = 1;
            screen.RowCount = 3;
            screen.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            screen.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            screen.RowStyles.Add(new RowStyle(SizeType.Absolute, 326));
            screen.BackColor = BorderLine;

            Panel gridHost = new Panel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.Padding = new Padding(12);
            gridHost.BackColor = SurfaceBack;

            Panel formHost = new Panel();
            formHost.Dock = DockStyle.Fill;
            formHost.Padding = new Padding(12);
            formHost.BackColor = SurfaceBack;

            Panel divider = new Panel();
            divider.Dock = DockStyle.Fill;
            divider.BackColor = BorderLine;

            assessmentsGrid = MakeGrid();
            assessmentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            assessmentsGrid.Columns.Add(MakeColumn("Id", "Id", false));
            assessmentsGrid.Columns.Add(MakeColumn("Year", "Year", true));
            assessmentsGrid.Columns.Add(MakeColumn("Capital", "Capital", true));
            assessmentsGrid.Columns.Add(MakeColumn("GrossSales", "Gross", true));
            assessmentsGrid.Columns.Add(MakeColumn("BusinessTax", "Bus. Tax", true));
            assessmentsGrid.Columns.Add(MakeColumn("MayorsPermit", "Permit", true));
            assessmentsGrid.Columns.Add(MakeColumn("Fees", "Fees", true));
            assessmentsGrid.Columns.Add(MakeColumn("Surcharge", "Surcharge", true));
            assessmentsGrid.Columns.Add(MakeColumn("Penalty", "Penalty", true));
            assessmentsGrid.Columns.Add(MakeColumn("Total", "Total", true));
            assessmentsGrid.Columns.Add(MakeColumn("Paid", "Paid", true));
            assessmentsGrid.Columns.Add(MakeColumn("Balance", "Balance", true));
            assessmentsGrid.SelectionChanged += AssessmentsGrid_SelectionChanged;
            gridHost.Controls.Add(assessmentsGrid);

            TableLayoutPanel lowerLayout = new TableLayoutPanel();
            lowerLayout.Dock = DockStyle.Fill;
            lowerLayout.ColumnCount = 1;
            lowerLayout.RowCount = 3;
            lowerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 218));
            lowerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            lowerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            lowerLayout.BackColor = SurfaceBack;

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Fill;
            form.ColumnCount = 4;
            form.RowCount = 6;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 6; i++)
            {
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 5 ? 50 : 32));
            }

            assessmentYearText = MakeYearTextBox();
            capitalText = MakeMoneyTextBox();
            grossSalesText = MakeMoneyTextBox();
            businessTaxText = MakeMoneyTextBox();
            mayorsPermitText = MakeMoneyTextBox();
            feesText = MakeMoneyTextBox();
            surchargeText = MakeMoneyTextBox();
            penaltyText = MakeMoneyTextBox();
            assessmentRemarksText = MakeTextBox();
            assessmentRemarksText.Multiline = true;

            AddLabeled(form, 0, 0, "Year", assessmentYearText);
            AddLabeled(form, 0, 2, "Capital", capitalText);
            AddLabeled(form, 1, 0, "Gross sales", grossSalesText);
            AddLabeled(form, 1, 2, "Business tax", businessTaxText);
            AddLabeled(form, 2, 0, "Mayor's permit", mayorsPermitText);
            AddLabeled(form, 2, 2, "Fees", feesText);
            AddLabeled(form, 3, 0, "Surcharge", surchargeText);
            AddLabeled(form, 3, 2, "Penalty", penaltyText);
            Control feePicker = MakeAssessmentFeePicker();
            AddLabeled(form, 4, 0, "Fee catalog", feePicker);
            form.SetColumnSpan(feePicker, 3);
            AddLabeled(form, 5, 0, "Remarks", assessmentRemarksText);
            form.SetColumnSpan(assessmentRemarksText, 3);

            TableLayoutPanel totals = new TableLayoutPanel();
            totals.Dock = DockStyle.Fill;
            totals.ColumnCount = 3;
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            totalAssessmentLabel = MakeTotalLabel("Total: 0.00");
            totalPaidLabel = MakeTotalLabel("Paid: 0.00");
            balanceLabel = MakeTotalLabel("Balance: 0.00");
            totals.Controls.Add(totalAssessmentLabel, 0, 0);
            totals.Controls.Add(totalPaidLabel, 1, 0);
            totals.Controls.Add(balanceLabel, 2, 0);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;

            Button newButton = MakeButton("New Assessment");
            newButton.Width = 135;
            newButton.Click += NewAssessment_Click;
            actions.Controls.Add(newButton);

            Button saveButton = MakeButton("Save Assessment");
            saveButton.Width = 140;
            saveButton.Click += SaveAssessment_Click;
            actions.Controls.Add(saveButton);

            Button deleteButton = MakeButton("Delete Assessment");
            deleteButton.Width = 145;
            deleteButton.Click += DeleteAssessment_Click;
            actions.Controls.Add(deleteButton);

            lowerLayout.Controls.Add(form, 0, 0);
            lowerLayout.Controls.Add(totals, 0, 1);
            lowerLayout.Controls.Add(actions, 0, 2);
            formHost.Controls.Add(lowerLayout);

            screen.Controls.Add(gridHost, 0, 0);
            screen.Controls.Add(divider, 0, 1);
            screen.Controls.Add(formHost, 0, 2);
            tab.Controls.Add(screen);
            return tab;
        }

        private void RefreshAssessmentsGrid(string selectAssessmentId)
        {
            loading = true;
            try
            {
                assessmentsGrid.Rows.Clear();
                if (selectedOwner != null && selectedOwner.Assessments != null)
                {
                    foreach (YearlyAssessment assessment in selectedOwner.Assessments.OrderByDescending(item => item.Year))
                    {
                        int rowIndex = assessmentsGrid.Rows.Add(new object[]
                        {
                            assessment.Id,
                            assessment.Year,
                            Money(assessment.Capital),
                            Money(assessment.GrossSales),
                            Money(assessment.BusinessTax),
                            Money(assessment.MayorsPermit),
                            Money(assessment.Fees),
                            Money(assessment.Surcharge),
                            Money(assessment.Penalty),
                            Money(assessment.TotalAssessment),
                            Money(assessment.TotalPaid),
                            Money(assessment.Balance)
                        });
                        assessmentsGrid.Rows[rowIndex].Tag = assessment.Id;
                    }
                }

                int selectedIndex = -1;
                if (!String.IsNullOrEmpty(selectAssessmentId))
                {
                    for (int i = 0; i < assessmentsGrid.Rows.Count; i++)
                    {
                        if (Convert.ToString(assessmentsGrid.Rows[i].Tag) == selectAssessmentId)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }

                assessmentsGrid.ClearSelection();
                if (selectedIndex >= 0)
                {
                    assessmentsGrid.Rows[selectedIndex].Selected = true;
                    assessmentsGrid.CurrentCell = assessmentsGrid.Rows[selectedIndex].Cells[1];
                }
            }
            finally
            {
                loading = false;
            }

            if (String.IsNullOrEmpty(selectAssessmentId))
            {
                ClearAssessmentFields();
            }
        }

        private void LoadAssessmentToFields(YearlyAssessment assessment)
        {
            loading = true;
            try
            {
                selectedAssessment = assessment;

                if (assessment == null)
                {
                    ClearAssessmentFields();
                    return;
                }

                assessmentYearText.Text = assessment.Year.ToString();
                capitalText.Text = Money(assessment.Capital);
                grossSalesText.Text = Money(assessment.GrossSales);
                businessTaxText.Text = Money(assessment.BusinessTax);
                mayorsPermitText.Text = Money(assessment.MayorsPermit);
                feesText.Text = Money(assessment.Fees);
                surchargeText.Text = Money(assessment.Surcharge);
                penaltyText.Text = Money(assessment.Penalty);
                assessmentRemarksText.Text = Safe(assessment.Remarks);
            }
            finally
            {
                loading = false;
            }

            UpdateAssessmentTotals();
        }

        private void ClearAssessmentFields()
        {
            loading = true;
            try
            {
                selectedAssessment = null;
                assessmentYearText.Text = DateTime.Today.Year.ToString();
                capitalText.Text = "0.00";
                grossSalesText.Text = "0.00";
                businessTaxText.Text = "0.00";
                mayorsPermitText.Text = "0.00";
                feesText.Text = "0.00";
                surchargeText.Text = "0.00";
                penaltyText.Text = "0.00";
                assessmentRemarksText.Text = "";
            }
            finally
            {
                loading = false;
            }

            UpdateAssessmentTotals();
        }

        private void NewAssessment_Click(object sender, EventArgs e)
        {
            selectedAssessment = null;
            ClearAssessmentFields();
            assessmentYearText.Focus();
        }

        private void SaveAssessment_Click(object sender, EventArgs e)
        {
            if (selectedOwner == null || !database.Owners.Any(owner => owner.Id == selectedOwner.Id))
            {
                MessageBox.Show("Save the business owner first.", "Business owner required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int year;
            if (!Int32.TryParse(assessmentYearText.Text.Trim(), out year) || !LedgerValidation.IsValidAssessmentYear(year))
            {
                string validationMessage = "Enter a valid assessment year from 1900 up to next year.";
                MessageBox.Show(validationMessage, "Invalid year", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowValidation(assessmentYearText, validationMessage);
                return;
            }

            decimal capital;
            decimal grossSales;
            decimal businessTax;
            decimal mayorsPermit;
            decimal fees;
            decimal surcharge;
            decimal penalty;

            if (!ValidateMoneyField("Capital", capitalText, out capital) ||
                !ValidateMoneyField("Gross sales", grossSalesText, out grossSales) ||
                !ValidateMoneyField("Business tax", businessTaxText, out businessTax) ||
                !ValidateMoneyField("Mayor's permit", mayorsPermitText, out mayorsPermit) ||
                !ValidateMoneyField("Fees", feesText, out fees) ||
                !ValidateMoneyField("Surcharge", surchargeText, out surcharge) ||
                !ValidateMoneyField("Penalty", penaltyText, out penalty))
            {
                return;
            }

            YearlyAssessment assessment = selectedAssessment;
            YearlyAssessment beforeAssessment = null;
            if (assessment == null)
            {
                assessment = new YearlyAssessment();
            }
            else
            {
                beforeAssessment = AuditChangeFormatter.CloneAssessment(assessment);
            }

            assessment.Year = year;
            assessment.Capital = capital;
            assessment.GrossSales = grossSales;
            assessment.BusinessTax = businessTax;
            assessment.MayorsPermit = mayorsPermit;
            assessment.Fees = fees;
            assessment.Surcharge = surcharge;
            assessment.Penalty = penalty;
            assessment.Remarks = assessmentRemarksText.Text.Trim();

            string message = "";
            bool isNew = false;
            bool saved = false;
            RunWithBusy("Saving assessment record...", delegate
            {
                saved = CreateAssessmentService().SaveAssessment(selectedOwner, assessment, beforeAssessment, out message, out isNew);
            });

            if (!saved)
            {
                MessageBox.Show(message, "Assessment not saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if ((message ?? "").IndexOf("year", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ShowValidation(assessmentYearText, message);
                }
                else
                {
                    ShowValidation(assessmentRemarksText, message);
                }
                return;
            }

            selectedAssessment = assessment;
            ClearValidation();
            RefreshAssessmentsGrid(assessment.Id);
            RefreshPaymentAssessmentCombo(assessment.Id);
            RefreshReport();
            RefreshAuditLog();
            MessageBox.Show("Assessment saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteAssessment_Click(object sender, EventArgs e)
        {
            if (selectedOwner == null || selectedAssessment == null)
            {
                return;
            }

            DialogResult result = MessageBox.Show("Delete this assessment and its payments?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                return;
            }

            string message = "";
            bool deleted = false;
            RunWithBusy("Deleting assessment record...", delegate
            {
                deleted = CreateAssessmentService().DeleteAssessment(database, selectedOwner, selectedAssessment, currentUser, out message);
            });

            if (!deleted)
            {
                MessageBox.Show(message, "Assessment not deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selectedAssessment = null;
            RefreshAssessmentsGrid(null);
            RefreshPaymentAssessmentCombo(null);
            RefreshReport();
            RefreshAuditLog();
        }

        private void AssessmentsGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (loading || assessmentsGrid.CurrentRow == null)
            {
                return;
            }

            string assessmentId = Convert.ToString(assessmentsGrid.CurrentRow.Tag);
            YearlyAssessment assessment = FindAssessment(assessmentId);
            if (assessment == null)
            {
                return;
            }

            LoadAssessmentToFields(assessment);
            RefreshPaymentAssessmentCombo(assessment.Id);
        }

        private void MoneyTextChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                UpdateAssessmentTotals();
            }
        }

        private void UpdateAssessmentTotals()
        {
            decimal total = DecimalFrom(businessTaxText) + DecimalFrom(mayorsPermitText) + DecimalFrom(feesText) + DecimalFrom(surchargeText) + DecimalFrom(penaltyText);
            decimal paid = selectedAssessment == null ? 0m : selectedAssessment.TotalPaid;
            decimal balance = total - paid;
            totalAssessmentLabel.Text = "Total: " + Money(total);
            totalPaidLabel.Text = "Paid: " + Money(paid);
            balanceLabel.Text = "Balance: " + Money(balance);
            balanceLabel.ForeColor = balance > 0m ? Color.Firebrick : Color.SeaGreen;
        }

        private Control MakeAssessmentFeePicker()
        {
            TableLayoutPanel picker = new TableLayoutPanel();
            picker.Dock = DockStyle.Fill;
            picker.ColumnCount = 2;
            picker.RowCount = 1;
            picker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            picker.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            picker.Margin = new Padding(0, 0, 14, 8);

            assessmentFeeCatalogCombo = MakeComboBox(new string[] { "Select fee..." });
            Button applyButton = MakeButton("Apply Fee");
            applyButton.Width = 104;
            applyButton.Margin = new Padding(8, 0, 0, 0);
            applyButton.Click += ApplyFeeCatalog_Click;

            picker.Controls.Add(assessmentFeeCatalogCombo, 0, 0);
            picker.Controls.Add(applyButton, 1, 0);
            return picker;
        }

        private void RefreshAssessmentFeeCatalogCombo()
        {
            if (assessmentFeeCatalogCombo == null)
            {
                return;
            }

            object previous = assessmentFeeCatalogCombo.SelectedItem;
            string previousId = previous is FeeCatalogItem ? ((FeeCatalogItem)previous).Id : "";
            assessmentFeeCatalogCombo.Items.Clear();
            assessmentFeeCatalogCombo.Items.Add("Select fee...");

            if (database != null && database.FeeCatalog != null)
            {
                foreach (FeeCatalogItem fee in database.FeeCatalog
                    .Where(item => item.IsActive)
                    .OrderBy(item => Safe(item.Code))
                    .ThenBy(item => Safe(item.Description)))
                {
                    assessmentFeeCatalogCombo.Items.Add(fee);
                }
            }

            int selectedIndex = 0;
            if (!String.IsNullOrEmpty(previousId))
            {
                for (int i = 0; i < assessmentFeeCatalogCombo.Items.Count; i++)
                {
                    FeeCatalogItem fee = assessmentFeeCatalogCombo.Items[i] as FeeCatalogItem;
                    if (fee != null && fee.Id == previousId)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            assessmentFeeCatalogCombo.SelectedIndex = selectedIndex;
        }

        private void ApplyFeeCatalog_Click(object sender, EventArgs e)
        {
            FeeCatalogItem fee = assessmentFeeCatalogCombo == null ? null : assessmentFeeCatalogCombo.SelectedItem as FeeCatalogItem;
            if (fee == null)
            {
                MessageBox.Show("Select an active fee item first.", "Fee required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal currentFees;
            if (!ValidateMoneyField("Fees", feesText, out currentFees))
            {
                return;
            }

            feesText.Text = Money(currentFees + fee.Amount);

            string note = "Applied fee: " + Safe(fee.Code) + " - " + Safe(fee.Description) + " (" + Money(fee.Amount) + ")";
            if (String.IsNullOrWhiteSpace(assessmentRemarksText.Text))
            {
                assessmentRemarksText.Text = note;
            }
            else if (!assessmentRemarksText.Text.Contains(note))
            {
                assessmentRemarksText.Text = assessmentRemarksText.Text.Trim() + Environment.NewLine + note;
            }

            assessmentFeeCatalogCombo.SelectedIndex = 0;
            feesText.Focus();
        }

        private YearlyAssessment FindAssessment(string assessmentId)
        {
            if (selectedOwner == null || selectedOwner.Assessments == null || String.IsNullOrEmpty(assessmentId))
            {
                return null;
            }

            return selectedOwner.Assessments.FirstOrDefault(item => item.Id == assessmentId);
        }
    }
}
