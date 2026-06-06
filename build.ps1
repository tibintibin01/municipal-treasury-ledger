$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$distDir = Join-Path $projectDir "dist"
$exePath = Join-Path $distDir "MunicipalTreasuryLedger.exe"
$sqliteDll = Join-Path $projectDir "lib\System.Data.SQLite.dll"
$sqliteNativeDir = Join-Path $distDir "x64"

New-Item -ItemType Directory -Force -Path $distDir | Out-Null

$compilerCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$compiler = $null
foreach ($candidate in $compilerCandidates) {
    if (Test-Path $candidate) {
        $compiler = $candidate
        break
    }
}

if ($null -eq $compiler) {
    throw "C# compiler was not found. Install Visual Studio or the .NET SDK."
}

if (!(Test-Path $sqliteDll)) {
    throw "SQLite provider was not found at $sqliteDll"
}

Push-Location $projectDir
try {
    & $compiler `
        /nologo `
        /target:winexe `
        /platform:anycpu `
        /optimize+ `
        /out:$exePath `
        /win32manifest:app.manifest `
        /reference:System.dll `
        /reference:System.Core.dll `
        /reference:System.Data.dll `
        /reference:System.Drawing.dll `
        /reference:System.IO.Compression.dll `
        /reference:System.IO.Compression.FileSystem.dll `
        /reference:System.Security.dll `
        /reference:System.Windows.Forms.dll `
        /reference:System.Windows.Forms.DataVisualization.dll `
        /reference:System.Xml.dll `
        /reference:$sqliteDll `
        AssemblyInfo.cs Program.cs Models.cs LedgerValidation.cs CsvImportService.cs ExcelExportService.cs ArchiveService.cs DataProtectionService.cs SecurityService.cs AuditService.cs AuditHashService.cs AuditChangeFormatter.cs BackupService.cs BackupVerificationService.cs SystemHealthService.cs EncryptedBackupService.cs EncryptedDatabaseContainerService.cs DelinquencyService.cs ReceiptVerificationService.cs QrCodeService.cs PrintableReportService.cs LedgerServices.cs TreasuryDataStore.cs LoginForm.cs ChangePasswordForm.cs BackupPasswordForm.cs ImportPreviewForm.cs ArchivePurgeForm.cs MainForm.cs MainForm.Dashboard.cs MainForm.Owner.cs MainForm.Assessment.cs MainForm.Fees.cs MainForm.Payment.cs MainForm.Verification.cs MainForm.Report.cs MainForm.Settings.cs MainForm.Audit.cs MainForm.Users.cs MainForm.Services.cs MainForm.FileSession.cs MainForm.Helpers.cs

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Write-Host "Built: $exePath"

    Copy-Item -LiteralPath $sqliteDll -Destination (Join-Path $distDir "System.Data.SQLite.dll") -Force
    New-Item -ItemType Directory -Force -Path $sqliteNativeDir | Out-Null
    Copy-Item -LiteralPath (Join-Path $projectDir "lib\x64\e_sqlite3.dll") -Destination (Join-Path $sqliteNativeDir "e_sqlite3.dll") -Force
}
finally {
    Pop-Location
}
