# Implementation Plan: Minimal Viable Healthcare Call Agent

**Branch**: `001-minimal-call-agent` | **Date**: 2026-01-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-minimal-call-agent/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a minimal healthcare outreach call agent using Azure Communication Services to place outbound calls, ask 3 healthcare enrollment verification questions via text-to-speech, capture DTMF (yes/no) responses, and persist call sessions and responses to Azure SQL Database. The system handles voicemail detection, call failure scenarios, and provides HTTP APIs for call initiation and webhook event processing. Target: 1000 concurrent users, <500ms API latency (p95), 3-minute call duration for cooperative participants.

## Technical Context

**Language/Version**: C# / .NET 9 (LTS)  
**Primary Dependencies**: 
- Azure.Communication.CallAutomation (latest stable)
- Microsoft.EntityFrameworkCore 8+ (Azure SQL provider)
- Microsoft.Azure.Functions.Worker.Extensions.Http (isolated worker model)
- Azure.Identity (for managed identity authentication)

**Storage**: Azure SQL Server (production), SQL Server 2025 (local testing), Database name: `CallistraAgent`  
**Testing**: xUnit with FluentAssertions, Moq for mocking, in-memory test server for Azure Functions HTTP triggers  
**Target Platform**: Azure Functions (consumption plan), Linux runtime  
**Project Type**: Backend service (Azure Functions + SQL database)  
**Performance Goals**: 
- Call initiation: <400ms (p95)
- Member query: <50ms (p95)
- Support 1000 concurrent calls
- API responses: <500ms (p95)

**Constraints**: 
- Azure Communication Services SLA for call latency (external dependency)
- Azure Functions 5-minute timeout (consumption plan)
- SQL Server 2 MB item limit (not applicable to relational data)
- HIPAA compliance for data handling (no PHI in voicemail messages)

**Scale/Scope**: 
- 100k+ members in database
- 1M+ call session records
- 3 endpoints, 3 database tables
- Simplified MVP scope

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Based on **Callistra-Agent Constitution v1.0.0** (see `.specify/memory/constitution.md`):

### Initial Check (Pre-Design)

- ✅ **Pragmatism**: MVP approach explicitly documented with out-of-scope features tracked. Technical debt: Using consumption plan (cold starts accepted) to minimize infrastructure complexity; will optimize with premium plan if metrics show >3s cold start impact on user experience.
  
- ✅ **Code Quality**: Linting: .editorconfig with C# formatting rules. Testing: xUnit framework, target 80%+ coverage. Code review: Required via PR process. Documentation: Inline XML comments for public APIs, README for setup.
  
- ✅ **Testing Standards**: 
  - Unit tests: All domain logic (call session state management, response validation)
  - Integration tests: All 3 HTTP endpoints (InitiateCall, CallWebhook events)
  - E2E test: Full call flow (initiate → connect → ask question → capture response → persist)
  - Performance tests: NOT required for MVP (Azure Communication Services SLA sufficient)
  - Test suite target: <5 minutes total execution
  
- ✅ **UX Consistency**: 
  - No UI in MVP (backend API only)
  - Voice prompts use consistent wording (defined in spec: 3 questions + voicemail callback message)
  - Error responses follow standard HTTP status codes with actionable JSON messages
  - Accessibility: N/A for voice-only interface (standard DTMF tone detection)
  
- ✅ **Performance Requirements**: 
  - SLAs documented: Call initiation <400ms (p95), Member query <50ms (p95)
  - Client-side operations: N/A (backend service only)
  - Load test strategy: Azure Load Testing with 1000 concurrent simulated call initiations before release
  - Monitoring: Application Insights for telemetry (call duration, DTMF capture rate, error rates)
  - Optimization triggers: If p95 latency >500ms OR error rate >1%, investigate and optimize

### Post-Design Re-Check (Phase 1 Complete)

- ✅ **Pragmatism**: All technical decisions documented in research.md with rationale. No new technical debt introduced. Design remains simple with 3 endpoints, 3 tables, standard Azure services.

- ✅ **Code Quality**: Project structure defined with clear separation of concerns (Functions, Models, Services, Data, Repositories). EF Core configuration documented in data-model.md. API contracts specified in OpenAPI format.

- ✅ **Testing Standards**: Test project structure defined with unit/integration/e2e/load test organization. Fixtures for mocking Azure SDK clients documented. All public APIs have contract definitions.

- ✅ **UX Consistency**: API error responses follow RFC 7807 Problem Details format (consistent across all endpoints). Voice prompts standardized (person-detection → 3 questions → voicemail fallback).

- ✅ **Performance Requirements**: Database indexes defined on all foreign keys and frequently queried fields. Connection pooling and retry policies specified. Compiled queries identified for hot paths (member lookup).

**Constitution Gate**: ✅ PASS (both pre-design and post-design) - All principles satisfied. Design is implementation-ready.

## Project Structure

### Documentation (this feature)

