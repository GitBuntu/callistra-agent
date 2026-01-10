# CallistraAgent Database Setup Guide

This guide will help you create the CallistraAgent database and tables on your SQL Server instance.

## Prerequisites

- SQL Server 2019 or later (or SQL Server 2025 as per spec)
- SQL Server Management Studio (SSMS) OR Azure Data Studio OR sqlcmd
- Appropriate permissions to create databases

## Option 1: Using SQL Server Management Studio (SSMS) or Azure Data Studio

### Step 1: Open the Setup Script

1. Open SQL Server Management Studio or Azure Data Studio
2. Connect to your SQL Server instance
3. Open the file: `CallistraAgent/setup-database.sql`

### Step 2: Execute the Script

1. Click **Execute** (or press F5)
2. The script will:
   - Create the `CallistraAgent` database
   - Create 3 tables: `Members`, `CallSessions`, `CallResponses`
   - Create all indexes and constraints
   - Create update triggers for timestamp management

### Step 3: Insert Test Data (Optional)

1. Open the file: `CallistraAgent/insert-test-data.sql`
2. Click **Execute** (or press F5)
3. This will insert 5 test members for development/testing

### Step 4: Verify Installation

Run this query to verify:

```sql
USE CallistraAgent;
GO

SELECT 
    t.name AS TableName,
    COUNT(c.column_id) AS ColumnCount
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
    AND t.name IN ('Members', 'CallSessions', 'CallResponses')
GROUP BY t.name;
```

You should see 3 tables with the correct column counts.

## Option 2: Using sqlcmd (Command Line)

### Windows

```powershell
# Navigate to the CallistraAgent directory
cd D:\source\callistra-agent\CallistraAgent

# Execute setup script (add -C to trust server certificate for local dev)
sqlcmd -S localhost -E -C -i setup-database.sql

# Insert test data (optional)
sqlcmd -S localhost -E -C -i insert-test-data.sql
```

### Linux/macOS

```bash
# Navigate to the CallistraAgent directory
cd /path/to/callistra-agent/CallistraAgent

# Execute setup script (add -C to trust server certificate for local dev)
sqlcmd -S localhost -U sa -P 'YourPassword' -C -i setup-database.sql

# Insert test data (optional)
sqlcmd -S localhost -U sa -P 'YourPassword' -C -i insert-test-data.sql
```

## Option 3: Using Entity Framework Core Migrations

If you prefer to use EF Core migrations instead of SQL scripts:

### Step 1: Create Initial Migration

```bash
cd src/CallistraAgent.Functions
dotnet ef migrations add InitialCreate
```

### Step 2: Apply Migration to Database

```bash
dotnet ef database update
```

**Note**: Make sure your connection string in `local.settings.json` is correct before running migrations.

## Connection Strings

### Local Development (Windows Authentication)

```
Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;
```

### Local Development (SQL Authentication)

```
Server=localhost;Database=CallistraAgent;User ID=sa;Password=YourPassword;TrustServerCertificate=true;
```

### Azure SQL Database

```
Server=tcp:your-server.database.windows.net,1433;Database=CallistraAgent;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;
```

## Update local.settings.json

After creating the database, update your connection string in:
`src/CallistraAgent.Functions/local.settings.json`

```json
{
  "Values": {
    "ConnectionStrings__CallistraAgentDb": "Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

## Troubleshooting

### Error: Database already exists

If you get an error that the database already exists, you can:

1. Drop the existing database:
   ```sql
   USE master;
   DROP DATABASE CallistraAgent;
   ```
2. Re-run `setup-database.sql`

### Error: Permission denied

Ensure your SQL Server user has permissions to:
- Create databases
- Create tables
- Create indexes
- Create triggers

You may need to run SSMS or sqlcmd as Administrator.

### Error: Cannot connect to SQL Server

1. Check if SQL Server service is running
2. Verify the server name (use `localhost` or `(localdb)\MSSQLLocalDB` for local instances)
3. Enable TCP/IP protocol in SQL Server Configuration Manager

## Database Schema Overview

### Members Table
- Stores healthcare program enrollees
- Fields: Id, FirstName, LastName, PhoneNumber, Program, Status
- Unique constraint on PhoneNumber

### CallSessions Table
- Tracks individual call attempts
- Fields: Id, MemberId (FK), CallConnectionId, Status, StartTime, EndTime
- Statuses: Initiated, Ringing, Connected, Completed, Disconnected, Failed, NoAnswer, VoicemailMessage

### CallResponses Table
- Stores member answers to questions
- Fields: Id, CallSessionId (FK), QuestionNumber, QuestionText, ResponseValue, RespondedAt
- One response per question per call (unique constraint)

## Next Steps

After setting up the database:

1. ✅ Update `local.settings.json` with your connection string
2. ✅ Configure Azure Communication Services settings
3. ✅ Run the application: `cd src/CallistraAgent.Functions && func start`
4. ✅ Test the API endpoints

For more information, see the main [README.md](../README.md).
