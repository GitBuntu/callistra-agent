# Quick Start Guide: Minimal Healthcare Call Agent

**Feature**: 001-minimal-call-agent  
**Last Updated**: January 10, 2026  
**Estimated Setup Time**: 60 minutes

This guide walks you through setting up the minimal healthcare call agent from scratch to your first successful call.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- Azure subscription with permissions to create resources
- SQL Server (Azure SQL Database or local instance)
- [Azure Dev Tunnels](https://learn.microsoft.com/azure/developer/dev-tunnels/get-started) for local webhook testing

---

## Phase 1: Azure Resource Setup (20 minutes)

### Step 1.1: Create Azure Communication Services Resource

```bash
# Login to Azure
az login

# Create resource group (if needed)
az group create --name rg-callistra --location eastus

# Create Azure Communication Services resource
az communication create \
  --name acs-callistra-dev \
  --resource-group rg-callistra \
  --data-location UnitedStates

# Get connection string
az communication list-key \
  --name acs-callistra-dev \
  --resource-group rg-callistra \
  --query primaryConnectionString -o tsv
```

**Save the connection string** - you'll need it for configuration.

### Step 1.2: Purchase Phone Number

```bash
# Search for available phone numbers (US toll-free example)
az communication phonenumber list-available \
  --country-code US \
  --phone-number-type tollFree \
  --assignment-type application \
  --capabilities calling

# Purchase a phone number (replace with actual number from search)
az communication phonenumber buy \
  --phone-number +18005551234 \
  --resource-group rg-callistra \
  --communication-service-name acs-callistra-dev
```

**Note**: Phone number provisioning may take 5-10 minutes. Alternatively, use the [Azure Portal](https://portal.azure.com) UI.

### Step 1.3: Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name sql-callistra-dev \
  --resource-group rg-callistra \
  --location eastus \
  --admin-user callistradmin \
  --admin-password 'YourStrongPassword123!'

# Create database
az sql db create \
  --name CallAgentDb \
  --server sql-callistra-dev \
  --resource-group rg-callistra \
  --service-objective Basic

# Allow Azure services to access
az sql server firewall-rule create \
  --name AllowAzureServices \
  --server sql-callistra-dev \
  --resource-group rg-callistra \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Get connection string
az sql db show-connection-string \
  --name CallAgentDb \
  --server sql-callistra-dev \
  --client ado.net
```

**Replace** `<username>` and `<password>` in the connection string with your admin credentials.

---

## Phase 2: Local Project Setup (15 minutes)

### Step 2.1: Create Azure Functions Project

```bash
# Navigate to repository root
cd /path/to/callistra-agent

# Create solution file
dotnet new sln --name CallAgent

# Create Azure Functions project
dotnet new func --name CallAgent.Functions --worker-runtime dotnet-isolated --target-framework net8.0
cd CallAgent.Functions

# Add to solution
cd ..
dotnet sln add src/CallAgent.Functions/CallAgent.Functions.csproj
```

### Step 2.2: Install Dependencies

```bash
cd src/CallAgent.Functions

# Azure Communication Services SDK
dotnet add package Azure.Communication.CallAutomation --version 1.2.0

# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0

# Configuration
dotnet add package Microsoft.Extensions.Configuration.UserSecrets --version 8.0.0
```

### Step 2.3: Configure Local Settings

Create `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "AzureCommunicationServices__ConnectionString": "<YOUR-ACS-CONNECTION-STRING>",
    "AzureCommunicationServices__PhoneNumber": "+18005551234",
    "AzureCommunicationServices__CallbackBaseUrl": "https://<tunnel-id>.devtunnels.ms",
    
    "ConnectionStrings__CallAgentDb": "Server=tcp:sql-callistra-dev.database.windows.net,1433;Database=CallAgentDb;User ID=callistradmin;Password=YourStrongPassword123!;Encrypt=True;TrustServerCertificate=False;"
  }
}
```

**Note**: Replace placeholders with your actual values. We'll set up the dev tunnel in the next step.

### Step 2.4: Set Up Dev Tunnel

```bash
# Install Azure Dev Tunnels
dotnet tool install -g Microsoft.DevTunnels.Cli

# Login
devtunnel user login

# Create persistent tunnel
devtunnel create --allow-anonymous

# Expose port 7071 (Functions default)
devtunnel port create -p 7071

