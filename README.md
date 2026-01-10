# Callistra Agent - Healthcare Outreach Call Automation

A minimal viable healthcare outreach call agent built with Azure Communication Services and .NET 9. The system initiates outbound calls to members, asks healthcare enrollment verification questions via text-to-speech, captures DTMF responses, and persists call data to Azure SQL Database.

## Features

- **Automated Outbound Calls**: Initiate calls to members using Azure Communication Services
- **Call Status Tracking**: Monitor call lifecycle (Initiated, Ringing, Connected, Completed, Failed, NoAnswer)
- **Real-time Webhooks**: Process call events (connected, disconnected, failed) via CloudEvents
- **Database Persistence**: Entity Framework Core with SQL Server for Members, CallSessions, and CallResponses

## Architecture

- **Runtime**: .NET 9 with Azure Functions v4 (isolated worker model)
- **Telephony**: Azure Communication Services Call Automation SDK v1.2.0
- **Database**: SQL Server 2025 (local) / Azure SQL Database (production)
- **Testing**: xUnit with FluentAssertions and Moq
- **Development Tools**: Azure Functions Core Tools, Microsoft Dev Tunnels for webhook testing

## Project Structure

```
CallistraAgent.sln
├── src/CallistraAgent.Functions/           # Azure Functions project
│   ├── Functions/                          # HTTP-triggered functions
│   │   ├── InitiateCallFunction.cs         # POST /api/calls/initiate/{memberId}
│   │   ├── CallEventWebhookFunction.cs     # POST /api/calls/events
│   │   └── CallStatusFunction.cs           # GET /api/calls/status/{callConnectionId}
│   ├── Services/                           # Business logic
│   │   ├── ICallService.cs
│   │   └── CallService.cs
│   ├── Data/                               # Entity Framework Core
│   │   ├── CallistraAgentDbContext.cs
│   │   └── Repositories/
│   ├── Models/                             # Entities and DTOs
│   │   ├── Member.cs, CallSession.cs, CallResponse.cs
│   │   └── DTOs/
│   ├── Configuration/                      # Options classes
│   └── Constants/                          # Healthcare questions, voicemail messages
├── tests/CallistraAgent.Functions.Tests/   # Unit and integration tests
│   └── Unit/Services/CallServiceTests.cs   # 9 tests, 8 passing
├── CallistraAgent/                         # SQL Database project
│   ├── Tables/                             # SQL table definitions
│   │   ├── Members.sql
│   │   ├── CallSessions.sql
│   │   └── CallResponses.sql
│   └── setup-database.sql                  # Database creation script
└── specs/001-minimal-call-agent/           # Feature specification
    ├── spec.md, plan.md, tasks.md
    ├── data-model.md                       # Entity definitions
    ├── contracts/                          # OpenAPI specs
    ├── checklists/requirements.md          # Quality checklist (✅ PASSED)
    └── research.md                         # Technology decisions
```

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server 2025](https://www.microsoft.com/sql-server/sql-server-downloads) or SQL Server LocalDB
- [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) v4.x
- [Microsoft Dev Tunnels](https://aka.ms/devtunnels) (for webhook testing)
- Azure Communication Services resource with phone number

## Quick Start

### 1. Database Setup

```powershell
# Run from repository root
cd CallistraAgent
./setup-database.ps1
```

Or on Linux/Mac:
```bash
cd CallistraAgent
./setup-database.sh
```

This creates the `CallistraAgent` database with 3 tables: Members, CallSessions, CallResponses.

### 2. Configuration

Create `src/CallistraAgent.Functions/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ConnectionStrings__CallistraAgent": "Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;",
    "AzureCommunicationServices__ConnectionString": "endpoint=https://<your-acs-resource>.communication.azure.com/;accesskey=<your-key>",
    "AzureCommunicationServices__CallbackBaseUrl": "https://<your-devtunnel>.devtunnels.ms",
    "AzureCommunicationServices__SourcePhoneNumber": "+1234567890"
  }
}
```

Replace placeholders:
- `<your-acs-resource>`: Your Azure Communication Services resource name
- `<your-key>`: Your ACS access key
- `<your-devtunnel>`: Your Dev Tunnel URL (see step 4)
- `+1234567890`: Your ACS phone number

### 3. Insert Test Data

```powershell
# From repository root
cd CallistraAgent
sqlcmd -S localhost -d CallistraAgent -E -C -i insert-test-data.sql
```

Creates 5 test members with MemberId 1-5.

### 4. Start Dev Tunnel (for webhooks)

Azure Communication Services requires a publicly accessible webhook endpoint. Dev Tunnels provide this for local development.

```powershell
# Install (once)
winget install Microsoft.devtunnel

# Authenticate with GitHub
devtunnel user login -g

# Host tunnel on Functions port
devtunnel host -p 7071 --allow-anonymous
```

Copy the tunnel URL (e.g., `https://abc123-7071.devtunnels.ms`) and update `CallbackBaseUrl` in `local.settings.json`.

See [DEVTUNNEL-SETUP.md](DEVTUNNEL-SETUP.md) for detailed setup and troubleshooting.

### 5. Run Azure Functions

```powershell
# From repository root
cd src/CallistraAgent.Functions
func start
```

The Functions will start on `http://localhost:7071`.

### 6. Initiate a Test Call

```powershell
# Initiate call to Member 1
curl -X POST http://localhost:7071/api/calls/initiate/1
```

Expected response (202 Accepted):
```json
{
  "callConnectionId": "aHR0cHM6Ly...",
  "callSessionId": 1,
  "memberPhoneNumber": "+1234567890",
  "status": "Initiated",
  "message": "Call initiated successfully"
}
```

Your phone should ring within 5 seconds!

## API Endpoints

### Initiate Call
```http
POST /api/calls/initiate/{memberId}
```

**Success Response (202 Accepted)**:
```json
{
  "callConnectionId": "string",
  "callSessionId": 1,
  "memberPhoneNumber": "+1234567890",
  "status": "Initiated",
  "message": "Call initiated successfully"
}
```

**Error Responses**:
- `404 Not Found`: Member not found
- `409 Conflict`: Member already has an active call
- `422 Unprocessable Entity`: Member not active
- `500 Internal Server Error`: Azure Communication Services unavailable

### Call Status
```http
GET /api/calls/status/{callConnectionId}
```

**Success Response (200 OK)**:
```json
{
  "callConnectionId": "string",
  "status": "Connected",
  "callSessionId": 1,
  "startTime": "2026-01-10T10:30:00Z",
  "endTime": null
}
```

### Call Event Webhook (ACS Callbacks)
```http
POST /api/calls/events
Content-Type: application/cloudevents+json
```

Processes CloudEvents from Azure Communication Services:
- `Microsoft.Communication.CallConnected`: Updates CallSession status to Connected
- `Microsoft.Communication.CallDisconnected`: Updates CallSession status to Disconnected, sets EndTime
- `Microsoft.Communication.CallTransferFailed`: Updates CallSession status to Failed

**Response**: `200 OK` (event processed)

## Database Schema

### Members
- `MemberId` (PK, int, identity)
- `FirstName` (nvarchar(100))
- `LastName` (nvarchar(100))
- `PhoneNumber` (varchar(20), unique)
- `Status` (varchar(20): Active, Inactive)
- `CreatedAt` (datetime2)
- `UpdatedAt` (datetime2, auto-updated via trigger)

### CallSessions
- `CallSessionId` (PK, int, identity)
- `MemberId` (FK → Members)
- `CallConnectionId` (nvarchar(255), unique)
- `Status` (varchar(20): Initiated, Ringing, Connected, Completed, Disconnected, Failed, NoAnswer)
- `StartTime` (datetime2)
- `EndTime` (datetime2, nullable)
- `CreatedAt` (datetime2)
- `UpdatedAt` (datetime2, auto-updated via trigger)

### CallResponses
- `CallResponseId` (PK, int, identity)
- `CallSessionId` (FK → CallSessions)
- `QuestionNumber` (tinyint: 1-3)
- `QuestionText` (nvarchar(500))
- `ResponseValue` (varchar(10): Yes, No, Skipped)
- `CreatedAt` (datetime2)

## Testing

### Run Unit Tests
```powershell
# From repository root
dotnet test tests/CallistraAgent.Functions.Tests/CallistraAgent.Functions.Tests.csproj
```

**Current Status**: 9 tests, 8 passing

Test coverage:
- ✅ `InitiateCallAsync` validation (member not found, not active, duplicate call, ACS unavailable)
- ✅ Webhook handlers (CallConnected, CallDisconnected, CallFailed, NoAnswer)
- ⚠️ 1 test may have logging verification issues (HandleCallConnectedAsync_CallSessionNotFound)

### Manual Testing Workflow

See [QUICKSTART-TESTING.md](QUICKSTART-TESTING.md) for comprehensive end-to-end testing guide covering:
1. Starting Dev Tunnel
2. Updating configuration
3. Inserting test members
4. Starting Azure Functions
5. Initiating calls
6. Verifying database records
7. Common troubleshooting scenarios

## Development Tools

### Dev Tunnels for Webhook Testing
Dev Tunnels create a secure, publicly accessible tunnel to your local Functions instance, enabling Azure Communication Services webhooks to reach your development environment.

**Key Commands**:
```powershell
# Authenticate (GitHub recommended due to OAuth issues with Microsoft accounts)
devtunnel user login -g

# Host tunnel
devtunnel host -p 7071 --allow-anonymous

# List active tunnels
devtunnel list

# Create persistent tunnel (optional)
devtunnel create --allow-anonymous
devtunnel port create -p 7071
devtunnel host
```

See [DEVTUNNEL-SETUP.md](DEVTUNNEL-SETUP.md) for complete setup guide.

### Database Management
```powershell
# Drop database (clean slate)
cd CallistraAgent
sqlcmd -S localhost -E -C -i drop-database.sql

# Recreate database
./setup-database.ps1

# Insert test data
sqlcmd -S localhost -d CallistraAgent -E -C -i insert-test-data.sql

# Query call sessions
sqlcmd -S localhost -d CallistraAgent -E -C -Q "SELECT * FROM CallSessions ORDER BY CallSessionId DESC"
```

**Note**: All `sqlcmd` commands include `-C` flag to trust SQL Server's self-signed SSL certificate.

## Project Status

### Completed (✅)
- **Phase 1**: Project setup (solution, packages, configuration)
- **Phase 2**: Foundational layer (database schema, EF Core entities, repositories, DTOs, constants)
- **Phase 3 User Story 1**: Service layer (CallService) and API endpoints (InitiateCall, CallEventWebhook, CallStatus)
- **Unit Tests**: CallServiceTests with 8 passing tests covering business logic
- **Manual Testing**: Successfully validated with real phone calls via Azure Communication Services
- **Documentation**: DEVTUNNEL-SETUP.md, QUICKSTART-TESTING.md

### Current Scope: MVP User Story 1
✅ **Implemented**: Initiate outbound calls and track connection status
- Initiate calls to members via HTTP API
- Track call lifecycle events (Connected, Disconnected, Failed)
- Persist call sessions to database
- Handle error scenarios (member not found, duplicate call, ACS unavailable)

### Future Work (Pending)
- **Phase 4 User Story 2**: Ask healthcare questions with TTS and DTMF capture
  - Person-detection prompt to identify voicemail
  - 3 healthcare questions with DTMF recognition
  - Voicemail callback message (no PHI)
- **Phase 5 User Story 3**: Capture and persist DTMF responses
  - Save CallResponse records with question text and answer
  - Handle partial responses on mid-call disconnect
- **Phase 6**: Integration tests for webhook endpoints
- **Phase 7**: Polish (structured logging, Application Insights metrics, health checks, load testing)

See [tasks.md](specs/001-minimal-call-agent/tasks.md) for complete task breakdown (131 tasks, ~46% complete).

## Troubleshooting

### Phone Doesn't Ring
- ✅ Verify ACS phone number format: `+1234567890` (E.164)
- ✅ Check ACS connection string is correct in `local.settings.json`
- ✅ Confirm phone number is provisioned and active in Azure Portal
- ✅ Review Function logs for `CreateCallAsync` errors

### Webhook Events Not Received
- ✅ Confirm Dev Tunnel is running: `devtunnel list`
- ✅ Verify `CallbackBaseUrl` matches Dev Tunnel URL
- ✅ Check CallEventWebhookFunction logs for incoming requests
- ✅ Test webhook URL directly: `curl https://<tunnel-url>/api/calls/events`

### Database Errors with Triggers
If you see "Could not save changes because the target table has database triggers":
- ✅ Ensure DbContext has `.HasTrigger("UpdateTimestamp")` configured for Members and CallSessions
- ✅ See [CallistraAgentDbContext.cs](src/CallistraAgent.Functions/Data/CallistraAgentDbContext.cs) lines 41-42, 55-56

### SSL Certificate Trust Issues
Use `-C` flag with all sqlcmd commands:
```powershell
sqlcmd -S localhost -E -C -d CallistraAgent -Q "SELECT * FROM Members"
```

### Dev Tunnel OAuth Errors
If `devtunnel user login` shows OAuth errors, use GitHub authentication:
```powershell
devtunnel user login -g
```

See [DEVTUNNEL-SETUP.md](DEVTUNNEL-SETUP.md) for detailed troubleshooting.

## Performance Characteristics

**Target SLAs** (from specification):
- Call initiation: <400ms (p95)
- Member query: <50ms (p95)
- API responses: <500ms (p95)
- Concurrent calls: 1000 users
- System error rate: <5%

**Database Performance**:
- Indexes on all foreign keys (MemberId, CallSessionId)
- Unique filtered index on CallConnectionId (excludes NULLs)
- Connection pooling with retry policies (3 attempts, 10s max delay)
- 30s command timeout

## Security Considerations

### Development Environment
- Local SQL Server uses Windows Authentication (no credentials in config)
- Dev Tunnels use `--allow-anonymous` flag (acceptable for local testing)
- `local.settings.json` excluded from source control (.gitignore)

### Production Environment
- Azure SQL Database with managed identity authentication
- Function key authentication for HTTP endpoints
- Private ACS connection strings stored in Azure Key Vault
- No PHI in voicemail messages (HIPAA compliance)

### Documentation Security (Constitution v1.0.1)
- ✅ All documentation uses placeholder values (no real credentials)
- ✅ Real phone numbers and connection strings omitted from docs
- ✅ Examples use `<your-resource>`, `+1234567890`, etc.

## Contributing

This project follows the [Callistra-Agent Constitution v1.0.1](.specify/memory/constitution.md):

1. **Pragmatism**: Ship working solutions incrementally, document trade-offs
2. **Code Quality**: 
   - Use .editorconfig for formatting
   - Target 80%+ test coverage
   - Require code review for all PRs
   - Add XML comments to public APIs
3. **Testing Standards**:
   - Unit tests for business logic
   - Integration tests for HTTP endpoints
   - E2E tests for critical user paths
4. **UX Consistency**: RFC 7807 error responses, consistent messaging

## License

See [LICENSE](LICENSE) file for details.

## Resources

- [Azure Communication Services Call Automation](https://learn.microsoft.com/azure/communication-services/concepts/call-automation/call-automation)
- [Azure Functions .NET Isolated Worker](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Microsoft Dev Tunnels](https://learn.microsoft.com/azure/developer/dev-tunnels/)
- [Feature Specification](specs/001-minimal-call-agent/spec.md)
- [Implementation Plan](specs/001-minimal-call-agent/plan.md)
- [Task Breakdown](specs/001-minimal-call-agent/tasks.md)

## Support

For issues, questions, or contributions, please see the project documentation in `specs/001-minimal-call-agent/`.


