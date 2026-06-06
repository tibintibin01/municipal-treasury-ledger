# Business Tax & Permit Collection System

Desktop app for recording business owner registrations, renewals, yearly assessments, and payments.

## What it can do

- Login with user accounts and roles.
- Admin user management for Cashier, Treasurer, and Admin accounts.
- PBKDF2 password hashing for new and changed passwords.
- Account lockout after repeated failed login attempts.
- Auto-lock after inactivity.
- Change-password screen for signed-in users.
- Forced password change when the first-run `admin/admin123` password is still active.
- Automatic daily backup on app startup after successful login.
- Automatic and manual backups are verified after creation.
- Excel `.xlsx` and CSV import for business owners, yearly assessments, and optional payment rows, with preview/validation and a safety backup before import.
- Excel `.xlsx` export plus CSV export for ledger review and Excel-based reporting.
- Archive old assessment years to Excel, with optional purge after archive and safety backup.
- Configurable backup folder via the **Backup To** header button.
- Auto-backup retention cleanup for backups older than 30 days.
- Manual encrypted backup files (`.mtobak`) with password-based AES encryption and integrity check.
- SQLite live database with protected sensitive fields and no separate database master-password prompt before app login.
- Settings tab for LGU/report profile details, including municipality, province, office, treasurer, collector/cashier, default report year, optional seal/logo path, report footer note, and backup settings.
- SQLite database storage with tables, constraints, indexes, and numeric money columns for reporting totals.
- Sensitive text/date SQLite fields use Windows DPAPI. Money columns are stored as numeric values so SQL totals, sorting, and comparisons work correctly.
- One-time migration from the legacy XML file when `business-ledger.db` does not exist.
- Register business owners and business profiles.
- Data Privacy Act / RA 10173 support on owner records: privacy notice text, consent accepted flag, consent date, consent method, and privacy notice version.
- Search business records as you type and sort the sidebar list by business, owner, status, latest year, or latest balance.
- Show search result counts in the business list.
- Reduce tab clutter by showing management tabs based on role.
- Show a busy status and progress indicator during save, import, export, backup, restore, archive, and record update operations.
- Add icon + text styling to action buttons, with red destructive buttons.
- Import business owner/index-card data from Excel or CSV using the **Import** header button.
- Search by owner/business fields plus address, remarks, assessment year, OR number, payment schedule, payment remarks, date, or amount.
- Record yearly assessment details:
  - Year
  - Capital
  - Gross sales
  - Business tax
  - Mayor's permit
  - Fees
  - Surcharge
  - Penalty
- Record payments:
  - Date of payment
  - OR number
  - Payment schedule
  - Amount paid
  - Remarks
- Print taxpayer-facing payment receipt previews from saved payment records.
- Printed payment receipts include a QR code plus readable verification code containing OR number, date, amount, and a hash check value.
- Verify tab for checking scanned receipt QR payloads or manual OR number plus verification code against saved ledger payments.
- Optional treasurer and collector/cashier signature image paths in Settings, rendered on receipts and delinquency notices.
- Lock closed fiscal years from payment add/delete actions through the Settings tab.
- System Health panel in Settings for SQLite integrity, encryption status, audit-chain status, backup age, locked years, record counts, and user-account status.
- Track total assessment, total paid, and balance.
- Dashboard tab for collection totals, collection rate, status counts, monthly collection chart, year-over-year revenue chart, schedule totals, recent payments, top balances, and line-of-business totals.
- Fees tab for maintaining a standard fees and charges catalog, with an Assessment-tab picker that adds active catalog fees into the assessment Fees amount.
- Delinquency detection using quarterly due-date checkpoints, with dashboard/report status and a **Run Check** button.
- Keep an audit log for login, create, update, delete, backup, restore, and export actions.
- Show old/new field-level details for owner, assessment, and user-account updates.
- Store structured audit detail rows for field-level changes and show them in the Audit Log tab when an entry is selected.
- Export audit logs and field-level changes to CSV for external review.
- Print-preview an Audit Trail Review report from the Audit Log tab.
- Seal audit entries with a SHA-256 hash chain and stored chain tip so modified, reordered, or deleted audit records can be detected during review.
- Route owner, assessment, payment, and user-account changes through service classes instead of keeping those rules directly inside the form.
- Split the main form into partial files by workflow so each tab can be reviewed and maintained separately.
- Filter reports by year and payment status.
- Filter reports by year, payment status, payment schedule, and free-text search.
- Print preview for Collection Summary, Delinquent List, and Delinquency Notice letters with LGU profile header/footer settings.
- Export an Excel or CSV report.

