# System Design: Minimal Viable Healthcare Call Agent

**Version**: 1.0  
**Date**: January 10, 2026  
**Feature**: 001-minimal-call-agent  
**Architect**: Azure Solution Architecture  
**Status**: Ready for Implementation

---

## Executive Summary

Automated healthcare outreach system using Azure Communication Services to place outbound calls, ask enrollment verification questions via text-to-speech, capture DTMF responses, and persist data for healthcare coordinator review. MVP targets 100 members, 1000 call sessions, 1-day implementation timeline.

**Key Design Principles:**
- **Pragmatic**: Serverless architecture eliminates infrastructure management
- **HIPAA-Aware**: Person-detection prompt before PHI disclosure, generic voicemail messages
- **Cost-Optimized**: Consumption-based pricing, no idle resource costs
- **Minimal Complexity**: 3 Azure Functions, 3 database tables, single project

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Azure Cloud Platform                            │
│                                                                         │
│  ┌──────────────────────┐                                              │
│  │  Healthcare Admin    │  (1) POST /api/calls/initiate/{memberId}     │
│  │   (API Consumer)     │────────────────────────────────┐             │
│  └──────────────────────┘                                 │             │
│                                                            │             │
│  ┌─────────────────────────────────────────────────────────▼──────────┐│
│  │              Azure Functions (Consumption Plan)                    ││
│  │                                                                     ││
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐ ││
│  │  │ CallInitiation   │  │   CallEvents     │  │   CallStatus     │ ││
│  │  │  HTTP Trigger    │  │  HTTP Trigger    │  │  HTTP Trigger    │ ││
│  │  └────────┬─────────┘  └────────▲─────────┘  └──────────────────┘ ││
│  │           │                      │ (3) Webhook                     ││
│  │           │                      │     (CloudEvents)               ││
│  │           │ (2) CreateCall       │                                 ││
│  └───────────┼──────────────────────┼─────────────────────────────────┘│
│              │                      │                                  │
│  ┌───────────▼──────────────────────┴───────────────────┐             │
│  │   Azure Communication Services (ACS)                 │             │
│  │   - CallAutomation API                               │             │
│  │   - Phone Number Resource (+1-XXX-XXX-XXXX)         │             │
│  │   - Text-to-Speech Engine                           │             │
│  │   - DTMF Recognition                                 │             │
│  └──────────────────────────┬───────────────────────────┘             │
│                             │ (4) Outbound Call                        │
│  ┌──────────────────────────┼──────────────────────────┐              │
│  │  Entity Framework Core   │                          │              │
│  │  ┌───────────────────────▼─────────┐                │              │
│  │  │    CallAgentDbContext           │                │              │
│  │  │  - Members (existing)           │                │              │
│  │  │  - CallSessions (new)           │                │              │
│  │  │  - CallResponses (new)          │                │              │
│  │  └─────────────────────────────────┘                │              │
│  │                    │                                 │              │
│  │  ┌─────────────────▼────────────────┐               │              │
│  │  │   Azure SQL Database             │               │              │
│  │  │   (Basic/Standard Tier)          │               │              │
│  │  └──────────────────────────────────┘               │              │
│  └─────────────────────────────────────────────────────┘              │
└─────────────────────────────────────────────────────────────────────────┘
                             │
                             │ (5) PSTN Call
                             │
                    ┌────────▼─────────┐
                    │  Member Phone    │
                    │  +1-XXX-XXX-XXXX │
                    └──────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Technology |
