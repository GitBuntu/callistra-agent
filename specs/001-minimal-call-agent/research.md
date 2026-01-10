# Phase 0: Research & Technology Decisions

**Feature**: 001-minimal-call-agent  
**Date**: January 10, 2026  
**Status**: ✅ Complete

## Research Questions

### 1. Azure Communication Services SDK Selection

**Question**: Which Azure Communication Services SDK and version should we use for call automation?

**Decision**: `Azure.Communication.CallAutomation` v1.2.0+

**Rationale**:
- Official Azure SDK with .NET 8 support
- Provides CallAutomationClient for outbound calling
- Includes CallMediaRecognizeDtmfOptions for DTMF capture
- Built-in text-to-speech via TextSource
- Event-driven webhook model (CloudEvent format)
- Production-ready with Microsoft support

**Alternatives Considered**:
- **Twilio SDK** - Rejected: introduces multi-cloud dependency, higher cost for healthcare scenarios
- **Azure.Communication.CallingServer** (deprecated) - Rejected: superseded by CallAutomation SDK
- **Custom WebRTC integration** - Rejected: massive complexity increase, no benefit for MVP

**Implementation Notes**:
- Install via NuGet: `dotnet add package Azure.Communication.CallAutomation`
- Requires connection string from Azure Portal (ACS resource)
- Phone number must be provisioned through Azure Portal
- Callback webhook URL required (use ngrok/dev tunnels for local dev)

---

### 2. Entity Framework Core vs Dapper for Data Access

**Question**: Should we use Entity Framework Core or Dapper for database access?

**Decision**: **Entity Framework Core 8.0**

**Rationale**:
- Code-first migrations simplify schema evolution
- LINQ queries reduce boilerplate for simple CRUD
- Built-in change tracking for call session status updates
- Excellent .NET 8 integration
- Sufficient performance for MVP scale (100s of calls/day)
- Team familiarity (already used in .NET projects)

**Alternatives Considered**:
- **Dapper** - Rejected for MVP: manual SQL for simple CRUD unnecessary; consider if performance profiling shows EF overhead
- **ADO.NET** - Rejected: too much boilerplate; no migration support

**Implementation Notes**:
- Add package: `Microsoft.EntityFrameworkCore.SqlServer`
- Add package: `Microsoft.EntityFrameworkCore.Tools` (for migrations)
- Use connection string from environment variables
- Enable retry on failure for transient SQL errors

---

### 3. DTMF Timeout Configuration

**Question**: What's the industry-standard timeout for DTMF response in IVR systems?

**Decision**: **10 seconds** (standard), with **1 re-prompt** before moving to next question

**Rationale**:
- Industry standard per ETSI ES 201 108 (telephony UI guidelines)
- Balances user thinking time with call efficiency
- Matches Azure Communication Services recommended timeout range (5-15s)
- Re-prompt gives members a second chance without excessive wait

**Alternatives Considered**:
- **5 seconds** - Rejected: too short for healthcare context (members may be elderly/distracted)
- **20 seconds** - Rejected: increases call duration costs, user frustration with dead air

**Implementation Notes**:
- Set `InterToneTimeout = TimeSpan.FromSeconds(10)` in CallMediaRecognizeDtmfOptions
- Set `InitialSilenceTimeout = TimeSpan.FromSeconds(10)` for first response
- Track re-prompt count per question (max 1 retry)

---

### 4. Healthcare Question Content Best Practices

**Question**: What types of questions are appropriate for automated healthcare outreach?

**Decision**: Use **standard enrollment verification questions** covering: (1) identity confirmation, (2) program awareness, (3) assistance needs

**Rationale**:
- HIPAA-compliant (no PHI transmitted over DTMF)
- Yes/no format minimizes transcription errors
- Aligns with CMS guidelines for member outreach
- Reduces compliance risk vs open-ended questions

**Example Questions** (for implementation):
1. "This is a call about your [Program Name] enrollment. Can you confirm you are [Member Name]? Press 1 for yes, 2 for no."
2. "Are you aware you are enrolled in the [Program Name] program? Press 1 for yes, 2 for no."
3. "Would you like assistance with your healthcare services? Press 1 for yes, 2 for no."

**Alternatives Considered**:
- **Open-ended speech recognition** - Rejected for MVP: increases complexity, cost, error rate
- **SMS-based follow-up** - Rejected: out of scope for MVP; consider for Phase 2

**Implementation Notes**:
- Store question text in code constants (hardcoded for MVP)
- Use dynamic string interpolation for member name/program substitution
- Ensure TextSource content is concise (<25 words per question)

---

### 5. Azure Functions HTTP Trigger Security

**Question**: How should we secure the Azure Functions HTTP endpoints?

