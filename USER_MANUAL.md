# Business Tax & Permit Collection System

User Manual

Version: v0.3.44

Prepared for: Municipal Treasurer's Office staff

## Introduction

The Business Tax & Permit Collection System is a desktop application for recording business registrations, renewal assessments, and payments. It is designed for municipal treasury staff who currently record business owner information, yearly tax assessments, and payments manually.

The app helps the office keep business tax and permit records in one place. It can store owner profiles, yearly assessments, payment records, official receipt numbers, report summaries, backup files, audit logs, and receipt verification details.

The main benefits are:

- Faster searching of business records.
- Cleaner tracking of yearly assessments and balances.
- Safer records through login accounts, backups, and audit logs.
- Easier reporting through Excel, CSV, printable summaries, receipts, and delinquency notices.
- Better review of payment status, delinquent accounts, and collection totals.

This system is for business tax and permit collection records. It is not a full municipal treasury system for all revenue types. It does not replace Real Property Tax systems or certified official government receipt systems.

## Screenshot Placeholders

Use the placeholders below when preparing the printed or PDF version. Replace each placeholder with an actual screenshot from the app.

[Screenshot: Login screen]

[Screenshot: Main dashboard]

[Screenshot: Business owner form]

[Screenshot: Yearly assessment tab]

[Screenshot: Payments tab]

[Screenshot: Reports tab]

[Screenshot: Settings tab]

[Screenshot: Audit log tab]

## Getting Started

### System Requirements

The app is a Windows desktop application. It should be used on an office computer with access limited to authorized treasury staff.

Recommended setup:

- Windows 10 or Windows 11.
- A dedicated folder for the app.
- A backup folder outside the main app folder, preferably on a separate drive or network location.
- BitLocker or another disk protection feature enabled on production computers.

### Accessing the App

1. Open the folder where the app is saved.
2. Double-click `MunicipalTreasuryLedger.exe`.
3. Wait for the login screen to appear.

### First Login

On first run, the app creates a temporary administrator account.

Default first-run login:

- Username: `admin`
- Password: `admin123`

Important: Change the temporary password before using the app for real office records.

### Creating User Accounts

Only an Admin user can create and manage user accounts.

1. Log in as an Admin.
2. Open the **Users** tab.
3. Click **New User**.
4. Enter the username, full name, role, and temporary password.
5. Choose whether the account is active.
6. Click **Save User**.

Available roles:

- Admin: Can manage users, settings, backups, audit review, and records.
- Treasurer: Can manage settings, backups, reports, and treasury records.
- Cashier: Can record business owners, assessments, and payments based on allowed access.

### Logging In

1. Open the app.
2. Enter your username.
3. Enter your password.
4. Click **Login**.

If the username or password is incorrect several times, the account may be temporarily locked.

### Logging Out

To log out, close the app window. The next user must open the app again and log in with their own account.

If the app is left idle, it will lock after a period of inactivity and require login again.

### Changing Your Password

1. Log in to the app.
2. Click the **Password** button in the top header.
3. Enter your current password.
4. Enter the new password.
5. Confirm the new password.
6. Click **Save** or **OK**.

Use a password that is not easy to guess. Avoid using names, birthdays, or simple passwords.

### Resetting a Password

If a user forgets their password:

1. Ask an Admin user to log in.
2. Open the **Users** tab.
3. Select the user account.
4. Enter a new temporary password.
5. Click **Save User**.
6. Tell the user to log in and change the temporary password.

## App Overview

### Main Window

After login, the app opens in full screen. The main window has three areas:

1. Top header: Main command buttons such as Password, Theme, Save Data, Backup, Restore, Import, and Export.
2. Left panel: Business Records search and list.
3. Main workspace: Tabs such as Dashboard, Owner, Assessment, Payments, Verify, Reports, Fees, Settings, Audit Log, and Users.

### Top Header Buttons