|-----------|---------------|------------|
| **CallInitiation Function** | Accept HTTP request, validate member, create CallSession record, initiate outbound call via ACS | Azure Functions HTTP Trigger (.NET 8) |
| **CallEvents Function** | Receive ACS webhooks (CallConnected, RecognizeCompleted, CallDisconnected), orchestrate call flow (person-detection → questions → responses), update CallSession status | Azure Functions HTTP Trigger (.NET 8) |
| **CallStatus Function** | Query call session and responses by CallConnectionId for coordinator review | Azure Functions HTTP Trigger (.NET 8) |
| **Azure Communication Services** | PSTN connectivity, text-to-speech playback, DTMF recognition, call lifecycle management | Azure.Communication.CallAutomation SDK v1.2.0+ |
| **Azure SQL Database** | Persistent storage for Members, CallSessions, CallResponses | SQL Server (Basic/S0 tier for MVP) |
| **Entity Framework Core** | ORM, code-first migrations, change tracking | EF Core 8.0 with SQL Server provider |

---

## Data Flow

### 1. Call Initiation Flow

```
Admin → POST /api/calls/initiate/{memberId}
  ↓
CallInitiation Function
  ↓
Validate Member (exists, active, valid phone number)
  ↓
Create CallSession record (Status = Initiated)
  ↓
CallAutomationClient.CreateCall(targetPhone, callbackUrl)
  ↓
Update CallSession with CallConnectionId
  ↓
Return 202 Accepted {callSessionId, status}
  ↓
ACS initiates PSTN call to member phone
```

### 2. Call Connected & Person-Detection Flow

```
ACS → POST /api/calls/events (CallConnected event)
  ↓
CallEvents Function
  ↓
Update CallSession Status = Connected
  ↓
Play Person-Detection Prompt:
  "Press 1 if you can hear this message"
  ↓
Wait 5 seconds for DTMF response
  ↓
┌─────────────────────────────┬──────────────────────────────┐
│ DTMF "1" received           │ No DTMF response (voicemail) │
│   ↓                         │   ↓                          │
│ Proceed to Healthcare Q1    │ Play Generic Callback Msg:   │
│                             │ "This is [Org] calling about │
│                             │  healthcare enrollment..."   │
│                             │   ↓                          │
│                             │ Update Status = Voicemail    │
│                             │   ↓                          │
│                             │ Hang up call                 │
└─────────────────────────────┴──────────────────────────────┘
```

### 3. Healthcare Questions & Response Capture Flow

```
Play Question N (text-to-speech)
  "Can you confirm you are enrolled in [Program]? Press 1 for yes, 2 for no."
  ↓
Wait 10 seconds for DTMF response
  ↓
┌────────────────────┬───────────────────┬──────────────────────┐
│ DTMF "1" or "2"    │ No DTMF (timeout) │ Invalid DTMF (3-9)   │
│   ↓                │   ↓               │   ↓                  │
│ Save CallResponse  │ Re-prompt once    │ Re-prompt (max 2x)   │
│   ↓                │   ↓               │   ↓                  │
│ Question N+1       │ If timeout again: │ If still invalid:    │
│   OR               │ Skip to Q(N+1)    │ Skip to Q(N+1)       │
│ If Q3 complete:    │                   │                      │
│ Hang up, Status=   │                   │                      │
│ Completed          │                   │                      │
└────────────────────┴───────────────────┴──────────────────────┘
```

### 4. Call Status Query Flow

```
Coordinator → GET /api/calls/status/{callConnectionId}
  ↓
CallStatus Function
  ↓
Query CallSession by CallConnectionId
  ↓
Eager load: Member, CallResponses[]
  ↓
Return JSON:
  {
    "callSessionId": 42,
    "memberId": 1,
    "memberName": "John Doe",
    "status": "Completed",
    "startTime": "2026-01-10T14:30:00Z",
    "endTime": "2026-01-10T14:33:15Z",
    "responses": [
      {"questionNumber": 1, "questionText": "...", "responseValue": 1},
      {"questionNumber": 2, "questionText": "...", "responseValue": 2},
      {"questionNumber": 3, "questionText": "...", "responseValue": 1}
    ]
  }
```

---

## Azure Services Configuration

### Azure Communication Services

**SKU**: Standard (pay-as-you-go)  
**Resources Required**:
- Communication Services resource
- Phone number (toll-free or local, outbound-enabled)
- Callback URL endpoint (Azure Functions URL)