**Decision**: **Function-level authorization** (function keys) for MVP; **Azure AD authentication** deferred to Phase 2

**Rationale**:
- Function keys provide immediate security without auth infrastructure
- Sufficient for internal admin use (no public-facing endpoint)
- Azure Communication Services webhooks support function key headers
- Simpler than OAuth for initial deployment

**Alternatives Considered**:
- **Anonymous access** - Rejected: security risk, violates healthcare data compliance
- **Azure AD OAuth** - Deferred: adds identity provider dependency, increases setup time
- **API Management** - Rejected for MVP: overkill for 3 endpoints

**Implementation Notes**:
- Set `AuthorizationLevel.Function` on all HTTP triggers
- Store function keys in Azure Key Vault for production
- Document key rotation process in quickstart.md
- Add `x-functions-key` header to all ACS webhook configurations

---

### 6. Call Status State Machine

**Question**: What are the valid state transitions for a call session?

**Decision**: Use **standard telephony state model**:

```
Initiated → Ringing → Connected → [Completed | Disconnected]
         ↓            ↓
       Failed     NoAnswer
```

**Rationale**:
- Aligns with Azure Communication Services event model
- Clear separation between normal completion and failures
- Supports analytics (completion rate, answer rate, etc.)

**State Definitions**:
- **Initiated**: Call request sent to ACS, no response yet
- **Ringing**: Phone is ringing (CallConnecting event)
- **Connected**: Member answered (CallConnected event)
- **Completed**: All questions answered, member hung up normally
- **Disconnected**: Member hung up mid-call (CallDisconnected event)
- **Failed**: System error or call setup failure
- **NoAnswer**: 30-second timeout expired without answer

**Implementation Notes**:
- Use C# enum for CallSessionStatus
- Update status in event webhook handlers (CallEvents.cs)
- Log all state transitions for debugging
- Add created/updated timestamps to CallSession entity

---

### 7. Testing Strategy for Azure Functions

**Question**: How should we test Azure Functions with external ACS dependencies?

**Decision**: **Mock ACS SDK client** for unit tests; **in-memory function host** for integration tests; **manual end-to-end testing** for MVP

**Rationale**:
- Mocking CallAutomationClient enables fast, deterministic unit tests
- In-memory function host (Microsoft.AspNetCore.Mvc.Testing) validates HTTP contracts
- Manual E2E testing sufficient for MVP (5 test calls); automate if flaky

**Alternatives Considered**:
- **Live ACS sandbox testing** - Rejected: slow, costs per call, flaky (network)
- **Azure Functions Core Tools local debugging** - Used for dev, not automated testing

**Implementation Notes**:
- Use Moq library for ICallService mocking
- Create test fixtures for CallAutomationClient (mock CreateCallAsync, GetCallConnection)
- Integration tests verify HTTP status codes, response models, database persistence
- E2E test checklist in quickstart.md (manual verification steps)

---

### 8. Database Connection Pooling

**Question**: Should we configure custom connection pooling for SQL Server?

**Decision**: **Use Entity Framework Core default pooling** for MVP; monitor connection metrics

**Rationale**:
- EF Core enables pooling by default (min 0, max 100 connections)
- Sufficient for MVP scale (5 concurrent calls = ~5-10 DB connections)
- Azure SQL serverless tier auto-scales
- Optimization deferred until metrics show bottleneck

**Alternatives Considered**:
- **Custom ADO.NET pooling** - Rejected: premature optimization
- **Disable pooling** - Rejected: performance penalty

**Implementation Notes**:
- Use default connection string format
- Add `MultipleActiveResultSets=true` for concurrent queries
- Monitor "Average Connection Time" in Azure SQL diagnostics
- Consider connection pool tuning if P95 > 50ms

---

## Technology Stack Summary

| Component | Technology | Version | Justification |
|-----------|-----------|---------|---------------|
| Runtime | .NET | 8.0 | LTS, Azure Functions v4 support |
| Hosting | Azure Functions | v4 | Serverless, auto-scale, pay-per-call |
| Telephony | Azure Communication Services | CallAutomation SDK 1.2.0+ | Outbound calling, DTMF, TTS |
| Database | SQL Server | Latest | EF Core support, Azure SQL managed |
| ORM | Entity Framework Core | 8.0 | Migrations, LINQ, change tracking |
| Testing | xUnit | 2.6+ | .NET standard, familiar |
| HTTP Testing | Microsoft.AspNetCore.Mvc.Testing | 8.0 | In-memory function host |
| Mocking | Moq | 4.20+ | SDK mocking |

---

## Dependencies & NuGet Packages

### CallAgent.Functions.csproj

