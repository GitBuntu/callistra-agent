# CallistraAgent Database Setup Script (PowerShell)
# This script creates the database using sqlcmd or provides alternative instructions

Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host "CallistraAgent Database Setup" -ForegroundColor Cyan
Write-Host "==============================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if sqlcmd is available
$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue

if ($sqlcmdPath) {
    Write-Host "✓ sqlcmd found at: $($sqlcmdPath.Source)" -ForegroundColor Green
    Write-Host ""

    # Prompt for server name
    $serverName = Read-Host "Enter SQL Server instance name (default: localhost)"
    if ([string]::IsNullOrWhiteSpace($serverName)) {
        $serverName = "localhost"
    }

    # Prompt for authentication method
    Write-Host ""
    Write-Host "Select authentication method:"
    Write-Host "1. Windows Authentication (Integrated Security)"
    Write-Host "2. SQL Server Authentication"
    $authChoice = Read-Host "Enter choice (1 or 2)"

    Write-Host ""
    Write-Host "Creating database..." -ForegroundColor Yellow

    try {
        if ($authChoice -eq "2") {
            $username = Read-Host "Enter SQL Server username"
            $password = Read-Host "Enter SQL Server password" -AsSecureString
            $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

            sqlcmd -S $serverName -U $username -P $passwordPlain -C -i "setup-database.sql"
        } else {
            sqlcmd -S $serverName -E -C -i "setup-database.sql"
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✓ Database created successfully!" -ForegroundColor Green
            Write-Host ""

            # Prompt for test data
            $insertTestData = Read-Host "Do you want to insert test data? (Y/N)"
            if ($insertTestData -eq "Y" -or $insertTestData -eq "y") {
                Write-Host ""
                Write-Host "Inserting test data..." -ForegroundColor Yellow

                if ($authChoice -eq "2") {
                    sqlcmd -S $serverName -U $username -P $passwordPlain -C -i "insert-test-data.sql"
                } else {
                    sqlcmd -S $serverName -E -C -i "insert-test-data.sql"
                }

                if ($LASTEXITCODE -eq 0) {
                    Write-Host ""
                    Write-Host "✓ Test data inserted successfully!" -ForegroundColor Green
                }
            }

            Write-Host ""
            Write-Host "==============================================================================" -ForegroundColor Cyan
            Write-Host "Setup Complete!" -ForegroundColor Green
            Write-Host "==============================================================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Connection string for local.settings.json:" -ForegroundColor Yellow
            if ($authChoice -eq "2") {
                Write-Host "Server=$serverName;Database=CallistraAgent;User ID=$username;Password=***;TrustServerCertificate=true;" -ForegroundColor White
            } else {
                Write-Host "Server=$serverName;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;" -ForegroundColor White
            }
            Write-Host ""
        } else {
            Write-Host ""
            Write-Host "✗ Error creating database. Check the error messages above." -ForegroundColor Red
            Write-Host ""
        }
    } catch {
        Write-Host ""
        Write-Host "✗ Error: $_" -ForegroundColor Red
        Write-Host ""
    }
} else {
    Write-Host "✗ sqlcmd not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please use one of these alternatives:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Install SQL Server Command-Line Tools" -ForegroundColor Cyan
    Write-Host "  Download from: https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility" -ForegroundColor White
    Write-Host ""
    Write-Host "Option 2: Use SQL Server Management Studio (SSMS)" -ForegroundColor Cyan
    Write-Host "  1. Open SSMS and connect to your SQL Server" -ForegroundColor White
    Write-Host "  2. Open file: $PSScriptRoot\setup-database.sql" -ForegroundColor White
    Write-Host "  3. Click Execute (F5)" -ForegroundColor White
    Write-Host "  4. Optionally run: $PSScriptRoot\insert-test-data.sql" -ForegroundColor White
    Write-Host ""
    Write-Host "Option 3: Use Azure Data Studio" -ForegroundColor Cyan
    Write-Host "  1. Open Azure Data Studio and connect to your SQL Server" -ForegroundColor White
    Write-Host "  2. Open file: $PSScriptRoot\setup-database.sql" -ForegroundColor White
    Write-Host "  3. Click Run" -ForegroundColor White
    Write-Host "  4. Optionally run: $PSScriptRoot\insert-test-data.sql" -ForegroundColor White
    Write-Host ""
    Write-Host "Option 4: Use Entity Framework Core Migrations" -ForegroundColor Cyan
    Write-Host "  cd ..\src\CallistraAgent.Functions" -ForegroundColor White
    Write-Host "  dotnet ef migrations add InitialCreate" -ForegroundColor White
    Write-Host "  dotnet ef database update" -ForegroundColor White
    Write-Host ""
}

Write-Host "For more information, see DATABASE-SETUP.md" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
