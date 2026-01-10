# Implementation Plan: Minimal Viable Healthcare Call Agent

**Branch**: `001-minimal-call-agent` | **Date**: January 10, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-minimal-call-agent/spec.md`

## Summary

Build a minimal healthcare outreach call system with Azure Communication Services. System places outbound calls via HTTP API, asks 3 healthcare questions using text-to-speech, captures yes/no responses via DTMF, and persists data to SQL database. Target: 1-day MVP with 3 Azure Function endpoints and 3 database tables.

## Technical Context

**Language/Version**: C# / .NET 8  
**Primary Dependencies**: Azure Functions v4, Azure.Communication.CallAutomation SDK, Entity Framework Core, SQL Server  
**Storage**: SQL Server (3 tables: Members, CallSessions, CallResponses)  
**Testing**: xUnit, Microsoft.AspNetCore.Mvc.Testing (in-memory function host)  
**Target Platform**: Azure Functions Consumption Plan (Windows/Linux)  
**Project Type**: Single serverless API project  
**Performance Goals**: 5 concurrent calls, <5s call initiation, <3s question playback  
**Constraints**: 30s no-answer timeout, 10s DTMF response timeout, 5-minute function timeout  
**Scale/Scope**: MVP supports 100 members, 1000 call sessions, single question flow

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Based on **Callistra-Agent Constitution v1.0.0** (see `.specify/memory/constitution.md`):

- ✅ **Pragmatism**: MVP-first approach documented; complexity deliberately minimal (3 endpoints, 3 tables); optimization deferred until user feedback received; no technical debt in Phase 1 (new greenfield project)
- ✅ **Code Quality**: .NET coding standards enforced via .editorconfig; target 80% test coverage; code review required (branch protection); inline documentation for call flow logic; no duplication (single-purpose functions)
- ✅ **Testing Standards**: xUnit framework specified; integration tests planned for all 3 HTTP endpoints covering happy path, error cases, and data persistence; end-to-end test for P1 flow (initiate → connect → question); no performance tests (ACS SLA sufficient for MVP)
- ✅ **UX Consistency**: N/A - No user interface; backend API only; error messages in API responses follow standard HTTP problem details format
- ✅ **Performance Requirements**: Target SLAs defined (5s initiation, 3s playback, 10s DTMF timeout); using Azure Communication Services default SLA (<200ms API responses); no load testing required for MVP (5 concurrent calls well within Azure Functions limits)

## Project Structure

### Documentation (this feature)

```text
specs/001-minimal-call-agent/
├── plan.md              # This file
├── research.md          # Phase 0: Azure SDK decisions, best practices
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Setup and first call guide
├── contracts/           # Phase 1: HTTP endpoint OpenAPI specs
│   ├── initiate-call.yaml
│   ├── call-events.yaml
│   └── call-status.yaml
└── tasks.md             # Phase 2: Implementation tasks (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── CallAgent.Functions/          # Azure Functions project
│   ├── Functions/
│   │   ├── CallInitiation.cs    # HTTP trigger: POST /api/calls/initiate/{memberId}
│   │   ├── CallEvents.cs        # HTTP trigger: POST /api/calls/events
│   │   └── CallStatus.cs        # HTTP trigger: GET /api/calls/status/{callConnectionId}
│   ├── Services/
│   │   ├── ICallService.cs      # Interface for call operations
│   │   └── CallService.cs       # Implements call initiation, DTMF handling
│   ├── Data/
│   │   ├── CallAgentDbContext.cs    # EF Core context
│   │   ├── Entities/
│   │   │   ├── Member.cs        # Member entity
│   │   │   ├── CallSession.cs   # Call session entity
│   │   │   └── CallResponse.cs  # Response entity
│   │   └── Migrations/          # EF migrations
│   ├── Models/
│   │   ├── InitiateCallRequest.cs   # API request models
│   │   └── CallEventPayload.cs      # ACS event models
│   ├── Configuration/
│   │   └── AcsOptions.cs        # Azure Communication Services settings
│   ├── host.json                # Function runtime config
│   ├── local.settings.json      # Local development settings
│   └── CallAgent.Functions.csproj
│
tests/
├── CallAgent.Functions.Tests/
│   ├── Integration/
│   │   ├── CallInitiationTests.cs      # Test call initiation endpoint
│   │   ├── CallEventsTests.cs          # Test event webhook
│   │   └── CallStatusTests.cs          # Test status endpoint
│   ├── Unit/
│   │   └── CallServiceTests.cs         # Test business logic
│   └── CallAgent.Functions.Tests.csproj
│
database/
└── init.sql                     # Initial schema for Members table

