# Feature Specification: Minimal Viable Healthcare Call Agent

**Feature Branch**: `001-minimal-call-agent`  
**Created**: January 10, 2026  
**Status**: Draft  
**Input**: User description: "Implement minimal viable healthcare call agent with Azure Communication Services - simplified approach with 3 endpoints, 3 tables, and basic call flow"

## Clarifications

### Session 2026-01-10

- Q: No-Answer Timeout Duration → A: 30 seconds (standard IVR timeout)
- Q: DTMF Response Timeout Duration → A: 10 seconds (standard IVR response timeout)
- Q: Healthcare Question Content → A: Standard healthcare enrollment verification questions
- Q: Call Initiation Trigger Method → A: HTTP API endpoint
- Q: Call Session Status Values → A: Standard telephony status model (Initiated, Ringing, Connected, Completed, Disconnected, Failed, NoAnswer)
- Q: Voicemail Handling → A: Use person-detection prompt; if voicemail detected, leave generic callback message (no PHI)
- Q: What should the Azure SQL Database be named? → A: CallistraAgent
- Q: What are the exact 3 healthcare questions to be asked during calls? → A: "Press 1 to confirm your identity", "Press 1 if you are aware of your enrollment in [Program]", "Press 1 if you need assistance with your program"
- Q: What database platform should host the CallistraAgent database? → A: Azure SQL Server (production); SQL Server 2025 (local testing)
- Q: What data access technology should the application use? → A: Entity Framework Core 8+
- Q: What .NET runtime version should the Azure Functions use? → A: .NET 9

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Make Outbound Healthcare Call (Priority: P1)

Healthcare administrators need to initiate automated calls to enrolled program members to collect basic health information.

**Why this priority**: This is the core functionality - without the ability to place calls, the system has no value. This is the foundation upon which all other features depend.

**Independent Test**: Can be fully tested by providing a member phone number, triggering the call initiation, and verifying the phone rings. Delivers immediate value by demonstrating basic telephony integration works.

**Acceptance Scenarios**:

1. **Given** a member record exists with a valid phone number, **When** an HTTP API call is made to initiate a call for that member, **Then** the system initiates an outbound call through Azure Communication Services
2. **Given** a call is initiated successfully, **When** the call connects, **Then** the system creates a call session record with status "Connected"
3. **Given** a call fails to connect, **When** the timeout occurs, **Then** the system records the failure and updates call session status to "Failed"

---

### User Story 2 - Ask Healthcare Questions (Priority: P2)

Once a call is connected, the system needs to ask pre-defined healthcare questions to the member using automated voice.

**Why this priority**: Without questions, the call has no purpose. This transforms the system from a basic dialer into an interactive health assessment tool.

**Independent Test**: Can be tested by establishing a call and verifying that text-to-speech audio plays the first question. Delivers value by demonstrating interactive voice capability.

**Acceptance Scenarios**:

1. **Given** a call is successfully connected, **When** the call connected event is received, **Then** the system plays the first healthcare question using text-to-speech
2. **Given** a question has been played, **When** the member needs to respond, **Then** the system prompts for DTMF input (press 1 for yes, 2 for no)
3. **Given** multiple questions are configured, **When** one question is answered, **Then** the system automatically progresses to the next question

---

### User Story 3 - Capture Member Responses (Priority: P3)

The system must record member answers to questions for analysis and follow-up by healthcare coordinators.

**Why this priority**: Response capture enables data-driven healthcare decisions. Without this, the call provides information but cannot inform care coordination.

**Independent Test**: Can be tested by simulating DTMF input during a call and verifying responses are saved to the database with correct call session association.

**Acceptance Scenarios**:

1. **Given** a question has been played to the member, **When** the member presses 1 or 2 on their phone keypad, **Then** the system captures the DTMF tone as a response
2. **Given** a response is captured, **When** the DTMF event is processed, **Then** the system saves a call response record linking the member, question, and answer
3. **Given** all questions have been answered, **When** the final response is recorded, **Then** the system updates the call session status to "Completed"

---

### Edge Cases

- **No Answer**: When a member doesn't answer the call within 30 seconds, the system marks the call session as "NoAnswer" and terminates the call attempt
- **Voicemail Detection**: When a call connects, system plays person-detection prompt ("Press 1 if you can hear this message"). If no DTMF response within 5 seconds, system assumes voicemail, plays generic callback message with no PHI ("Hello, this is [Organization] calling about your healthcare enrollment. Please call us back at [phone number]. Thank you."), then hangs up and marks session as "VoicemailMessage"
- **Person Confirmed**: When DTMF response (pressing 1) is received to person-detection prompt, system proceeds with healthcare questions
- **Mid-Call Hangup**: When a member hangs up before completing all questions, the system updates call session status to "Disconnected" and saves any responses captured up to that point
- **Invalid DTMF Input**: When a member presses a key other than 1 or 2, the system re-prompts with the same question (maximum 2 retries before moving to next question)
- **Response Timeout**: When a member doesn't press any key within 10 seconds, the system re-prompts once, then moves to the next question if no response received after second 10-second timeout
- **Service Unavailable**: When Azure Communication Services is temporarily unavailable during call initiation, the system returns an error and does not create a call session record
- **Network Issues During Call**: When network connectivity issues occur during an active call, the system relies on Azure Communication Services reconnection logic and marks session as "Disconnected" if call drops

### Constitution Testing Requirements

Based on **Callistra-Agent Constitution v1.0.0**:

