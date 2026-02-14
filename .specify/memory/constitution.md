<!-- SYNC IMPACT REPORT
Version Change: 0.0.0 → 1.0.0 (Initial constitution established)
New Principles: All 5 core principles newly defined
Sections Added: Healthcare & Security Requirements, Development Workflow
Templates Updated: Requires spec-template.md alignment (✓ COMPLETED via TDD+EARS integration)
Follow-up: None deferred
-->

# CallistraAgent Constitution

## Core Principles

### I. Minimal Viable Scope (Non-Negotiable)
MVP-first approach is fundamental to CallistraAgent's delivery model. Every feature proposal must:
- Start with the smallest independently-testable slice
- Declare what is explicitly OUT OF SCOPE (see: 001-minimal-call-agent/spec.md for model)
- Ship incrementally; defer complexity to post-MVP phases
- Apply YAGNI principle strictly - no speculative features, no organizational-only work
- Scope creep violates constitution; requires formal amendment to expand principle boundaries

**Rationale**: Healthcare automation requires rapid iteration. Minimal scope enables daily delivery cycles, early user feedback, and faster adaptation to regulatory/operational changes.

### II. Test-Driven Development (Non-Negotiable)
TDD is mandatory across all code. Red-Green-Refactor cycle strictly enforced:
1. **RED**: Write test case(s) first; tests MUST fail initially (proof no implementation exists)
2. **GREEN**: Write minimal code to make test pass; no over-engineering
3. **REFACTOR**: Improve code health while keeping tests green; extract helpers, remove duplication
4. Integration tests focus: Inter-Azure-service communication, CloudEvents webhook processing, call flow orchestration
5. Unit test focus: Business logic isolation (services, state machines, validation), database repository patterns
6. All PRs must include test cases; code review gates on coverage metrics

**Rationale**: Healthcare data integrity and HIPAA compliance require high confidence. TDD provides executable specifications and regression protection. Call automation systems are stateful; tests catch edge cases early.

### III. Event-Driven Architecture (Async-First)
System operates on asynchronous CloudEvents webhooks from Azure Communication Services; no polling:
- Call lifecycle managed via CallConnected → RecognizeCompleted/Failed → CallDisconnected events
- Azure Functions triggered by webhook events, never by scheduled polling
- Each Function invocation is independent; state persisted in Azure SQL Database
- Long-running operations decomposed into multiple events; no blocking waits in Functions
- Error handling: Retry logic with exponential backoff (CloudEvents Framework); dead-letter queues for unhandled events

**Rationale**: Real-time call detection requires event-driven model. Polling would cause missed events and scalability bottlenecks. Stateless Functions allow horizontal scaling.

### IV. Healthcare Data Integrity & Privacy (Non-Negotiable)
Every feature must respect healthcare regulations and member privacy:
- Person detection MUST occur before any PHI (Protected Health Information) is played or captured
- No member phone numbers, names, or PII in application logs or errors
- Call responses and member data encrypted at rest in SQL Database
- Voicemail fallback: Leave generic callback message (no program name, no healthcare details)
- HIPAA audit trail: Track call events with timestamps, outcomes, and error codes only
- No recording of calls or voice capture beyond DTMF responses

**Rationale**: HIPAA compliance is non-negotiable. Data breaches have legal, financial, and reputational consequences. Privacy-by-design prevents incidents.

### V. Azure-Native Cloud Patterns & Cost Optimization
Leverage Azure infrastructure efficiently and pragmatically:
- Consumption-based services preferred (Azure Functions, Communication Services); minimize fixed infrastructure costs
- Use managed services over custom-built solutions (Azure Cognitive Services for TTS, ACS for telephony)
- Azure SQL Database with elastic scaling for variable call volume
- Connection strings and credentials via Azure Key Vault only; no hardcoded secrets
- Monitor and log via Application Insights; set cost alerts; monthly cost reviews during standup

**Rationale**: Healthcare organizations operate on constrained budgets. Azure native patterns reduce operational overhead and enable rapid scaling. Cost awareness prevents runaway bills as call volume grows.

## Healthcare & Security Requirements

- All member communications must be encrypted in transit (TLS 1.2+) and at rest (SQL Transparent Data Encryption)
- Call initiation APIs require authentication; API key rotation quarterly
- Database access via Entity Framework Core parameterized queries (prevent SQL injection)
- Secrets managed in Azure Key Vault; never committed to source control
- Voicemail detection algorithm must follow Azure Communication Services documented pattern (AMD via Recognize API with DTMF)
- Member consent for outbound calls assumed; future consent UI out of MVP scope but architecture must support audit trail

## Development Workflow & Quality Gates

- Feature branch naming: `###-feature-description` (e.g., `001-minimal-call-agent`)
- PRs require:
  - All tests passing (xUnit with FluentAssertions)
  - Minimum code coverage: 80% for business logic
  - Specification updated (per TDD+EARS template integration)
  - Two approvals before merge (pair coding / reviews)
  - CHANGELOG entry documenting user-facing changes
- Releases tagged as `v{MAJOR}.{MINOR}.{PATCH}` (Semantic Versioning)
- Breaking changes require MAJOR version bump and migration documentation
- Deployment gated: Staging → Production sign-off from healthcare coordinator

## Governance

This constitution supersedes all conflicting practices and project conventions. Changes to core principles require:
- Written amendment proposal (GitHub issue or PR discussion)
- Rationale explaining business/technical justification
- Migration plan for existing code/tests
- Unanimous team approval before ratification

All PRs must reference which principle(s) they implement or reinforce. Guidance for daily development lives in [README.md](../../README.md) and feature specifications (per TDD+EARS template).

**Version**: 1.0.0 | **Ratified**: 2026-02-14 | **Last Amended**: 2026-02-14
