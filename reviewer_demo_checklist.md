# Business Tax & Permit Collection System - Reviewer Demo Checklist

Version: v0.3.43

## Positioning

This app is a desktop ledger for business registration, renewal assessment, business tax and permit fee recording, payment tracking, reporting, backup, audit review, and basic receipt verification.

It is not positioned as a complete Municipal Treasury replacement. It does not cover Real Property Tax, market fees, cashiering for all revenue sources, or multi-user network operation.

## Suggested Demo Flow

1. Login
   - Use an admin or treasurer account.
   - Confirm the app opens full screen.
   - Confirm role-based tabs appear based on the signed-in user.

2. Business owner record
   - Create a sample owner/business profile.
   - Enter TIN, contact number, line of business, status, registration type, and Data Privacy consent.
   - Try an invalid TIN/contact format and confirm inline validation highlights the field.

3. Yearly assessment
   - Add a current-year assessment.
   - Enter capital, gross sales, business tax, mayor's permit, fees, surcharge, and penalty.
   - Confirm total, paid, and balance update.
   - Try invalid or negative amounts and confirm validation feedback.

4. Fee catalog
   - Add a fee item in the Fees tab.
   - Apply the fee to an assessment.
   - Confirm the fee amount and remarks update.

5. Payment recording
   - Add a quarterly or annual payment with OR number and date.
   - Confirm the assessment balance updates.
   - Try a duplicate OR number and confirm the app rejects it.
   - Use Find OR to locate the payment.

6. Dashboard and delinquency
   - Open the Dashboard.
   - Run the delinquency check.
   - Confirm summary totals, collection rate, charts, top balances, recent payments, schedule totals, and line-of-business totals render.

7. Reports and print preview
   - Preview collection summary.
   - Preview delinquent list.
   - Preview delinquency notices.
   - Preview a payment receipt from the Payments tab.
   - Confirm QR verification data appears on receipts/notices where applicable.

8. Export and archive
   - Export the ledger to Excel.
   - Import or export Excel/CSV if needed.
   - Preview Archive/Purge using an old year before actually purging.

9. Backup and restore
   - Create a backup.
   - Confirm backup verification succeeds.
   - Review automatic backup folder settings.
   - Confirm Restore Last is visible for Admin/Treasurer users.

10. Security and audit
    - Open Audit Log.
    - Confirm recent create/update/delete/backup/login actions appear.
    - Verify audit chain.
    - Open Users tab as Admin and confirm role/user management is available.

11. Appearance
    - Use the Theme header button to enable dark mode, or enable it in Settings under Appearance.
    - Save, close, and reopen the app.
    - Confirm dark mode is applied.
    - Confirm controls remain readable on the target monitor.

## Known Limitations

- Single-workstation desktop app; no networked multi-user concurrency.
- SQLite is used with DPAPI field protection, but not SQLCipher full-database encryption.
- Use BitLocker and restricted Windows accounts for production machines.
- Receipt preview is a taxpayer-facing ledger receipt/reference printout, not a certified official government OR replacement.
- Real Property Tax and other treasury modules are intentionally outside this app's scope.

## Reviewer Questions to Ask

- Are the owner, assessment, and payment fields complete for our local office process?
- Are fee categories and report columns aligned with the municipal forms used by the office?
- Which users should be Admin, Treasurer, and Cashier?
- Where should daily backups be stored outside the local PC?
- What report format should be prioritized next for official submission?