# Start tunnel (keep this running in a separate terminal)
devtunnel host

# Note the tunnel URL (e.g., https://abc123xyz.devtunnels.ms)
```

**Update** `local.settings.json` with your tunnel URL in the `CallbackBaseUrl` setting.

---

## Phase 3: Database Initialization (10 minutes)

### Step 3.1: Run Initial Schema Script

Connect to your SQL database using Azure Data Studio or SQL Server Management Studio, then run:

```sql
-- Members table
CREATE TABLE Members (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(20) NOT NULL,
    Program NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT CHK_Member_Status CHECK (Status IN ('Active', 'Pending', 'Inactive')),
    CONSTRAINT UQ_Member_PhoneNumber UNIQUE (PhoneNumber)
);

CREATE INDEX IX_Members_Status ON Members(Status);

-- CallSessions table
CREATE TABLE CallSessions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT NOT NULL,
    CallConnectionId NVARCHAR(100) NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Initiated',
    StartTime DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndTime DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_CallSession_Member FOREIGN KEY (MemberId) REFERENCES Members(Id),
    CONSTRAINT CHK_CallSession_Status CHECK (Status IN ('Initiated', 'Ringing', 'Connected', 'Completed', 'Disconnected', 'Failed', 'NoAnswer')),
    CONSTRAINT CHK_CallSession_EndTime CHECK (EndTime IS NULL OR EndTime >= StartTime),
    CONSTRAINT UQ_CallSession_CallConnectionId UNIQUE (CallConnectionId)
);

CREATE INDEX IX_CallSessions_MemberId ON CallSessions(MemberId);
CREATE INDEX IX_CallSessions_Status ON CallSessions(Status);
CREATE INDEX IX_CallSessions_CallConnectionId ON CallSessions(CallConnectionId);
CREATE INDEX IX_CallSessions_StartTime ON CallSessions(StartTime DESC);

-- CallResponses table
CREATE TABLE CallResponses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CallSessionId INT NOT NULL,
    QuestionNumber INT NOT NULL,
    QuestionText NVARCHAR(500) NOT NULL,
    ResponseValue INT NOT NULL,
    RespondedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_CallResponse_CallSession FOREIGN KEY (CallSessionId) REFERENCES CallSessions(Id),
    CONSTRAINT CHK_CallResponse_QuestionNumber CHECK (QuestionNumber BETWEEN 1 AND 3),
    CONSTRAINT CHK_CallResponse_ResponseValue CHECK (ResponseValue IN (1, 2)),
    CONSTRAINT UQ_CallResponse_Question UNIQUE (CallSessionId, QuestionNumber)
);

CREATE INDEX IX_CallResponses_CallSessionId ON CallResponses(CallSessionId);
```

### Step 3.2: Insert Test Member

```sql
INSERT INTO Members (FirstName, LastName, PhoneNumber, Program, Status)
VALUES ('Test', 'Member', '+15555551234', 'Diabetes Care', 'Active');

-- Note the Id (likely 1) for testing
SELECT Id, FirstName, LastName, PhoneNumber FROM Members;
```

**Replace** `+15555551234` with a real phone number you can answer (your cell phone for testing).

---

## Phase 4: Implement Core Functions (Implementation Phase)

At this point, you should have:
- ✅ Azure Communication Services resource with phone number
- ✅ Azure SQL Database with schema
- ✅ Azure Functions project with dependencies
- ✅ Dev tunnel running
- ✅ Test member in database

**Next steps** (covered in tasks.md):
1. Implement CallInitiation.cs function
2. Implement CallEvents.cs webhook
3. Implement CallStatus.cs query endpoint
4. Implement CallService.cs business logic
5. Implement EF Core DbContext
6. Write integration tests

---

## Phase 5: First Call Test (15 minutes)

### Step 5.1: Start Local Functions

```bash
cd src/CallAgent.Functions
func start
```

You should see:

```
Functions:
  CallInitiation: [POST] http://localhost:7071/api/calls/initiate/{memberId}
  CallEvents: [POST] http://localhost:7071/api/calls/events
  CallStatus: [GET] http://localhost:7071/api/calls/status/{callConnectionId}
```

### Step 5.2: Get Function Key

```bash
# The function key is displayed in the console output
# Look for: "Http Functions: ... x-functions-key: <KEY>"
```

### Step 5.3: Initiate Test Call

```bash
# Replace with your function key and member ID
curl -X POST "http://localhost:7071/api/calls/initiate/1" \
  -H "x-functions-key: YOUR_FUNCTION_KEY" \
  -H "Content-Type: application/json"
