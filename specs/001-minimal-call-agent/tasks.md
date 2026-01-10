# Tasks: Minimal Viable Healthcare Call Agent

**Feature**: 001-minimal-call-agent  
**Generated**: 2026-01-10  
**Input**: Design documents from `/specs/001-minimal-call-agent/`

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Constitution Alignment**: All tasks align with **Callistra-Agent Constitution v1.0.0** principles:
- Setup tasks → Pragmatism (documented tech stack, simple MVP)
- Code/Test tasks → Code Quality + Testing Standards (80%+ coverage, xUnit, code review)
- API tasks → UX Consistency (RFC 7807 error responses, consistent messaging)
- Performance → User & Performance Requirements (<500ms API p95, 1000 concurrent users)

## Task Format

```
- [ ] [TaskID] [P?] [Story?] Description with file path
```

- **[P]**: Parallelizable (different files, no blocking dependencies)
- **[Story]**: User story label (US1, US2, US3) for phase 3+ tasks only
- File paths follow plan.md structure: `src/CallistraAgent.Functions/`, `CallistraAgent/Tables/`, `tests/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create project structure and install dependencies

- [ ] T001 Create solution file `CallistraAgent.sln` at repository root
- [ ] T002 [P] Create Azure Functions project `src/CallistraAgent.Functions/CallistraAgent.Functions.csproj` (.NET 9 isolated worker)
- [ ] T003 [P] Create test project `tests/CallistraAgent.Functions.Tests/CallistraAgent.Functions.Tests.csproj` (xUnit)
- [ ] T004 [P] Add NuGet package `Azure.Communication.CallAutomation` to Functions project
- [ ] T005 [P] Add NuGet package `Microsoft.EntityFrameworkCore.SqlServer` version 8.x to Functions project
- [ ] T006 [P] Add NuGet package `Microsoft.Azure.Functions.Worker.Extensions.Http` to Functions project
- [ ] T007 [P] Add NuGet package `Microsoft.ApplicationInsights.WorkerService` to Functions project
- [ ] T008 [P] Add NuGet package `xUnit` to test project
- [ ] T009 [P] Add NuGet package `FluentAssertions` to test project
- [ ] T010 [P] Add NuGet package `Moq` to test project
- [ ] T011 Create `.editorconfig` at repository root with C# formatting rules
- [ ] T012 Create `README.md` at repository root with setup instructions
- [ ] T013 [P] Create `host.json` in Functions project with Azure Functions runtime configuration
- [ ] T014 [P] Create `local.settings.json` in Functions project with dev configuration template (ACS connection string, SQL connection string)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database Schema

- [ ] T015 Create SQL table script `CallistraAgent/Tables/Members.sql` with schema from data-model.md
- [ ] T016 [P] Create SQL table script `CallistraAgent/Tables/CallSessions.sql` with schema from data-model.md
- [ ] T017 [P] Create SQL table script `CallistraAgent/Tables/CallResponses.sql` with schema from data-model.md
- [ ] T018 Deploy database schema to local SQL Server 2025 using sqlpackage
- [ ] T019 Insert test member data for local development (3 test members with valid phone numbers)

### Entity Framework Core Setup

- [ ] T020 Create `Member` entity class in `src/CallistraAgent.Functions/Models/Member.cs` with properties from data-model.md
- [ ] T021 [P] Create `CallSession` entity class in `src/CallistraAgent.Functions/Models/CallSession.cs` with properties from data-model.md
- [ ] T022 [P] Create `CallResponse` entity class in `src/CallistraAgent.Functions/Models/CallResponse.cs` with properties from data-model.md
- [ ] T023 Create `CallStatus` enum in `src/CallistraAgent.Functions/Models/CallStatus.cs` with values: Initiated, Ringing, Connected, Completed, Disconnected, Failed, NoAnswer, VoicemailMessage
- [ ] T024 Create `CallistraAgentDbContext` in `src/CallistraAgent.Functions/Data/CallistraAgentDbContext.cs` with entity configurations from data-model.md
- [ ] T025 Configure DbContext indexes and constraints per data-model.md specifications
- [ ] T026 Create EF Core migration `InitialCreate` using `dotnet ef migrations add`
- [ ] T027 Apply migration to local database using `dotnet ef database update`

### Configuration & Dependency Injection

- [ ] T028 Create `AzureCommunicationServicesOptions` class in `src/CallistraAgent.Functions/Configuration/AzureCommunicationServicesOptions.cs`
- [ ] T029 [P] Create `DatabaseOptions` class in `src/CallistraAgent.Functions/Configuration/DatabaseOptions.cs`
- [ ] T030 Create `Program.cs` in Functions project with DI container setup (DbContext, CallAutomationClient, services)
- [ ] T031 Configure connection pooling and retry policies for DbContext in Program.cs
- [ ] T032 Register CallAutomationClient as singleton in DI container
- [ ] T033 Configure Application Insights telemetry worker service in Program.cs

### Repository Pattern

- [ ] T034 Create `IMemberRepository` interface in `src/CallistraAgent.Functions/Data/Repositories/IMemberRepository.cs`
- [ ] T035 [P] Create `ICallSessionRepository` interface in `src/CallistraAgent.Functions/Data/Repositories/ICallSessionRepository.cs`
- [ ] T036 Implement `MemberRepository` in `src/CallistraAgent.Functions/Data/Repositories/MemberRepository.cs` with async CRUD methods
- [ ] T037 [P] Implement `CallSessionRepository` in `src/CallistraAgent.Functions/Data/Repositories/CallSessionRepository.cs` with async CRUD methods
- [ ] T038 Register repositories as scoped services in Program.cs DI container

### DTOs for API Contracts

- [ ] T039 Create `InitiateCallRequest` DTO in `src/CallistraAgent.Functions/Models/DTOs/InitiateCallRequest.cs` (empty body, memberId from route)
- [ ] T040 [P] Create `InitiateCallResponse` DTO in `src/CallistraAgent.Functions/Models/DTOs/InitiateCallResponse.cs` per contracts/initiate-call.yaml
- [ ] T041 [P] Create `CallEventPayload` DTO in `src/CallistraAgent.Functions/Models/DTOs/CallEventPayload.cs` for CloudEvent parsing
- [ ] T042 [P] Create `CallStatusResponse` DTO in `src/CallistraAgent.Functions/Models/DTOs/CallStatusResponse.cs` per contracts/call-status.yaml

### Constants

- [ ] T043 Create `HealthcareQuestions` constants class in `src/CallistraAgent.Functions/Constants/HealthcareQuestions.cs` with 3 question texts from spec.md
- [ ] T044 [P] Create `VoicemailMessages` constants class in `src/CallistraAgent.Functions/Constants/VoicemailMessages.cs` with person-detection prompt and callback message

**Checkpoint**: ✅ Foundation complete - user story implementation can begin

---

## Phase 3: User Story 1 - Make Outbound Healthcare Call (Priority: P1) 🎯 MVP

**Goal**: Initiate outbound calls to members and handle call connection/failure events

**Independent Test**: Provide member phone number, trigger call initiation, verify phone rings and CallSession record created

### Tests for User Story 1 (Constitution Requirement: Integration tests for public APIs)

- [ ] T045 [P] [US1] Create test fixture `TestWebApplicationFactory` in `tests/CallistraAgent.Functions.Tests/Fixtures/TestWebApplicationFactory.cs` for in-memory Functions host
- [ ] T046 [P] [US1] Integration test for InitiateCall endpoint - success case in `tests/CallistraAgent.Functions.Tests/Integration/InitiateCallIntegrationTests.cs`
- [ ] T047 [P] [US1] Integration test for InitiateCall endpoint - member not found (404) in InitiateCallIntegrationTests.cs
- [ ] T048 [P] [US1] Integration test for InitiateCall endpoint - member already has active call (409) in InitiateCallIntegrationTests.cs
- [ ] T049 [P] [US1] Integration test for CallConnected webhook event in `tests/CallistraAgent.Functions.Tests/Integration/CallWebhookIntegrationTests.cs`
- [ ] T050 [P] [US1] Integration test for CallDisconnected webhook event in CallWebhookIntegrationTests.cs

### Service Layer for User Story 1

- [ ] T051 [P] [US1] Create `ICallService` interface in `src/CallistraAgent.Functions/Services/ICallService.cs` with InitiateCallAsync method
- [ ] T052 [US1] Implement `CallService` in `src/CallistraAgent.Functions/Services/CallService.cs` with call initiation logic (depends on T034, T035)
- [ ] T053 [US1] Add method to CallService for handling CallConnected event (update CallSession status to Connected)
- [ ] T054 [US1] Add method to CallService for handling CallDisconnected event (update CallSession status to Disconnected, set EndTime)
- [ ] T055 [US1] Add method to CallService for handling call failure scenarios (update status to Failed)
- [ ] T056 [US1] Add method to CallService for handling NoAnswer timeout (update status to NoAnswer, set EndTime)
- [ ] T057 [US1] Register CallService as scoped service in Program.cs DI container

### API Endpoints for User Story 1

- [ ] T058 [US1] Create `InitiateCallFunction` in `src/CallistraAgent.Functions/Functions/InitiateCallFunction.cs` with POST /api/calls/initiate/{memberId} endpoint
- [ ] T059 [US1] Implement request validation in InitiateCallFunction (memberId > 0, member exists, no active call)
- [ ] T060 [US1] Implement error handling in InitiateCallFunction with RFC 7807 Problem Details responses
- [ ] T061 [US1] Create `CallEventWebhookFunction` in `src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs` with POST /api/calls/events endpoint
- [ ] T062 [US1] Implement CloudEvent parsing in CallEventWebhookFunction using CallAutomationEventParser
- [ ] T063 [US1] Route CallConnected event to CallService.HandleCallConnected method
- [ ] T064 [US1] Route CallDisconnected event to CallService.HandleCallDisconnected method
- [ ] T065 [US1] Add idempotency handling in CallEventWebhookFunction using event ID deduplication

### Unit Tests for User Story 1

- [ ] T066 [P] [US1] Unit test for CallService.InitiateCallAsync - success case in `tests/CallistraAgent.Functions.Tests/Unit/Services/CallServiceTests.cs`
- [ ] T067 [P] [US1] Unit test for CallService.InitiateCallAsync - member not found in CallServiceTests.cs
- [ ] T068 [P] [US1] Unit test for CallService.InitiateCallAsync - ACS service unavailable in CallServiceTests.cs
- [ ] T069 [P] [US1] Unit test for CallService.HandleCallConnected in CallServiceTests.cs
- [ ] T070 [P] [US1] Unit test for CallService.HandleCallDisconnected in CallServiceTests.cs
- [ ] T071 [P] [US1] Unit test for InitiateCallFunction - validation failures in `tests/CallistraAgent.Functions.Tests/Unit/Functions/InitiateCallFunctionTests.cs`

**Story 1 Deliverable**: ✅ System can initiate calls and track connection status

---

## Phase 4: User Story 2 - Ask Healthcare Questions (Priority: P2)

**Goal**: Play person-detection prompt and 3 healthcare questions using text-to-speech after call connects

**Independent Test**: Establish call and verify TTS audio plays first question

### Tests for User Story 2

- [ ] T072 [P] [US2] Integration test for playing person-detection prompt on CallConnected event in CallWebhookIntegrationTests.cs
- [ ] T073 [P] [US2] Integration test for playing first question after person confirmation in CallWebhookIntegrationTests.cs
- [ ] T074 [P] [US2] Integration test for question progression (Q1 → Q2 → Q3) in CallWebhookIntegrationTests.cs

### Service Layer for User Story 2

- [ ] T075 [P] [US2] Create `IQuestionService` interface in `src/CallistraAgent.Functions/Services/IQuestionService.cs` with PlayQuestion method
- [ ] T076 [US2] Implement `QuestionService` in `src/CallistraAgent.Functions/Services/QuestionService.cs` with TTS playback logic
- [ ] T077 [US2] Add method to QuestionService for playing person-detection prompt with DTMF recognition options
- [ ] T078 [US2] Add method to QuestionService for playing healthcare question with DTMF recognition options (10s timeout)
- [ ] T079 [US2] Add method to QuestionService for handling invalid DTMF (re-prompt logic, max 2 retries)
- [ ] T080 [US2] Add method to QuestionService for handling DTMF timeout (re-prompt once, then skip)
- [ ] T081 [US2] Register QuestionService as scoped service in Program.cs DI container

### Voicemail Detection Logic

- [ ] T082 [US2] Add method to CallService for handling person-detection timeout (no DTMF response within 5s)
- [ ] T083 [US2] Add method to QuestionService for playing voicemail callback message (no PHI)
- [ ] T084 [US2] Update CallService to mark CallSession as VoicemailMessage status and hang up after callback message

### Question Flow State Management

- [ ] T085 [US2] Create `CallSessionState` in-memory cache class in `src/CallistraAgent.Functions/Services/CallSessionState.cs` to track current question number
- [ ] T086 [US2] Add method to CallService to initialize call state on CallConnected event (current question = 0 for person detection)
- [ ] T087 [US2] Add method to CallService to progress to next question after DTMF response received
- [ ] T088 [US2] Update CallEventWebhookFunction to route PlayCompleted event to trigger next DTMF recognition

### Unit Tests for User Story 2

- [ ] T089 [P] [US2] Unit test for QuestionService.PlayPersonDetectionPrompt in `tests/CallistraAgent.Functions.Tests/Unit/Services/QuestionServiceTests.cs`
- [ ] T090 [P] [US2] Unit test for QuestionService.PlayHealthcareQuestion with question number 1-3 in QuestionServiceTests.cs
- [ ] T091 [P] [US2] Unit test for QuestionService.HandleInvalidDtmf - retry logic in QuestionServiceTests.cs
- [ ] T092 [P] [US2] Unit test for CallService.HandlePersonDetectionTimeout - voicemail flow in CallServiceTests.cs
- [ ] T093 [P] [US2] Unit test for call state progression through questions in CallServiceTests.cs

**Story 2 Deliverable**: ✅ System asks questions and detects voicemail

---

## Phase 5: User Story 3 - Capture Member Responses (Priority: P3)

**Goal**: Capture DTMF responses and save to database with CallResponse records

**Independent Test**: Simulate DTMF input and verify responses saved to database with correct associations

### Tests for User Story 3

- [ ] T094 [P] [US3] Integration test for RecognizeCompleted webhook event with DTMF response in CallWebhookIntegrationTests.cs
- [ ] T095 [P] [US3] Integration test for saving CallResponse record after DTMF capture in CallWebhookIntegrationTests.cs
- [ ] T096 [P] [US3] Integration test for completing call after all 3 questions answered in CallWebhookIntegrationTests.cs
- [ ] T097 [P] [US3] Database integration test for CallResponse foreign key constraints in `tests/CallistraAgent.Functions.Tests/Integration/DatabaseIntegrationTests.cs`

### Service Layer for User Story 3

- [ ] T098 [P] [US3] Create `ICallResponseRepository` interface in `src/CallistraAgent.Functions/Data/Repositories/ICallResponseRepository.cs`
- [ ] T099 [US3] Implement `CallResponseRepository` in `src/CallistraAgent.Functions/Data/Repositories/CallResponseRepository.cs` with async save method
- [ ] T100 [US3] Register CallResponseRepository as scoped service in Program.cs DI container
- [ ] T101 [US3] Add method to CallService for handling RecognizeCompleted event (extract DTMF tones)
- [ ] T102 [US3] Add method to CallService for saving CallResponse record with question number, text, and response value
- [ ] T103 [US3] Add method to CallService for checking if all questions answered (progress to Completed status)
- [ ] T104 [US3] Update CallEventWebhookFunction to route RecognizeCompleted event to CallService

### Call Completion Logic

- [ ] T105 [US3] Add method to CallService to mark CallSession as Completed after final question answered
- [ ] T106 [US3] Add method to CallService to hang up call automatically after completion
- [ ] T107 [US3] Update CallService to handle partial responses on mid-call disconnect (save what was captured)

### Unit Tests for User Story 3

- [ ] T108 [P] [US3] Unit test for CallService.HandleRecognizeCompleted - valid DTMF response in CallServiceTests.cs
- [ ] T109 [P] [US3] Unit test for CallService.SaveCallResponse - database persistence in CallServiceTests.cs
- [ ] T110 [P] [US3] Unit test for CallService call completion logic - all questions answered in CallServiceTests.cs
- [ ] T111 [P] [US3] Unit test for partial response handling on disconnect in CallServiceTests.cs

**Story 3 Deliverable**: ✅ System captures and persists all responses

---

## Phase 6: End-to-End Testing (Constitution Requirement)

**Purpose**: Validate critical user path (US1 → US2 → US3) works end-to-end

- [ ] T112 Create E2E test fixture with mocked Azure Communication Services client in `tests/CallistraAgent.Functions.Tests/EndToEnd/FullCallFlowTests.cs`
- [ ] T113 E2E test: Initiate call → CallConnected → Person detection → Q1 → Q2 → Q3 → Completed in FullCallFlowTests.cs
- [ ] T114 E2E test: Initiate call → CallConnected → Person detection timeout → Voicemail message → VoicemailMessage status in FullCallFlowTests.cs
- [ ] T115 E2E test: Initiate call → CallConnected → Q1 answered → Mid-call disconnect → Partial responses saved in FullCallFlowTests.cs

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Non-functional requirements and production readiness

### Error Handling & Logging

- [ ] T116 [P] Add structured logging to all service methods using ILogger
- [ ] T117 [P] Add Application Insights custom metrics for call duration, completion rate, DTMF capture rate
- [ ] T118 [P] Implement global exception handler middleware in Functions project
- [ ] T119 [P] Add retry logic for transient ACS failures in CallService

### Configuration & Security

- [ ] T120 [P] Create Azure Key Vault configuration provider for production secrets
- [ ] T121 [P] Document function key rotation process in README.md
- [ ] T122 [P] Add managed identity configuration for ACS and SQL Database in production

### Health Check & Monitoring

- [ ] T123 Create `HealthCheckFunction` in `src/CallistraAgent.Functions/Functions/HealthCheckFunction.cs` with GET /api/health endpoint
- [ ] T124 Add database connectivity check to HealthCheckFunction
- [ ] T125 Add ACS connectivity check to HealthCheckFunction

### Documentation

- [ ] T126 [P] Document API endpoints in README.md with curl examples
- [ ] T127 [P] Add inline XML comments to all public APIs per code quality principle
- [ ] T128 [P] Create deployment guide for Azure environment in `docs/deployment.md`

### Load Testing (Constitution Requirement: 1000 concurrent users)

- [ ] T129 Create Azure Load Testing configuration in `tests/CallistraAgent.LoadTests/InitiateCallLoadTest.jmx`
- [ ] T130 Run load test with 1000 concurrent InitiateCall requests and verify p95 latency <400ms
- [ ] T131 Analyze load test results and document in `docs/performance-report.md`

---

## Implementation Strategy

### MVP Scope (Minimum Viable Product)

**MVP = User Story 1 ONLY**: Initiate calls and track connection status

Rationale: Demonstrates core telephony integration works. Can validate Azure Communication Services setup before building interactive features.

**MVP Tasks**: T001-T071 (Setup + Foundation + Story 1)
**Estimated Effort**: 2-3 days for experienced .NET developer

### Incremental Delivery

1. **Phase 1-2**: Setup + Foundation (T001-T044) - ~1 day
2. **Phase 3**: User Story 1 (T045-T071) - ~1 day
3. **Phase 4**: User Story 2 (T072-T093) - ~1 day  
4. **Phase 5**: User Story 3 (T094-T111) - ~1 day
5. **Phase 6-7**: Testing + Polish (T112-T131) - ~1 day

**Total Estimated Effort**: 5-6 days for full feature

### Parallel Execution Opportunities

Tasks marked **[P]** can be executed in parallel within each phase:

**Phase 1 (Setup)**: T002, T003, T004-T010, T013-T014 can run concurrently (9 parallel tasks)

**Phase 2 (Foundation)**: 
- T016-T017 (SQL tables) - 2 parallel
- T021-T022 (entities) - 2 parallel
- T028-T029 (config classes) - 2 parallel
- T034-T035 (repository interfaces) - 2 parallel
- T036-T037 (repository implementations) - 2 parallel
- T039-T042 (DTOs) - 4 parallel
- T043-T044 (constants) - 2 parallel

**Phase 3 (User Story 1)**:
- T045-T050 (tests) - 6 parallel
- T051 (interface) can start early
- T066-T071 (unit tests) - 6 parallel

**Phase 4-5**: Similar parallelization for tests and independent components

**Phase 7 (Polish)**: T116-T122, T126-T128 can run concurrently (9 parallel tasks)

### Dependencies Summary

**Critical Path** (blocking dependencies):
1. T001-T014 (Setup) → T015-T044 (Foundation) → T052-T065 (Story 1 Implementation) → T076-T088 (Story 2 Implementation) → T099-T107 (Story 3 Implementation)

**Story Independence**:
- Story 1 can be deployed and tested independently (MVP)
- Story 2 depends on Story 1 (needs CallConnected event handling)
- Story 3 depends on Story 2 (needs question flow to capture responses)

**User Story Completion Order**:
1. US1: Make Outbound Healthcare Call (foundation for all others)
2. US2: Ask Healthcare Questions (requires US1 call connection)
3. US3: Capture Member Responses (requires US2 question flow)

---

## Validation Checklist

Before marking feature complete, verify:

- [ ] All 131 tasks completed and checked off
- [ ] Constitution principles validated:
  - [ ] 80%+ test coverage achieved (run `dotnet test --collect:"XPlat Code Coverage"`)
  - [ ] All public APIs have integration tests
  - [ ] E2E test passes for critical user path
  - [ ] Load test passes with 1000 concurrent users, p95 <400ms
  - [ ] Code review completed for all PRs
  - [ ] Linting passes with no warnings
- [ ] All 3 user stories independently testable and passing
- [ ] Success criteria from spec.md met:
  - [ ] SC-001: Call rings within 5 seconds
  - [ ] SC-002: First question plays within 3 seconds of connection
  - [ ] SC-003: DTMF captured within 2 seconds
  - [ ] SC-004: Full call completes in <3 minutes
  - [ ] SC-005: 100% call sessions persisted
  - [ ] SC-006: 100% responses correctly associated
  - [ ] SC-007: 5 concurrent calls handled without degradation
  - [ ] SC-008: System error rate <5%
- [ ] Edge cases from spec.md handled:
  - [ ] No Answer (30s timeout)
  - [ ] Voicemail Detection (person detection prompt)
  - [ ] Mid-Call Hangup (partial responses saved)
  - [ ] Invalid DTMF Input (re-prompt logic)
  - [ ] Response Timeout (skip question after retry)
- [ ] Documentation complete:
  - [ ] README.md with setup instructions
  - [ ] API documentation with examples
  - [ ] Deployment guide
  - [ ] Inline code comments on public APIs

---

## Task Generation Metadata

**Generated by**: `/speckit.tasks` command  
**Based on**:
- [spec.md](spec.md) - User stories US1, US2, US3
- [plan.md](plan.md) - Project structure and tech stack
- [data-model.md](data-model.md) - Entities: Member, CallSession, CallResponse
- [contracts/](contracts/) - API endpoints: InitiateCall, CallEvents, CallStatus
- [research.md](research.md) - Technology decisions

**Task Count**: 131 tasks (14 setup, 30 foundation, 27 US1, 22 US2, 18 US3, 4 E2E, 16 polish)

**Estimated Total Effort**: 5-6 days for experienced .NET developer

**Parallel Opportunities**: 50+ tasks can run in parallel within phases

**Ready for implementation**: ✅ All tasks have clear descriptions and file paths