**Configuration**:
```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://xxx.communication.azure.com/;accesskey=xxx",
    "SourcePhoneNumber": "+18005551234",
    "CallbackUrl": "https://myfunction.azurewebsites.net/api/calls/events"
  }
}
```

**Pricing Estimate (MVP)**:
- Phone number: ~$2/month
- Outbound call minutes: ~$0.013/min (US PSTN)
- 100 calls/day × 3 min/call × 30 days = 9,000 min/month = **~$117/month**

---

### Azure Functions

**Plan**: Consumption Plan (serverless)  
**Runtime**: .NET 8 (isolated worker process)  
**Configuration**:
- Authorization: Function-level API keys
- CORS: Enable for admin portal origins
- Application Insights: Enable for telemetry

**Scaling**:
- Auto-scale: 0 to 200 instances
- Cold start mitigation: Not required for MVP (async call initiation acceptable)
- Concurrency: Default 1000 requests/instance sufficient

**Environment Variables**:
```
AzureCommunicationServices__ConnectionString
AzureCommunicationServices__SourcePhoneNumber
AzureCommunicationServices__CallbackUrl
SqlConnectionString
```

**Pricing Estimate (MVP)**:
- Executions: ~300/day (100 call initiations + 200 webhook events) × 30 = 9,000/month
- Execution time: ~500ms average × 9,000 = 4,500 GB-seconds
- **Cost: Free tier** (first 1M executions + 400k GB-seconds free)

---

### Azure SQL Database

**Tier**: Basic (5 DTU) or Standard S0 (10 DTU) for MVP  
**Size**: 2 GB (sufficient for 100k members + 1M call sessions)  
**Backup**: Automated 7-day retention (included)

**Schema**:
- **Members** table (existing): Id, FirstName, LastName, PhoneNumber, Program, Status
- **CallSessions** table (new): Id, MemberId, CallConnectionId, Status, StartTime, EndTime
- **CallResponses** table (new): Id, CallSessionId, QuestionNumber, QuestionText, ResponseValue

**Indexes**:
- Members.PhoneNumber (unique)
- CallSessions.CallConnectionId (unique, for status queries)
- CallSessions.Status (for dashboard filtering)

**Pricing Estimate (MVP)**:
- Basic tier: **~$5/month**
- Standard S0: **~$15/month**

---

## Security & Compliance

### Authentication & Authorization

- **Azure Functions**: Function-level API keys (x-functions-key header)
- **Azure Communication Services**: Connection string in Key Vault (production) or environment variables (MVP)
- **SQL Database**: SQL authentication with connection string in secure config

**Production Hardening** (post-MVP):
- Azure AD authentication for Functions
- Managed Identity for ACS access
- Private Endpoint for SQL Database

---

### HIPAA Compliance Considerations

| Requirement | Implementation |
|------------|----------------|
| **PHI Protection** | Person-detection prompt plays BEFORE member name spoken; voicemail receives generic message with no PHI |
| **Data Encryption** | SQL TDE enabled (default), HTTPS enforced for all endpoints |
| **Audit Logging** | Application Insights captures all function invocations, ACS events logged |
| **Access Control** | Function keys restrict API access, SQL credentials not in source control |
| **Data Retention** | Call sessions retained per organizational policy (e.g., 90 days); soft-delete recommended |

**BAA Requirements** (production):
- Sign Microsoft BAA for Azure services
- Enable Azure Security Center recommendations
- Configure Azure Policy for compliance validation

---

### Network Security

**MVP Configuration**:
- Public endpoints (Azure Functions, ACS)
- HTTPS enforced (TLS 1.2+)
- Function app allows only HTTPS traffic

**Production Configuration** (optional):
- Virtual Network integration for Azure Functions
- Private Endpoint for SQL Database
- Azure Firewall for egress traffic control

---

## Monitoring & Observability

### Application Insights

