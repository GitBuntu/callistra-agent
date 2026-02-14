# Tasks: Human Voice Response Recognition (Feature 002)

**Feature**: Add human voice response recognition alongside DTMF keypad recognition  
**Branch**: `002-voice-response`  
**Input Documents**: spec.md, plan.md, domain-review.md, data-model.md, contracts/  
**Organization**: Grouped by user story for independent implementation and testing

---

## Overview

- **Total Tasks**: 48 tasks across 6 phases
- **Estimated Story Points**: 18-21 (MEDIUM complexity)
- **Test Coverage Target**: 80%+ (TDD: write tests first)
- **Regression Protection**: 9 SAFE extensions, 0 prohibited modifications (risky pattern mitigated via overload)
- **Independent Testing**: Each user story can be implemented, tested, and deployed separately

### User Stories (Execution Order)

| Story | Priority | Description | Tasks | Test Tasks |
|-------|----------|-------------|-------|-----------|
| **US1** | P1 | DTMF Recognition (Backwards Compatibility) | T007-T013 | T007, T009, T012-T013 |
| **US2** | P1 | Human Voice Response Recognition | T014-T030 | T018-T020, T025-T027, T029-T030 |
| **US3** | P1 | Campaign-Level Mode Configuration | T031-T041 | T035-T036, T040-T041 |

### Dependency Graph

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational - BLOCKING)
    ├─ T004: Reuse Scan
    ├─ T005: Database Context Setup
    └─ T006: Configuration (Speech Services)
    ↓
Phase 3 (US1: DTMF)  →  Phase 4 (US2: Voice)  →  Phase 5 (US3: Mode Config)
                                 ↓
                        Phase 6 (Polish)
```

**Critical Path**: Phase 1 → Phase 2 (T004-T006 must complete first) → US1 (T007-T013) → US2 (T014-T030) → US3 (T031-T041) → Polish (T042-T048)

### Parallel Execution Opportunities

**After Phase 2 complete**:
- [P] T007 (US1 test) ↔ T014 (US2 test) — Can write tests in parallel for different user stories
- [P] T008 (US1 DTMF validation) ↔ T015 (US2 speech config) — Different code paths
- [P] T016 (US2 entity) ↔ T031 (US3 schema) — Different tables/models
- [P] T017-T019 (US2 services) ↔ T032-T033 (US3 config) — Different service areas

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Initialize project structure and prepare for implementation  
**Duration**: 2-3 hours  
**Blocking**: Nothing (can start immediately)  
**Deliverable**: Project ready for foundational tasks

- [ ] T001 Create database migration script 002_AddVoiceSupport.sql in CallistraAgent/Migrations/

  **Details**: Create SQL migration script that:
  - Adds `ResponseMode` column to `CallSessions` table (NVARCHAR(20), default 'dtmf-only', check constraint)
  - Adds `ResponseSource` column to `CallResponses` table (NVARCHAR(20), default 'dtmf', check constraint)
  - Creates `VoiceRecognitionResults` table with voice metadata columns
  - Adds supporting indexes on CallSessionId, ProcessedAt
  - Includes rollback script
  
  **Reference**: [data-model.md](data-model.md) migration script section

- [ ] T002 Add voice-specific constants to Constants/VoiceClassification.cs

  **Details**: Create new constants file with:
  - Confidence thresholds (MIN_CONFIDENCE = 0.75, HIGH_CONFIDENCE = 0.90)
  - Intent mappings ("yes", "no", "skip" voice patterns)
  - Response sources enum : "dtmf", "voice"
  - Response modes enum: "dtmf-only", "voice-only"
  - Reprompt message templates
  
  **File**: `src/CallistraAgent.Functions/Constants/VoiceClassification.cs`

- [ ] T003 [P] Update Configuration/AzureCommunicationServicesOptions.cs with Speech Services properties

  **Details**: Add to existing AzureCommunicationServicesOptions class:
  - `SpeechServicesEndpoint` property (string, from config)
  - `SpeechServicesKey` property (string, from config/Key Vault)
  - `SpeechServicesRegion` property (string, e.g., "eastus")
  - Documentation comments explaining each property
  
  **Regression Protection**: ⚠️ Do NOT modify existing ACS properties. Add only new properties.  
  **File**: `src/CallistraAgent.Functions/Configuration/AzureCommunicationServicesOptions.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infrastructure that MUST complete before ANY user story implementation  
**Duration**: 4-6 hours  
**Blocking**: All user stories depend on this phase completing  
**Deliverable**: Database schema ready, configuration ready, reuse strategy validated

- [ ] T004 Scan codebase for reusable components and mapping to voice feature

  **Details**: Analyze existing codebase and document:
  - Existing services: CallService, CallSessionState, QuestionService methods that can be reused
  - Database schema: CallSession, CallResponse, Member relationships
  - Error handling patterns: RFC 7807 Problem Details, logging patterns
  - State machine: Call status transitions, question progression
  - Webhook handling: CloudEvents parsing, deserialization
  - Mapping: Link each requirement (FR-001 through FR-015) to safe reuse patterns
  
  **Deliverable**: Reuse mapping document in `specs/002-voice-response/reuse-mapping.md`  
  **Reference**: [domain-review.md](domain-review.md) reuse strategy section  
  **File**: Create `specs/002-voice-response/reuse-mapping.md`

- [ ] T005 [P] Run database migration script and verify schema changes

  **Details**: Execute the migration script created in T001:
  - Connect to CallistraAgent database (local or dev)
  - Run 002_AddVoiceSupport.sql script
  - Verify new columns exist: CallSessions.ResponseMode, CallResponses.ResponseSource
  - Verify new table exists: VoiceRecognitionResults
  - Verify constraints applied (check constraints on ResponseMode, ResponseSource values)
  - Verify indexes created on VoiceRecognitionResults
  - Test rollback script works (roll back, then reapply)
  
  **Regression Protection**: ⚠️ Verify existing columns unchanged. No data migration needed (new columns have defaults).  
  **File**: Run from `/CallistraAgent/Migrations/002_AddVoiceSupport.sql`