## Archive and purge

Use **Settings > Archive/Purge** to export old assessment/payment years to an Excel workbook. Choose an archive-through year, review the count, and save the `.xlsx` archive file.

Purge is optional. If selected, the app first creates a safety database backup, then removes only yearly assessment and payment rows through the selected year. Business owner profiles remain in the live ledger. The action is written to the audit log.

## Default login

On first run only, the app creates a temporary admin account:

```text
Username: admin
Password: admin123
Role: Admin
```

On login, the app forces this temporary password to be changed before the ledger opens. Change operational practice before real use: create named user accounts in the **Users** tab, then avoid sharing the default admin account. The app no longer recreates the default admin account if an existing data file has no users.

## Build

Run this in PowerShell:

```powershell
.\build.ps1
```

The app will be created here:

```text
dist\MunicipalTreasuryLedger.exe
```

## Excel and CSV import

Use **Import** to migrate owner and assessment records from Excel `.xlsx` or CSV. The app first shows a preview with the number of owners, assessments, and payments that will be created or updated, plus validation notes for skipped rows. If you continue from the preview, the app creates a safety backup before committing the import.

Supported headers include:

```text
Owner Name,Business Name,Owner Address,Business Address,Contact Number,Line of Business,TIN,Status,Registration Type,Remarks,Year,Capital,Gross Sales,Business Tax,Mayor's Permit,Fees,Surcharge,Penalty,Payment Date,OR Number,Payment Schedule,Payment Amount,Payment Remarks
```

TIN is used for matching existing owners when available. Otherwise, the app matches by owner name plus business name. Payment rows are imported only when the OR number is not already recorded.

Optional data privacy headers are also supported: `Privacy Consent`, `Consent Date`, `Consent Method`, and `Privacy Notice Version`.

## Data privacy notice

Owner records include a privacy notice and consent tracking fields for RA 10173 review. The notice states that personal and business information is collected and processed for business registration, renewal assessment, collection, reporting, audit, and legally required LGU records. Consent fields are saved, exported, and included in owner audit logs when changed.

## Data file

The app saves the live ledger in a local SQLite database:

```text
dist\business-ledger.db
```

On startup, the app opens this database and then shows the normal user login screen. There is no separate database master-password prompt.

During save, the app may also create:

```text
dist\business-ledger.db.bak
```

That `.bak` file is the latest automatic safety copy of the working database.

Because the live file is a local SQLite database, protect the computer with Windows account controls, screen lock, antivirus, and preferably BitLocker. DPAPI is still used for sensitive text/date fields, so controlled migration is recommended when moving production data to another Windows user/profile.

## Backup and restore

Use the top buttons inside the app:

- **Backup** defaults to an encrypted treasury backup (`.mtobak`) when using the normal live database.
- In the Backup dialog, choosing `Encrypted treasury backup (*.mtobak)` creates a separate password-protected AES backup. Keep that backup password safely because the app cannot recover it.
- In the Backup dialog, choosing `Plain SQLite backup (*.db)` creates a plain technical copy of the database. Handle this carefully and do not use it as the routine office backup.
- **Backup To** sets the folder used for automatic daily backups. The default folder is `dist\Backups`.
- **Restore** loads encrypted `.mtobak`, normal `.db`, or legacy `.xml` backup files and replaces the current ledger.

After backup creation, the app reopens/verifies the backup and checks SQLite integrity plus the audit hash chain. Restore also performs a dry-run verification before replacing the current ledger.

The app creates one automatic backup per calendar day after successful login. Auto-backup files use this naming pattern:

```text
municipal-treasury-auto-YYYYMMDD.db
```

Normal deployments create `.db` automatic backups.

Before restore, the app creates a safety copy like:

```text
business-ledger.db before restore
```

Recommended office routine:

- Back up at the end of every working day.
- Keep at least one backup outside the computer, such as a USB drive.
- Keep monthly backups separately, especially after renewal season.

## Delinquency rules

The dashboard and report mark an assessment as delinquent when the required paid amount is overdue and the assessment still has a balance:

- After March 31: at least 25% should be paid.
- After June 30: at least 50% should be paid.
- After September 30: at least 75% should be paid.
- After December 31: 100% should be paid.

The app runs a delinquency check after successful login and the Dashboard has a **Run Check** button. Owners with overdue assessments are auto-flagged as `Delinquent` unless their status is `Closed` or `Transferred`.

## Printable reports

The **Reports** tab has print-preview buttons for:

- **Print Summary**: collection summary with business counts, assessment/paid/balance totals, schedule totals, top balances, and line-of-business totals.
- **Print Delinq.**: delinquent assessment list with total, paid, balance, and overdue reason.
- **Print Notices**: one printable delinquency notice letter per delinquent assessment for the selected year/report period.

The **Settings** tab controls the printed report heading, footer, backup settings, signatures, and fiscal-year lock. Admin or Treasurer users can set the municipality/province, office name, treasurer, collector/cashier, optional seal/logo image, optional treasurer and collector/cashier signature images, default report year, footer note, and locked fiscal years. Enter locked years such as `2024, 2025`; payments dated in those years cannot be added or deleted.

The Payments tab can preview/print a taxpayer-facing payment receipt from a saved payment record. The receipt is generated from ledger data for taxpayer reference and reconciliation against official treasury records.

The Payments tab also has **Find OR**, which jumps to the owner, assessment, and payment row for a saved OR number.

## Inline validation

The app highlights invalid fields with a red background, shows a field-level error marker, and displays a validation banner under the main title. This is applied to owner, assessment, fee catalog, payment, user, and settings forms so users can see exactly what needs correction.

## Appearance and DPI

The **Settings** tab includes a dark mode option. Save the setting, close the app, and reopen it to apply the appearance change. The executable also includes a Windows DPI-awareness manifest and uses DPI scaling so controls render more reliably on high-resolution displays.

## Restore last save

The **Settings** tab includes **Restore Last** for Admin and Treasurer users. It restores the previous saved database copy (`business-ledger.db.bak`) after verification and keeps a safety copy of the current file before replacing it.

## Review checklist

Use `reviewer_demo_checklist.md` as the suggested walkthrough for senior review and office demo testing.

## Receipt verification

The **Verify** tab checks printed receipt QR payloads or a manual OR number plus verification code against saved ledger payments. A result of `Verified` means the code matches the saved OR number, payment date, amount, assessment year, and business name.

## System health

The **Settings** tab includes a System Health panel. Use **Run Health Check** to review:

- SQLite integrity and foreign-key consistency.
- Database protection status.
- Audit hash-chain verification.
- Latest automatic backup age.
- Locked fiscal years.
- Record and user-account counts.

## Audit review

The **Audit Log** tab shows recent activity and the selected entry's field-level changes. Use:

- **Print Audit** for a printable Audit Trail Review of the latest 100 entries.
- **Export Audit** for a complete CSV with one row per audit entry or field-level change.
- **Verify Chain** to check the audit hash chain and detect tampering with sealed audit rows.

## Notes for the next version

This app uses SQLite for local storage and DPAPI for sensitive text/date fields. It no longer asks for a separate database master password before login.

Important remaining limitations before real municipal deployment:

- This phase does not use SQLCipher full-database encryption. Protect the PC and use BitLocker for production machines.
- Multi-user network access is not supported.
- Payment receipt previews are taxpayer-facing reference printouts, not a certified official government OR replacement.
- Audit entries are tamper-evident through a local hash chain, but there is not yet an external notarization/signature service for proving the chain tip outside the app.

Planned UI/UX phases:

- Phase 1: search result count, clearer delete confirmations, role-based tabs. Done.
- Phase 2: loading/progress feedback for save/import/export/archive and record updates. Done.
- Phase 3: icon + text buttons and visual polish pass. Done.
- Phase 4: inline validation/highlighted fields. Done.
- Phase 5: dark mode and high-DPI layout pass. Done.
- Phase 6: undo/safety restore workflow for recent edits. Done.
