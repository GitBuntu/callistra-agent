# Feature Specification: Human Voice Response Recognition

**Feature Branch**: `002-voice-response`  
**Created**: 2026-02-14  
**Status**: Clarified & Ready for Planning  
**Input**: "Add human voice response recognition alongside DTMF with configurable toggle"

---

## Clarifications (Session 2026-02-14)

Applied before planning to simplify scope and eliminate ambiguities:

| Question | Answer | Impact |
|----------|--------|--------|
| Speech-to-text service? | **Azure Speech Services** (native, low-latency) | Specification set; dependency documented |
| Voice prompts? | **Mode-specific**: "Say yes or no" for voice; "Press 1 for yes" for DTMF | Improved UX; dual prompt implementation |
| Fallback on STT unavailable? | **No fallback**. Single-mode campaigns (DTMF-only OR voice-only, never both) | Simpler error handling; call fails gracefully |
| Edge cases? | **Not covered in MVP** | Scope reduction; single-mode campaigns eliminate most complexity |

**Result**: MVP scope simplified from dual-mode with fallback to single-mode campaigns. All requirements now testable without edge case handling.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - DTMF Recognition (Backwards Compatibility) (Priority: P1)

Existing DTMF keypad tone recognition continues to work unchanged when campaign is in DTMF-only mode.

**Why this priority**: MVP-critical. Existing agents rely on DTMF. Zero regression accepted.

**Independent Test**: DTMF-only campaign → User presses 1 on phone → System captures & advances → Testable standalone.

**Acceptance Scenarios**:

