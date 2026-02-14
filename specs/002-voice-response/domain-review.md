# Domain Review: Human Voice Response Recognition (Feature 002)

**Date**: 2026-02-14  
**Spec**: [specs/002-voice-response/spec.md](../002-voice-response/spec.md)  
**Status**: ✅ Ready for Plan (After Recommendations Applied)

---

## Executive Summary

Spec aligns well with existing DTMF infrastructure. **8 of 14 requirements are safely extensible** from existing CallService and CallSession entities. **2 requirements are RISKY** (require modifying CallService signature and error handling paths). **4 requirements are NEW** (Azure Speech Services integration, voice-specific prompt logic, VoiceRecognitionResult entity).

**Key Finding**: Clarifications (single-mode campaigns, no fallback) **dramatically reduce complexity**. No dual-mode branching needed. Scope consolidation achieved: 
- **Requirements**: 14 functional (down from 15 pre-clarification)
- **Reuse**: 9 SAFE extensions vs 2 RISKY modifications
- **New Build**: 4 net new capabilities (Azure STT, mode-specific prompts, voice entity, confidence logic)
- **Database Changes**: 1 new column (CallSession.ResponseMode), 1 new table (VoiceRecognitionResult)
- **Breaking Changes**: 1 (CallService.SaveCallResponseAsync signature for voice responses)

---

## Requirement Analysis

### High-Value Requirements (Necessity: 4-5) — KEEP AS-IS ✅

