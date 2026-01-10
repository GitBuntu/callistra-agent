<!-- 
SYNC IMPACT REPORT v1.0.0
=========================
Version Change: N/A (Initial)
New Constitution Created: 2026-01-10
Principles Added: 5 (Pragmatism, Code Quality, Testing Standards, UX Consistency, User & Performance Requirements)
New Sections: User & Performance Requirements, Development Workflow & Quality Gates
Templates Updated: 
  - ✅ plan-template.md (Constitution Check section)
  - ✅ spec-template.md (testing requirements alignment)
  - ✅ tasks-template.md (task categorization by principles)
No Deferred Items
-->

# Callistra-Agent Constitution

## Core Principles

### I. Pragmatism

Prioritize working solutions over perfection. Make data-driven decisions. Accept technical debt only when explicitly documented and tracked. Ship first, optimize later when metrics justify it.

**Non-negotiable rules:**
- Every technical debt decision MUST be documented with rationale and tracked (e.g., in a debt registry or issue tracker)
- Ship incremental value early; gather user feedback to drive optimization priorities
- All optimization efforts MUST be justified by quantified metrics (response time, error rate, resource usage)
- Complex trade-offs require brief architectural decision records (ADRs) with trade-off analysis

**Rationale**: Healthcare outreach requires iterative refinement based on real user behavior. Perfect solutions delay time-to-value. Documented debt ensures we remain aware of compromises and address them systematically.

### II. Code Quality

Enforce consistent code style and patterns across codebase. Maintain minimum 80% test coverage. Require code reviews before all merges. Document complex logic and architectural decisions inline. Follow DRY principle and remove duplication.

**Non-negotiable rules:**
- All changes MUST pass code review before merge (human review required, no auto-merge)
- Test coverage MUST NOT fall below 80% for any module; new code MUST include corresponding tests
- Code style MUST conform to project linter rules; auto-format checks run on CI
- Complex business logic (domain models, orchestration) MUST include inline documentation explaining intent
- Duplicated code blocks >5 lines MUST be refactored into shared utilities or base classes

**Rationale**: Healthcare data is sensitive; maintainable code reduces bugs and security risks. Reviews catch issues early. High test coverage provides confidence in refactoring and change safety.

### III. Testing Standards

Write tests first (TDD where applicable). All public APIs must have comprehensive integration tests. Critical user paths require end-to-end tests. Performance tests required for operations exceeding 100ms. Maintain test suite execution under 5 minutes.

**Non-negotiable rules:**
- All public API endpoints MUST have integration tests that verify contract, error handling, and data flow
- Critical user paths (e.g., member enrollment, call initiation, response capture) MUST have end-to-end tests
- Operations with latency >100ms (e.g., external service calls, database queries, Azure Speech) MUST have performance tests
- Test suite MUST run in <5 minutes total; slow tests MUST be parallelized or moved to separate CI stage
- TDD encouraged for complex domain logic; test-first development ensures specs are testable before implementation

**Rationale**: Azure integration and healthcare workflows are complex and error-prone. Tests provide safety net for continuous deployment. Sub-5-minute suite supports rapid iteration.

### IV. UX Consistency

Maintain strict design system compliance. Consistent error handling and user-facing messaging. Standardized loading, empty, and error states across all features. Regular accessibility audits (WCAG 2.1 AA minimum). Validate all changes with user feedback before release.

**Non-negotiable rules:**
- All user-facing text (errors, confirmations, prompts) MUST follow centralized message templates; no ad-hoc messaging
- Loading, empty, and error states MUST be styled consistently across all interfaces (web, API responses, voice prompts)
- Accessibility audits MUST be conducted quarterly; all new features MUST meet WCAG 2.1 AA minimum
- Releases MUST include user validation (internal testing, beta feedback, or explicit sign-off)
- Error messages MUST be actionable and user-friendly; never expose technical stack traces in user-facing UI

**Rationale**: Healthcare users expect consistent, trustworthy interfaces. Accessibility is both ethical and legal requirement. Validation prevents surprises in production.

### V. User and Performance Requirements

Define clear SLAs for all services. Client-side operations must complete within 100ms. API responses at p95 latency under 500ms. Support minimum 1000 concurrent users. Track user metrics, engagement, and performance data. Optimize based on real user monitoring data.

**Non-negotiable rules:**
- All public API endpoints MUST document target SLA (p95 latency) and error rate targets; production telemetry MUST track these metrics
- Client-side operations (UI interactions, local computations) MUST complete within 100ms; operations exceeding this MUST show loading state
- API responses (p95) MUST remain under 500ms; violations trigger incident investigation
- Infrastructure MUST support minimum 1000 concurrent users; load testing required before each major release
- User metrics (engagement, call completion, response quality) and performance data (latency, error rates) MUST be collected and reviewed weekly
- Optimization decisions MUST be driven by monitored data; no optimizations without evidence of impact