- **Code Quality** (Principle II): Target ≥80% code coverage for core call management logic. Initially, focus on happy path coverage, then expand to edge cases in subsequent iterations.
- **Testing Standards** (Principle III):
  - Public APIs (call initiation, event handling endpoints) MUST have integration tests covering:
    - Successful call flow contract
    - Error response handling (invalid phone numbers, failed connections)
    - Data flow validation (call session creation, response recording)
  - Critical user path (P1: Initiate call → Connect → Play question) requires end-to-end test that exercises the full Azure Functions workflow
  - Performance tests NOT required for MVP - call latency is primarily determined by Azure Communication Services SLA
  - Unit test framework: xUnit (already established in project)
  - Integration test approach: In-memory test server for Azure Functions HTTP triggers with mocked Azure Communication Services client
- **Performance Requirements** (Principle V): Not applicable for MVP - using Azure Communication Services default SLA
- **UX Consistency** (Principle IV): Not applicable - this is a backend API feature with no user interface

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST initiate outbound phone calls to member phone numbers using Azure Communication Services via HTTP API endpoint
- **FR-002**: System MUST create a call session record when a call is initiated, storing member reference, start time, and initial status
- **FR-003**: System MUST handle call connected events by triggering the first question in the healthcare assessment flow
- **FR-004**: System MUST play pre-defined questions using text-to-speech when a call connects
- **FR-005**: System MUST prompt members to provide yes/no answers using DTMF input (1=yes, 2=no)
- **FR-006**: System MUST capture DTMF input responses and save them as call response records
- **FR-007**: System MUST associate each response with the correct call session and question identifier
- **FR-008**: System MUST progress automatically to the next question after capturing a response
- **FR-009**: System MUST update call session status as the call progresses through standard telephony states (Initiated → Ringing → Connected → Completed/Disconnected/Failed/NoAnswer)
- **FR-010**: System MUST handle call disconnection events by updating call session status to "Disconnected"
- **FR-011**: System MUST persist all call sessions and responses to the database for healthcare coordinator review
- **FR-012**: System MUST support a minimum of 3 healthcare questions per call flow
- **FR-013**: System MUST use a configurable callback URL for Azure Communication Services webhook events
- **FR-014**: System MUST ask the following 3 questions in order: (1) "Press 1 to confirm your identity", (2) "Press 1 if you are aware of your enrollment in [Program]" (with [Program] replaced by member's program name), (3) "Press 1 if you need assistance with your program"
- **FR-015**: System MUST play person-detection prompt ("Press 1 if you can hear this message") when call connects and wait 5 seconds for DTMF response
- **FR-016**: System MUST play generic callback message containing no PHI if no DTMF response received to person-detection prompt, then mark call as "VoicemailMessage" and hang up
- **FR-017**: System MUST proceed with healthcare questions only after receiving DTMF confirmation (pressing 1) to person-detection prompt

### Key Entities

- **Member**: Represents a healthcare program enrollee; includes name, phone number, and program identifier. This entity already exists in the database.
- **CallSession**: Represents a single call attempt; includes member reference, call connection ID, status (Initiated/Ringing/Connected/Completed/Disconnected/Failed/NoAnswer/VoicemailMessage), start time, and end time.
- **CallResponse**: Represents a member's answer to a specific question; includes call session reference, question number, question text, and response value (1 for yes, 2 for no).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: System successfully places a call that rings a real phone number within 5 seconds of initiation request
- **SC-002**: When a call connects, the first question plays within 3 seconds of connection
- **SC-003**: System captures and records DTMF responses within 2 seconds of member key press
- **SC-004**: Complete 3-question call flow executes from initiation to completion in under 3 minutes for a cooperative participant
- **SC-005**: 100% of call sessions are persisted to database with accurate status tracking
- **SC-006**: 100% of member responses are correctly associated with their call session and question
- **SC-007**: System handles at least 5 concurrent calls without degradation (based on Azure Functions consumption plan limits)
- **SC-008**: Call failure rate due to system errors (not member unavailability) is less than 5%

## Assumptions

- Members have valid phone numbers capable of receiving calls
- Members understand English and can use phone keypads
- Azure Communication Services phone number is already provisioned
- Azure SQL Database is named `CallistraAgent`
- Database platform is Azure SQL Server (production) with SQL Server 2025 for local development/testing
- Data access layer uses Entity Framework Core 8+ for ORM and migrations
- Azure Functions runtime is .NET 9 (LTS)
- Healthcare questions are predefined and do not require dynamic customization per member
- DTMF input is sufficient for yes/no responses (no speech recognition required for MVP)
- Call sessions do not need real-time monitoring UI for MVP
- Standard Azure Functions timeout (5 minutes for consumption plan) is sufficient for call duration
- Database schema for Members table already exists
- Call session initiation is triggered via HTTP API endpoint (allows manual testing and future automation integration)
- Voicemail detection strategy: Play person-detection prompt first; no DTMF response within 5 seconds indicates voicemail or unresponsive member; leave generic callback message with no member names or program details
- Generic callback message content is configurable but must not contain PHI (member names, program specifics)

## Out of Scope

The following are explicitly NOT included in this minimal implementation:

- Speech-to-text transcription of open-ended responses
- Custom voice profiles or premium neural voices
- Multi-language support
- Real-time call monitoring dashboard
- Retry logic for failed calls
- Call recording and playback
- Integration with external healthcare systems (EMR/EHR)
- Member authentication or identity verification during calls
- Scheduled call campaigns or batch processing
- SMS fallback for unreachable members
- Compliance reporting or audit logs (beyond basic call session records)
- Load testing beyond 5 concurrent calls
- Advanced telephony features (call transfer, conferencing, hold music)

These features may be added in future iterations based on user feedback and demonstrated need.
