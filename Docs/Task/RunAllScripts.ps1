# FamilyFirst — Create DB and run all SQL scripts on LocalDB
param(
    [ValidatePattern('\A[A-Za-z0-9_]{1,128}\z')]
    [string]$Database = "FamilyFirstDB"
)

$server     = "(localdb)\MSSQLLocalDB"
$apiRoot    = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptsDir = Join-Path $apiRoot "FamilyFirst.Infrastructure\Data\Scripts"
# Discover every numbered script in order, including 106 and 107. Script 087 is intentionally absent.
$scripts    = @(Get-ChildItem -LiteralPath $scriptsDir -Filter "*.sql" -File -ErrorAction Stop | Sort-Object Name)
if ($scripts.Count -eq 0) { throw "No SQL scripts found in '$scriptsDir'." }

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  FamilyFirst Database Setup" -ForegroundColor Cyan
Write-Host "  Server   : $server" -ForegroundColor Cyan
Write-Host "  Database : $database" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Create database
Write-Host "Step 1: Creating database '$database'..." -ForegroundColor Yellow
sqlcmd -S $server -E -d master -I -b -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$database') BEGIN CREATE DATABASE [$database]; PRINT 'Database created.'; END ELSE PRINT 'Already exists.';"
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR creating database." -ForegroundColor Red; exit 1 }
Write-Host ""

# Step 2: Run all scripts
$total   = $scripts.Count
$passed  = 0
$failed  = 0
$errors  = @()

Write-Host "Step 2: Executing $total scripts..." -ForegroundColor Yellow
Write-Host ""

foreach ($script in $scripts) {
    $num = $passed + $failed + 1
    Write-Host "  [$num/$total] $($script.Name) ... " -NoNewline

    $output   = sqlcmd -S $server -E -d $database -I -b -i $script.FullName 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0) {
        Write-Host "FAILED" -ForegroundColor Red
        $failed++
        $errors += [PSCustomObject]@{ Script = $script.Name; Output = ($output -join "`n") }
        # Stop on the first failure: later scripts depend on earlier ones and would only cascade-fail.
        break
    } else {
        Write-Host "OK" -ForegroundColor Green
        $passed++
    }
}

# Summary
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Passed : $passed / $total" -ForegroundColor Green
Write-Host "  Failed : $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
if ($failed -gt 0) {
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
    foreach ($e in $errors) {
        Write-Host ">> $($e.Script)" -ForegroundColor Red
        Write-Host $e.Output -ForegroundColor DarkYellow
        Write-Host ""
    }
    exit 1
} else {
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Done. '$database' is ready." -ForegroundColor Green
}
