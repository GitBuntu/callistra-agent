````chatagent
---
description: Challenge feature requirements against domain knowledge and existing codebase to improve necessity, reduce scope creep, and maximize reuse.
handoffs: 
  - label: Build Technical Plan
    agent: speckit.plan
    prompt: Create a plan for the spec. I am building with...
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

## Outline

Goal: Apply critical domain knowledge and codebase analysis to question, refine, and optimize feature requirements before technical planning. Identify unnecessary requirements, overlaps with existing features, and opportunities to extend existing entities instead of creating new ones.

**Prerequisite**: `/speckit.specify` and optionally `/speckit.clarify` must be complete. This command runs BEFORE `/speckit.plan`.

## Execution Steps

### 1. Initialize Context

Run `.specify/scripts/bash/check-prerequisites.sh --json --paths-only` from repo root **once**. Parse JSON for:
- `FEATURE_DIR`
- `FEATURE_SPEC` (spec.md path)
- `IMPL_PLAN` (plan.md path, if it exists)

For single quotes in args like "I'm Groot", use escape syntax: e.g 'I'\''m Groot' (or double-quote if possible: "I'm Groot").

### 2. Load & Analyze Feature Spec

Read `FEATURE_SPEC` completely. Extract:
- **User Stories**: List with acceptance criteria
- **Functional Requirements**: Extract each discrete requirement (FR-XXX from EARS format)
- **Non-Functional Requirements**: Performance, security, scale
- **Key Entities**: Data models mentioned or implied
- **Out-of-Scope**: Explicitly excluded items
- **Assumptions**: Any stated assumptions

### 3. Scan Codebase for Reuse Opportunities

**Search for existing implementations** across the codebase:
- **Database Schema**: Query existing tables, columns, relationships
  - Check for similar entities (Members, Users, Accounts, etc.)
  - Check for state/status columns that might cover new requirements
  - Review foreign key relationships and data cardinality
  
- **Services & Components**: Scan for existing service classes
  - Business logic services, validation, audit services
  - Configuration management patterns
  - Error handling & logging infrastructure
  - Existing domain services

- **Models & DTOs**: Check for existing data shapes
  - Are required entities already represented?
  - Can new fields extend existing models instead of creating new tables?

- **React Components** (if UI-heavy):
  - Form components, validation utilities
  - State management patterns (Redux slices, Context providers, hooks)
  - API client utilities
  - UI component library usage

### 4. Critical Domain Challenge Questions

For EACH functional requirement, ask and document answers:

**Necessity**:
- Why does this requirement exist? What user problem does it solve?
- Is this a MUST-have or NICE-to-have for MVP?
- What would break if we removed this requirement?
- Does it address a real gap or is it nice-to-have scope creep?

**Reuse**:
- Does an existing service/component already handle this?
- Can we extend an existing database table (add column) instead of creating new table?
- Are there existing DTOs/models we can leverage?
- Could this be handled by configuring existing features differently?

**Domain Fit**:
- Does this align with the core domain and user stories?
- Is this a cross-cutting concern better handled as infrastructure (logging, monitoring, error handling)?
- Should this be a separate feature (out-of-scope for this sprint)?

**Optimization**:
- Can we achieve the user goal with fewer requirements?
- Are there redundant requirements that could merge?
- What's the minimal viable requirement set for MVP?

### 5. Regression Prevention Analysis

**CRITICAL GATE**: Classify all reuse decisions by modification risk. This prevents regression by enforcing strict change boundaries.

For each requirement that reuses existing code:

**SAFE (No Modification Risk)** ✅
- Calling existing method with unchanged signature
- Adding new method alongside existing ones
- Adding new column to existing table (backward-compatible)
- Creating new class that extends/composes existing behavior
- Using existing validation/error handling patterns without change

Example safe reuse:
- "Call existing `[ExistingMethod]()` with unchanged signature"
- "Add new method `[NewMethod]()` to [ExistingService]"
- "Extend [ExistingTable] table with `[newColumn]` column (new column only)"

**RISKY (Requires Modification)** ❌
- Changing method signature (new parameter, return type change)
- Modifying existing logic (adding conditional, changing behavior)
- Renaming existing method/field
- Reinterpreting existing column meaning
- Changing existing table column type/constraints
- Altering existing error handling paths

Example risky modification:
- ❌ "Add `[newParameter]` parameter to `[ExistingMethod](existing_params)` signature"
- ❌ "Modify `[ExistingMethod]()` to skip [behavior] if [condition]"
- ❌ "Change [ExistingTable].[column] meaning to include [new meaning]"

### Regression Prevention Matrix

Add to critique matrix:

| Requirement | Reuse Type | Modification Scope | Risk Level | Justification Required? |
|---|---|---|---|---|
| FR-001 | Extend [Service] | Add new method | ✅ SAFE | No—new method, existing unaffected |
| FR-002 | Extend [Table] | Add column | ✅ SAFE | No—backward-compatible |
| FR-004 | Modify [Service] | Change `[ExistingMethod]()` signature | ❌ RISKY | YES—explain why signature change necessary; consider optional parameter pattern instead |

**Gate Rule**: If ANY requirement requires RISKY modification:
1. Flag as HIGH RISK in domain-review.md
2. Require explicit justification in "Suggested Workaround" column
3. Suggest safer alternative (add new method/class, extend with new field, create new interface)
4. Ask user: "Accept risky modification? (yes to proceed with risk noted / no to redesign for safer reuse)"