**Telemetry Collected**:
- HTTP request traces (call initiation, webhook events, status queries)
- Exception tracking (ACS failures, SQL connection errors)
- Custom events: CallInitiated, CallConnected, QuestionPlayed, ResponseCaptured, CallCompleted
- Dependencies: ACS API calls, SQL queries

**Key Metrics**:
- Call initiation rate (calls/hour)
- Call completion rate (Completed / Total)
- Average call duration
- DTMF response rate per question
- Voicemail detection rate
- Function execution time (p50, p95, p99)

**Alerts** (production):
- Call failure rate > 5%
- Function error rate > 1%
- SQL connection pool exhaustion
- ACS webhook latency > 5s

---

### Logging Strategy

**Log Levels**:
- **Information**: Call initiated (memberId, callSessionId), call connected, question played, response captured
- **Warning**: DTMF timeout, re-prompt triggered, invalid DTMF input
- **Error**: ACS API failure, SQL connection error, member not found, invalid phone number

**Sample Log Entry**:
```json
{
  "timestamp": "2026-01-10T14:30:05Z",
  "level": "Information",
  "message": "Call initiated successfully",
  "memberId": 1,
  "callSessionId": 42,
  "callConnectionId": "aHR0cHM6Ly...",
  "targetPhone": "+18005559999"
}
```

---

## Scalability & Performance

### Performance Targets

| Metric | Target | Rationale |
|--------|--------|-----------|
| Call initiation latency | < 5 seconds | User expectation for API response |
| Text-to-speech playback | < 3 seconds | Minimize dead air after connection |
| DTMF capture latency | < 2 seconds | Real-time responsiveness |
| Concurrent calls | 5 (MVP) | Sufficient for 100 members/day |
| Database query time | < 50ms (p95) | EF Core with connection pooling |

### Scaling Limits

**MVP Constraints**:
- Azure Functions Consumption: 200 instance limit (10,000+ concurrent calls theoretical)
- ACS: No hard limit on concurrent calls (pay-per-minute)
- SQL Basic tier: 5 DTU (sufficient for 100 calls/day)

**Growth Path**:
- Phase 1 (100 members): Basic SQL tier, Consumption Functions
- Phase 2 (1,000 members): Standard S0 SQL tier, add Application Insights sampling
- Phase 3 (10,000+ members): Premium Functions (VNet integration), SQL elastic pool, Redis cache for member lookups

---

## Disaster Recovery & Availability

### High Availability

**MVP Approach**:
- Azure Functions: Built-in redundancy across availability zones
- ACS: Microsoft-managed 99.9% SLA
- SQL Database: Zone-redundant backup (7-day retention)

**RTO/RPO**:
- RTO (Recovery Time Objective): 1 hour (restore SQL from backup, redeploy Functions)
- RPO (Recovery Point Objective): 5 minutes (transaction log backups)

### Failure Scenarios

| Failure | Impact | Mitigation |
|---------|--------|-----------|
| **Function cold start** | 2-3s delay on first call | Acceptable for MVP; use Premium plan if critical |
| **ACS service degradation** | Call initiation fails | Retry logic (exponential backoff), alert on-call |
| **SQL connection timeout** | Call session not created | Connection pooling, retry on transient error |
| **Member phone unreachable** | NoAnswer status recorded | Expected behavior; retry logic out of scope |
| **Invalid phone number** | 400 Bad Request returned | Validation at initiation prevents call attempt |

---

## Cost Analysis

### Monthly Cost Breakdown (MVP - 100 calls/day)

| Service | Configuration | Monthly Cost |
|---------|--------------|--------------|
| Azure Communication Services | Phone number + 9,000 min PSTN | $117 |
| Azure Functions | Consumption plan (9k executions) | $0 (free tier) |
| Azure SQL Database | Basic tier (5 DTU, 2GB) | $5 |
| Application Insights | 5 GB ingestion | $0 (free tier) |
| **Total** | | **~$122/month** |