.editorconfig                    # C# code style rules
CallAgent.sln                    # Solution file
```

**Structure Decision**: Single Azure Functions project with Entity Framework Core for data access. Minimal separation: Functions (HTTP triggers), Services (business logic), Data (EF Core entities). No separate API/UI projects - this is backend-only. Testing uses in-memory function host for integration tests.

---

## Phase 0: Research ✅

**Status**: Complete  
**Output**: [research.md](research.md)

### Key Decisions Made

1. **Azure Communication Services SDK**: CallAutomation SDK v1.2.0+ for outbound calling and DTMF
2. **Data Access**: Entity Framework Core 8.0 (sufficient performance for MVP, migration support)
3. **DTMF Timeout**: 10 seconds (industry standard) with 1 re-prompt
4. **Healthcare Questions**: Standard enrollment verification (identity, awareness, assistance)
5. **Security**: Function-level authorization keys for MVP
6. **Call Status Model**: 7-state telephony model (Initiated → Ringing → Connected → Completed/Disconnected/Failed/NoAnswer)
7. **Testing Strategy**: Mock ACS SDK for unit tests, in-memory function host for integration tests
8. **Connection Pooling**: EF Core defaults sufficient for MVP scale

All technical unknowns resolved. No blocking issues identified.

---

## Phase 1: Design ✅

**Status**: Complete  
**Outputs**: 
- [data-model.md](data-model.md) - Entity definitions and relationships
- [contracts/](contracts/) - OpenAPI specs for 3 HTTP endpoints
- [quickstart.md](quickstart.md) - Setup and testing guide

### Data Model

**3 Entities**:
1. **Member** - Healthcare program enrollees (Id, Name, Phone, Program, Status)
2. **CallSession** - Call attempts (Id, MemberId, CallConnectionId, Status, Times)
3. **CallResponse** - Question responses (Id, CallSessionId, QuestionNumber, ResponseValue)

**Relationships**: Member 1:N CallSession 1:N CallResponse

**Status Enum**: Initiated → Ringing → Connected → [Completed | Disconnected | Failed | NoAnswer]

### API Contracts

**3 HTTP Endpoints**:
1. **POST /api/calls/initiate/{memberId}** - Initiate outbound call (returns 202 Accepted)
2. **POST /api/calls/events** - ACS webhook for CloudEvent processing (CallConnected, RecognizeCompleted, CallDisconnected)
3. **GET /api/calls/status/{callConnectionId}** - Query call status and responses

All endpoints use RFC 7807 Problem Details for error responses. Function key authorization required.

### Agent Context Updated

GitHub Copilot context file created with:
- Language: C# / .NET 8
- Framework: Azure Functions v4, Azure.Communication.CallAutomation SDK, Entity Framework Core
- Database: SQL Server (3 tables)
- Project type: Single serverless API project

---

## Phase 2: Implementation Planning

**Status**: Ready for `/speckit.tasks`  
**Output**: tasks.md (NOT generated by this command)

### High-Level Task Breakdown

Phase 2 will generate detailed tasks for:

1. **Project Setup** (~30 minutes)
   - Create Azure Functions project structure
   - Configure NuGet packages
   - Set up Entity Framework Core DbContext
   - Create database migration

2. **Core Functions** (~2 hours)
   - Implement CallInitiation.cs (HTTP trigger)
   - Implement CallEvents.cs (webhook handler)
   - Implement CallStatus.cs (query endpoint)

3. **Business Logic** (~1.5 hours)
   - Implement CallService.cs (ACS integration)
   - Implement question flow logic
   - Implement DTMF response handling

4. **Testing** (~2 hours)
   - Write unit tests for CallService
   - Write integration tests for all 3 endpoints
   - Write end-to-end test checklist

5. **Documentation** (~30 minutes)
   - Create .editorconfig for code style
   - Update README with architecture
   - Document deployment process

**Total Estimated Time**: 6.5 hours (within 1-day MVP target)

---

## Constitution Re-Check (Post-Design)

Based on **Callistra-Agent Constitution v1.0.0**:

- ✅ **Pragmatism**: Design remains minimal; no scope creep; all complexity justified
- ✅ **Code Quality**: Clear separation of concerns (Functions → Services → Data); testable architecture
- ✅ **Testing Standards**: All public APIs covered by integration tests; critical path (P1) has E2E test plan
- ✅ **UX Consistency**: Error messages use standard Problem Details format; consistent across endpoints
- ✅ **Performance Requirements**: SLAs align with spec (5s initiation, 3s playback, 10s DTMF); no changes needed

**Gate**: ✅ PASSED - Proceed to task breakdown

---

## Next Steps

1. Run `/speckit.tasks` to generate detailed implementation tasks in tasks.md
2. Execute tasks sequentially (or assign to team members)
3. Follow quickstart.md for Azure resource provisioning
4. Test locally using dev tunnels
5. Deploy to Azure Functions for production testing

---

## Implementation Plan Complete ✅

Branch: `001-minimal-call-agent`  
Ready for task generation via `/speckit.tasks` command.
