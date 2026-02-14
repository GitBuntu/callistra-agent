# Phase 0: Research & Technology Decisions

**Feature**: Human Voice Response Recognition (002)  
**Date**: 2026-02-14  
**Status**: ✅ Complete — All NEEDS CLARIFICATION items resolved in /speckit.clarify phase

---

## Summary

No research needed. All architectural and technology ambiguities resolved during `/speckit.clarify` phase (Clarifications Q1-Q4). This document exists for completeness per Speckit workflow.

---

## Clarifications Applied (Q1-Q4)

| Question | Answer | Research Status | Impact |
|----------|--------|---|---|
| **Q1**: Which speech-to-text service? | **Azure Speech Services** | ✅ Decided | Technology choice locked; no alternatives evaluated (lower cost, native Azure integration) |
| **Q2**: Voice prompt strategy? | **Mode-specific prompts** ("Say yes or no" for voice; "Press 1" for DTMF) | ✅ Decided | UX clarified; dual-prompt implementation straightforward |
| **Q3**: Fallback on STT unavailable? | **No fallback.** Single-mode campaigns only. | ✅ Decided | Simplifies error handling; graceful failure pattern |
| **Q4**: Retry logic & edge cases? | **Not in MVP.** Deferred to Phase 2. | ✅ Decided | Scope reduction; 8 core tests sufficient |

---

## Technology Stack — Confirmed ✅

All technologies confirmed via prior features (001-minimal-call-agent) or clarifications:

| Component | Decision | Rationale | Confidence |
|-----------|----------|-----------|-----------|
| **Speech-to-Text Service** | Azure Speech Services (Cognitive Services API) | Native Azure integration, low latency, pay-per-use, no setup required | ✅ HIGH |
| **REST vs SDK** | TBD in Phase 1 design (Azure.CognitiveServices NuGet vs HTTP) | Both viable; design phase will select based on performance requirements | 🟡 MEDIUM |
| **Language/Locale** | en-US only for MVP | Hardcoded per clarification; multi-language → Phase 2 | ✅ HIGH |
| **Confidence Threshold** | 75% minimum | Domain-standard for voice recognition; aligned with healthcare use case | ✅ HIGH |
| **Intent Mapping** | Simple deterministic logic (yes/no/digit classifier) | Transcription → intent; no ML needed for MVP | ✅ HIGH |
| **Call Event Handling** | Existing CallEventWebhookFunction pattern | Reuse webhook infrastructure; voice callbacks same cloudEvents type | ✅ HIGH |
| **State Management** | Extend CallSessionState (in-memory cache per call) | Existing pattern; add voice-only checks | ✅ HIGH |

---

## Architecture Decisions — Confirmed ✅

| Decision | Rationale | Alternatives Considered | Winner |
|----------|-----------|------------------------|--------|
| **Single-Mode Campaigns** | Immutable at creation; no runtime switching | Dual-mode with fallback (more complex) | Single-mode ✅ |
| **No Fallback Logic** | Graceful failure; calls end if STT unavailable | Fallback to DTMF (adds retry state machine) | Graceful failure ✅ |
| **Voice Metadata Storage** | New VoiceRecognitionResult table | Store in CallResponse (semantic overload) | New table ✅ |
| **Prompt Rendering** | Mode-specific overloads in QuestionService | Branch inside existing method (risky modification) | New overloads ✅ |
| **Configuration** | Extend AzureCommunicationServicesOptions | New separate config class (splits ownership) | Extend existing ✅ |

---

## No Further Research Required

All design decisions are grounded in:
1. **Feature spec** (spec.md with clarifications integrated)
2. **Domain analysis** (domain-review.md with reuse assessment)
3. **Constitution alignment** (all 5 principles verified in plan.md)
4. **Technology confirmation** (proven in prior features)

Proceed to **Phase 1: Design** to generate:
- `data-model.md` (entity schemas, migrations)
- `contracts/` (API + webhook specs)
- `quickstart.md` (developer onboarding)

