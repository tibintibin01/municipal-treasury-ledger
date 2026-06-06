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

        private TextBox MakeTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = InputBack;
            textBox.ForeColor = TextMain;
            textBox.Font = new Font("Segoe UI", 9.5F);
            textBox.Margin = new Padding(0, 0, 14, 10);
            return textBox;
        }

        private TextBox MakeYearTextBox()
        {
            TextBox textBox = MakeTextBox();
            textBox.TextAlign = HorizontalAlignment.Right;
            textBox.MaxLength = 4;
            textBox.KeyPress += NumericOnly_KeyPress;
            return textBox;
        }

        private TextBox MakeMoneyTextBox()
        {
            TextBox textBox = MakeTextBox();
            textBox.TextAlign = HorizontalAlignment.Right;
            textBox.Text = "0.00";
            textBox.TextChanged += MoneyTextChanged;
            return textBox;
        }

        private void NumericOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsControl(e.KeyChar) && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private ComboBox MakeComboBox(string[] values)
        {
            ComboBox combo = new ComboBox();
            combo.Dock = DockStyle.Fill;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = InputBack;
            combo.ForeColor = TextMain;
            combo.Font = new Font("Segoe UI", 9.5F);
            foreach (string value in values)
            {
                combo.Items.Add(value);
            }

            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }

            return combo;
        }

        private Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = TextMain;
            label.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            return label;
        }

        private Label MakeTotalLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            label.ForeColor = TextMain;
            return label;
        }

        private Button MakeButton(string text)
        {
            Button button = new Button();
            button.Text = ButtonText(text);
            button.Width = 86;
            button.Height = 32;
            button.Margin = new Padding(0, 4, 8, 4);
            button.FlatStyle = FlatStyle.Flat;
            bool dangerButton = IsDangerButton(text);
            button.BackColor = dangerButton ? Danger : Accent;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = new Padding(4, 0, 4, 0);
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderColor = dangerButton ? Color.FromArgb(127, 29, 29) : AccentDark;
            button.FlatAppearance.MouseOverBackColor = dangerButton ? Color.FromArgb(153, 27, 27) : AccentDark;
            button.FlatAppearance.MouseDownBackColor = dangerButton ? Color.FromArgb(127, 29, 29) : Color.FromArgb(13, 84, 79);
            button.Tag = text;
            EnsureButtonWidth(button);
            button.SizeChanged += delegate { EnsureButtonWidth(button); };
            return button;
        }

        private Button MakeHeaderButton(string text)
        {
            Button button = MakeButton(text);
            button.Height = 34;
            button.Margin = new Padding(8, 2, 0, 4);
            button.Font = new Font("Segoe UI Semibold", 9.2F, FontStyle.Bold);
            EnsureButtonWidth(button);
            return button;
        }

        private string ButtonText(string text)
        {
            string icon = ButtonIcon(text);
            return String.IsNullOrEmpty(icon) ? text : icon + "  " + text;
        }

        private string ButtonIcon(string text)
        {
            text = (text ?? "").Trim().ToLowerInvariant();
            if (text.StartsWith("new"))
            {
                return "+";
            }

            if (text.StartsWith("save"))
            {
                return "✓";
            }

            if (text.StartsWith("delete"))
            {
                return "×";
            }

            if (text.StartsWith("print"))
            {
                return "▣";
            }

            if (text.StartsWith("export"))
            {
                return "⇩";
            }

            if (text.StartsWith("import"))
            {
                return "⇧";
            }

            if (text.StartsWith("backup"))
            {
                return "◷";
            }

            if (text.StartsWith("restore"))
            {
                return "↺";
            }

            if (text.StartsWith("password"))
            {
                return "●";
            }

            if (text.StartsWith("theme"))
            {
                return "◐";
            }

            if (text.StartsWith("refresh") || text.StartsWith("reload"))
            {
                return "↻";
            }

            if (text.StartsWith("run") || text.StartsWith("verify"))
            {
                return "✓";
            }

            if (text.StartsWith("clear"))
            {
                return "⌫";
            }

            if (text.StartsWith("find"))
            {
                return "⌕";
            }

            if (text.StartsWith("browse"))
            {
                return "…";
            }

            if (text.StartsWith("archive"))
            {
                return "▤";
            }

            if (text.StartsWith("apply"))
            {
                return "+";
            }

            return "";
        }

        private bool IsDangerButton(string text)
        {
            return (text ?? "").Trim().ToLowerInvariant().StartsWith("delete");
        }

        private void EnsureButtonWidth(Button button)
        {
            if (button == null)
            {
                return;
            }

            int minimum = TextRenderer.MeasureText(button.Text, button.Font).Width + button.Padding.Left + button.Padding.Right + 28;
            if (button.Width < minimum)
            {
                button.Width = minimum;
            }
        }

        private void BeginOperation(string message)
        {
            if (operationStatusPanel != null)
            {
                operationStatusPanel.Visible = true;
            }

            if (operationStatusLabel != null)
            {
                operationStatusLabel.Text = message;
            }

            if (operationProgressBar != null)
            {
                operationProgressBar.Visible = true;
                operationProgressBar.Style = ProgressBarStyle.Marquee;
                operationProgressBar.MarqueeAnimationSpeed = 30;
            }

            UseWaitCursor = true;
            Cursor = Cursors.WaitCursor;
            Refresh();
            Application.DoEvents();
        }

        private void EndOperation()
        {
            if (operationProgressBar != null)
            {
                operationProgressBar.MarqueeAnimationSpeed = 0;
                operationProgressBar.Visible = false;
            }

            if (operationStatusLabel != null)
            {
                operationStatusLabel.Text = "";
            }

            if (operationStatusPanel != null)
            {
                operationStatusPanel.Visible = false;
            }

            UseWaitCursor = false;
            Cursor = Cursors.Default;
            Refresh();
        }

        private void RunWithBusy(string message, Action action)
        {
            BeginOperation(message);
            try
            {
                action();
            }
            finally
            {
                EndOperation();
            }
        }

        private void ClearValidation()
        {
            if (validationProvider != null)
            {
                validationProvider.Clear();
            }

            if (validationBackColors != null)
            {
                foreach (KeyValuePair<Control, Color> item in validationBackColors)
                {
                    if (item.Key != null)
                    {
                        item.Key.BackColor = item.Value;
                    }
                }

                validationBackColors.Clear();
            }

            if (validationStatusLabel != null)
            {
                validationStatusLabel.Text = "";
                validationStatusLabel.Visible = false;
            }
        }

        private void ShowValidation(Control control, string message)
        {
            EnsureValidationProvider();
            if (validationStatusLabel != null)
            {
                validationStatusLabel.Text = "Validation: " + (message ?? "Please check the highlighted field.");
                validationStatusLabel.Visible = true;
            }

            if (control != null)
            {
                if (!validationBackColors.ContainsKey(control))
                {
                    validationBackColors[control] = control.BackColor;
                }

                control.BackColor = DarkThemeEnabled ? Color.FromArgb(92, 38, 38) : Color.FromArgb(255, 242, 242);
                validationProvider.SetIconAlignment(control, ErrorIconAlignment.MiddleRight);
                validationProvider.SetIconPadding(control, 2);
                validationProvider.SetError(control, message ?? "Please check this field.");
                control.Focus();
            }
        }

        private void EnsureValidationProvider()
        {
            if (validationProvider == null)
            {
                validationProvider = new ErrorProvider();
                validationProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
                validationProvider.ContainerControl = this;
            }

            if (validationBackColors == null)
            {
                validationBackColors = new Dictionary<Control, Color>();
            }
        }

        private DataGridView MakeGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ReadOnly = true;
            grid.MultiSelect = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = SurfaceBack;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = BorderLine;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AccentSoft;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMain;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AccentSoft;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextMain;
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            grid.ColumnHeadersHeight = 28;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.DefaultCellStyle.BackColor = SurfaceBack;
            grid.DefaultCellStyle.ForeColor = TextMain;
            grid.DefaultCellStyle.SelectionBackColor = Accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.2F);
            grid.AlternatingRowsDefaultCellStyle.BackColor = GridAltBack;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = TextMain;
            grid.RowTemplate.Height = 30;
            return grid;
        }

        private DataGridViewTextBoxColumn MakeColumn(string name, string header, bool visible)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.Visible = visible;
            return column;
        }

        private void SetColumnWidth(DataGridView grid, string columnName, int width)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].Width = width;
                grid.Columns[columnName].MinimumWidth = Math.Min(width, 60);
            }
        }

        private void SetColumnFill(DataGridView grid, string columnName, float fillWeight, int minimumWidth)
        {
            if (grid.Columns.Contains(columnName))
            {
                grid.Columns[columnName].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[columnName].FillWeight = fillWeight;
                grid.Columns[columnName].MinimumWidth = minimumWidth;
            }
        }

        private void AddLabeled(TableLayoutPanel grid, int row, int labelColumn, string label, Control control)
        {
            Label labelControl = MakeLabel(label);
            labelControl.Margin = new Padding(0, 0, 8, 8);
            control.Margin = new Padding(0, 0, 14, 8);
            grid.Controls.Add(labelControl, labelColumn, row);
            grid.Controls.Add(control, labelColumn + 1, row);
        }

        private void SelectComboValue(ComboBox combo, string value, string fallback)
        {
            int index = combo.Items.IndexOf(value);
            if (index < 0)
            {
                index = combo.Items.IndexOf(fallback);
            }

            combo.SelectedIndex = index < 0 && combo.Items.Count > 0 ? 0 : index;
        }

        private decimal DecimalFrom(TextBox textBox)
        {
            decimal value;
            if (textBox == null)
            {
                return 0m;
            }

            if (LedgerValidation.TryParseMoney(textBox.Text, out value))
            {
                return value;
            }

            return 0m;
        }

        private bool ValidateMoneyField(string fieldName, TextBox textBox, out decimal value)
        {
            value = 0m;
            if (!LedgerValidation.TryParseMoney(textBox.Text, out value))
            {
                ShowValidation(textBox, fieldName + " must be a valid number.");
                MessageBox.Show(fieldName + " must be a valid number.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox.Focus();
                return false;
            }

            if (value < 0m)
            {
                ShowValidation(textBox, fieldName + " cannot be negative.");
                MessageBox.Show(fieldName + " cannot be negative.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox.Focus();
                return false;
            }

            ClearValidation();
            return true;
        }

        private bool ValidateNonNegativeMoney(string fieldName, TextBox textBox)
        {
            decimal value = DecimalFrom(textBox);
            if (value < 0m)
            {
                ShowValidation(textBox, fieldName + " cannot be negative.");
                MessageBox.Show(fieldName + " cannot be negative.", "Invalid amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox.Focus();
                return false;
            }

            ClearValidation();
            return true;
        }

        private bool OrNumberExists(string orNumber)
        {
            if (String.IsNullOrEmpty(orNumber))
            {
                return false;
            }

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

                    foreach (PaymentRecord payment in assessment.Payments)
                    {
                        if (String.Equals(payment.OrNumber, orNumber, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string Money(decimal value)
        {
            return value.ToString("N2");
        }

        private string Safe(string value)
        {
            return value == null ? "" : value;
        }
    }
}