### 6. Build Critique Matrix

Create a structured analysis table mapping each requirement:

| Requirement ID | Requirement Text | Necessity Score (1-5) | Reuse Opportunity? | Recommended Action | Regression Risk |
|---|---|---|---|---|---|
| FR-001 | [text] | 5 | Extend CallService | Keep as-is | ✅ SAFE—new method |
| FR-002 | [text] | 3 | Add column to CallResponse table | Refine: merge with FR-003 | Reduces new work |
| FR-003 | [text] | 2 | Duplicate of feature already in QuestionService | Remove | Scope reduction |

**Scoring**: 1 = optional/low-value, 5 = core/blocking/MVP-critical

### 7. Produce Domain Review Document

Generate `FEATURE_DIR/domain-review.md` with sections:

```markdown
# Domain Review: [FEATURE NAME]

**Date**: [TODAY]  
**Spec**: [Link to spec.md]  
**Status**: Ready for plan / Recommended refinements

## Executive Summary

[If no changes]: Spec aligns well with existing domain. Reuse strategy: [brief summary]

[If refinements recommended]: [N] recommendations to improve necessity, reduce scope, maximize reuse. MVP scope can be reduced by [X]% by consolidating requirements.

## Requirement Analysis

### High-Value Requirements (Necessity: 4-5)
- FR-001: [text] → Action: Keep as-is / Extend [existing service]
- FR-005: [text] → Action: Extend CallSession table with new column

### Optimize-for-Reuse (Necessity: 3, High Reuse)
- FR-003: [text] → Action: Consolidate with FR-002 (both handled by QuestionService)
- FR-007: [text] → Action: Use existing validation pattern from [Service]

### Recommended Removals (Necessity: 1-2, Duplicate/Out-of-Scope)
- FR-004: [text] → Reason: Duplicate of feature already in [Service]. Recommendation: Remove or defer to post-MVP.
- FR-008: [text] → Reason: Enhancement, not MVP-blocking. Recommendation: Move to Phase 2 backlog.

## Reuse Strategy

### Extend Existing Entities
- **[ExistingTable]**: Add `[newField]` instead of new table
- **[ExistingEntity]**: Extend with `[newProperty]` (existing entity, new field only)

### Leverage Existing Services
- **[ExistingService]**: Already handles [domain concern]. [Requirement] can reuse `[ExistingMethod]()` with configuration.
- **[AnotherService]**: Extend with new public method instead of new service class.

### Share Patterns
- Error handling: Use existing exception types without modifying exception handling logic
- Logging: Use existing `ILogger` configuration patterns without refactoring infrastructure

## Architecture Impact

- **New Tables**: [count] (consolidated from original [original count])
- **New Services**: [count] (consolidated from original [original count])
- **Database Changes**: [description] (minimal migration or new columns only)
- **Implementation Complexity**: Reduced by [%] via consolidation

## Ambiguities or Questions Remaining

[If any]: 
- Q1: [Clarification question about requirement scope/timing]
- Q2: [Clarification question about data retention/lifecycle]

## Next Steps

1. Review recommendations and approve/override
2. If accepted, update spec.md to reflect consolidations
3. Proceed to `/speckit.plan` with optimized requirements
```

Include a **Regression Prevention Summary** section:

```markdown
## Regression Prevention

### Change-Free Reuse (Safe)
- Call existing `[ExistingMethod]()`—no signature change
- Add new method `[NewMethod]()` to [ExistingService]—new method, existing unchanged
- Extend [ExistingTable] with `[newColumn]` column—new column, no migration risk

#### Modifications Requiring Justification (Risky)
- Modify `[ExistingMethod]()` signature to add parameter—all existing callers affected. **Alternative**: Create new overload or use optional parameter pattern.

### Risk Assessment
All risky decisions have been evaluated and justified. Proceed with risk awareness in testing.
```

### 8. Gate Decision

**Before handing off to plan**:
- If **no critical changes**: Confirm spec is ready for plan
- If **refinements recommended**: Ask user: "Review domain review. Should spec be updated before plan? (yes/no/review-later)"
  - **Yes**: User updates spec, then `/speckit.plan`
  - **No/Review-Later**: Proceed to `/speckit.plan` with current spec (risks rework)

### 9. Handoff to Plan

Report:
- Domain review location: `FEATURE_DIR/domain-review.md`
- Reuse opportunities identified: [N] consolidations, [M] extensions
- Recommended spec updates: [List any] 
- Proceed to: `/speckit.plan`

### Regression Prevention Enforcement
- **RISKY modifications flagged**: Yes/No + count
- **Safer alternatives suggested**: List any compromises
- **User acknowledgment**: Risky decisions require explicit user approval

## Key Principles

- **Maximize Reuse**: Extend existing entities and services rather than create new ones
- **Minimize Scope**: Challenge necessity of every requirement; ruthlessly cut low-value items
- **Document Tradeoffs**: Every recommendation includes rationale and risk
- **Preserve MVP Value**: All consolidations must maintain user value; no feature stripping
- **Prevent Regression**: Reuse by extension/addition only; flag modifications as risky; require justification
- **Enable Informed Decisions**: Present findings clearly so user can accept/override with confidence

````