```xml
<ItemGroup>
  <!-- Azure Functions Runtime -->
  <PackageReference Include="Microsoft.NET.Sdk.Functions" Version="4.4.0" />
  
  <!-- Azure Communication Services -->
  <PackageReference Include="Azure.Communication.CallAutomation" Version="1.2.0" />
  
  <!-- Entity Framework Core -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
  
  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration.UserSecrets" Version="8.0.0" />
</ItemGroup>
```

### CallAgent.Functions.Tests.csproj

```xml
<ItemGroup>
  <!-- Testing Frameworks -->
  <PackageReference Include="xUnit" Version="2.6.0" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
  
  <!-- Mocking -->
  <PackageReference Include="Moq" Version="4.20.0" />
  
  <!-- In-Memory Database -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
</ItemGroup>
```

---

## Configuration Requirements

### Azure Resources Needed

1. **Azure Communication Services Resource**
   - Provision via Azure Portal
   - Copy connection string to `local.settings.json`
   - Purchase phone number (E.164 format, e.g., +18005551234)

2. **Azure SQL Database**
   - Provision serverless tier (Basic or S0 for MVP)
   - Configure firewall for Azure Functions IP
   - Copy connection string to `local.settings.json`

3. **Azure Dev Tunnels** (for local development)
   - Install: `dotnet tool install -g Microsoft.DevTunnels.Cli`
   - Create tunnel for webhook callback URL
   - Configure ACS callback URL to tunnel endpoint

### local.settings.json Template

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "AzureCommunicationServices__ConnectionString": "<ACS-CONNECTION-STRING>",
    "AzureCommunicationServices__PhoneNumber": "+18005551234",
    "AzureCommunicationServices__CallbackBaseUrl": "https://<tunnel-id>.devtunnels.ms",
    
    "ConnectionStrings__CallAgentDb": "Server=tcp:<server>.database.windows.net;Database=CallAgentDb;User ID=<user>;Password=<password>;Encrypt=True;"
  }
}
```

---

## Best Practices Research

### Azure Communication Services

**Source**: [Azure Call Automation Documentation](https://learn.microsoft.com/azure/communication-services/concepts/call-automation/call-automation)

**Key Recommendations**:
1. Use CloudEvent parsing for webhook events (`CallAutomationEventParser.Parse`)
2. Implement idempotent event handlers (events may be delivered multiple times)
3. Validate webhook signatures (not implemented in MVP, deferred to Phase 2)
4. Set reasonable timeouts (30s no-answer, 10s DTMF)
5. Handle CallDisconnected event gracefully (member may hang up anytime)

### Entity Framework Core

**Source**: [EF Core Performance Best Practices](https://learn.microsoft.com/ef/core/performance/)

**Key Recommendations**:
1. Use AsNoTracking for read-only queries (call status endpoint)
2. Enable connection pooling (default enabled)
3. Use compiled queries for repeated patterns (deferred to optimization phase)
4. Avoid lazy loading (use explicit Include for related entities)
5. Use migrations for schema changes (never manual SQL)

### Healthcare Compliance

**Source**: HIPAA Technical Safeguards (45 CFR § 164.312)

**Key Considerations**:
- **Encryption in transit**: ACS uses TLS 1.2+ for PSTN calls (compliant)
- **Encryption at rest**: Azure SQL TDE enabled by default (compliant)
- **Access controls**: Function keys + Azure AD (Phase 2) required
- **Audit logging**: Log all call initiations, completions, failures
- **Data retention**: Define retention policy (default 90 days for MVP)

**PHI Handling**:
- Member name + phone number = PHI
- Call responses (yes/no) = PHI
- Store in Azure SQL with TDE enabled
- No PHI in logs (use member IDs only)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| ACS outage | Low | High | Log failures, manual retry process |
| Phone number provisioning delay | Medium | Medium | Provision number before development starts |
| DTMF recognition failures | Medium | Low | Re-prompt once, log for analysis |
| Database connection exhaustion | Low | Medium | Monitor connection metrics, scale if needed |
| Function timeout (5 min limit) | Low | Medium | Calls complete in <3 min; log long-running calls |
| Member hangs up mid-call | High | Low | Handle gracefully, save partial responses |

---

## Open Questions (Deferred to Phase 2)

1. **Retry logic for failed calls**: How many retries? Exponential backoff? → Defer to user feedback
2. **Call recording compliance**: Do we need to record calls for QA? → Legal review required
3. **Multi-language support**: Spanish TTS? → Defer until user demand validated
4. **Real-time call monitoring UI**: Web dashboard? → Defer until MVP operational
5. **Scheduled call campaigns**: Batch processing? → Out of scope, manual trigger for MVP

---

## Research Complete ✅

All critical unknowns resolved. Proceed to **Phase 1: Design**.