- **Password**: Change the current user's password.
- **Theme**: Switch between light mode and dark mode. Close and reopen the app to apply the change.
- **Save Data**: Manually save the current ledger data.
- **Backup To**: Choose the backup folder.
- **Backup**: Create a backup file.
- **Restore**: Restore from a backup file.
- **Import**: Import records from Excel `.xlsx` or CSV `.csv`.
- **Export**: Export ledger records to Excel or CSV.

### Left Business Records Panel

Use this panel to search and select business records.

- Search box: Type owner name, business name, OR number, year, status, address, or remarks.
- New: Start a new business owner record.
- Save: Save the selected business owner record.
- Delete: Delete the selected business owner, if your role allows it.
- Business list: Shows matching businesses and key details.

### Main Tabs

- **Dashboard**: Shows collection summary, charts, recent payments, balances, and delinquency totals.
- **Owner**: Stores owner and business profile details.
- **Assessment**: Records yearly assessment amounts.
- **Payments**: Records payments, OR numbers, schedule, and receipt preview.
- **Verify**: Verifies receipt QR payloads or manual verification codes.
- **Reports**: Shows report data and print-preview options.
- **Fees**: Manages reusable fee items.
- **Settings**: Manages office details, report settings, backup settings, fiscal year locks, and appearance.
- **Audit Log**: Shows who changed what and when.
- **Users**: Manages user accounts and roles.

Some tabs are only visible to Admin or Treasurer users.

## Main Features

### Dashboard

The Dashboard gives a quick view of collection status.

It shows:

- Number of businesses.
- Active, closed, and delinquent counts.
- Total assessment, paid amount, balance, and collection rate.
- Monthly collections chart.
- Year-over-year revenue chart.
- Top unpaid balances.
- Recent payments.
- Payment schedule totals.
- Line of business totals.

To use the Dashboard:

1. Open the **Dashboard** tab.
2. Select a year or choose **All**.
3. Click **Refresh**.
4. Click **Run Check** to update delinquency status.

### Business Owner Records

The Owner tab stores the owner and business profile.

Common fields include:

- Owner name.
- Business name.
- Owner address.
- Business address.
- Contact number.
- Line of business.
- TIN.
- Status.
- Registration type.
- Remarks.
- Data Privacy consent details.

To create a business owner:

1. Click **New** in the left Business Records panel.
2. Open the **Owner** tab.
3. Enter the owner and business details.
4. Complete the Data Privacy consent fields.
5. Click **Save Owner** or **Save**.

To edit a business owner:

1. Search for the business in the left panel.
2. Select the business record.
3. Update the fields.
4. Click **Save Owner** or **Save**.

To delete a business owner:

1. Select the business record.
2. Click **Delete**.
3. Read the confirmation message carefully.
4. Confirm only if you are sure.

Deleting an owner can also delete related assessments and payments. Use this carefully.

### Yearly Assessment

The Assessment tab records the yearly tax and permit amounts for a business.

Common fields include:

- Year.
- Capital.
- Gross sales.
- Business tax.
- Mayor's permit.
- Fees.
- Surcharge.
- Penalty.
- Remarks.

To add a yearly assessment:

1. Select a business owner.
2. Open the **Assessment** tab.
3. Click **New Assessment**.
4. Enter the year.
5. Enter the assessment amounts.
6. Click **Save Assessment**.

To edit an assessment:

1. Select a business owner.
2. Open the **Assessment** tab.
3. Select the assessment row.
4. Update the fields.
5. Click **Save Assessment**.

To delete an assessment:

1. Select the assessment.
2. Click **Delete Assessment**.
3. Confirm only if you are sure.

If the fiscal year is locked in Settings, payments for that year cannot be added or deleted.

### Fee Catalog

The Fees tab stores reusable fee items that can be applied to assessments.

To create a fee item:

1. Open the **Fees** tab.
2. Click **New Fee**.
3. Enter the fee code.
4. Enter the fee description.
5. Enter the amount.
6. Choose whether the fee is active.
7. Click **Save Fee**.

To apply a fee to an assessment:

1. Select a business owner.
2. Open the **Assessment** tab.
3. Choose an active fee item.
4. Click **Apply Fee**.
5. Review the updated Fees amount.
6. Save the assessment.