```text
specs/001-minimal-call-agent/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (already exists, will enhance)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (partially exists, will enhance)
│   ├── call-events.yaml
│   ├── call-status.yaml
│   └── initiate-call.yaml
├── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
CallistraAgent/          # SQL Database project (already exists)
├── CallistraAgent.sqlproj
└── Tables/              # To be created in implementation phase
    ├── Members.sql
    ├── CallSessions.sql
    └── CallResponses.sql

src/
├── CallistraAgent.Functions/    # Azure Functions project
│   ├── Functions/
│   │   ├── InitiateCallFunction.cs
│   │   ├── CallEventWebhookFunction.cs
│   │   └── HealthCheckFunction.cs
│   ├── Models/
│   │   ├── Member.cs
│   │   ├── CallSession.cs
│   │   ├── CallResponse.cs
│   │   └── DTOs/
│   │       ├── InitiateCallRequest.cs
│   │       ├── InitiateCallResponse.cs
│   │       └── CallEventPayload.cs
│   ├── Services/
│   │   ├── ICallService.cs
│   │   ├── CallService.cs
│   │   ├── IQuestionService.cs
│   │   └── QuestionService.cs
│   ├── Data/
│   │   ├── CallistraAgentDbContext.cs
│   │   └── Repositories/
│   │       ├── IMemberRepository.cs
│   │       ├── MemberRepository.cs
│   │       ├── ICallSessionRepository.cs
│   │       └── CallSessionRepository.cs
│   ├── Configuration/
│   │   ├── AzureCommunicationServicesOptions.cs
│   │   └── DatabaseOptions.cs
│   ├── host.json
│   ├── local.settings.json
│   └── CallistraAgent.Functions.csproj
│
└── CallistraAgent.Core/         # Shared domain logic (optional, can inline in Functions if simpler)
    ├── Enums/
    │   └── CallStatus.cs
    ├── Constants/
    │   └── HealthcareQuestions.cs
    └── CallistraAgent.Core.csproj

tests/
├── CallistraAgent.Functions.Tests/
│   ├── Unit/
│   │   ├── Services/
│   │   │   ├── CallServiceTests.cs
│   │   │   └── QuestionServiceTests.cs
│   │   └── Functions/
│   │       ├── InitiateCallFunctionTests.cs
│   │       └── CallEventWebhookFunctionTests.cs
│   ├── Integration/
│   │   ├── InitiateCallIntegrationTests.cs
│   │   ├── CallWebhookIntegrationTests.cs
│   │   └── DatabaseIntegrationTests.cs
│   ├── EndToEnd/
│   │   └── FullCallFlowTests.cs
│   ├── Fixtures/
│   │   └── TestWebApplicationFactory.cs
│   └── CallistraAgent.Functions.Tests.csproj
│
└── CallistraAgent.LoadTests/
    ├── InitiateCallLoadTest.cs
    └── CallistraAgent.LoadTests.csproj

.editorconfig                    # C# formatting rules
CallistraAgent.sln              # Solution file
README.md                        # Setup and running instructions
```

**Structure Decision**: Backend service with Azure Functions (isolated worker model) for API endpoints and webhook handlers. Separate SQL project for database schema management. Testing organized by type (unit/integration/e2e/load). Core domain logic can be inlined in Functions project for MVP simplicity (avoid premature abstraction), extract to separate Core project only if shared logic emerges.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations. All Constitution principles satisfied for this MVP scope.

---

## Plan Completion Summary

**Status**: ✅ Phase 0 & Phase 1 Complete | **Date**: 2026-01-10

### Deliverables Generated

| Artifact | Status | Path |
|----------|--------|------|
| Implementation Plan | ✅ Complete | [plan.md](plan.md) |
| Research & Technology Decisions | ✅ Complete | [research.md](research.md) |
| Data Model & Schema | ✅ Enhanced | [data-model.md](data-model.md) |
| API Contracts | ✅ Enhanced | [contracts/](contracts/) |
| Quickstart Guide | ✅ Enhanced | [quickstart.md](quickstart.md) |
| Agent Context | ✅ Updated | `.github/agents/copilot-instructions.md` |

### Key Decisions Finalized

- **Runtime**: .NET 9 with Azure Functions isolated worker model
- **Database**: Azure SQL Server (CallistraAgent) with EF Core 8+
- **Call Management**: Azure Communication Services Call Automation SDK
- **Testing**: xUnit + FluentAssertions + Moq with 4-tier strategy (unit/integration/e2e/load)
- **Voicemail Detection**: DTMF-based person-detection prompt (5-second timeout)
- **Healthcare Questions**: 3 specific questions defined in spec
- **Status Model**: 8 states including VoicemailMessage for voicemail detection

### Constitution Gates

- ✅ **Pre-Design Gate**: PASS (all principles satisfied)
- ✅ **Post-Design Gate**: PASS (implementation-ready)

### Next Steps

1. **Run `/speckit.tasks`** to generate task breakdown and estimation
2. **Begin implementation** following project structure in this plan
3. **Set up local development environment** using [quickstart.md](quickstart.md)
4. **Create feature branch**: `001-minimal-call-agent`
5. **Follow TDD workflow**: Write tests first, then implement

### Implementation Readiness

| Category | Readiness | Notes |
|----------|-----------|-------|
| Requirements | 100% | All user stories, edge cases, and success criteria defined |
| Architecture | 100% | Project structure, dependencies, and data model finalized |
| Contracts | 100% | API endpoints and event schemas documented |
| Environment Setup | 100% | Local dev and Azure resource setup documented |
| Testing Strategy | 100% | Test framework, mocking approach, and coverage targets defined |
| Compliance | 100% | HIPAA considerations documented, no PHI in voicemail |

**Ready for implementation phase**. All planning artifacts complete.
