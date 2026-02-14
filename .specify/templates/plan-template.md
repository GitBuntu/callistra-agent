# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., C# 12 / .NET 9, SQL Server 2022, React 18 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., Entity Framework Core, Azure SDK, React Router, Axios or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., xUnit, FluentAssertions, Jest, React Testing Library or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]
**Project Type**: [single/web/mobile - determines source structure]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, 10k lines/sec, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

## Coding Standards & Best Practices

**Enforced across this feature** to ensure quality, maintainability, and team consistency.

### Architecture & Design

- ✅ **SOLID Principles**: Code against interfaces; dependency injection for loosely coupled components
- ✅ **Separation of Concerns**: Clear layers (Models, Services, Repositories, API endpoints)
- ✅ **DRY (Don't Repeat Yourself)**: Extract duplicated logic into reusable methods
- ✅ **Interface-Driven Design**: Define contracts before implementation; enables mocking & testing
- ✅ **Design Patterns**: Leverage Repository pattern for data access, Factory for object creation
- ✅ **No Magic Numbers**: Use named constants for all non-obvious values

### Testing & Quality

- ✅ **Test-Driven Development (TDD)**: Write tests FIRST, then code; verify tests fail before implementation
- ✅ **Target Coverage**: 80%+ code coverage across unit + integration tests
- ✅ **Unit Tests**: All domain logic (validation, state transitions, business rules)
- ✅ **Integration Tests**: Component interactions, database operations, service boundaries
- ✅ **Test Naming**: Descriptive names reflecting behavior: `ShouldThrowExceptionWhenInputIsNull`
- ✅ **Arrange-Act-Assert**: Organize tests with setup, action, verification phases
- ✅ **Independent Tests**: No test interdependencies; each test runs in isolation
- ✅ **Mock External Dependencies**: Use mocks for databases, APIs, external services

### Code Style & Readability

**C# Standards**:
- ✅ **Naming**: `PascalCase` for classes/methods, `camelCase` for fields/variables, `SCREAMING_SNAKE_CASE` for constants
- ✅ **Formatting**: Follow .editorconfig rules; use Roslynator for automated linting
- ✅ **Max Line Length**: 100 characters (hard limit)
- ✅ **Function Size**: Keep methods <30 lines; single responsibility
- ✅ **XML Documentation**: Public classes/methods require XML doc comments for IDE intellisense
- ✅ **Null Handling**: Use nullable reference types (`string?`); avoid bare null references
- ✅ **Async/Await**: Use async methods for I/O operations; never block on async calls

**React Standards**:
- ✅ **Component Naming**: Files match component name: `UserProfile.tsx` for `function UserProfile()`
- ✅ **Functional Components**: Use functional components + hooks; avoid class components
- ✅ **Custom Hooks**: Extract logic into custom hooks for reuse (e.g., `useAuth`, `useFetch`)
- ✅ **Props Validation**: Enforce TypeScript types; avoid generic `any` type
- ✅ **Imports Organization**: Sort imports (React, dependencies, local); use absolute paths
- ✅ **Naming**: `camelCase` for functions/variables, `PascalCase` for components
- ✅ **Constants**: Define magic strings/numbers as named constants at module top
- ✅ **Max File Size**: Keep components <300 lines; extract subcomponents if larger

**Both Platforms**:
- ✅ **Comments**: Explain "why", not "what" (code shows what); complex logic warrants comments
- ✅ **Meaningful Names**: Self-documenting code; avoid single-letter variables (except loop counters)
- ✅ **Formatting Applied**: Use linters (C#: Roslynator, React: ESLint + Prettier) on every commit

### Error Handling

- ✅ **Fail Fast**: Validate inputs at entry points; throw specific exceptions early
- ✅ **Specific Exceptions**: Throw domain-specific exceptions (not generic `Exception`)
- ✅ **Graceful Degradation**: Catch known errors; don't crash the system
- ✅ **Logging with Context**: Include operation ID, user ID, parameters in error logs
- ✅ **User-Friendly Messages**: Public error messages don't expose internals; technical details go to logs

### Security

- ✅ **Input Validation**: Validate all external input (API requests, form submissions)
- ✅ **SQL Injection Prevention**: Use parameterized queries or ORM (Entity Framework); never string concatenate SQL
- ✅ **Authentication/Authorization**: Enforce access control at API/component level
- ✅ **Secrets Management**: Store credentials in environment variables or vaults; never hardcode
- ✅ **HTTPS/TLS**: All network communication encrypted in transit
- ✅ **Data Encryption**: Encrypt sensitive data at rest (passwords, PII)
- ✅ **Audit Logging**: Log security-relevant events (login, permission changes, data access)

### Documentation

- ✅ **Code Comments**: Document complex logic, assumptions, non-obvious edge cases
- ✅ **API Contracts**: Use OpenAPI/Swagger specs; document endpoints, parameters, response codes
- ✅ **Architecture Decisions**: Document "why" structural or technology choices were made
- ✅ **README**: Setup instructions, dependencies, running/testing procedures
- ✅ **Public API Docs**: XML comments (C#) or JSDoc (React) on all public functions/components
- ✅ **Changelog**: Track breaking changes and new features

### Data Management

- ✅ **Immutability Preference**: Use immutable structures where practical (e.g., `readonly` collections in C#)
- ✅ **Null Handling**: Explicit handling of nulls; avoid nullable return types where possible
- ✅ **Validation**: Enforce constraints (type, range, format) at model boundaries
- ✅ **Foreign Keys**: Maintain referential integrity; define relationships explicitly
- ✅ **Migrations**: Version database schema; track changes in version control
- ✅ **Audit Trail**: Log who changed what and when (for compliance/debugging)

### Version Control & Collaboration

- ✅ **Commit Messages**: Clear, atomic commits: "Add: user authentication " vs. "fix"
- ✅ **Feature Branches**: Develop features on separate branches; keep main/develop clean
- ✅ **Pull Request Reviews**: At least one other developer reviews before merge
- ✅ **Small PRs**: Easier to review and faster feedback; aim for <400 LOC per PR
- ✅ **PR Description**: Explain what changed and why; link to relevant tickets/issues
- ✅ **No Force Push**: Preserve history for traceability and debugging

**Enforcement**: These standards are verified during code review (PR). Non-compliant code must be addressed before merge.

## Regression Prevention Strategy

Protect existing functionality by distinguishing safe reuse from risky modification:

### Safe Reuse (Extension Only) ✅

- ✅ **Call existing methods unchanged**: Use existing method with unchanged signature
- ✅ **Add new methods alongside**: Create new method; leave existing methods untouched
- ✅ **Extend tables with new columns**: Add new column; don't modify existing columns
- ✅ **Create new classes composing existing**: New class using existing services/components
- ✅ **Leverage patterns without modification**: Use existing error handling patterns; don't refactor infrastructure

**Impact**: Zero regression risk; existing tests remain valid; production callers unaffected.

### Risky Modification (Requires Justification) ❌

- ❌ **Change method signatures**: Altering existing method parameters or return types
- ❌ **Modify existing logic**: Adding conditionals or changing behavior in existing methods
- ❌ **Rename fields**: Changing existing field/column names
- ❌ **Reinterpret column meaning**: Using existing columns for new purposes
- ❌ **Alter exception behavior**: Changing when/how existing error handling throws

**If unavoidable**: Document justification, update all existing callers, add regression tests.

### Enforcement Rules

- **Default**: Extend, don't modify. Add new, don't change old.
- **Threshold**: Modifications affecting >1 existing caller require code review + team approval
- **Testing**: Any modification increases test coverage requirement to 95%+ (vs. 80% baseline)
- **Monitoring**: Production monitoring alerts on risky code paths after deployment

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
# [REMOVE IF UNUSED] Option 1: Single project (DEFAULT)
src/
├── models/
├── services/
├── cli/
└── lib/

tests/
├── contract/
├── integration/
└── unit/

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
