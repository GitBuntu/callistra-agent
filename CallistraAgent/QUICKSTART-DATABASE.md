# Quick Setup: CallistraAgent Database

## 🚀 Quick Start (Choose One Method)

### Method 1: PowerShell Script (Windows - Recommended)
```powershell
cd D:\source\callistra-agent\CallistraAgent
.\setup-database.ps1
```
The script will guide you through the setup interactively.

### Method 2: SQL Server Management Studio (SSMS)
1. Open SSMS and connect to your SQL Server
2. Open: `D:\source\callistra-agent\CallistraAgent\setup-database.sql`
3. Press **F5** to execute
4. (Optional) Run `insert-test-data.sql` for test members

### Method 3: Azure Data Studio
1. Open Azure Data Studio and connect to your SQL Server
2. Open: `D:\source\callistra-agent\CallistraAgent\setup-database.sql`
3. Click **Run**
4. (Optional) Run `insert-test-data.sql` for test members

### Method 4: Command Line (if sqlcmd is installed)

**Windows (PowerShell):**
```powershell
cd D:\source\callistra-agent\CallistraAgent
sqlcmd -S localhost -E -C -i setup-database.sql
sqlcmd -S localhost -E -C -i insert-test-data.sql  # Optional: test data
```

**Linux/macOS/WSL:**
```bash
cd /d/source/callistra-agent/CallistraAgent
./setup-database.sh
```

### Method 5: Entity Framework Core Migrations
```bash
cd src/CallistraAgent.Functions
dotnet ef migrations add InitialCreate
dotnet ef database update
```
*Note: Update connection string in local.settings.json first*

## 📋 What Gets Created

- **Database**: `CallistraAgent`
- **Tables**:
  - `Members` (8 columns, 2 indexes, 1 trigger)
  - `CallSessions` (8 columns, 4 indexes, 1 trigger)
  - `CallResponses` (6 columns, 2 indexes)

## 🔧 Connection String

After setup, update `src/CallistraAgent.Functions/local.settings.json`:

**Windows Authentication:**
```json
"ConnectionStrings__CallistraAgentDb": "Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;"
```

**SQL Server Authentication:**
```json
"ConnectionStrings__CallistraAgentDb": "Server=localhost;Database=CallistraAgent;User ID=sa;Password=YourPassword;TrustServerCertificate=true;"
```

**Azure SQL:**
```json
"ConnectionStrings__CallistraAgentDb": "Server=tcp:yourserver.database.windows.net,1433;Database=CallistraAgent;User ID=youruser;Password=yourpass;Encrypt=True;"
```

## 🧪 Test Data (Optional)

5 test members are inserted if you run `insert-test-data.sql`:
- John Doe (+18005551001) - Diabetes Care - Active
- Jane Smith (+18005551002) - Wellness Program - Active
- Bob Johnson (+18005551003) - Heart Health - Active
- Alice Williams (+18005551004) - Mental Health - Active
- Charlie Brown (+18005551005) - Preventive Care - Pending

## ✅ Verify Installation

Run this query in SSMS or Azure Data Studio:

```sql
USE CallistraAgent;
SELECT name, type_desc FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo');
SELECT COUNT(*) AS MemberCount FROM Members;
```

Expected output: 3 tables, test data count

## 🆘 Troubleshooting

### "SSL Provider: Certificate chain not trusted"
- Add `-C` flag to trust the server certificate (for local development):
  ```powershell
  sqlcmd -S localhost -E -C -i setup-database.sql
  ```
- Or use the PowerShell script which handles this automatically

### "sqlcmd not found"
- **Install**: Download [SQL Server Command Line Tools](https://learn.microsoft.com/sql/tools/sqlcmd/sqlcmd-utility)
- **Alternative**: Use SSMS or Azure Data Studio (GUI methods above)

### "Cannot connect to SQL Server"
- Ensure SQL Server service is running
- Try different server names: `localhost`, `(localdb)\MSSQLLocalDB`, or `.\SQLEXPRESS`
- Check Windows Firewall settings

### "Permission denied"
- Run as Administrator
- Ensure your user has `dbcreator` role in SQL Server

### "Database already exists"
To recreate:
```sql
USE master;
DROP DATABASE CallistraAgent;
-- Then re-run setup-database.sql
```

## 📚 Full Documentation

See [DATABASE-SETUP.md](DATABASE-SETUP.md) for complete instructions.

## ⏭️ Next Steps

1. ✅ Database created
2. Update `local.settings.json` with connection string
3. Configure Azure Communication Services settings
4. Run: `cd src/CallistraAgent.Functions && func start`
5. Test: `curl -X POST http://localhost:7071/api/calls/initiate/1`