```

**Expected response**:

```json
{
  "callSessionId": 1,
  "memberId": 1,
  "status": "Initiated",
  "startTime": "2026-01-10T14:30:00Z",
  "callbackUrl": "https://abc123xyz.devtunnels.ms/api/calls/events"
}
```

### Step 5.4: Answer the Phone

Your test phone number should ring within 5 seconds. Answer the call and:

1. **Listen** for the first question (identity confirmation)
2. **Press 1** for yes or **Press 2** for no
3. **Listen** for the second question (program awareness)
4. **Press 1** or **2**
5. **Listen** for the third question (assistance needs)
6. **Press 1** or **2**

The call should end automatically after all questions are answered.

### Step 5.5: Verify Call Status

```bash
# Use the callConnectionId from the CallConnected event logs
curl -X GET "http://localhost:7071/api/calls/status/<callConnectionId>" \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

**Expected response**:

```json
{
  "callSessionId": 1,
  "memberId": 1,
  "memberName": "Test Member",
  "callConnectionId": "abc123-def456",
  "status": "Completed",
  "startTime": "2026-01-10T14:30:00Z",
  "endTime": "2026-01-10T14:33:00Z",
  "duration": 180,
  "responses": [
    {
      "questionNumber": 1,
      "questionText": "Can you confirm you are Test Member? Press 1 for yes, 2 for no.",
      "responseValue": 1,
      "responseLabel": "Yes",
      "respondedAt": "2026-01-10T14:31:00Z"
    },
    {
      "questionNumber": 2,
      "questionText": "Are you aware you are enrolled in the Diabetes Care program? Press 1 for yes, 2 for no.",
      "responseValue": 1,
      "responseLabel": "Yes",
      "respondedAt": "2026-01-10T14:32:00Z"
    },
    {
      "questionNumber": 3,
      "questionText": "Would you like assistance with your healthcare services? Press 1 for yes, 2 for no.",
      "responseValue": 2,
      "responseLabel": "No",
      "respondedAt": "2026-01-10T14:33:00Z"
    }
  ]
}
```

---

## Troubleshooting

### Phone Doesn't Ring

- **Check**: Is the phone number in E.164 format? (e.g., +15555551234, not 555-555-1234)
- **Check**: Did you purchase the ACS phone number successfully? `az communication phonenumber list`
- **Check**: Is the ACS connection string correct in `local.settings.json`?
- **Check**: Are there errors in the Functions console output?

### Webhook Events Not Received

- **Check**: Is the dev tunnel running? `devtunnel host`
- **Check**: Is the callback URL correct in `local.settings.json`? Should match tunnel URL.
- **Check**: Are webhook requests being blocked? Check `devtunnel` output for incoming requests.
- **Check**: Is the function key valid? Azure Functions requires authorization.

### DTMF Not Recognized

- **Check**: Are you pressing keys clearly? Wait for the prompt to finish.
- **Check**: Is your phone using DTMF tones (not pulse dialing)?
- **Check**: Check Functions logs for `RecognizeCompleted` events - tones should be captured.

### Database Connection Errors

- **Check**: Is the connection string correct? Test with Azure Data Studio.
- **Check**: Is the SQL Server firewall allowing your IP? Add your client IP to firewall rules.
- **Check**: Are the credentials correct? Username and password in connection string.

---

## Next Steps

Once your first call succeeds:

1. **Review** the implementation code generated from tasks.md
2. **Write** integration tests for all 3 endpoints
3. **Deploy** to Azure Functions for production testing
4. **Monitor** call metrics and logs in Azure Portal
5. **Iterate** based on user feedback

---

## Clean Up (Optional)

To avoid Azure costs after testing:

```bash
# Delete resource group (removes all resources)
az group delete --name rg-callistra --yes --no-wait

# Or delete individual resources
az communication delete --name acs-callistra-dev --resource-group rg-callistra
az sql db delete --name CallAgentDb --server sql-callistra-dev --resource-group rg-callistra
az sql server delete --name sql-callistra-dev --resource-group rg-callistra
```

---

## Quick Start Complete ✅

You've successfully set up and tested the minimal healthcare call agent!