1. **Given** campaign is in DTMF-only mode, **When** user presses valid tone (1-3), **Then** system captures and proceeds
2. **Given** campaign is in DTMF-only mode, **When** user presses invalid tone (#), **Then** system rejects and reprompts

---

### User Story 2 - Human Voice Response Recognition (Priority: P1)

System recognizes spoken word/number responses when campaign is in voice-only mode. Uses Azure Speech Services to transcribe and interpret intent.

**Why this priority**: Core new feature. Enables voice-only campaigns.

**Independent Test**: Voice-only campaign → User speaks "yes" → System captures intent → Testable standalone.

**Acceptance Scenarios**:

1. **Given** campaign is in voice-only mode and system prompts "Say yes or no", **When** user speaks "yes", **Then** system interprets intent and advances
2. **Given** campaign is in voice-only mode, **When** Azure Speech Services returns confidence <75%, **Then** system prompts "Please repeat"

---

### User Story 3 - Campaign-Level Mode Configuration (Priority: P1)

Administrator designates campaign response mode (DTMF-only or voice-only) at campaign creation. Mode is immutable and applies to all calls in that campaign.

**Why this priority**: Enables operator control. Different campaigns have different requirements.

**Independent Test**: Create campaign with mode="voice-only" → Query campaign → Verify mode persists → New call uses voice capture → Testable standalone.

**Acceptance Scenarios**:

1. **Given** campaign creation request includes `responseMode: "voice-only"`, **When** campaign is created, **Then** mode is persisted
2. **Given** campaign is voice-only, **When** call is initiated, **Then** DTMF capture is disabled, Azure Speech Services invoked

---

## Test-Driven Plan *(mandatory - TDD Phase 1: Red)*

### Unit & Integration Tests

| Test ID | Category | Description | Linked Requirement(s) | Status |
|---------|----------|-------------|----------------------|--------|
| **TC-001** | Unit | DTMF tone parsing extracts correct digit from event payload | FR-001, FR-002 | TBD |
| **TC-002** | Unit | Invalid DTMF tones rejected (non-1/2/3) | FR-003 | TBD |
| **TC-003** | Unit | Speech-to-text confidence score evaluated (≥75% threshold) | FR-004 | TBD |
| **TC-004** | Unit | Voice transcription interpreted to intent (yes/no → response) | FR-005 | TBD |
| **TC-005** | Integration | CallEventWebhookFunction correctly processes DTMF event | FR-001, FR-012 | TBD |
| **TC-006** | Integration | CallEventWebhookFunction correctly processes voice event with Azure Speech Services | FR-006, FR-013 | TBD |
| **TC-007** | Integration | Campaign mode persists; call uses correct capture mode | FR-009 | TBD |
| **TC-008** | Integration | Voice-only campaign plays "Say yes or no" prompt (not "Press 1") | FR-014 | TBD |

### Acceptance Tests (BDD Format)

```gherkin
Scenario: DTMF-only campaign captures keypad tone
  Given campaign is created with responseMode="dtmf-only"
  And call is initiated in that campaign
  When user presses "1" on keypad
  Then system captures tone and advances to next question
  
Scenario: Voice-only campaign captures spoken response
  Given campaign is created with responseMode="voice-only"
  And call is initiated in that campaign
  When user speaks "yes" during question prompt
  Then Azure Speech Services transcribes and interprets intent
  And system advances to next question

Scenario: Mode-specific prompts
  Given voice-only campaign
  When questionnaire plays first question
  Then prompt includes "Say yes or no" (not "Press 1")
```

---

## Requirements *(mandatory)*

### Functional Requirements

#### Ubiquitous Requirements (Always Active)

- **FR-001** *(UBIQUITOUS)*: The system shall recognize DTMF keypad tones and extract digit from call event
- **FR-002** *(UBIQUITOUS)*: The system shall validate DTMF tones against allowed campaign values (typically 1-3)
- **FR-003** *(UBIQUITOUS)*: The system shall reject invalid DTMF tones and reprompt without advancing state
- **FR-015** *(UBIQUITOUS)*: The system shall use Azure Speech Services (en-US, confidence threshold 75%) for all voice recognition in voice-only campaigns

#### Event-Driven Requirements (Triggered)

- **FR-004** *(EVENT-DRIVEN)*: When voice-only campaign is active and user speaks during question, the system shall invoke Azure Speech Services to transcribe audio
- **FR-005** *(EVENT-DRIVEN)*: When Azure Speech Services returns transcription, the system shall interpret result to response intent (yes/no/digit)
- **FR-006** *(EVENT-DRIVEN)*: When speech-to-text confidence score is below 75%, the system shall prompt user to repeat response

#### Unwanted-Behavior Requirements (Error Handling)

- **FR-008** *(UNWANTED-BEHAVIOR)*: If webhook event is malformed, the system shall log error with call ID and timestamp
- **FR-009** *(UNWANTED-BEHAVIOR)*: If Azure Speech Services returns error or is unavailable, the system shall log error and fail call gracefully (no fallback to DTMF)
- **FR-010** *(UNWANTED-BEHAVIOR)*: If campaign mode configuration fails to load, the system shall log error and reject call initiation

#### State-Driven Requirements (Context-Dependent)

- **FR-011** *(STATE-DRIVEN)*: While call is in "question-awaiting-response" state and campaign is DTMF-only, the system shall capture DTMF tone (not invoke voice service)
- **FR-012** *(STATE-DRIVEN)*: While call is in "question-awaiting-response" state and campaign is voice-only, the system shall invoke Azure Speech Services to capture response

#### Optional Requirements (Feature-Dependent)

- **FR-013** *(OPTIONAL)*: Where campaign specifies English language, Azure Speech Services shall use en-US locale for prompts and recognition

#### New Voice-Specific Requirements

- **FR-014** *(EVENT-DRIVEN)*: When voice-only campaign plays question, the system shall render voice-specific prompt (e.g., "Say yes or no, zero to skip") instead of generic prompt

### Traceability Matrix

| Requirement | EARS Pattern | Linked Test Cases | Coverage Status |
|-------------|--------------|-------------------|-----------------|
| **FR-001** | UBIQUITOUS | TC-001, TC-005 | ✓ Covered |
| **FR-002** | UBIQUITOUS | TC-001 | ✓ Covered |
| **FR-003** | UBIQUITOUS | TC-002 | ✓ Covered |
| **FR-004** | EVENT-DRIVEN | TC-003, TC-006 | ✓ Covered |
| **FR-005** | EVENT-DRIVEN | TC-004 | ✓ Covered |
| **FR-006** | EVENT-DRIVEN | TC-003 | ✓ Covered |
| **FR-008** | UNWANTED-BEHAVIOR | TC-005, TC-006 | ✓ Covered |
| **FR-009** | UNWANTED-BEHAVIOR | TC-006 | ✓ Covered |
| **FR-010** | UNWANTED-BEHAVIOR | TC-007 | ✓ Covered |
| **FR-011** | STATE-DRIVEN | TC-005 | ✓ Covered |
| **FR-012** | STATE-DRIVEN | TC-006 | ✓ Covered |
| **FR-013** | OPTIONAL | TC-006 | ✓ Covered |
| **FR-014** | EVENT-DRIVEN | TC-008 | ✓ Covered |
| **FR-015** | UBIQUITOUS | TC-003, TC-006 | ✓ Covered |

### Key Entities

- **Campaign**: Extended with `responseMode` field (dtmf-only or voice-only, immutable)
- **CallSession**: Extended with `responseMode` inherited from campaign
- **CallResponse**: Extended with `responseSource` field (dtmf or voice)
- **VoiceRecognitionResult**: New value object with transcription, confidence_score, timestamp

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: DTMF recognition success rate remains ≥99% (zero regression)
- **SC-002**: Voice recognition success rate (confidence ≥75%) ≥85% for healthcare questions
- **SC-003**: Response capture time ≤2 seconds for both DTMF and voice modes
- **SC-004**: 100% of test cases (TC-001 through TC-008) pass before production deployment
- **SC-005**: Azure Speech Services integration verified with mock and live endpoints
- **SC-006**: Voice prompts tested for clarity and unambiguity with sample users
- **SC-007**: No DTMF-only campaigns regress; mode-specific prompts render correctly

---

## Out of Scope (MVP)

- Dual-mode campaigns (voice + DTMF simultaneously)
- Service fallback behavior (campaigns configured for single mode upfront)
- Complex retry logic across failure types
- Speech-to-text service unavailability recovery (call fails gracefully)
- Language support beyond English (FR-013 deferred)
- Real-time mode switching (campaigns set at creation, immutable)