### Cost Scaling

| Volume | Monthly Cost Estimate | Notes |
|--------|----------------------|-------|
| 100 calls/day | $122 | MVP baseline |
| 500 calls/day | $580 | ACS: $585/mo, SQL Standard S0: $15/mo |
| 1,000 calls/day | $1,160 | Add App Insights: ~$30/mo |
| 5,000 calls/day | $5,800 | Consider Premium Functions: +$150/mo |

**Cost Optimization**:
- Use Azure Reservations for SQL (save 30%)
- Monitor call duration to minimize PSTN charges
- Implement call scheduling to batch during off-peak (if rate-limited by carrier)

---

## Deployment Architecture

### Environments

| Environment | Purpose | Configuration |
|-------------|---------|---------------|
| **Development** | Local testing with dev tunnels | local.settings.json, ACS trial phone number, LocalDB/Azure SQL |
| **Staging** | Pre-production validation | Dedicated Azure Functions app, shared SQL database (staging schema), test phone numbers |
| **Production** | Live member calls | Dedicated Functions app, production SQL database, production phone number |

### CI/CD Pipeline

**Build Pipeline** (GitHub Actions / Azure DevOps):
1. Restore NuGet packages
2. Run unit tests (xUnit)
3. Run integration tests (in-memory function host)
4. Build Azure Functions project
5. Publish artifact (zip deployment package)

**Release Pipeline**:
1. Deploy to Staging (auto-trigger on main branch)
2. Run smoke tests (health check, sample call initiation)
3. Manual approval gate
4. Deploy to Production (zero-downtime swap)
5. Database migration (EF Core `dotnet ef database update`)

**Deployment Steps**:
```bash
# Deploy Functions
az functionapp deployment source config-zip \
  --resource-group rg-callistra-prod \
  --name func-call-agent-prod \
  --src ./publish.zip

# Apply EF Core migrations
dotnet ef database update \
  --connection "Server=tcp:sql-callistra-prod.database.windows.net;..."
```

---

## Implementation Roadmap

### Phase 0: Azure Resource Provisioning (Day 0 - 2 hours)

- [ ] Create Azure Resource Group
- [ ] Provision Azure Communication Services resource
- [ ] Purchase phone number (toll-free or local)
- [ ] Create Azure SQL Database (Basic tier)
- [ ] Create Azure Functions app (Consumption plan)
- [ ] Configure Application Insights
- [ ] Set environment variables (connection strings, callback URL)

### Phase 1: MVP Implementation (Day 1 - 6.5 hours)

- [ ] Setup: Create project, add NuGet packages (30 min)
- [ ] Foundational: EF Core entities, DbContext, migration (1.5 hours)
- [ ] User Story 1: Call initiation endpoint + tests (2 hours)
- [ ] User Story 2: Person-detection + healthcare questions + tests (2 hours)
- [ ] User Story 3: Response capture + status query + tests (1.5 hours)
- [ ] Polish: Logging, documentation (30 min)

### Phase 2: Testing & Validation (Day 2 - 4 hours)

- [ ] Deploy to Azure Staging environment
- [ ] Run integration tests against live ACS
- [ ] Manual test: Full call flow with real phone number
- [ ] Performance validation (5 concurrent calls)
- [ ] Security review (API keys, SQL connection string)

### Phase 3: Production Deployment (Day 3 - 2 hours)

- [ ] Deploy to Production environment
- [ ] Configure monitoring alerts
- [ ] Document operational procedures
- [ ] Train healthcare coordinators on status query API
- [ ] Go-live with pilot group (10 members)

---

## Operational Procedures

### Monitoring Checklist

**Daily**:
- Review Application Insights dashboard (call volume, error rate)
- Check call completion rate (target: >85%)
- Verify SQL database size (alert if >80% capacity)

**Weekly**:
- Analyze DTMF response patterns (adjust questions if low engagement)
- Review voicemail detection rate (optimize 5s timeout if needed)
- Cost analysis (ACS minutes, Function executions)