### Payments

The Payments tab records payments for a selected assessment.

Common fields include:

- Assessment year.
- Payment date.
- OR number.
- Payment schedule.
- Amount.
- Remarks.

To add a payment:

1. Select a business owner.
2. Open the **Assessment** tab and select the correct year.
3. Open the **Payments** tab.
4. Select the assessment year.
5. Enter the payment date.
6. Enter the OR number.
7. Choose the schedule, such as Annual, 1st Quarter, 2nd Quarter, 3rd Quarter, or 4th Quarter.
8. Enter the amount.
9. Click **Add Payment**.

To delete a payment:

1. Select the payment row.
2. Click **Delete Payment**.
3. Confirm only if you are sure.

To find a payment by OR number:

1. Open the **Payments** tab.
2. Enter the OR number.
3. Click **Find OR**.
4. The app will move to the matching owner, assessment, and payment if found.

To preview a receipt:

1. Select a saved payment.
2. Click **Print Receipt**.
3. Review the print preview.
4. Print only if the details are correct.

Note: The payment receipt preview is for taxpayer reference and reconciliation. It is not a certified official government OR replacement.

### Receipt Verification

The Verify tab checks receipt verification information.

To verify by QR payload:

1. Open the **Verify** tab.
2. Paste or scan the QR payload into the payload box.
3. Click **Verify**.
4. Review the result.

To verify manually:

1. Open the **Verify** tab.
2. Enter the OR number.
3. Enter the verification code.
4. Click **Verify**.
5. Review the result.

### Reports

The Reports tab is used for summaries, lists, and printing.

Available report tools include:

- Export CSV.
- Print Summary.
- Print Delinq.
- Print Notices.

To view reports:

1. Open the **Reports** tab.
2. Choose filters such as year, status, schedule, or search text.
3. Review the report grid.

To print a collection summary:

1. Open the **Reports** tab.
2. Set the desired filters.
3. Click **Print Summary**.
4. Review the print preview.
5. Print if correct.

To print a delinquent list:

1. Open the **Reports** tab.
2. Select the year or filters.
3. Click **Print Delinq.**
4. Review the print preview.

To print delinquency notices:

1. Open the **Reports** tab.
2. Select the year or filters.
3. Click **Print Notices**.
4. Review the notices before printing.

### Import Records

The app can import records from Excel `.xlsx` or CSV `.csv`.

To import records:

1. Click **Import** in the top header.
2. Choose an Excel or CSV file.
3. Review the import preview.
4. Check skipped rows and validation notes.
5. Continue only if the preview is correct.
6. The app creates a safety backup before committing the import.

Recommended Excel columns:

- Owner Name
- Business Name
- Line of Business
- Status
- Registration Type
- Year
- Capital
- Gross Sales
- Business Tax
- Mayor's Permit
- Fees
- Surcharge
- Penalty
- Payment Date
- OR Number
- Payment Schedule
- Payment Amount
- Payment Remarks

### Export Records

To export records:

1. Click **Export** in the top header.
2. Choose Excel `.xlsx` or CSV `.csv`.
3. Select the save location.
4. Click **Save**.

### Backup

Backups protect the office from data loss.

To set the backup folder:

1. Click **Backup To**.
2. Choose a backup folder.
3. Click **OK**.

To create a backup:

1. Click **Backup**.
2. Choose the backup type and file location.
3. Enter a backup password if creating an encrypted backup.
4. Wait for verification.
5. Confirm that the app says the backup was saved and verified.

### Restore

Restore should be used carefully because it replaces the current ledger data.

To restore from backup:

1. Click **Restore**.
2. Choose the backup file.
3. Confirm the restore warning.
4. Wait for verification and restore.
5. Review the records after restore.

To restore the previous save:

1. Open **Settings**.
2. Click **Restore Last**.
3. Confirm the warning.
4. Review the records after restore.

### Audit Log

The Audit Log records important user actions.

It can show:

- Login activity.
- Created records.
- Updated records.
- Deleted records.
- Backup and restore activity.
- Settings changes.

