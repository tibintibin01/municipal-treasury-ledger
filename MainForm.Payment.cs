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

        private TabPage BuildPaymentTab()
        {
            TabPage tab = new TabPage("Payments");
            tab.UseVisualStyleBackColor = false;
            tab.BackColor = WindowBack;
            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;
            split.SplitterDistance = 230;
            split.SplitterWidth = 1;
            split.BackColor = BorderLine;
            split.Panel1.Padding = new Padding(12);
            split.Panel2.Padding = new Padding(12);
            split.Panel1.BackColor = SurfaceBack;
            split.Panel2.BackColor = SurfaceBack;

            paymentsGrid = MakeGrid();
            paymentsGrid.Columns.Add(MakeColumn("Id", "Id", false));
            paymentsGrid.Columns.Add(MakeColumn("DatePaid", "Date", true));
            paymentsGrid.Columns.Add(MakeColumn("OrNumber", "OR Number", true));
            paymentsGrid.Columns.Add(MakeColumn("Schedule", "Schedule", true));
            paymentsGrid.Columns.Add(MakeColumn("Amount", "Amount", true));
            paymentsGrid.Columns.Add(MakeColumn("Remarks", "Remarks", true));
            split.Panel1.Controls.Add(paymentsGrid);

            TableLayoutPanel form = new TableLayoutPanel();
            form.Dock = DockStyle.Top;
            form.AutoSize = true;
            form.ColumnCount = 4;
            form.RowCount = 4;
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (int i = 0; i < 4; i++)
            {
                form.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 3 ? 76 : 34));
            }

            paymentAssessmentCombo = MakeComboBox(new string[] { });
            paymentAssessmentCombo.SelectedIndexChanged += PaymentAssessmentCombo_SelectedIndexChanged;
            paymentDatePicker = new DateTimePicker();
            paymentDatePicker.Format = DateTimePickerFormat.Short;
            paymentDatePicker.Dock = DockStyle.Fill;
            orNumberText = MakeTextBox();
            paymentScheduleCombo = MakeComboBox(new string[] { "Annual", "1st Qtr", "2nd Qtr", "3rd Qtr", "4th Qtr" });
            paymentAmountText = MakeMoneyTextBox();
            paymentRemarksText = MakeTextBox();
            paymentRemarksText.Multiline = true;

            AddLabeled(form, 0, 0, "Assessment year", paymentAssessmentCombo);
            AddLabeled(form, 0, 2, "Date paid", paymentDatePicker);
            AddLabeled(form, 1, 0, "OR number", orNumberText);
            AddLabeled(form, 1, 2, "Schedule", paymentScheduleCombo);
            AddLabeled(form, 2, 0, "Amount", paymentAmountText);
            AddLabeled(form, 3, 0, "Remarks", paymentRemarksText);
            form.SetColumnSpan(paymentRemarksText, 3);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Top;
            actions.Height = 42;

            Button addButton = MakeButton("Add Payment");
            addButton.Width = 120;
            addButton.Click += AddPayment_Click;
            actions.Controls.Add(addButton);

            Button clearButton = MakeButton("Clear Payment");
            clearButton.Width = 120;
            clearButton.Click += ClearPayment_Click;
            actions.Controls.Add(clearButton);

            Button deleteButton = MakeButton("Delete Payment");
            deleteButton.Width = 130;
            deleteButton.Click += DeletePayment_Click;
            actions.Controls.Add(deleteButton);

            Button printButton = MakeButton("Print Receipt");
            printButton.Width = 128;
            printButton.Click += PrintPaymentReceipt_Click;
            actions.Controls.Add(printButton);

            Button findOrButton = MakeButton("Find OR");
            findOrButton.Width = 96;
            findOrButton.Click += FindPaymentByOr_Click;
            actions.Controls.Add(findOrButton);

            split.Panel2.Controls.Add(actions);
            split.Panel2.Controls.Add(form);

            tab.Controls.Add(split);
            return tab;
        }

        private void RefreshPaymentAssessmentCombo(string selectAssessmentId)
        {
            loading = true;
            try
            {
                paymentAssessmentCombo.Items.Clear();

                if (selectedOwner != null && selectedOwner.Assessments != null)
                {
                    foreach (YearlyAssessment assessment in selectedOwner.Assessments.OrderByDescending(item => item.Year))
                    {
                        paymentAssessmentCombo.Items.Add(new AssessmentComboItem(assessment));
                    }
                }

                int selectedIndex = -1;
                if (!String.IsNullOrEmpty(selectAssessmentId))
                {
                    for (int i = 0; i < paymentAssessmentCombo.Items.Count; i++)
                    {
                        AssessmentComboItem item = paymentAssessmentCombo.Items[i] as AssessmentComboItem;
                        if (item != null && item.Assessment.Id == selectAssessmentId)
                        {
                            selectedIndex = i;
                            break;
                        }
                    }
                }

                if (selectedIndex < 0 && paymentAssessmentCombo.Items.Count > 0)
                {
                    selectedIndex = 0;
                }

                paymentAssessmentCombo.SelectedIndex = selectedIndex;
            }
            finally
            {
                loading = false;
            }

            AssessmentComboItem selected = paymentAssessmentCombo.SelectedItem as AssessmentComboItem;
            selectedAssessment = selected == null ? null : selected.Assessment;
            RefreshPaymentsGrid();
            UpdateAssessmentTotals();
        }

        private void PaymentAssessmentCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loading)
            {
                return;
            }

            AssessmentComboItem item = paymentAssessmentCombo.SelectedItem as AssessmentComboItem;
            selectedAssessment = item == null ? null : item.Assessment;
            if (selectedAssessment != null)
            {
                LoadAssessmentToFields(selectedAssessment);
            }
            RefreshPaymentsGrid();
        }

        private void RefreshPaymentsGrid()
        {
            paymentsGrid.Rows.Clear();
            if (selectedAssessment == null || selectedAssessment.Payments == null)
            {
                return;
            }

            foreach (PaymentRecord payment in selectedAssessment.Payments.OrderByDescending(item => item.DatePaid))
            {
                int rowIndex = paymentsGrid.Rows.Add(new object[]
                {
                    payment.Id,
                    payment.DatePaid.ToString("yyyy-MM-dd"),
                    Safe(payment.OrNumber),
                    Safe(payment.Schedule),
                    Money(payment.Amount),
                    Safe(payment.Remarks)
                });
                paymentsGrid.Rows[rowIndex].Tag = payment.Id;
            }
        }

        private void AddPayment_Click(object sender, EventArgs e)
        {
            if (selectedOwner == null || selectedAssessment == null)
            {
                MessageBox.Show("Select a business owner and assessment year first.", "Assessment required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            decimal amount;
            if (!ValidateMoneyField("Payment amount", paymentAmountText, out amount))
            {
                return;
            }

            PaymentRecord payment = new PaymentRecord();
            payment.DatePaid = paymentDatePicker.Value.Date;
            payment.OrNumber = orNumberText.Text.Trim();
            payment.Schedule = paymentScheduleCombo.Text.Trim();
            payment.Amount = amount;
            payment.Remarks = paymentRemarksText.Text.Trim();

            string paymentMessage = "";
            bool saved = false;
            RunWithBusy("Saving payment record...", delegate
            {
                saved = CreatePaymentService().AddPayment(selectedOwner, selectedAssessment, payment, out paymentMessage);
            });

            if (!saved)
            {
                MessageBox.Show(paymentMessage, "Invalid payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FocusPaymentValidation(paymentMessage);
                return;
            }

            RefreshAssessmentsGrid(selectedAssessment.Id);
            RefreshPaymentsGrid();
            RefreshReport();
            RefreshAuditLog();
            ClearPaymentFields();
            ClearValidation();
            MessageBox.Show("Payment added.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearPayment_Click(object sender, EventArgs e)
        {
            ClearPaymentFields();
        }

        private void DeletePayment_Click(object sender, EventArgs e)
        {
            if (selectedAssessment == null || paymentsGrid.CurrentRow == null)
            {
                return;
            }

            string paymentId = Convert.ToString(paymentsGrid.CurrentRow.Tag);
            if (String.IsNullOrEmpty(paymentId))
            {
                return;
            }

            PaymentRecord selectedPayment = SelectedPaymentRecord();
            string paymentDetails = selectedPayment == null
                ? ""
                : "\n\nOR " + Safe(selectedPayment.OrNumber) + " | " + selectedPayment.DatePaid.ToString("yyyy-MM-dd") + " | " + Money(selectedPayment.Amount);
            DialogResult result = MessageBox.Show(
                "Delete selected payment record?" + paymentDetails + "\n\nThis cannot be undone except by restoring a backup.",
                "Confirm payment delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            string message = "";
            bool deleted = false;
            RunWithBusy("Deleting payment record...", delegate
            {
                deleted = CreatePaymentService().DeletePayment(selectedOwner, selectedAssessment, paymentId, currentUser, out message);
            });

            if (!deleted)
            {
                MessageBox.Show(message, "Payment not deleted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            RefreshAssessmentsGrid(selectedAssessment.Id);
            RefreshPaymentsGrid();
            RefreshReport();
            RefreshAuditLog();
        }

        private void PrintPaymentReceipt_Click(object sender, EventArgs e)
        {
            PaymentRecord payment = SelectedPaymentRecord();
            if (selectedOwner == null || selectedAssessment == null || payment == null)
            {
                MessageBox.Show("Select a saved payment first.", "Payment required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (PrintableReportService.PreviewPaymentReceipt(this, selectedOwner, selectedAssessment, payment, currentUser, database == null ? null : database.Settings))
            {
                LogAction("Print Payment Receipt", "PaymentRecord", payment.Id, "OR " + Safe(payment.OrNumber) + " - " + Money(payment.Amount) + " - " + AuditService.OwnerName(selectedOwner));
                dataStore.Save(database);
                RefreshAuditLog();
            }
        }

        private void FindPaymentByOr_Click(object sender, EventArgs e)
        {
            string orNumber = orNumberText.Text.Trim();
            if (String.IsNullOrEmpty(orNumber))
            {
                string message = "Enter an OR number to find.";
                MessageBox.Show(message, "OR required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowValidation(orNumberText, message);
                return;
            }

            BusinessOwner foundOwner = null;
            YearlyAssessment foundAssessment = null;
            PaymentRecord foundPayment = null;
            foreach (BusinessOwner owner in database.Owners)
            {
                if (owner.Assessments == null)
                {
                    continue;
                }

                foreach (YearlyAssessment assessment in owner.Assessments)
                {
                    if (assessment.Payments == null)
                    {
                        continue;
                    }

                    foundPayment = assessment.Payments.FirstOrDefault(payment => String.Equals(payment.OrNumber ?? "", orNumber, StringComparison.OrdinalIgnoreCase));
                    if (foundPayment != null)
                    {
                        foundOwner = owner;
                        foundAssessment = assessment;
                        break;
                    }
                }

                if (foundPayment != null)
                {
                    break;
                }
            }

            if (foundPayment == null)
            {
                MessageBox.Show("No payment was found for OR number " + orNumber + ".", "OR not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            searchBox.Text = "";
            RefreshOwnerList(foundOwner.Id);
            selectedOwner = foundOwner;
            selectedAssessment = foundAssessment;
            RefreshAssessmentsGrid(foundAssessment.Id);
            RefreshPaymentAssessmentCombo(foundAssessment.Id);
            SelectPaymentRow(foundPayment.Id);
            MessageBox.Show(
                "Found OR " + foundPayment.OrNumber + ".\n\n" + Safe(foundOwner.BusinessName) + "\n" + foundPayment.DatePaid.ToString("yyyy-MM-dd") + " | " + Money(foundPayment.Amount),
                "OR found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SelectPaymentRow(string paymentId)
        {
            if (paymentsGrid == null || String.IsNullOrEmpty(paymentId))
            {
                return;
            }

            foreach (DataGridViewRow row in paymentsGrid.Rows)
            {
                if (String.Equals(Convert.ToString(row.Tag), paymentId, StringComparison.Ordinal))
                {
                    row.Selected = true;
                    paymentsGrid.CurrentCell = row.Cells["DatePaid"];
                    return;
                }
            }
        }

        private PaymentRecord SelectedPaymentRecord()
        {
            if (selectedAssessment == null || selectedAssessment.Payments == null || paymentsGrid.CurrentRow == null)
            {
                return null;
            }

            string paymentId = Convert.ToString(paymentsGrid.CurrentRow.Tag);
            if (String.IsNullOrEmpty(paymentId))
            {
                return null;
            }

            return selectedAssessment.Payments.FirstOrDefault(item => item.Id == paymentId);
        }

        private void ClearPaymentFields()
        {
            paymentDatePicker.Value = DateTime.Today;
            orNumberText.Text = "";
            paymentScheduleCombo.SelectedIndex = 0;
            paymentAmountText.Text = "0.00";
            paymentRemarksText.Text = "";
            orNumberText.Focus();
        }

        private class AssessmentComboItem
        {
            public YearlyAssessment Assessment { get; private set; }

            public AssessmentComboItem(YearlyAssessment assessment)
            {
                Assessment = assessment;
            }

            public override string ToString()
            {
                return Assessment.Year + " - Balance " + Assessment.Balance.ToString("N2");
            }
        }
    }
}
