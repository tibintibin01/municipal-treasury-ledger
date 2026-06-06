using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace MunicipalTreasuryLedger
{
    public partial class MainForm : Form, IMessageFilter
    {
        private static Color WindowBack = Color.FromArgb(246, 248, 251);
        private static Color SurfaceBack = Color.White;
        private static Color SidebarBack = Color.FromArgb(240, 247, 247);
        private static Color Accent = Color.FromArgb(15, 118, 110);
        private static Color AccentDark = Color.FromArgb(17, 94, 89);
        private static Color AccentSoft = Color.FromArgb(222, 244, 241);
        private static Color TextMain = Color.FromArgb(31, 41, 55);
        private static Color TextMuted = Color.FromArgb(99, 111, 128);
        private static Color BorderLine = Color.FromArgb(210, 218, 228);
        private static Color Danger = Color.FromArgb(185, 28, 28);
        private static Color InputBack = Color.White;
        private static Color GridAltBack = Color.FromArgb(248, 250, 252);
        private static Color ChartGridLine = Color.FromArgb(235, 240, 245);
        private static bool DarkThemeEnabled;
        private const int InactivityLockMinutes = 15;

        private readonly TreasuryDataStore dataStore;
        private LedgerDatabase database;
        private readonly UserAccount currentUser;
        private BusinessOwner selectedOwner;
        private YearlyAssessment selectedAssessment;
        private bool loading;
        private bool lockingForInactivity;
        private DateTime lastActivityAt;
        private Timer inactivityTimer;

        private TextBox searchBox;
        private DataGridView ownerList;
        private string ownerSortColumn = "business";
        private bool ownerSortAscending = true;
        private Label ownerSearchCountLabel;
        private Label dataFileLabel;

        private TextBox ownerNameText;
        private TextBox businessNameText;
        private TextBox ownerAddressText;
        private TextBox businessAddressText;
        private TextBox contactNumberText;
        private TextBox lineOfBusinessText;
        private TextBox tinText;
        private ComboBox statusCombo;
        private ComboBox registrationTypeCombo;
        private TextBox ownerRemarksText;
        private CheckBox privacyConsentCheck;
        private DateTimePicker privacyConsentDatePicker;
        private ComboBox privacyConsentMethodCombo;
        private TextBox privacyNoticeVersionText;

        private DataGridView assessmentsGrid;
        private TextBox assessmentYearText;
        private TextBox capitalText;
        private TextBox grossSalesText;
        private TextBox businessTaxText;
        private TextBox mayorsPermitText;
        private TextBox feesText;
        private ComboBox assessmentFeeCatalogCombo;
        private TextBox surchargeText;
        private TextBox penaltyText;
        private TextBox assessmentRemarksText;
        private Label totalAssessmentLabel;
        private Label totalPaidLabel;
        private Label balanceLabel;

        private ComboBox paymentAssessmentCombo;
        private DateTimePicker paymentDatePicker;
        private TextBox orNumberText;
        private ComboBox paymentScheduleCombo;
        private TextBox paymentAmountText;
        private TextBox paymentRemarksText;
        private DataGridView paymentsGrid;

        private TextBox verificationPayloadText;
        private TextBox verificationOrText;
        private TextBox verificationCodeText;
        private Label verificationStatusLabel;
        private DataGridView verificationGrid;

        private ComboBox dashboardYearFilter;
        private Label dashboardBusinessCountLabel;
        private Label dashboardActiveCountLabel;
        private Label dashboardClosedCountLabel;
        private Label dashboardDelinquentCountLabel;
        private Label dashboardAssessmentLabel;
        private Label dashboardPaidLabel;
        private Label dashboardBalanceLabel;
        private Label dashboardCollectionRateLabel;
        private Chart dashboardMonthlyCollectionsChart;
        private Chart dashboardYearComparisonChart;
        private DataGridView dashboardQuarterGrid;
        private DataGridView dashboardRecentPaymentsGrid;
        private DataGridView dashboardTopBalancesGrid;
        private DataGridView dashboardLineBusinessGrid;

        private DataGridView feeCatalogGrid;
        private TextBox feeCodeText;
        private TextBox feeDescriptionText;
        private TextBox feeAmountText;
        private ComboBox feeActiveCombo;
        private FeeCatalogItem selectedFeeCatalogItem;

        private Label reportSummaryLabel;
        private DataGridView reportGrid;
        private ComboBox reportYearFilter;
        private ComboBox reportStatusFilter;
        private ComboBox reportScheduleFilter;
        private TextBox reportSearchText;
        private TextBox settingsMunicipalityText;
        private TextBox settingsProvinceText;
        private TextBox settingsOfficeText;
        private TextBox settingsTreasurerText;
        private TextBox settingsCollectorText;
        private TextBox settingsDefaultYearText;
        private TextBox settingsSealPathText;
        private TextBox settingsTreasurerSignaturePathText;
        private TextBox settingsCollectorSignaturePathText;
        private TextBox settingsFooterNoteText;
        private TextBox settingsBackupFolderText;
        private TextBox settingsBackupRetentionText;
        private TextBox settingsLockedFiscalYearsText;
        private CheckBox settingsDarkModeCheck;
        private DataGridView systemHealthGrid;
        private DataGridView auditGrid;
        private DataGridView auditDetailsGrid;
        private Label auditChainStatusLabel;
        private DataGridView usersGrid;
        private TextBox userUsernameText;
        private TextBox userFullNameText;
        private TextBox userPasswordText;
        private ComboBox userRoleCombo;
        private ComboBox userActiveCombo;
        private UserAccount selectedUser;
        private Label userLabel;
        private Panel operationStatusPanel;
        private Label operationStatusLabel;
        private ProgressBar operationProgressBar;
        private Label validationStatusLabel;
        private ErrorProvider validationProvider;
        private Dictionary<Control, Color> validationBackColors;

        public MainForm()
            : this(
                new TreasuryDataStore(Path.Combine(Application.StartupPath, "business-ledger.db")),
                null,
                null)
        {
        }

        public MainForm(TreasuryDataStore dataStore, LedgerDatabase database, UserAccount currentUser)
        {
            Text = "Business Tax & Permit Collection System v0.3.44";
            StartPosition = FormStartPosition.CenterScreen;
            Width = 1220;
            Height = 760;
            MinimumSize = new Size(1050, 650);
            WindowState = FormWindowState.Maximized;
            Font = new Font("Segoe UI", 9.5F);
            AutoScaleMode = AutoScaleMode.Dpi;

            this.dataStore = dataStore;
            this.database = database ?? dataStore.Load();
            this.currentUser = currentUser ?? this.database.Users.FirstOrDefault();
            ApplyThemePalette(IsDarkThemeEnabled());

            BackColor = WindowBack;
            ForeColor = TextMain;

            InitializeControls();
            RefreshOwnerList(null);
            RefreshAssessmentFeeCatalogCombo();
            RefreshFeeCatalogGrid(null);
            RefreshReport();
            RefreshAuditLog();
            RefreshUsersGrid();
            LoadSettingsToForm();
            StartInactivityMonitor();
            FormClosed += MainForm_FormClosed;
        }

        private void InitializeControls()
        {
            TableLayoutPanel shell = new TableLayoutPanel();
            shell.Dock = DockStyle.Fill;
            shell.ColumnCount = 1;
            shell.RowCount = 2;
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.BackColor = WindowBack;
            Controls.Add(shell);

            shell.Controls.Add(BuildHeader(), 0, 0);

            TableLayoutPanel workspace = new TableLayoutPanel();
            workspace.Dock = DockStyle.Fill;
            workspace.ColumnCount = 3;
            workspace.RowCount = 1;
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            workspace.BackColor = BorderLine;
            shell.Controls.Add(workspace, 0, 1);

            Panel leftHost = new Panel();
            leftHost.Dock = DockStyle.Fill;
            leftHost.Padding = new Padding(14, 12, 10, 14);
            leftHost.BackColor = SidebarBack;
            leftHost.Controls.Add(BuildOwnerPanel());
            workspace.Controls.Add(leftHost, 0, 0);

            Panel divider = new Panel();
            divider.Dock = DockStyle.Fill;
            divider.BackColor = BorderLine;
            workspace.Controls.Add(divider, 1, 0);

            Panel rightHost = new Panel();
            rightHost.Dock = DockStyle.Fill;
            rightHost.Padding = new Padding(12, 12, 14, 14);
            rightHost.BackColor = WindowBack;
            workspace.Controls.Add(rightHost, 2, 0);

            TabControl tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(102, 38);
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.Multiline = true;
            tabs.Padding = new Point(16, 6);
            tabs.DrawItem += Tabs_DrawItem;
            tabs.TabPages.Add(BuildDashboardTab());
            tabs.TabPages.Add(BuildProfileTab());
            tabs.TabPages.Add(BuildAssessmentTab());
            tabs.TabPages.Add(BuildPaymentTab());
            tabs.TabPages.Add(BuildVerificationTab());
            tabs.TabPages.Add(BuildReportTab());
            if (SecurityService.IsAdmin(currentUser) || SecurityService.IsTreasurer(currentUser))
            {
                tabs.TabPages.Add(BuildFeesTab());
                tabs.TabPages.Add(BuildSettingsTab());
                tabs.TabPages.Add(BuildAuditTab());
            }

            if (SecurityService.IsAdmin(currentUser))
            {
                tabs.TabPages.Add(BuildUsersTab());
            }

            rightHost.Controls.Add(tabs);
        }

        private bool IsDarkThemeEnabled()
        {
            return database != null && database.Settings != null && database.Settings.DarkModeEnabled;
        }

        private static void ApplyThemePalette(bool darkMode)
        {
            DarkThemeEnabled = darkMode;
            if (darkMode)
            {
                WindowBack = Color.FromArgb(17, 24, 39);
                SurfaceBack = Color.FromArgb(31, 41, 55);
                SidebarBack = Color.FromArgb(24, 38, 45);
                Accent = Color.FromArgb(20, 184, 166);
                AccentDark = Color.FromArgb(13, 148, 136);
                AccentSoft = Color.FromArgb(38, 68, 72);
                TextMain = Color.FromArgb(243, 244, 246);
                TextMuted = Color.FromArgb(196, 204, 216);
                BorderLine = Color.FromArgb(75, 85, 99);
                Danger = Color.FromArgb(248, 113, 113);
                InputBack = Color.FromArgb(17, 24, 39);
                GridAltBack = Color.FromArgb(24, 34, 48);
                ChartGridLine = Color.FromArgb(55, 65, 81);
                return;
            }

            WindowBack = Color.FromArgb(246, 248, 251);
            SurfaceBack = Color.White;
            SidebarBack = Color.FromArgb(240, 247, 247);
            Accent = Color.FromArgb(15, 118, 110);
            AccentDark = Color.FromArgb(17, 94, 89);
            AccentSoft = Color.FromArgb(222, 244, 241);
            TextMain = Color.FromArgb(31, 41, 55);
            TextMuted = Color.FromArgb(99, 111, 128);
            BorderLine = Color.FromArgb(210, 218, 228);
            Danger = Color.FromArgb(185, 28, 28);
            InputBack = Color.White;
            GridAltBack = Color.FromArgb(248, 250, 252);
            ChartGridLine = Color.FromArgb(235, 240, 245);
        }

        private Control BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = SurfaceBack;
            header.Padding = new Padding(24, 14, 24, 14);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
            layout.BackColor = SurfaceBack;

            TableLayoutPanel titleStack = new TableLayoutPanel();
            titleStack.Dock = DockStyle.Fill;
            titleStack.ColumnCount = 1;
            titleStack.RowCount = 3;
            titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            titleStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            titleStack.BackColor = SurfaceBack;

            Label title = new Label();
            title.Text = "Business Tax & Permit Collection System";
            title.UseMnemonic = false;
            title.AutoSize = false;
            title.Dock = DockStyle.Fill;
            title.Font = new Font("Segoe UI Semibold", 13.8F, FontStyle.Bold);
            title.ForeColor = TextMain;
            title.TextAlign = ContentAlignment.BottomLeft;

            Label subtitle = new Label();
            subtitle.Text = "Business registration, renewal assessment, and payment records";
            subtitle.AutoSize = false;
            subtitle.Dock = DockStyle.Fill;
            subtitle.Font = new Font("Segoe UI", 9.5F);
            subtitle.ForeColor = TextMuted;
            subtitle.TextAlign = ContentAlignment.MiddleLeft;

            validationStatusLabel = new Label();
            validationStatusLabel.AutoSize = false;
            validationStatusLabel.Dock = DockStyle.Fill;
            validationStatusLabel.Font = new Font("Segoe UI Semibold", 8.7F, FontStyle.Bold);
            validationStatusLabel.ForeColor = Danger;
            validationStatusLabel.TextAlign = ContentAlignment.TopLeft;
            validationStatusLabel.Visible = false;

            TableLayoutPanel rightStack = new TableLayoutPanel();
            rightStack.Dock = DockStyle.Fill;
            rightStack.ColumnCount = 1;
            rightStack.RowCount = 3;
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rightStack.BackColor = SurfaceBack;

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = true;
            actions.Padding = new Padding(0, 0, 0, 0);

            Button exportButton = MakeHeaderButton("Export");
            exportButton.Width = 112;
            exportButton.Click += ExportMenu_Click;
            actions.Controls.Add(exportButton);

            Button importButton = MakeHeaderButton("Import");
            importButton.Width = 112;
            importButton.Click += ImportCsv_Click;
            actions.Controls.Add(importButton);

            Button restoreButton = MakeHeaderButton("Restore");
            restoreButton.Width = 112;
            restoreButton.Click += RestoreBackup_Click;
            actions.Controls.Add(restoreButton);

            Button backupButton = MakeHeaderButton("Backup");
            backupButton.Width = 112;
            backupButton.Click += BackupData_Click;
            actions.Controls.Add(backupButton);

            Button backupFolderButton = MakeHeaderButton("Backup To");
            backupFolderButton.Width = 126;
            backupFolderButton.Click += ConfigureBackupFolder_Click;
            actions.Controls.Add(backupFolderButton);

            Button saveButton = MakeHeaderButton("Save Data");
            saveButton.Width = 118;
            saveButton.Click += SaveMenu_Click;
            actions.Controls.Add(saveButton);

            Button themeButton = MakeHeaderButton("Theme");
            themeButton.Width = 108;
            themeButton.Click += ToggleTheme_Click;
            actions.Controls.Add(themeButton);

            Button passwordButton = MakeHeaderButton("Password");
            passwordButton.Width = 122;
            passwordButton.Click += ChangePassword_Click;
            actions.Controls.Add(passwordButton);

            userLabel = new Label();
            userLabel.Text = CurrentUserText();
            userLabel.AutoSize = false;
            userLabel.Dock = DockStyle.Fill;
            userLabel.ForeColor = CurrentUserTextColor();
            userLabel.TextAlign = ContentAlignment.TopRight;

            Panel line = new Panel();
            line.Dock = DockStyle.Bottom;
            line.Height = 1;
            line.BackColor = BorderLine;

            titleStack.Controls.Add(title, 0, 0);
            titleStack.Controls.Add(subtitle, 0, 1);
            titleStack.Controls.Add(validationStatusLabel, 0, 2);

            layout.Controls.Add(titleStack, 0, 0);
            rightStack.Controls.Add(actions, 0, 0);
            rightStack.Controls.Add(userLabel, 0, 1);
            rightStack.Controls.Add(BuildOperationStatusPanel(), 0, 2);
            layout.Controls.Add(rightStack, 1, 0);

            header.Controls.Add(layout);
            header.Controls.Add(line);
            return header;
        }

        private Control BuildOperationStatusPanel()
        {
            operationStatusPanel = new Panel();
            operationStatusPanel.Dock = DockStyle.Fill;
            operationStatusPanel.Visible = false;
            operationStatusPanel.BackColor = SurfaceBack;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Top;
            layout.Height = 24;
            layout.ColumnCount = 2;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

            operationStatusLabel = new Label();
            operationStatusLabel.Dock = DockStyle.Fill;
            operationStatusLabel.ForeColor = AccentDark;
            operationStatusLabel.Font = new Font("Segoe UI Semibold", 8.6F, FontStyle.Bold);
            operationStatusLabel.TextAlign = ContentAlignment.MiddleRight;

            operationProgressBar = new ProgressBar();
            operationProgressBar.Dock = DockStyle.Fill;
            operationProgressBar.Style = ProgressBarStyle.Marquee;
            operationProgressBar.MarqueeAnimationSpeed = 30;
            operationProgressBar.Margin = new Padding(8, 4, 0, 4);

            layout.Controls.Add(operationStatusLabel, 0, 0);
            layout.Controls.Add(operationProgressBar, 1, 0);
            operationStatusPanel.Controls.Add(layout);
            return operationStatusPanel;
        }

        private string CurrentUserText()
        {
            if (currentUser == null)
            {
                return "Signed in: unknown";
            }

            string name = String.IsNullOrEmpty(currentUser.FullName) ? currentUser.Username : currentUser.FullName;
            string text = "Signed in: " + name + " (" + currentUser.Role + ")";
            if (SecurityService.IsUsingDefaultAdminPassword(currentUser))
            {
                text += " - temporary password active";
            }

            return text;
        }

        private Color CurrentUserTextColor()
        {
            return SecurityService.IsUsingDefaultAdminPassword(currentUser) ? Danger : TextMuted;
        }

        private void Tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null)
            {
                return;
            }

            bool selected = e.Index == tabs.SelectedIndex;
            Rectangle bounds = e.Bounds;
            bounds.Inflate(-3, -4);

            using (SolidBrush background = new SolidBrush(selected ? Accent : SurfaceBack))
            using (SolidBrush textBrush = new SolidBrush(selected ? Color.White : TextMain))
            using (Pen border = new Pen(selected ? Accent : BorderLine))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                e.Graphics.FillRectangle(background, bounds);
                e.Graphics.DrawRectangle(border, bounds);
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, Font, textBrush, bounds, format);
            }
        }

    }
}