| Requirement | Necessity | Reuse Opportunity | Recommendation | Regression Risk |
|---|---|---|---|---|
| **FR-001** (DTMF tone parsing) | 5 | Reuse [CallService.HandleRecognizeCompletedAsync](../../src/CallistraAgent.Functions/Services/CallService.cs#L376) callback parser | Keep as-is; already implemented | ✅ SAFE—no change |
| **FR-002** (DTMF validation 1-3) | 5 | Reuse [CallService.SaveCallResponseAsync](../../src/CallistraAgent.Functions/Services/CallService.cs#L408) validation logic | Keep as-is; already implemented | ✅ SAFE—no change |
| **FR-003** (Reject invalid DTMF) | 5 | Reuse [QuestionService](../../src/CallistraAgent.Functions/Services/QuestionService.cs) reprompt pattern | Keep as-is; already implemented | ✅ SAFE—no change |
| **FR-011** (DTMF-only state check) | 4 | Extend [CallSessionState](../../src/CallistraAgent.Functions/Services/CallSessionState.cs) to check campaign mode before DTMF capture | Add method `bool IsVoiceOnlyMode(callConnectionId)` | ✅ SAFE—new method |
| **FR-015** (Azure Speech Services specified) | 4 | Define [AzureCommunicationServicesOptions](../../src/CallistraAgent.Functions/Configuration/AzureCommunicationServicesOptions.cs) with STT endpoint | Add property `SpeechEndpoint` to config | ✅ SAFE—new property |

### Reuse-Optimized Requirements (Necessity: 4, High Reuse) — CONSOLIDATE SAFELY ✅

| Requirement | Necessity | Existing Pattern | Recommendation | Regression Risk |
|---|---|---|---|---|
| **FR-008** (Malformed webhook) | 4 | [CallEventWebhookFunction](../../src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs#L115) already logs with call ID | Use existing logger pattern; no code change required | ✅ SAFE—pattern reuse |
| **FR-010** (Config load error) | 4 | [CallService.InitiateCallAsync](../../src/CallistraAgent.Functions/Services/CallService.cs#L92) already validates ACS config | Use existing validation pattern; add STT config check | ✅ SAFE—pattern reuse |
| **FR-012** (Voice-only state check) | 4 | Extend [CallSessionState](../../src/CallistraAgent.Functions/Services/CallSessionState.cs) | Add method (parallel to FR-011) | ✅ SAFE—new method |
| **FR-014** (Mode-specific prompts) | 4 | Extend [QuestionService](../../src/CallistraAgent.Functions/Services/QuestionService.cs) prompt generation | Add overload `PlayQuestionAsync(callConnection, questionNumber, responseMode)` | ✅ SAFE—new overload |

### Strategic New Requirements (Necessity: 4-5, New Build) — REQUIRES NEW INFRASTRUCTURE ⚠️

| Requirement | Necessity | New Capability | Recommendation | Complexity |
|---|---|---|---|---|
| **FR-004** (Invoke STT on voice) | 5 | Azure Speech Services REST API call | New method in CallService: `InvokeAzureSpeechServices(audioStream, locale)` | HIGH—external API integration |
| **FR-005** (Interpret transcription) | 5 | Intent mapping (yes/no/digit→intent) | New utility class: `VoiceResponseInterpreter` with mapping logic | MEDIUM—simple logic |
| **FR-006** (Low confidence reprompt) | 4 | Confidence scoring threshold | Extend [QuestionService.PlayQuestionAsync](../../src/CallistraAgent.Functions/Services/QuestionService.cs) with confidence gate | MEDIUM—conditional prompting |
| **FR-013** (Language locale) | 2 | Locale handling (Optional, MVP-deferred) | Defer to Phase 2; hardcode en-US for MVP | LOW—future enhancement |

### Out-of-Scope Validations — VERIFY SCOPE

| Requirement | Necessity | Scope Decision | Justification |
|---|---|---|---|
| **FR-009** (STT unavailable error) | 4 | **MVP-Scoped** per clarification Q3 | Campaigns configured for single mode upfront (DTMF-only or voice-only). No fallback. Call fails gracefully. Simpler than retry logic. |
| Dual-mode campaigns | 1 | **Out of Scope** | Single-mode simplifies state machine, error handling, and prompt logic. User explicitly chose this in clarification. |
| Fallback to DTMF | 1 | **Out of Scope** | No fallback per Q3. Campaigns pre-configured for mode. Call fails if STT unavailable. |
| Complex retry logic | 1 | **Out of Scope** | Per clarification Q4: "Don't cover edge cases for MVP". Scope reduction by design. |

---

## Reuse Strategy

### ✅ SAFE Reuse (No Modification Risk)

#### Extend Existing Services
1. **[CallService](../../src/CallistraAgent.Functions/Services/CallService.cs)**
   - ✅ **ADD** new method `InvokeAzureSpeechServicesAsync(callConnectionId, audioStream, locale)` — captures voice, returns transcription + confidence
   - ✅ **ADD** new method `InterpretVoiceResponseAsync(transcription)` — maps spoken word to intent (yes/no/digit)
   - ✅ **REUSE** existing `SaveCallResponseAsync(callSessionId, questionNumber, responseValue)` for both DTMF and voice (no signature change required; value can be string-based intent like "yes" or "1")
   - ✅ **Call existing** `HandleRecognizeCompletedAsync(callConnectionId, dtmfTones)` — unchanged for DTMF path

2. **[CallSessionState](../../src/CallistraAgent.Functions/Services/CallSessionState.cs)**
   - ✅ **ADD** new method `GetCampaignResponseMode(callConnectionId)` — retrieves responseMode from call session state
   - ✅ **ADD** new method `IsVoiceOnlyMode(callConnectionId)` — boolean check for voice-only validation

3. **[QuestionService](../../src/CallistraAgent.Functions/Services/QuestionService.cs)**
   - ✅ **ADD** new overload `PlayQuestionAsync(callConnection, questionNumber, responseMode, cancellationToken)` — routes to mode-specific prompt
   - ✅ **ADD** new method `PlayVoiceQuestionAsync(callConnection, questionNumber, cancellationToken)` — renders "Say yes or no" prompt
   - ✅ **KEEP** existing `PlayQuestionAsync(callConnection, questionNumber)` — unchanged for DTMF path

4. **[AzureCommunicationServicesOptions](../../src/CallistraAgent.Functions/Configuration/AzureCommunicationServicesOptions.cs)**
   - ✅ **ADD** new property `SpeechServicesEndpoint` (string) — stores Azure Speech Services endpoint URL
   - ✅ **ADD** new property `SpeechServicesKey` (string) — stores API key (from config)
   - ✅ **ADD** new property `SpeechServicesRegion` (string) — stores region (e.g., "eastus")
   - ✅ **KEEP** existing ACS properties unchanged

#### Extend Existing Entities (Database)
1. **[CallSession table](../../CallistraAgent/Tables/CallSessions.sql)**
   - ✅ **ADD** new column `ResponseMode` (NVARCHAR(20), default 'dtmf-only') — immutable campaign mode
   - ✅ **KEEP** existing columns unchanged (Status, StartTime, EndTime, etc.)
   - ✅ **Add constraint**: `CHK_CallSession_ResponseMode CHECK (ResponseMode IN ('dtmf-only', 'voice-only'))`

2. **[CallResponse table](../../CallistraAgent/Tables/CallResponses.sql)**
   - ✅ **ADD** new column `ResponseSource` (NVARCHAR(20), default 'dtmf') — tracks DTMF vs voice
   - ✅ **KEEP** ResponseValue semantics unchanged (can store "1", "2", "yes", "no", "skip" for both modes)
   - ✅ **Note**: No migration risk; new column is optional, backward-compatible

3. **NEW table: VoiceRecognitionResult**
   - ✅ **CREATE** new entity to store voice recognition details:
     ```sql
     CREATE TABLE VoiceRecognitionResult (
       Id INT IDENTITY(1,1) PK,
       CallSessionId INT FK,
       QuestionNumber INT,
       AudioDuration DECIMAL(10,3),
       TranscribedText NVARCHAR(500),
       ConfidenceScore DECIMAL(3,2),
       Intent NVARCHAR(50),  -- "yes", "no", digit, "skip"
       ProcessedAt DATETIME2 DEFAULT GETUTCDATE()
     )
     ```

#### Reuse Validation & Error Handling Patterns
- ✅ **Error handling**: [CallEventWebhookFunction](../../src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs#L115) already uses RFC 7807 Problem Details. Extend with voice-specific error types.
- ✅ **Logging**: Extend existing [ILogger](../../src/CallistraAgent.Functions/Functions/CallEventWebhookFunction.cs) patterns without changing infrastructure.
- ✅ **State transitions**: [CallSessionState](../../src/CallistraAgent.Functions/Services/CallSessionState.cs) already manages question progression. Add voice-specific state (e.g., "awaiting-voice-input").

---

### ⚠️ RISKY Modifications (Requires Justification & Testing)

#### Risk 1: SaveCallResponseAsync Signature Change ❌

**Current Signature** (from [CallService.cs](../../src/CallistraAgent.Functions/Services/CallService.cs#L408)):
```csharp
public async Task SaveCallResponseAsync(
    int callSessionId, 
    int questionNumber, 
    string dtmfResponse,  // ← Currently DTMF-only (expects "1" or "2")
    CancellationToken cancellationToken = default)
```

**Requirement**: FR-005 (Interpret transcription) needs to save voice intent ("yes", "no") alongside DTMF responses.

**The Problem**: 
- Current method validates `dtmfResponse ∈ {1, 2}` (see line 413: `if (!int.TryParse(dtmfResponse, out var responseValue) || (responseValue != 1 && responseValue != 2))`)
- Voice responses need to store intent string ("yes", "no", "yes-confident", "skip")
- Changing parameter to `string responseValue` (voice-agnostic) affects **all 4 call sites**:
  1. [HandleRecognizeCompletedAsync](../../src/CallistraAgent.Functions/Services/CallService.cs#L393) — line 405
  2. Test mock: [CallServiceTests.SaveCallResponseAsync](../../tests/CallistraAgent.Functions.Tests/Unit/Services/CallServiceTests.cs#L240)
  3. Test mock: [HandleRecognizeCompletedAsync test](../../tests/CallistraAgent.Functions.Tests/Unit/Services/CallServiceTests.cs#L275)
  4. Future voice webhook handler (new)

**Suggested SAFE Workaround** ✅:
Instead of changing signature, **add new overload**:
```csharp
// Existing method — UNCHANGED
public async Task SaveCallResponseAsync(int callSessionId, int questionNumber, string dtmfResponse, CancellationToken cancellationToken = default)
{ /* existing DTMF logic */ }

// NEW overload for voice responses
public async Task SaveVoiceResponseAsync(int callSessionId, int questionNumber, string intent, decimal confidenceScore, CancellationToken cancellationToken = default)
{ 
    // Validate intent ∈ {"yes", "no", "skip", digit}, store in CallResponse + VoiceRecognitionResult
    // NO change to existing DTMF path
}
```

**Decision**: ✅ **USE NEW OVERLOAD** — Zero regression risk, no existing callers affected, maintains backward compatibility.

---

#### Risk 2: Error Recovery Path ❌ (Clarified as Out-of-Scope)

**Requirement**: FR-009 (Azure Speech Services unavailable handling)

**Clarification Q3 Answer**: "No fallback. Single-mode campaigns. Call fails gracefully."

**Status**: ✅ **NO MODIFICATION NEEDED** — Graceful failure uses existing pattern:
- Current: [HandleCallFailedAsync](../../src/CallistraAgent.Functions/Services/CallService.cs#L198) sets status to `Failed`
- Voice path: Same handler, no change required
- No retry logic, no fallback branching

---

### Regression Prevention Matrix

| Requirement | Reuse Pattern | Modification Type | Risk Level | Justification | Alternative if RISKY |
|---|---|---|---|---|---|
| FR-001 | Reuse `HandleRecognizeCompletedAsync` | No change | ✅ SAFE | Existing method unchanged; voice callback will also use it | — |
| FR-002 | Extend validation | New method `ValidateVoiceIntent()` | ✅ SAFE | New method, existing validation unchanged | — |
| FR-003 | Reuse reprompt pattern | Use existing method | ✅ SAFE | [QuestionService.PlayQuestionAsync](../../src/CallistraAgent.Functions/Services/QuestionService.cs) unchanged for DTMF | — |
| FR-004 | NEW Azure STT integration | New method `InvokeAzureSpeechServicesAsync()` | ✅ SAFE | New method, no existing callers affected | — |
| FR-005 | NEW intent translator | New method `InterpretVoiceResponseAsync()` | ✅ SAFE | New method, no existing callers affected | — |
| FR-006 | Extend confidence gate | New overload `PlayQuestionAsync()` | ✅ SAFE | Overload, existing DTMF path unchanged | — |
| FR-008 | Reuse logging pattern | Use existing logger | ✅ SAFE | No infrastructure change; pattern reuse | — |
| FR-009 | Reuse error handler | Use existing `HandleCallFailedAsync()` | ✅ SAFE | Existing method, same error path for voice | — |
| FR-010 | Extend config validation | Add STT config check | ✅ SAFE | Parallel validation, DTMF config path unchanged | — |
| FR-011 | Extend state check | New method `IsVoiceOnlyMode()` | ✅ SAFE | New method, no existing logic affected | — |
| FR-012 | Extend state check | New method `IsVoiceOnlyMode()` (same as FR-011) | ✅ SAFE | Reuse shared method | — |
| FR-013 | NEW locale config | New property in options | ✅ SAFE | New property, no existing config affected | — |
| FR-014 | Extend prompts | New overload `PlayQuestionAsync(responseMode)` | ✅ SAFE | Overload, existing DTMF prompt path unchanged | — |
| FR-015 | NEW Azure Speech Services | New config properties | ✅ SAFE | New properties, no existing ACS config affected | — |

**Summary**: 
- **✅ SAFE (No Modification)**: 13 of 14 requirements
- **⚠️ RISKY (Modify Existing)**: 1 requirement (FR-005 SaveCallResponseAsync) → **MITIGATED via overload pattern**
- **🚫 Breaking Changes**: 0 (all mitigated via safe alternatives)

---

## Architecture Impact

### Database Changes
- **New Columns**: 2
  - `CallSession.ResponseMode` (immutable campaign mode)
  - `CallResponse.ResponseSource` (DTMF vs voice source)
- **New Tables**: 1
  - `VoiceRecognitionResult` (voice metadata: transcription, confidence, intent)
- **Migration Complexity**: **LOW** — all new columns are backward-compatible; no data migration required
- **Backward Compatibility**: ✅ Yes — existing DTMF-only campaigns unaffected

### Service Layer Changes
- **New Methods**: 7 (all safe non-breaking additions)
  - `CallService.InvokeAzureSpeechServicesAsync()` 
  - `CallService.InterpretVoiceResponseAsync()`
  - `CallService.SaveVoiceResponseAsync()` (overload, not signature change)
  - `CallSessionState.IsVoiceOnlyMode()`
  - `CallSessionState.GetCampaignResponseMode()`
  - `QuestionService.PlayVoiceQuestionAsync()`
  - `QuestionService.PlayQuestionAsync(responseMode)` (overload)
- **Modified Methods**: 0 (no signature changes required)
- **Breaking Changes**: 0

### Configuration Changes
- **New Properties** in `AzureCommunicationServicesOptions`:
  - `SpeechServicesEndpoint`
  - `SpeechServicesKey`
  - `SpeechServicesRegion`

### Testing Impact
- **DTMF Path**: No regression risk; use existing test suite ([CallServiceTests](../../tests/CallistraAgent.Functions.Tests/Unit/Services/CallServiceTests.cs)) as-is
- **Voice Path**: Need 8 new tests (TC-001 through TC-008 from spec)
- **Integration**: New webhook handler test for voice events

### Implementation Complexity
- **DTMF Regression Prevention**: ZERO—no changes to existing code path
- **Voice Implementation**: **MEDIUM** — 4 new service methods, 1 new table, 1 new webhook handler  
- **Overall**: **18-21 story points** (vs 25-30 if dual-mode or with fallback logic)

---

## Ambiguities & Questions Resolved

✅ **All clarifications applied** in spec phase:
1. **Q1**: Which STT service? → **Azure Speech Services** (specified in FR-015)
2. **Q2**: Voice prompts? → **Mode-specific** (specified in FR-014)
3. **Q3**: Fallback behavior? → **No fallback, single-mode campaigns** (specified in FR-009, simplifies error handling)
4. **Q4**: Edge cases? → **Not in MVP** (reduces test count from 12+ to 8, scope manageable)

---

## Recommendations

### ✅ Approved As-Is
1. **Use new overload pattern** for `SaveVoiceResponseAsync()` — zero regression, backward-compatible
2. **Add responseMode to CallSession** — immutable field, enables DTMF-only vs voice-only logic
3. **Extend CallSessionState** with voice-specific queries — clean separation of concerns
4. **Create VoiceRecognitionResult table** — audit trail for voice interactions

### 🔄 Minor Refinements
1. **FR-013 (Language Locale)**: Hardcode `en-US` for MVP; defer multi-language support to Phase 2 (low necessity, optional requirement)
2. **Error Handling**: Use existing `HandleCallFailedAsync()` for voice STT failures; no new error types needed per clarification

### 📊 Scope Confirmation
- **Requirements**: 14 functional + 1 optional = 15 total
- **MVP Coverage**: 8 test cases sufficient (edge cases deferred)
- **Reuse Efficiency**: 9 SAFE extensions + 4 NEW capabilities = 13 reuse/extension ratio
- **Risk Level**: **LOW** — all risky decisions mitigated via safe alternatives

---

## Next Steps

1. ✅ **Domain Review**: Complete (this document)
2. ⏳ **Plan Phase**: Design technical architecture per clarified spec
   - Detail CallService method signatures
   - Design VoiceRecognitionResult storage/retrieval
   - Plan Azure Speech Services integration (REST API vs SDK decision)
   - Design quality gates and test strategy
3. ⏳ **Tasks Phase**: Break into T001-T0XX per user story
4. ⏳ **Implement Phase**: Execute per plan with regression prevention gates

---

## Sign-Off

**Domain Review Status**: ✅ **APPROVED**  
**Regression Risk**: ✅ **MITIGATED** — All risky decisions have safer alternatives  
**Recommended Actions**: Apply overload pattern for SaveVoiceResponseAsync; proceed to plan phase  
**Ready to Proceed**: ✅ **YES** — Spec clarity no longer blocks planning