### Incident Response

**Call Initiation Failures** (HTTP 500):
1. Check ACS connection string in environment variables
2. Verify phone number is active and outbound-enabled
3. Check SQL database connectivity
4. Review Application Insights exceptions

**Webhook Events Not Received**:
1. Verify callback URL is publicly accessible (no firewall blocking)
2. Check ACS event subscription configuration
3. Review Function app CORS settings
4. Test webhook endpoint with curl

**SQL Connection Pool Exhausted**:
1. Restart Function app (clears stale connections)
2. Review long-running queries in SQL DMVs
3. Increase max pool size in connection string (default: 100)

---

## Design Decisions & Trade-offs

### Why Azure Functions (vs. App Service)?

**Chosen**: Azure Functions Consumption Plan  
**Trade-off**: Cold start latency (2-3s) vs. zero idle cost  
**Rationale**: Call initiation is async (202 Accepted response), cold start doesn't block member experience. Cost savings ($150-500/month for App Service) justify trade-off for MVP.

---

### Why Entity Framework Core (vs. Dapper)?

**Chosen**: EF Core 8.0  
**Trade-off**: 10-20% slower query performance vs. reduced code complexity  
**Rationale**: MVP scale (100 calls/day) makes performance difference negligible (<50ms queries). Code-first migrations and LINQ queries accelerate development. Can optimize with Dapper for hot paths if profiling reveals bottlenecks.

---

### Why Person-Detection Prompt (vs. Answering Machine Detection)?

**Chosen**: DTMF-based person detection  
**Trade-off**: 5s delay on all calls vs. complex AMD heuristics  
**Rationale**: Eliminates HIPAA risk of speaking member name to voicemail. Simple to implement and test. AMD false positives (20-30%) would require fallback logic anyway.

---

### Why SQL Database (vs. Cosmos DB)?

**Chosen**: Azure SQL Database  
**Trade-off**: Manual scaling (DTU increases) vs. automatic global distribution  
**Rationale**: Relational data model (Member → CallSession → CallResponse) fits SQL naturally. MVP doesn't need geo-distribution. SQL expertise on team reduces operational risk. Cost: $5-15/month vs. $24+/month for Cosmos DB.

---

## Success Criteria

### Technical Success Metrics

- [ ] Call initiation latency < 5 seconds (p95)
- [ ] Call completion rate > 85% (excluding member no-answer)
- [ ] DTMF response capture latency < 2 seconds
- [ ] System uptime > 99.5% (excluding planned maintenance)
- [ ] Zero PHI disclosure to voicemail systems
- [ ] Test coverage ≥ 80% (unit + integration tests)

### Business Success Metrics

- [ ] 100 member calls completed in first week
- [ ] Healthcare coordinator satisfaction with response data quality
- [ ] Average call duration < 3 minutes
- [ ] Member callback rate < 10% (indicates clear messaging)
- [ ] Operational cost < $150/month for MVP

---

## References

- **Azure Communication Services**: [Call Automation Quickstart](https://learn.microsoft.com/azure/communication-services/quickstarts/call-automation/callflows-for-customer-interactions)
- **HIPAA on Azure**: [Microsoft Compliance Documentation](https://learn.microsoft.com/azure/compliance/offerings/offering-hipaa-us)
- **Azure Functions Best Practices**: [Performance and Reliability](https://learn.microsoft.com/azure/azure-functions/functions-best-practices)
- **Entity Framework Core**: [EF Core 8.0 Documentation](https://learn.microsoft.com/ef/core/)
- **DTMF Telephony Standards**: ETSI ES 201 108 (Human Factors Guidelines)

---

## Document History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2026-01-10 | Initial system design for MVP | Azure Solution Architect |

---

**Next Steps**: Review with stakeholders → Provision Azure resources → Begin Phase 1 implementation (see [tasks.md](../specs/001-minimal-call-agent/tasks.md))