- [ ] T006 [P] Add Entity Framework Core mappings for VoiceRecognitionResult and extended entities

  **Details**: Update CallistraAgentDbContext.OnModelCreating() to:
  - Map VoiceRecognitionResult entity (new)
  - Configure CallSession.ResponseMode property (new column mapping)
  - Configure CallResponse.ResponseSource property (new column mapping)
  - Add check constraints: ResponseMode ∈ {dtmf-only, voice-only}
  - Add check constraints: ResponseSource ∈ {dtmf, voice}
  - Configure foreign key: VoiceRecognitionResult → CallSession
  - Add indexes: VoiceRecognitionResult(CallSessionId), VoiceRecognitionResult(ProcessedAt)
  - Property validation rules (required fields, max lengths)
  
  **Regression Protection**: ⚠️ Do NOT modify existing entity mappings (Member, CallSession status handling, etc.). Add new mappings only.  
  **File**: `src/CallistraAgent.Functions/Data/CallistraAgentDbContext.cs`  
  **Reference**: [data-model.md](data-model.md) EF Core configuration section

- [ ] T007 [P] Create VoiceRecognitionResult model class with validation

  **Details**: Create new model in Models/:
  - Properties: Id (PK), CallSessionId (FK), QuestionNumber, TranscribedText, ConfidenceScore (decimal 0-1), Intent (enum), AudioDurationMs, ProcessedAt
  - XML documentation on all public properties
  - Validation attributes: [Required], [Range], [StringLength]
  - Navigation property: CallSession (EF relationship)
  - Immutable after creation (no setters on calculated fields)
  
  **File**: `src/CallistraAgent.Functions/Models/VoiceRecognitionResult.cs`  
  **Test**: Create unit test in `tests/CallistraAgent.Functions.Tests/Unit/Models/VoiceRecognitionResultTests.cs`

---

## Phase 3: User Story 1 - DTMF Recognition (Backwards Compatibility)

**Priority**: P1 (MVP-critical; zero regression)  
**Purpose**: Ensure existing DTMF functionality remains unchanged  
**Duration**: 3-4 hours  
**Independent Tests**: Yes (can test US1 standalone)  
**Success Criteria**: All DTMF tests pass; DTMF-only campaigns work identically to Feature 001  

**Why This Story First**: Foundation for all voice work; validates regression prevention gates

- [ ] T008 [US1] Write unit tests for DTMF tone parsing and validation (TDD: RED phase)

  **Details**: Create test file `tests/CallistraAgent.Functions.Tests/Unit/Services/DtmfRecognitionTests.cs` with tests (write BEFORE implementation):
  - Test: Valid DTMF tone "1" parsed correctly
  - Test: Valid DTMF tone "2" parsed correctly
  - Test: Invalid tone "#" rejected
  - Test: Invalid tone "*" rejected
  - Test: Malformed event (missing dtmfTones) handled gracefully
  - Use FluentAssertions: `result.Should().Be("1")`
  - Mock external dependencies (Azure Communication Services)
  
  **Reference**: spec.md test case TC-001, TC-002, TC-005  
  **File**: `tests/CallistraAgent.Functions.Tests/Unit/Services/DtmfRecognitionTests.cs`

- [ ] T009 [US1] Verify CallService.HandleRecognizeCompletedAsync processes DTMF unchanged

  **Details**: Code review and unit test:
  - Confirm HandleRecognizeCompletedAsync method signature unchanged (inputs: callConnectionId, dtmfTones)
  - Confirm DTMF parsing logic unchanged (extracts digit from event.ResultInformation.RecognitionInfo)
  - Confirm validation logic unchanged (checks allowed values 1-3)
  - Confirm SaveCallResponseAsync call unchanged (passes digit string)
  - Create integration test calling HandleRecognizeCompletedAsync end-to-end with DTMF event
  
  **Regression Protection**: ⚠️ Do NOT modify existing method or create overloads. Comment line 393-410 to document method is unchanged for feature 002.  
  **File**: `src/CallistraAgent.Functions/Services/CallService.cs` (read-only for this task)  
  **Test**: `tests/CallistraAgent.Functions.Tests/Integration/CallServiceDtmfTests.cs`