To review the audit log:

1. Open the **Audit Log** tab.
2. Select an entry.
3. Review details and field-level changes.
4. Click **Verify Chain** to check audit-chain integrity.
5. Use **Print Audit** or **Export Audit** if needed.

## Account Settings

### Update Profile Details

User profile details are managed by an Admin.

To update a user's details:

1. Log in as Admin.
2. Open the **Users** tab.
3. Select the user.
4. Update the username, full name, role, active status, or password.
5. Click **Save User**.

### Change Password

1. Click **Password** in the top header.
2. Enter the current password.
3. Enter the new password.
4. Confirm the new password.
5. Save the change.

### Notifications

The current version does not send SMS, email, or push notifications.

The app shows important notices inside the app using message boxes, validation banners, dashboard indicators, and report results.

Examples:

- Invalid field messages.
- Backup verification messages.
- Delinquency check results.
- Permission warnings.
- Restore confirmation warnings.

## Common Tasks

### Create a New Business Record

1. Click **New**.
2. Enter owner and business details.
3. Complete Data Privacy consent fields.
4. Click **Save Owner**.

### Edit a Business Record

1. Search for the business.
2. Select the record.
3. Edit the information.
4. Click **Save Owner**.

### Delete a Business Record

1. Select the business.
2. Click **Delete**.
3. Read the warning.
4. Confirm only if you are sure.

### Add a Renewal Assessment

1. Select the business.
2. Open **Assessment**.
3. Click **New Assessment**.
4. Enter the year and amounts.
5. Click **Save Assessment**.

### Record a Payment

1. Select the business and assessment year.
2. Open **Payments**.
3. Enter date, OR number, schedule, and amount.
4. Click **Add Payment**.
5. Check that paid amount and balance updated.

### Track a Delinquent Business

1. Open **Dashboard**.
2. Click **Run Check**.
3. Review delinquent count and unpaid balances.
4. Open **Reports**.
5. Print delinquent list or notices if needed.

### View Reports

1. Open **Reports**.
2. Select filters.
3. Review rows.
4. Print or export if needed.

### Submit or Print a Report

1. Open **Reports**.
2. Select the correct year and filters.
3. Click **Print Summary**, **Print Delinq.**, or **Print Notices**.
4. Review the print preview.
5. Print or save as PDF using the Windows print dialog.

### Import Existing Records from Excel

1. Prepare the Excel file using the recommended columns.
2. Click **Import**.
3. Select the Excel file.
4. Review the preview.
5. Continue only if the preview is correct.

### Make a Backup Before Major Work

1. Click **Backup**.
2. Save the backup file in the approved backup folder.
3. Confirm the backup verification message.
4. Continue with import, archive, purge, or other major changes.

## Troubleshooting

### I cannot log in

Check the username and password. If you forgot your password, ask an Admin to reset it.

### The account is locked

Wait for the lockout period or ask an Admin for help.

### I cannot see some tabs

Your role may not have permission to use those tabs. Ask the Admin or Treasurer.

### Import skipped some rows

Open the import preview notes. Common reasons include missing owner/business name, invalid TIN, invalid year, duplicate OR number, or payment amount greater than the balance.

### Backup failed

Check the backup folder, available disk space, and network drive connection if using a shared folder.

### Dark mode did not apply

Click **Theme** or enable dark mode in **Settings > Appearance**, then close and reopen the app.

## Good Office Practices

- Use one account per staff member.
- Do not share passwords.
- Back up the database daily.
- Keep at least one backup outside the computer.
- Review the audit log regularly.
- Lock old fiscal years when they should no longer be edited.
- Test restore procedures before relying on backups.
- Use BitLocker or another disk protection tool on production computers.

## Important Limitations

- The app is single-workstation and does not support simultaneous multi-user network access.
- It is for business tax and permit collection records only.
- It does not include Real Property Tax.
- Receipt previews are for reference and reconciliation only, not certified official government OR replacement.
- Full SQLCipher database encryption is not currently implemented.
