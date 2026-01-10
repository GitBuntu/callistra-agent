# Tasks: Minimal Viable Healthcare Call Agent

**Input**: Design documents from `/specs/001-minimal-call-agent/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Integration tests included per Constitution Principle III (Testing Standards). Tests follow TDD pattern - written and verified to fail before implementation.

**Organization**: Tasks grouped by user story to enable independent implementation and testing of each story.

**Constitution Principles**: All tasks align with **Callistra-Agent Constitution v1.0.0**:
- Setup tasks: **Pragmatism** (MVP-first, minimal complexity)
- Code/Implementation tasks: **Code Quality** (80%+ coverage target, single-purpose functions)
- Test tasks: **Testing Standards** (integration tests for all 3 HTTP endpoints)
- Performance tasks: **User & Performance Requirements** (5s initiation, 3s playback, 10s DTMF timeout)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create Azure Functions project CallAgent.Functions with .NET 8 target framework at src/CallAgent.Functions/
- [ ] T002 Add NuGet packages: Microsoft.NET.Sdk.Functions v4.x, Azure.Communication.CallAutomation v1.2.0+, Microsoft.EntityFrameworkCore v8.0, Microsoft.EntityFrameworkCore.SqlServer v8.0, Microsoft.EntityFrameworkCore.Design v8.0
- [ ] T003 [P] Create solution file CallAgent.sln at repository root linking src/CallAgent.Functions/
- [ ] T004 [P] Configure .editorconfig at repository root with C# code style rules per plan.md
- [ ] T005 [P] Create host.json in src/CallAgent.Functions/ with function runtime configuration
- [ ] T006 [P] Create local.settings.json template in src/CallAgent.Functions/ with ACS connection string, SQL connection string, and callback URL placeholders

**Checkpoint**: Project structure ready for code implementation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Create Member entity class in src/CallAgent.Functions/Data/Entities/Member.cs with Id, FirstName, LastName, PhoneNumber, Program, Status, CreatedAt, UpdatedAt properties
- [ ] T008 [P] Create CallSession entity class in src/CallAgent.Functions/Data/Entities/CallSession.cs with Id, MemberId, CallConnectionId, Status (enum: Initiated/Ringing/Connected/Completed/Disconnected/Failed/NoAnswer/VoicemailMessage), StartTime, EndTime, CreatedAt, UpdatedAt properties
- [ ] T009 [P] Create CallResponse entity class in src/CallAgent.Functions/Data/Entities/CallResponse.cs with Id, CallSessionId, QuestionNumber, QuestionText, ResponseValue, RespondedAt properties
- [ ] T010 Create CallAgentDbContext in src/CallAgent.Functions/Data/CallAgentDbContext.cs with DbSet properties for Member, CallSession, CallResponse, and OnModelCreating configuration
- [ ] T011 Configure entity relationships and indexes in src/CallAgent.Functions/Data/CallAgentDbContext.cs: Member 1:N CallSession 1:N CallResponse, unique index on Member.PhoneNumber, index on CallSession.Status
- [ ] T012 Create initial EF Core migration in src/CallAgent.Functions/Data/Migrations/ using dotnet ef migrations add InitialCreate
- [ ] T013 [P] Create AcsOptions configuration class in src/CallAgent.Functions/Configuration/AcsOptions.cs with ConnectionString, CallbackUrl, SourcePhoneNumber properties
- [ ] T014 [P] Create ICallService interface in src/CallAgent.Functions/Services/ICallService.cs with methods: InitiateCallAsync, HandleCallConnectedAsync, HandleDtmfResponseAsync, HandleCallDisconnectedAsync
- [ ] T015 Register services in Program.cs or Startup.cs: DbContext with SQL connection string, CallAutomationClient with ACS connection string, ICallService scoped registration

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Make Outbound Healthcare Call (Priority: P1) 🎯 MVP

**Goal**: Healthcare administrators can initiate automated calls to enrolled program members

**Independent Test**: Provide a member phone number, trigger call initiation API, verify phone rings and CallSession record created

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T016 [P] [US1] Create integration test CallInitiationTests.cs in tests/CallAgent.Functions.Tests/Integration/ testing POST /api/calls/initiate/{memberId} with valid member returns 202 Accepted with callSessionId
- [ ] T017 [P] [US1] Add test case in CallInitiationTests.cs for invalid member ID returns 404 Not Found with ProblemDetails
- [ ] T018 [P] [US1] Add test case in CallInitiationTests.cs for member with invalid phone number returns 400 Bad Request
- [ ] T019 [P] [US1] Add test case in CallInitiationTests.cs verifying CallSession record persisted to database with Status=Initiated

### Implementation for User Story 1

- [ ] T020 [US1] Implement CallInitiation.cs Azure Function in src/CallAgent.Functions/Functions/ with HTTP POST trigger on route "api/calls/initiate/{memberId}"
- [ ] T021 [US1] Add member lookup validation in CallInitiation.cs: verify member exists, Status=Active, PhoneNumber valid E.164 format
- [ ] T022 [US1] Create CallSession record in CallInitiation.cs with Status=Initiated, StartTime=now, MemberId reference
- [ ] T023 [US1] Implement CallService.InitiateCallAsync in src/CallAgent.Functions/Services/CallService.cs using CallAutomationClient.CreateCall with member PhoneNumber, callback URL from AcsOptions, source phone number
- [ ] T024 [US1] Update CallSession with CallConnectionId returned from ACS CreateCall response in CallService.InitiateCallAsync
- [ ] T025 [US1] Add error handling in CallInitiation.cs: catch ACS exceptions, update CallSession Status=Failed, return 500 with ProblemDetails
- [ ] T026 [US1] Return 202 Accepted response from CallInitiation.cs with callSessionId, memberId, status, startTime, callbackUrl

**Checkpoint**: User Story 1 complete - can manually initiate calls that ring target phone numbers

---

## Phase 4: User Story 2 - Ask Healthcare Questions (Priority: P2)

**Goal**: Once call connects, system plays pre-defined healthcare questions using text-to-speech

**Independent Test**: Establish a call connection and verify text-to-speech plays person-detection prompt followed by healthcare questions

### Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T027 [P] [US2] Create integration test CallEventsTests.cs in tests/CallAgent.Functions.Tests/Integration/ testing POST /api/calls/events with CallConnected CloudEvent triggers person-detection prompt
- [ ] T028 [P] [US2] Add test case in CallEventsTests.cs verifying person-detection DTMF timeout (5s no response) triggers generic voicemail message and marks session VoicemailMessage
- [ ] T029 [P] [US2] Add test case in CallEventsTests.cs verifying DTMF response (pressing 1) to person-detection proceeds with healthcare question 1
- [ ] T030 [P] [US2] Add test case in CallEventsTests.cs verifying healthcare questions play in sequence after person confirmation

### Implementation for User Story 2

- [ ] T031 [US2] Implement CallEvents.cs Azure Function in src/CallAgent.Functions/Functions/ with HTTP POST trigger on route "api/calls/events"
- [ ] T032 [US2] Add CloudEvent deserialization logic in CallEvents.cs to parse event type (CallConnected, RecognizeCompleted, PlayCompleted, CallDisconnected)
- [ ] T033 [US2] Implement CallService.HandleCallConnectedAsync in src/CallAgent.Functions/Services/CallService.cs: update CallSession Status=Connected, play person-detection prompt "Press 1 if you can hear this message" with 5s DTMF timeout
- [ ] T034 [US2] Add DTMF recognition handling in CallService: if no response to person-detection within 5s, play generic callback message "Hello, this is [Organization] calling about your healthcare enrollment. Please call us back at [phone number]. Thank you.", then hang up
- [ ] T035 [US2] Update CallSession Status=VoicemailMessage in CallService when voicemail message played
- [ ] T036 [US2] Add person confirmation logic in CallService: if DTMF "1" received to person-detection, play healthcare question 1 "Can you confirm you are enrolled in [Program]? Press 1 for yes, 2 for no." with 10s timeout
- [ ] T037 [US2] Create question progression logic in CallService: after RecognizeCompleted event for question N, play question N+1 (max 3 questions total)
- [ ] T038 [US2] Add re-prompt logic in CallService: if no DTMF response within 10s, re-play same question once before moving to next
- [ ] T039 [US2] Return 200 OK from CallEvents.cs after processing each event

**Checkpoint**: User Story 2 complete - calls play person-detection prompt, handle voicemail, and ask healthcare questions

---

## Phase 5: User Story 3 - Capture Member Responses (Priority: P3)

**Goal**: System records member DTMF answers (1=yes, 2=no) to questions for healthcare coordinator analysis

**Independent Test**: Simulate DTMF input during call and verify responses saved to database with correct call session association

### Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T040 [P] [US3] Add test case in CallEventsTests.cs verifying RecognizeCompleted event with DTMF "1" creates CallResponse record with ResponseValue=1
- [ ] T041 [P] [US3] Add test case in CallEventsTests.cs verifying RecognizeCompleted event with DTMF "2" creates CallResponse record with ResponseValue=2
- [ ] T042 [P] [US3] Add test case in CallEventsTests.cs verifying all 3 responses saved after completing question flow
- [ ] T043 [P] [US3] Add test case in CallEventsTests.cs verifying CallSession Status=Completed after final response captured
- [ ] T044 [P] [US3] Create integration test CallStatusTests.cs in tests/CallAgent.Functions.Tests/Integration/ testing GET /api/calls/status/{callConnectionId} returns session with responses array

### Implementation for User Story 3

- [ ] T045 [US3] Implement CallService.HandleDtmfResponseAsync in src/CallAgent.Functions/Services/CallService.cs to process RecognizeCompleted events
- [ ] T046 [US3] Extract DTMF tone from RecognizeCompleted event data in HandleDtmfResponseAsync (expecting "1" or "2")
- [ ] T047 [US3] Create CallResponse entity record in HandleDtmfResponseAsync with CallSessionId, QuestionNumber (1-3), QuestionText, ResponseValue (1 or 2), RespondedAt=now
- [ ] T048 [US3] Persist CallResponse to database via DbContext.SaveChangesAsync in HandleDtmfResponseAsync
- [ ] T049 [US3] Check if all 3 questions answered in HandleDtmfResponseAsync: if yes, update CallSession Status=Completed and hang up call
- [ ] T050 [US3] Implement CallStatus.cs Azure Function in src/CallAgent.Functions/Functions/ with HTTP GET trigger on route "api/calls/status/{callConnectionId}"
- [ ] T051 [US3] Query CallSession by CallConnectionId in CallStatus.cs including related Member and CallResponses (eager loading)
- [ ] T052 [US3] Return 200 OK from CallStatus.cs with JSON response: callSessionId, memberId, memberName, status, startTime, endTime, responses array (questionNumber, questionText, responseValue)
- [ ] T053 [US3] Add 404 Not Found handling in CallStatus.cs if CallConnectionId not found
- [ ] T054 [US3] Implement CallService.HandleCallDisconnectedAsync to update CallSession Status=Disconnected when member hangs up mid-call

**Checkpoint**: User Story 3 complete - full call flow works end-to-end with response capture and status querying

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T055 [P] Add XML documentation comments to all public methods in CallService.cs, CallInitiation.cs, CallEvents.cs, CallStatus.cs
- [ ] T056 [P] Create unit tests CallServiceTests.cs in tests/CallAgent.Functions.Tests/Unit/ with mocked CallAutomationClient covering InitiateCallAsync, HandleCallConnectedAsync, HandleDtmfResponseAsync, HandleCallDisconnectedAsync
- [ ] T057 Add structured logging using ILogger in all Functions and Services with log levels: Information (call initiated, connected, completed), Warning (timeouts, retries), Error (ACS failures, database errors)
- [ ] T058 [P] Update README.md at repository root with architecture diagram, setup instructions reference to quickstart.md, API endpoint documentation
- [ ] T059 Run quickstart.md validation: provision Azure resources, apply database migration, test all 3 endpoints with curl commands
- [ ] T060 [P] Add .gitignore entries for local.settings.json, bin/, obj/, .vs/

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational (Phase 2) completion
  - User Story 1 (Phase 3): Can start after Foundational - No dependencies on other stories
  - User Story 2 (Phase 4): Can start after Foundational - Requires US1 for call initiation but independently testable with mocked events
  - User Story 3 (Phase 5): Can start after Foundational - Requires US1/US2 for full flow but independently testable with mocked events
- **Polish (Phase 6)**: Depends on all user stories (Phase 3-5) completion

### Recommended Execution Strategy

**Sequential (Single Developer)**:
1. Complete Setup (T001-T006)
2. Complete Foundational (T007-T015)
3. Implement User Story 1 fully (T016-T026) - delivers basic call initiation
4. Implement User Story 2 fully (T027-T039) - adds interactive questions
5. Implement User Story 3 fully (T040-T054) - adds response capture
6. Complete Polish tasks (T055-T060)

**Parallel (Team of 2-3)**:
1. All team members: Complete Setup (T001-T006) together
2. All team members: Complete Foundational (T007-T015) together
3. Split user stories after Foundational complete:
   - Developer A: User Story 1 (T016-T026)
   - Developer B: User Story 2 tests (T027-T030), then wait for US1 CallService interface
   - Developer C: User Story 3 tests (T040-T044)
4. After US1 complete: Developer B implements US2 (T031-T039), Developer C implements US3 (T045-T054)
5. All: Polish tasks in parallel (T055-T060)

### Task Dependencies Within Each Phase

**Phase 1 (Setup)**: T001 → T002 → {T003, T004, T005, T006} in parallel

**Phase 2 (Foundational)**: {T007, T008, T009} in parallel → T010 → T011 → T012, then {T013, T014} in parallel → T015

**Phase 3 (User Story 1)**: {T016, T017, T018, T019} tests in parallel (all MUST fail) → T020 → T021 → T022 → T023 → T024 → T025 → T026

**Phase 4 (User Story 2)**: {T027, T028, T029, T030} tests in parallel (all MUST fail) → T031 → T032 → T033 → T034 → T035 → T036 → T037 → T038 → T039

**Phase 5 (User Story 3)**: {T040, T041, T042, T043, T044} tests in parallel (all MUST fail) → T045 → T046 → T047 → T048 → T049 → {T050, T054} in parallel → T051 → T052 → T053

**Phase 6 (Polish)**: {T055, T056, T058, T060} in parallel → T057 → T059

### Parallel Opportunities Summary

- **Setup**: 4 tasks can run in parallel (T003, T004, T005, T006)
- **Foundational**: 5 tasks can run in parallel (T007, T008, T009, then T013, T014)
- **User Story 1**: 4 test tasks can run in parallel (T016-T019)
- **User Story 2**: 4 test tasks can run in parallel (T027-T030)
- **User Story 3**: 5 test tasks can run in parallel (T040-T044), then 2 implementation tasks (T050, T054)
- **Polish**: 4 tasks can run in parallel (T055, T056, T058, T060)

**Total Parallelizable Tasks**: 22 of 60 tasks marked [P] (37%)

---

## Implementation Strategy

**MVP Scope (Day 1)**: Phase 1 + Phase 2 + Phase 3 (User Story 1) = **26 tasks** → Delivers: Basic call initiation to member phone numbers

**MVP+ Scope (Day 2)**: Add Phase 4 (User Story 2) = **+13 tasks** → Delivers: Interactive healthcare questions with person-detection and voicemail handling

**Full Feature (Day 3)**: Add Phase 5 (User Story 3) + Phase 6 (Polish) = **+21 tasks** → Delivers: Complete response capture, status querying, production-ready code

**Estimated Time**:
- Setup: 30 minutes (6 tasks)
- Foundational: 1.5 hours (9 tasks)
- User Story 1: 2 hours (11 tasks including tests)
- User Story 2: 2 hours (13 tasks including tests)
- User Story 3: 2 hours (15 tasks including tests)
- Polish: 1 hour (6 tasks)

**Total**: ~9 hours for complete feature (fits within 1-2 day sprint with testing)

---

## Validation Checklist

Before marking feature complete, verify:

- [ ] All 60 tasks completed and checked off
- [ ] All integration tests pass (CallInitiationTests, CallEventsTests, CallStatusTests)
- [ ] Unit tests for CallService pass with >80% coverage
- [ ] Manual test following quickstart.md succeeds: initiate call → phone rings → person-detection → healthcare questions → responses captured → status query returns data
- [ ] Constitution principles validated: Pragmatism (MVP delivered), Code Quality (80%+ coverage, documented), Testing Standards (integration tests for all endpoints), Performance (5s initiation, 3s playback, 10s DTMF)
- [ ] All 3 HTTP endpoints documented in README.md
- [ ] Database migration applied successfully
- [ ] Azure Communication Services integration tested with real phone number

---

**Tasks Complete**: Ready for implementation. Follow tasks sequentially or assign to team members based on parallel opportunities identified above.