- [ ] T010 [US1] [P] Write integration test for DTMF-only campaign end-to-end flow

  **Details**: Create integration test in `tests/CallistraAgent.Functions.Tests/Integration/DtmfCampaignFlowTests.cs`:
  - Setup: Create DTMF-only campaign with ResponseMode='dtmf-only'
  - Setup: Create call session in that campaign
  - Action: Send DTMF RecognizeCompleted CloudEvent (user presses "1")
  - Action: Parse event via CallEventWebhookFunction
  - Verify: CallResponse recorded with ResponseSource='dtmf', ResponseValue='1'
  - Verify: State transitions to next question
  - Verify: VoiceRecognitionResults table remains empty (no voice records)
  - Test both valid (1-3) and invalid (#) tones
  
  **File**: `tests/CallistraAgent.Functions.Tests/Integration/DtmfCampaignFlowTests.cs`

- [ ] T011 [US1] [P] Create fixture for DTMF test data and mock Azure Communication Services

  **Details**: Create test fixture in `tests/CallistraAgent.Functions.Tests/Fixtures/DtmfEventFixtures.cs`:
  - CloudEvents with DTMF tone payloads (valid: 1, 2, 3; invalid: #, *)
  - Mock Azure Communication Services responses
  - Factory methods for common test scenarios (DTMF complete, DTMF failed, malformed)
  - Reusable for multiple test files
  
  **File**: `tests/CallistraAgent.Functions.Tests/Fixtures/DtmfEventFixtures.cs`

- [ ] T012 [US1] Update CallEventWebhookFunction to validate incoming DTMF events

  **Details**: Review and enhance CallEventWebhookFunction.Run() method:
  - Confirm it routes DTMF RecognizeCompleted events to CallService.HandleRecognizeCompletedAsync
  - Confirm cloud event parsing works (CloudEvent deserialization from JSON)
  - Add logging: "Processing DTMF event for call {callConnectionId}"
  - Add error handling: catch malformed cloud events, return 400 with error details
  - Verify no changes to existing logic (read-only audit)
  
  **Regression Protection**: ⚠️ Do NOT modify routing logic. Add logging/validation only.  
  **File**: `src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs`  
  **Test**: Integration test in T010

- [ ] T013 [US1] [P] Create unit tests for DTMF reprompt when invalid tone received

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Unit/Services/DtmfRepromptTests.cs`:
  - Test: Invalid DTMF triggers reprompt (QuestionService.PlayQuestionAsync called again)
  - Test: Reprompt message doesn't change based on mode (consistent for DTMF)
  - Test: State remains on same question (no progression)
  - Mock QuestionService to verify reprompt called
  - Use FluentAssertions for assertions
  
  **File**: `tests/CallistraAgent.Functions.Tests/Unit/Services/DtmfRepromptTests.cs`

---

## Phase 4: User Story 2 - Human Voice Response Recognition

**Priority**: P1 (Core new feature)  
**Purpose**: Add voice recognition via Azure Speech Services  
**Duration**: 8-10 hours (most complex)  
**Independent Tests**: Yes (requires T004-T007 and US1 complete)  
**Success Criteria**: Voice-only campaigns recognize "yes"/"no" with 75%+ confidence; store transcriptions

**Why This Story After US1**: Builds on verified DTMF path; reuses webhook infrastructure

- [ ] T014 [US2] Write unit tests for Azure Speech Services integration (TDD: RED phase)

  **Details**: Create test file `tests/CallistraAgent.Functions.Tests/Unit/Services/AzureSpeechServicesTests.cs` with tests (write BEFORE implementation):
  - Test: InvokeAzureSpeechServicesAsync calls Azure Speech Services REST API
  - Test: Response parsed correctly (transcription, confidence score)
  - Test: Confidence score returned as decimal 0-1 (not 0-100)
  - Test: Language locale (en-US) passed to API
  - Test: API error (network, timeout) caught and wrapped in custom exception
  - Test: Audio stream handled correctly (stream position)
  - Mock Azure HTTP client using HttpClientFactory or Moq
  - Use FluentAssertions for assertions
  
  **Reference**: spec.md requirements FR-004, FR-015  
  **File**: `tests/CallistraAgent.Functions.Tests/Unit/Services/AzureSpeechServicesTests.cs`

- [ ] T015 [US2] [P] Create VoiceResponseInterpreter class for intent mapping

  **Details**: Create new service in Services/:
  - Class: VoiceResponseInterpreter
  - Method: InterpretVoiceResponse(string transcription) → string intent
  - Intent mappings (case-insensitive):
    - "yes", "yeah", "affirmative", "sure", "ok" → "yes"
    - "no", "nope", "negative", "never" → "no"
    - "skip", "pass", "n/a" → "skip"
    - Single digit "1", "2", "3" → "1", "2", "3" (for numeric questions)
  - XML documentation on public methods
  - Unit test coverage: all intent mappings validated
  - Immutable service (no state)
  
  **File**: `src/CallistraAgent.Functions/Services/VoiceResponseInterpreter.cs`  
  **Test**: `tests/CallistraAgent.Functions.Tests/Unit/Services/VoiceResponseInterpreterTests.cs` (with TC-004 coverage)

- [ ] T016 [US2] [P] Implement CallService.InvokeAzureSpeechServicesAsync method

  **Details**: Add new method to CallService:
  - Signature: `public async Task<SpeechRecognitionResult> InvokeAzureSpeechServicesAsync(string callConnectionId, Stream audioStream, string locale, CancellationToken cancellationToken)`
  - Behavior:
    - Build Azure Speech Services REST request (with authentication)
    - POST audio stream to Speech API
    - Parse response JSON: transcription, confidence score
    - Return: transcription (string), confidence (decimal 0-1), language (string), duration (ms)
  - Error handling: catch network errors, API errors, timeout; throw SpeechRecognitionException
  - Logging: Log request (call ID, locale), response (transcription, confidence)
  - Async/await throughout (no blocking I/O)
  
  **Regression Protection**: ⚠️ New method only. Do NOT modify existing CallService methods.  
  **File**: `src/CallistraAgent.Functions/Services/CallService.cs`  
  **Test**: Unit test from T014, integration test from T022
  
  **Reference**: [plan.md](plan.md) SAFE reuse section #1

- [ ] T017 [US2] [P] Implement CallService.SaveVoiceResponseAsync method (new overload)

  **Details**: Add new method to CallService:
  - Signature: `public async Task SaveVoiceResponseAsync(int callSessionId, int questionNumber, SpeechRecognitionResult speechResult, CancellationToken cancellationToken)`
  - Behavior:
    - Create VoiceRecognitionResult entity from speechResult
    - Save to VoiceRecognitionResults table
    - Create CallResponse record with ResponseSource='voice' (using VoiceResponseInterpreter to map transcription → intent)
    - Validate confidence >= 75% (log warning if lower)
    - Transaction: atomic save (both records or neither)
  - Immutability: Set CreatedAt, don't allow updates
  - Logging: Log voice response saved (call ID, question number, intent, confidence)
  
  **Regression Protection**: ⚠️ NEW method (overload). Do NOT modify existing SaveCallResponseAsync method.  
  **File**: `src/CallistraAgent.Functions/Services/CallService.cs`  
  **Test**: `tests/CallistraAgent.Functions.Tests/Unit/Services/CallServiceVoiceTests.cs`
  
  **Reference**: [domain-review.md](domain-review.md) "Suggested SAFE Workaround" section (use new overload, not signature change)

- [ ] T018 [US2] [P] Add VoiceRecognitionResult repository method to save voice metadata

  **Details**: Create/extend repository in Data/Repositories/:
  - Method: CreateVoiceRecognitionResultAsync(VoiceRecognitionResult result, CancellationToken cancellationToken)
  - Behavior: Save entity to context, flush to database
  - Return: ID of created record
  - Error handling: Catch database constraint violations, throw custom exception
  - Logging: Log record creation (call session ID)
  
  **File**: `src/CallistraAgent.Functions/Data/Repositories/VoiceRecognitionResultRepository.cs` or extend CallSessionRepository
  **Test**: Integration test with database

- [ ] T019 [US2] [P] Implement CallSessionState.IsVoiceOnlyMode method

  **Details**: Add new method to CallSessionState service:
  - Signature: `public async Task<bool> IsVoiceOnlyMode(string callConnectionId, CancellationToken cancellationToken)`
  - Behavior:
    - Query CallSession by callConnectionId
    - Check ResponseMode == 'voice-only'
    - Cache result in session state for 60 seconds (avoid repeated DB queries)
  - Error handling: Catch database errors, throw CallStateException
  - Logging: Log mode check result (call ID, outcome)
  
  **File**: `src/CallistraAgent.Functions/Services/CallSessionState.cs`  
  **Test**: `tests/CallistraAgent.Functions.Tests/Unit/Services/CallSessionStateTests.cs`
  
  **Reference**: [domain-review.md](domain-review.md) SAFE reuse section #2

- [ ] T020 [US2] [P] Write unit test for confidence threshold validation

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Unit/Services/ConfidenceThresholdTests.cs`:
  - Test: Confidence ≥ 0.75 → Accept response
  - Test: Confidence < 0.75 → Trigger reprompt
  - Test: Confidence value validated (0-1 range only)
  - Test: Low confidence logged with call ID and question number
  - Mock speech result with various confidence scores
  - Use FluentAssertions
  
  **Reference**: spec.md requirement FR-006, test case TC-003

- [ ] T021 [US2] Implement QuestionService.PlayVoiceQuestionAsync method (new)

  **Details**: Add new method to QuestionService:
  - Signature: `public async Task PlayVoiceQuestionAsync(ICallConnection callConnection, int questionNumber, CancellationToken cancellationToken)`
  - Behavior:
    - Retrieve question text from questionnaire
    - Generate voice-specific prompt (e.g., "Say yes or no" instead of "Press 1 for yes")
    - Use Azure Communication Services to play text-to-speech prompt
    - Set call to awaiting voice input state (via CallSessionState)
    - Logging: Log prompt played (call ID, question number)
  - Async/await throughout
  - Error handling: catch TTS errors, log, fail gracefully
  
  **File**: `src/CallistraAgent.Functions/Services/QuestionService.cs`  
  **Test**: Integration test in T023
  
  **Reference**: [domain-review.md](domain-review.md) SAFE reuse section #3 (new overload)

- [ ] T022 [US2] Write integration test for voice recognition end-to-end (up to response interpretation)

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/VoiceRecognitionTests.cs`:
  - Setup: voice-only campaign
  - Setup: call session initiated
  - Action: Mock audio stream received
  - Action: Call InvokeAzureSpeechServicesAsync with audio
  - Verify: Azure Speech Services called with correct parameters (locale, etc.)
  - Verify: Response parsed (transcription, confidence)
  - Action: Call VoiceResponseInterpreter.InterpretVoiceResponse with transcription
  - Verify: Intent mapped correctly ("yes" → "yes", "no" → "no", etc.)
  - Verify: SaveVoiceResponseAsync saves both VoiceRecognitionResult and CallResponse
  - Use test fixtures from T024 (voice event fixtures)
  
  **Reference**: spec.md test case TC-004, TC-006

- [ ] T023 [US2] [P] Write integration test for voice-specific prompt playback

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/VoicePromptTests.cs`:
  - Setup: voice-only campaign
  - Setup: call session initiated
  - Action: Call PlayVoiceQuestionAsync
  - Verify: Azure Communication Services called to play prompt
  - Verify: Prompt text is voice-specific ("Say yes or no", not "Press 1")
  - Verify: State set to awaiting-voice-input
  - Test different question types (yes/no, numeric, etc.)
  - Verify DTMF prompts NOT played (no "Press 1" message)
  
  **Reference**: spec.md requirement FR-014, test case TC-008

- [ ] T024 [US2] [P] Create fixture for voice test data and mock Azure Speech Services

  **Details**: Create test fixture in `tests/CallistraAgent.Functions.Tests/Fixtures/VoiceEventFixtures.cs`:
  - CloudEvents with voice RecognizeCompleted payloads
  - Mock Azure Speech Services responses (transcription, confidence)
  - Factory methods for common voice scenarios:
    - User says "yes" (confidence 0.92)
    - User says "no" (confidence 0.88)
    - User speaks unclear (confidence 0.60) → reprompt
    - User says digit "2" (confidence 0.95)
    - User speaks nothing (timeout → RecognizeFailed)
  - Audio stream mocks for testing
  - Mock AzureCommunicationServices client
  
  **File**: `tests/CallistraAgent.Functions.Tests/Fixtures/VoiceEventFixtures.cs`

- [ ] T025 [US2] [P] Write unit test for voice response saving with transcription

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Unit/Services/VoiceResponseSaveTests.cs`:
  - Test: SaveVoiceResponseAsync creates VoiceRecognitionResult record
  - Test: Transcription stored in VoiceRecognitionResult.TranscribedText
  - Test: Confidence score stored (decimal 0-1)
  - Test: Intent correctly derived from transcription
  - Test: CallResponse created with ResponseSource='voice'
  - Test: Both records saved in transaction (atomic)
  - Test: Low confidence warning logged
  - Mock database
  - Use FluentAssertions
  
  **Reference**: spec.md test case TC-006 (storing transcription and confidence)

- [ ] T026 [US2] [P] Write integration test for low confidence reprompt scenario

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/VoiceLowConfidenceTests.cs`:
  - Setup: voice-only campaign, call session
  - Action: Send RecognizeCompleted event with low confidence (0.60 < 0.75 threshold)
  - Verify: CallEventWebhookFunction identifies low confidence
  - Verify: QuestionService.PlayVoiceQuestionAsync called again (reprompt)
  - Verify: CallResponse NOT created (pending high confidence)
  - Verify: VoiceRecognitionResult still saved (for audit)
  - Verify: Error/warning logged (call ID, confidence score)
  - Test multiple reprompts in sequence
  
  **Reference**: spec.md requirement FR-006, test case TC-003

- [ ] T027 [US2] [P] Write unit test for RecognizeFailed error handling (no fallback)

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Unit/Services/RecognizeFailedTests.cs`:
  - Test: RecognizeFailed event (user spoke nothing, timeout)
  - Verify: System does NOT fall back to DTMF (single-mode campaigns only)
  - Verify: Error logged with call ID, reason (timeout)
  - Verify: Call fails gracefully (error response returned)
  - Verify: No CallResponse created
  - Verify: No VoiceRecognitionResult created (recognition failed)
  - Mock CallEventWebhookFunction error path
  
  **Reference**: spec.md requirement FR-009 (no fallback), test case TC-006 (recognize failed)

- [ ] T028 [US2] Create Voice-specific error types and exceptions

  **Details**: Create new exceptions in Models/ or Exceptions/:
  - SpeechRecognitionException (base)
  - SpeechServiceUnavailableException (Azure API down)
  - LowConfidenceException (confidence < 0.75, reprompt triggered)
  - MalformedVoiceEventException (missing audio data, invalid transcription)
  - VoiceIntentNotRecognizedException (transcription not mappable to intent)
  - XML documentation on each exception
  - Each includes call ID, timestamp, helpful error message
  
  **File**: `src/CallistraAgent.Functions/Models/VoiceExceptions.cs` or `src/CallistraAgent.Functions/Exceptions/`

- [ ] T029 [US2] Write integration test for malformed voice event handling

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/MalformedVoiceEventTests.cs`:
  - Test: RecognizeCompleted event missing transcription field
  - Test: RecognizeCompleted event with invalid confidence (not 0-1)
  - Test: RecognizeCompleted event with missing call connection ID
  - Verify: Each scenario returns 400 Bad Request
  - Verify: Error details logged (call ID, missing field)
  - Verify: No database records created
  - Verify: Error response includes RFC 7807 Problem Details
  
  **Reference**: spec.md requirement FR-008 (malformed webhook)

- [ ] T030 [US2] [P] Write comprehensive integration test for voice-only campaign full flow

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/VoiceOnlyCampaignFlowTests.cs`:
  - Setup: voice-only campaign (ResponseMode='voice-only')
  - Setup: call session created in that campaign
  - Action: Question 1 played (voice-specific prompt)
  - Action: User speaks "yes" with confidence 0.92
  - Verify: RecognizeCompleted event processed via CallEventWebhookFunction
  - Verify: VoiceRecognitionResult saved (transcription, confidence, intent)
  - Verify: CallResponse saved (ResponseSource='voice', ResponseValue='yes')
  - Verify: Advance to next question
  - Action: Question 2 played (voice-specific prompt)
  - Action: User speaks "no" with confidence 0.80
  - Verify: Second question/response follows same pattern
  - Complete ~3-question flow end-to-end
  
  **Reference**: spec.md test case TC-002, TC-006 (voice campaign full flow)

---

## Phase 5: User Story 3 - Campaign-Level Mode Configuration

**Priority**: P1 (Campaign-level control)  
**Purpose**: Designate and enforce campaign response mode at creation  
**Duration**: 6-8 hours  
**Independent Tests**: Yes (requires T004-T007 and US1 complete)  
**Success Criteria**: Campaigns created with ResponseMode persist; each call uses correct mode

**Why This Story After US2**: Integrates voice into campaign lifecycle; verifies mode-specific behavior

- [ ] T031 [US3] Write unit tests for campaign mode persistence (TDD: RED phase)

  **Details**: Create test file `tests/CallistraAgent.Functions.Tests/Unit/Services/CampaignModeTests.cs` with tests (write BEFORE implementation):
  - Test: Campaign created with responseMode='voice-only' → persisted correctly
  - Test: Campaign created with responseMode='dtmf-only' → persisted correctly
  - Test: Default responseMode='dtmf-only' if not specified
  - Test: ResponseMode cannot be updated after creation
  - Test: ResponseMode validation (only allows 'dtmf-only' or 'voice-only')
  - Test: Invalid mode rejected (e.g., 'both', 'auto', '')
  - Mock database
  - Use FluentAssertions
  
  **Reference**: spec.md requirement FR-009 (immutable mode), acceptance scenario 1, test case TC-007

- [ ] T032 [US3] [P] Extend Campaign model with ResponseMode property

  **Details**: Update Campaign model in Models/:
  - Add property: `public string ResponseMode { get; set; } = "dtmf-only";` (default DTMF)
  - Add validation attribute: `[RegularExpression(@"^(dtmf-only|voice-only)$", ErrorMessage="ResponseMode must be 'dtmf-only' or 'voice-only'")]`
  - Add validation attribute: `[Required]` (immutable after creation)
  - Add comment: "Immutable after campaign creation. Sets default for all calls in this campaign."
  - XML documentation
  
  **Regression Protection**: ⚠️ Do NOT modify existing Campaign properties. Add only ResponseMode property.  
  **File**: `src/CallistraAgent.Functions/Models/Campaign.cs` (if separate) or Models/

- [ ] T033 [US3] [P] Update EF Core Campaign entity mapping for ResponseMode

  **Details**: In CallistraAgentDbContext.OnModelCreating():
  - Map Campaign.ResponseMode to Campaigns table column (string, max 20)
  - Add check constraint: ResponseMode ∈ {dtmf-only, voice-only}
  - Configure as immutable (property)
  - Set default value in model: "dtmf-only"
  - Add index: Campaigns(ResponseMode) for queries filtering by mode
  
  **File**: `src/CallistraAgent.Functions/Data/CallistraAgentDbContext.cs`

- [ ] T034 [US3] [P] Extend CallSession to inherit ResponseMode from Campaign at initialization

  **Details**: Update CallService.InitiateCallAsync() method:
  - When creating new CallSession, load Campaign by ID
  - Copy Campaign.ResponseMode → CallSession.ResponseMode (immutable copy)
  - Validate: CallSession.ResponseMode set before saving
  - Logging: Log ResponseMode for each initiated call (call ID, mode)
  - Verify CallSession.ResponseMode used (not querying Campaign repeatedly)
  
  **Regression Protection**: ⚠️ Do NOT modify existing InitiateCallAsync signature. Add ResponseMode assignment only.  
  **File**: `src/CallistraAgent.Functions/Services/CallService.cs`

- [ ] T035 [US3] [P] Write unit test for ResponseMode routing in webhook handler

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Unit/Functions/WebhookRoutingTests.cs`:
  - Test: RecognizeCompleted event for DTMF-only campaign → routes to DTMF handler
  - Test: RecognizeCompleted event for voice-only campaign → routes to voice handler
  - Test: Routing decision based on CallSession.ResponseMode
  - Test: Wrong mode detection (voice event in DTMF campaign) → error logged, 400 returned
  - Mock CallSessionState to return different modes
  - Use FluentAssertions
  
  **Reference**: spec.md requirement FR-011, FR-012 (mode-based routing), acceptance scenario 2

- [ ] T036 [US3] [P] Write integration test for campaign mode enforcement at call time

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/CampaignModeEnforcementTests.cs`:
  - Setup: DTMF-only campaign (ResponseMode='dtmf-only')
  - Setup: Call initiated in DTMF campaign
  - Verify: CallSession.ResponseMode set to 'dtmf-only'
  - Action: Attempt to send voice RecognizeCompleted event
  - Verify: Webhook detects mode mismatch, returns error (400 or 422)
  - Repeat for voice-only campaign receiving DTMF event
  - Verify error message indicates mode mismatch
  
  **Reference**: spec.md requirement FR-011, FR-012 (state-driven behavior)

- [ ] T037 [US3] Implement CallEventWebhookFunction routing based on ResponseMode

  **Details**: Enhance CallEventWebhookFunction.Run() method:
  - Add logic: Check CallSession.ResponseMode from CallSessionState
  - If ResponseMode='dtmf-only': Route RecognizeCompleted → HandleRecognizeCompletedAsync (DTMF path)
  - If ResponseMode='voice-only': Route RecognizeCompleted → new voice handler method
  - If event type doesn't match mode (e.g., DTMF event in voice campaign): Log error, return 400
  - Logging: Log routing decision (call ID, mode, event type)
  - Error handling: wrap routing logic in try-catch
  
  **File**: `src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs`  
  **Test**: From T035-T036

- [ ] T038 [US3] [P] Create campaign mode API endpoints (GET/POST)

  **Details**: Create/extend API controller (or use HTTP-triggered function):
  - Endpoint: GET /api/campaigns/{campaignId}/response-mode
    - Query campaign by ID
    - Return: { responseMode: "voice-only" | "dtmf-only" }
    - Status 200 (success), 404 (not found)
  - Endpoint: POST /api/campaigns/{campaignId}/response-mode
    - Accept: { responseMode: string }
    - Validate: mode ∈ {dtmf-only, voice-only}
    - Business rule: Cannot change mode if campaign has active calls
    - Return: 200 (success), 400 (invalid), 409 (conflict - immutable), 404 (not found)
    - Logging: Log mode change attempts (campaign ID, old mode, new mode)
  - Authorization: Require API key
  - Use dependency injection for repositories
  
  **File**: `src/CallistraAgent.Functions/Functions/CampaignModeFunction.cs` (new HTTP-triggered function)  
  **Test**: API contract tests + integration tests

- [ ] T039 [US3] Update Campaign creation endpoint to include responseMode parameter

  **Details**: Extend campaign creation endpoint (if separate):
  - Accept HTTP request body: { name, program, responseMode }
  - Validate: responseMode ∈ {dtmf-only, voice-only}
  - Default: responseMode='dtmf-only' if not specified
  - Store: Set Campaign.ResponseMode before saving
  - Return: 201 Created with campaign details (including responseMode)
  - Logging: Log campaign created (campaign ID, name, responseMode)
  - Validation: Cannot be changed after creation
  
  **File**: Extend existing `/api/campaigns` POST endpoint or create new function
  **Test**: Integration test creating voice-only and DTMF-only campaigns

- [ ] T040 [US3] [P] Write integration test for campaign mode API endpoints

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/CampaignModeApiTests.cs`:
  - Test: GET /api/campaigns/{campaignId}/response-mode returns correct mode
  - Test: 404 if campaign not found
  - Test: POST /api/campaigns/{campaignId}/response-mode with valid mode succeeds
  - Test: POST with invalid mode returns 400
  - Test: POST cannot change mode if campaign has active calls (409 Conflict)
  - Test: Unauthorized requests rejected (401 if auth required)
  - Call endpoints via HttpClient (or function runner)
  - Verify response format matches OpenAPI contract
  
  **Reference**: contracts/campaign-config.yaml

- [ ] T041 [US3] [P] Write integration test for mode-specific prompts rendering

  **Details**: Create test in `tests/CallistraAgent.Functions.Tests/Integration/ModeSpecificPromptsTests.cs`:
  - Setup: DTMF-only campaign
  - Setup: Call initiated in DTMF campaign
  - Action: Question played
  - Verify: Prompt is DTMF prompt ("Press 1 for yes, 2 for no")
  - Setup: voice-only campaign
  - Setup: Call initiated in voice campaign
  - Action: Question played (via PlayVoiceQuestionAsync)
  - Verify: Prompt is voice prompt ("Say yes or no")
  - Verify: Different prompts for same question based on mode
  - Verify: Prompt text does NOT include both modalities ("Press" AND "Say")
  
  **Reference**: spec.md requirement FR-014 (mode-specific prompts), test case TC-008 (voice prompts tested)

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Integration, documentation, final validation, and deployment readiness  
**Duration**: 3-4 hours  
**Blocking**: Nothing (US1/US2/US3 can be tested in parallel)  
**Success Criteria**: All tests pass; 80%+ coverage; documentation complete

- [ ] T042 Run full test suite and validate 80%+ code coverage

  **Details**: Execute all tests created in previous phases:
  - Run: `dotnet test --filter "Category=Voice" --verbosity normal` (all voice tests)
  - Run: `dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80` (coverage check)
  - Verify: Coverage report shows:
    - CallService.cs ≥ 85%
    - VoiceResponseInterpreter.cs = 100%
    - CallSessionState.cs ≥ 85%
    - QuestionService.cs ≥ 80%
    - CallEventWebhookFunction.cs ≥ 80%
  - If coverage < 80%: Add missing tests (edge cases, error paths)
  - Generate HTML coverage report: `dotnet test /p:GenerateReport=true`
  
  **Deliverable**: Coverage report in `coverage-report.html`

- [ ] T043 [P] Format code and run linters (Roslynator, StyleCop)

  **Details**: Apply C# formatting and linting:
  - Run: `dotnet format --verify-no-changes` (check format compliance)
  - If needed: `dotnet format` (fix format violations)
  - Run: Roslynator code analysis (via CI or IDE)
  - Fix issues per plan.md coding standards:
    - Line length ≤ 100 characters
    - Naming: PascalCase classes, camelCase fields
    - No unused variables, parameters, imports
    - XML documentation on public APIs
    - Async methods have Async suffix
  - Commit: "Style: Apply formatting and linting rules"
  
  **File**: All modified C# source files

- [ ] T044 [P] Write API documentation for campaign mode endpoints

  **Details**: Create or update documentation:
  - Create `docs/API_VOICE_MODES.md` with:
    - Overview: Campaign response modes (DTMF-only vs voice-only)
    - Endpoint documentation: GET/POST /api/campaigns/{campaignId}/response-mode
    - Request/response examples (JSON)
    - Status codes explanation
    - Immutability note (mode cannot change after creation)
    - Authentication requirements
  - Verify OpenAPI contract matches documentation
  - Include examples: create voice-only campaign, query mode, attempt to update (409)
  
  **File**: `docs/API_VOICE_MODES.md`

- [ ] T045 [P] Write developer guide for voice feature usage

  **Details**: Create `docs/DEVELOPER_GUIDE_VOICE.md` with:
  - Overview: Adding voice recognition to campaigns
  - Creating a voice-only campaign: code example
  - Testing voice recognition locally: steps
  - Common scenarios: voice success, low confidence, no speech, STT error
  - Debugging voice issues: error codes, logs to check
  - Performance expectations: latency targets (< 2 seconds)
  - Troubleshooting: "Why is my voice response not recognized?"
  - Reference: links to spec.md, plan.md, data-model.md
  
  **File**: `docs/DEVELOPER_GUIDE_VOICE.md`

- [ ] T046 [P] Update project README with voice feature summary

  **Details**: Add section to `README.md`:
  - **New Features** section: "Voice Response Recognition (v2.0)"
  - Description: "Campaigns now support voice recognition via Azure Speech Services"
  - Key capabilities:
    - DTMF-only campaigns (unchanged, backward compatible)
    - Voice-only campaigns (new, requires Azure Speech Services)
    - Single-mode campaigns (immutable after creation)
  - Links to: quickstart.md, DEVELOPER_GUIDE_VOICE.md, API_VOICE_MODES.md
  - Building: `dotnet build`, running: `func start`, testing: `dotnet test`

- [ ] T047 Create integration test suite for regression prevention validation

  **Details**: Create comprehensive test in `tests/CallistraAgent.Functions.Tests/Integration/RegressionPreventionTests.cs`:
  - Verify existing DTMF-only behavior unchanged (Feature 001 tests pass)
  - Verify CallService.HandleRecognizeCompletedAsync unchanged (signature, inputs, outputs)
  - Verify CallResponse table: existing columns unchanged, only new ResponseSource added
  - Verify CallSession table: existing columns unchanged, only new ResponseMode added
  - Verify no modification to Member, Campaign (except ResponseMode property added)
  - Create fixtures for:
    - Old-style DTMF calls (pre-voice feature)
    - New voice-only calls
    - Mixed scenarios (multiple campaigns with different modes)
  - Assert: Backward compatibility maintained for all existing functionality
  
  **File**: `tests/CallistraAgent.Functions.Tests/Integration/RegressionPreventionTests.cs`

- [ ] T048 Create CHANGELOG entry and update version

  **Details**: Document feature in version control:
  - Update `CHANGELOG.md`:
    - **Version 2.0** (Feature 002):
      - ✨ NEW: Voice response recognition via Azure Speech Services
      - ✨ NEW: Single-mode campaigns (DTMF-only or voice-only)
      - ✨ NEW: Mode-specific prompt rendering
      - 🔧 CHORE: Extended CallSession, CallResponse, configuration
      - ⚙️ Database migration: 002_AddVoiceSupport.sql required
      - 🛡️ BREAKING: CallService.SaveVoiceResponseAsync new method (not breaking, extension-only)
  - Update project version in `.csproj`: `1.0.0` → `2.0.0` (or `1.1.0` if minor)
  - Document: All existing functionality backward compatible
  
  **File**: `CHANGELOG.md`, `src/CallistraAgent.Functions/CallistraAgent.Functions.csproj`

---

## Execution Strategy

### MVP Scope (Minimum Viable Product)
Implement **US1 only** for fastest delivery (3-4 days):
1. Phase 1: Setup (T001-T003) — 2 hours
2. Phase 2: Foundational (T004-T007) — 4 hours
3. Phase 3: US1 DTMF (T008-T013) — 3 hours
4. Phase 6 Polish (T042, T043, T048) — 2 hours

**Deliverable**: DTMF-only path verified working, zero regression, ready for voice work

### Full Feature (Production Ready)
Implement **US1 + US2 + US3** for complete voice support (8-10 days):
1. Phase 1-3: As above — 9 hours
2. Phase 4: US2 Voice (T014-T030) — 8 hours
3. Phase 5: US3 Mode Config (T031-T041) — 7 hours
4. Phase 6: Polish (T042-T048) — 3 hours

**Deliverable**: Full voice recognition feature with campaign-level control, 80%+ coverage, production-ready

### Parallel Opportunities (After Phase 2)

**Day 1-2 (Setup + Foundational)**:
- All team members: Phase 1-2 together

**Day 3 (US1 Implementation)**:
- Developer A: T008-T011 (DTMF tests + fixtures)
- Developer B: T012-T013 (webhook + reprompt)

**Day 4-5 (US2 Implementation - Longest)**:
- Developer A: T014-T017 (Azure Speech Services)
- Developer B: T018-T024 (repositories, fixtures, integration)
- Developer C: T025-T030 (advanced testing - could start after T014 complete)

**Day 6 (US3 Implementation)**:
- Developer A: T031-T034 (campaign model + persistence)
- Developer B: T035-T039 (API endpoints)
- Developer C: T040-T041 (integration tests)

**Day 7 (Polish)**:
- All: T042-T048 (coverage, documentation, release)

### Quality Gates (Enforce Before Merge)

✅ **Pre-PR Checklist**:
- [ ] All tests pass locally: `dotnet test`
- [ ] Coverage ≥ 80%: `dotnet test /p:CollectCoverage=true`
- [ ] Code formatted: `dotnet format --verify-no-changes`
- [ ] No hardcoded secrets: `git log -p | grep -i password`
- [ ] Existing tests still pass (regression check)
- [ ] PR description includes: reference to spec, test coverage %, regression notes

✅ **PR Review Checklist**:
- [ ] Code follows plan.md coding standards
- [ ] No modifications to existing methods (only extensions)
- [ ] All new public methods have XML documentation
- [ ] Tests are independent and can run in parallel
- [ ] Error handling consistent with existing patterns
- [ ] Logging includes call ID and timestamps
- [ ] No PII in logs or error messages

✅ **Merge Preconditions**:
- [ ] All PR review feedback addressed
- [ ] All tests pass in CI
- [ ] Coverage report attached
- [ ] At least 1 approval from team member

---

## Task Checklists by Story

### User Story 1: DTMF Recognition
- [ ] T008: Parsing tests (RED)
- [ ] T009: Verify existing HandleRecognizeCompletedAsync unchanged
- [ ] T010: Integration test (DTMF flow)
- [ ] T011: Test fixtures for DTMF
- [ ] T012: Webhook validation
- [ ] T013: Reprompt tests

**Deliverable**: DTMF feature verified, zero regression, ready for voice  
**Estimated Time**: 3-4 hours

### User Story 2: Voice Recognition
- [ ] T014: Azure Speech Services tests (RED)
- [ ] T015: VoiceResponseInterpreter service
- [ ] T016: CallService.InvokeAzureSpeechServicesAsync
- [ ] T017: CallService.SaveVoiceResponseAsync (new overload)
- [ ] T018-T019: Repository methods
- [ ] T020-T027: Confidence, error handling, integration tests
- [ ] T028: Voice exceptions
- [ ] T029-T030: Malformed events, full flow test

**Deliverable**: Voice recognition working, transcriptions saved, 75% confidence gating  
**Estimated Time**: 8-10 hours

### User Story 3: Mode Configuration
- [ ] T031: Campaign mode tests (RED)
- [ ] T032-T034: Campaign model + EF Core mapping
- [ ] T035-T036: Routing tests
- [ ] T037: Webhook routing implementation
- [ ] T038-T039: API endpoints
- [ ] T040-T041: Integration tests

**Deliverable**: Campaign modes persist, calls use correct mode, API endpoints working  
**Estimated Time**: 6-8 hours

---

## Success Metrics

- ✅ **Functional**: All 14 functional requirements implemented and testable
- ✅ **Quality**: 80%+ code coverage, all tests pass, zero regression on DTMF
- ✅ **Performance**: Voice recognition response < 2 seconds (p95); DTMF < 500ms
- ✅ **Security**: No hardcoded credentials, PII not logged, secrets in Key Vault
- ✅ **Documentation**: quickstart.md, API docs, developer guide, CHANGELOG updated
- ✅ **Backward Compatibility**: Feature 001 (DTMF-only campaigns) unchanged and fully supported
- ✅ **Deployment**: Migration script runs successfully; zero downtime; rollback available

---

## References

- **Spec**: [spec.md](spec.md) — Requirements (FR-001 through FR-015)
- **Plan**: [plan.md](plan.md) — Architecture, coding standards, regression prevention
- **Domain Review**: [domain-review.md](domain-review.md) — Reuse strategy, risk analysis
- **Data Model**: [data-model.md](data-model.md) — Entity schemas, migration script
- **API Contracts**: [contracts/](contracts/) — Campaign mode API, webhook callback schema
- **Quick Start**: [quickstart.md](quickstart.md) — Developer onboarding for implementation