**Rationale**: Healthcare outreach depends on responsiveness and reliability. SLAs enforce accountability. Real user monitoring ensures we optimize the right things.

## User & Performance Requirements

### Service Level Agreements (SLAs)

- **Member API** (CRUD, listing): p95 latency <250ms, error rate <0.5%
- **Call Session Management** (initiate, complete, query): p95 latency <400ms, error rate <0.1%
- **Speech Recognition** (Azure Speech integration): p95 latency <2s (Azure limit), retry on timeout
- **System Health**: 99.5% uptime target; downtime >30 min requires incident review

### Performance Targets

| Operation | Target | Mechanism |
|-----------|--------|-----------|
| Member database query | <50ms (p95) | Connection pooling, query optimization, indexes |
| Call session creation | <200ms (p95) | Async processing, message queues where applicable |
| UI interaction response | <100ms | Optimistic updates, debouncing, pagination |
| Third-party API fallback | <5s timeout | Circuit breaker, graceful degradation |
| Report generation | <10s for 1000 records | Async, pagination, caching |

### Scalability & Capacity

- Minimum concurrent user support: 1000
- Member database: support 100k+ members
- Call session history: support 1M+ sessions
- Load test before release: verify 1000 concurrent users
- Monitor auto-scaling metrics; alert on sustained >80% resource utilization

### Monitoring & Observability

- **Application telemetry**: Request latency, error rates, Azure Speech failures, database query times
- **User metrics**: Daily active users, call completion rate, member engagement, average response rate
- **Infrastructure**: CPU, memory, database connection pool, Azure service quota usage
- **Review cadence**: Weekly performance dashboard, monthly trend analysis
- **Alert thresholds**:
  - API p95 latency >500ms → investigate and optimize
  - Error rate >1% → page on-call
  - Member database query >100ms → investigate indexes/queries
  - Speech recognition failure rate >5% → fallback to alternative handling

## Development Workflow & Quality Gates

### Code Review Process

1. All changes MUST be submitted via pull request (no direct pushes to main/develop)
2. Code review MUST complete before merge; minimum 1 approval required
3. Reviewer MUST verify:
   - Test coverage maintained (≥80%)
   - Code style conformance (linter passes)
   - API contracts documented (if applicable)
   - No console.log/print statements left (use structured logging only)
   - Performance impact assessed (if touching latency-sensitive code)
   - Accessibility requirements met (if UI/messaging changes)

### CI/CD Gates

- **Unit tests**: MUST pass (target <2 minutes)
- **Integration tests**: MUST pass (target <3 minutes)
- **Linting**: MUST pass (auto-format applied)
- **Coverage check**: MUST remain ≥80% per module
- **API contract validation**: MUST match documented SLA targets in perf tests
- **Security scan**: MUST pass (no high/critical vulnerabilities)

### Branching & Release Strategy

- **Feature branches**: Prefix with feature number (e.g., `001-member-enrollment`)
- **Hotfix branches**: Prefix with `hotfix/` (e.g., `hotfix/speech-timeout`)
- **Release branches**: `release/vX.Y.Z` created after final QA, tagged after production deployment
- **Version bumping**: Follow semantic versioning; major for breaking API changes, minor for features, patch for fixes

## Governance

### Constitution Authority

This Constitution supersedes all informal practices and guides development decisions. When practices conflict, Constitution wins.

### Amendment Procedure

1. Propose change with rationale in a comment or issue (reference principle affected)
2. Document proposed change in a PR; include:
   - Principle being modified and why
   - Migration plan (how existing code adapts)
   - Version bump justification (MAJOR/MINOR/PATCH)
3. Team review & consensus (approval from tech lead + 1 contributor)
4. Update this file with new version and amendment date
5. Communicate change to team; plan grace period if applicable (e.g., "old pattern deprecated after 2 weeks")

### Compliance Review

- **Frequency**: Quarterly (January, April, July, October)
- **Scope**: Sample 10-15 recent PRs; verify:
  - All changes have test coverage and code review
  - SLA compliance (check production metrics)
  - Principle adherence (no shortcuts taken)
- **Outcome**: Issue summary of gaps; file tracking issues for remediation

### Reference Documents

- **Development Guidance**: See `.specify/memory/` for agent-specific runtime guidance
- **Task Template**: `.specify/templates/tasks-template.md` categorizes tasks by principle-driven type
- **Plan Template**: `.specify/templates/plan-template.md` includes Constitution Check gate
- **Spec Template**: `.specify/templates/spec-template.md` ensures testing requirements align with principle III

**Version**: 1.0.0 | **Ratified**: 2026-01-10 | **Last Amended**: 2026-01-10
