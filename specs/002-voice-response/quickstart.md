# Quick Start: Human Voice Response Recognition (Feature 002)

**Feature**: Add voice recognition alongside DTMF to healthcare call campaigns  
**Target Audience**: Backend developers implementing voice support  
**Estimated Setup Time**: 30-45 minutes  
**Prerequisites**: Feature 001 (minimal call agent) must be deployed

---

## What You're Building

Voice-enabled healthcare call campaigns where members can respond using spoken words or DTMF keypad tones. **Single-mode campaigns** (DTMF-only or voice-only, immutable at creation):

```
DTMF-Only Campaign:
  Member receives call → System plays question "Press 1 for yes, 2 for no"
  Member presses 1 on keypad → Recorded as DTMF response

Voice-Only Campaign:
  Member receives call → System plays question "Say yes or no"  
  Member says "yes" → Azure Speech Services transcribes → Recorded as voice response
```

---

## Part 1: Clone Feature Spec (5 minutes)

1. **Clone branch**:
   ```bash
   git checkout -b 002-voice-response origin/002-voice-response
   ```
   Alternatively, work on existing branch:
   ```bash
   git checkout 002-voice-response
   git pull
   ```

2. **Review spec artifacts** in `specs/002-voice-response/`:
   - `spec.md` - Feature requirements and test cases
   - `domain-review.md` - Reuse analysis and regression prevention
   - `plan.md` - Implementation architecture and coding standards
   - `data-model.md` - Entity schemas and database migrations
   - `contracts/` - API and webhook specifications

3. **Understand scope**:
   - ✅ Single-mode campaigns (no dual-mode)
   - ✅ Azure Speech Services integration
   - ✅ Mode-specific voice prompts ("Say yes or no" vs "Press 1")
   - ❌ No fallback (calls fail if STT unavailable)
   - ❌ No edge case handling (MVP scope only)

---

## Part 2: Database Setup (10 minutes)

### 2.1: Run Migration Script

1. **Open SQL Server Management Studio** (or Azure Data Studio)

2. **Connect to CallistraAgent database**:
   ```sql
   USE CallistraAgent;
   GO
   ```

3. **Run migration script**:
   ```sql
   -- Copy contents from: CallistraAgent/Migrations/002_AddVoiceSupport.sql
   -- This adds:
   --   - CallSession.ResponseMode column
   --   - CallResponse.ResponseSource column
   --   - VoiceRecognitionResults table
   --   - Supporting indexes and check constraints
   ```

4. **Verify schema changes**:
   ```sql
   -- Check new columns
   SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME IN ('CallSessions', 'CallResponses', 'VoiceRecognitionResults')
   ORDER BY TABLE_NAME, ORDINAL_POSITION;
   
   -- Check new table
   SELECT TABLE_NAME 
   FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_NAME = 'VoiceRecognitionResults';
   ```

### 2.2: Insert Test Campaign Data

```sql
-- Add test voice-only campaign
INSERT INTO Campaigns (Name, Program, ResponseMode, Status, CreatedAt)
VALUES ('Voice Demo Campaign', 'Diabetes Care', 'voice-only', 'Active', GETUTCDATE());

-- Query to verify
SELECT * FROM Campaigns WHERE ResponseMode = 'voice-only';
```

---

## Part 3: Configure Azure Speech Services (15 minutes)

### 3.1: Get Speech Services Credentials

1. **Navigate to Azure Portal**:
   - Go to your Azure subscription
   - Find **Azure Cognitive Services** → **Speech** resource

2. **Collect credentials**:
   - **Endpoint**: https://{region}.tts.speech.microsoft.com
   - **API Key**: Copy from "Keys and Endpoints" section
   - **Region**: e.g., "eastus", "westus"

### 3.2: Update Local Configuration

**File**: `src/CallistraAgent.Functions/local.settings.json`

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    
    "AzureCommunicationServices__ConnectionString": "endpoint=https://...;accesskey=...",
    "AzureCommunicationServices__PhoneNumber": "+1...",
    "AzureCommunicationServices__CallbackBaseUrl": "http://localhost:7071",
    "AzureCommunicationServices__CognitiveServicesEndpoint": "https://...",
    
    // NEW: Voice/Speech Services configuration
    "AzureCommunicationServices__SpeechServicesEndpoint": "https://{region}.tts.speech.microsoft.com",
    "AzureCommunicationServices__SpeechServicesKey": "{api-key}",
    "AzureCommunicationServices__SpeechServicesRegion": "{region}"
  }
}
```

### 3.3: Update Azure Key Vault (Production)

```bash
# For production deployment, store secrets in Key Vault
az keyvault secret set --vault-name CallistraKeyVault \
  --name "SpeechServicesEndpoint" \
  --value "https://eastus.tts.speech.microsoft.com"

az keyvault secret set --vault-name CallistraKeyVault \
  --name "SpeechServicesKey" \
  --value "{api-key}"

az keyvault secret set --vault-name CallistraKeyVault \
  --name "SpeechServicesRegion" \
  --value "eastus"
```

---

## Part 4: Code Review & Navigation (10 minutes)

### 4.1: Key New Classes

| Class | Purpose | Location |
|-------|---------|----------|
| `VoiceRecognitionResult` | Entity for voice metadata storage | Models/VoiceRecognitionResult.cs |
| `VoiceResponseInterpreter` | Maps transcription to intent (yes/no/digit) | Services/VoiceResponseInterpreter.cs |
| `VoiceClassification` | Constants for confidence thresholds | Constants/VoiceClassification.cs |

### 4.2: Extended Classes

| Class | Changes | Location |
|-------|---------|----------|
| `CallSession` | ➕ ResponseMode property | Models/CallSession.cs |
| `CallResponse` | ➕ ResponseSource property | Models/CallResponse.cs |
| `CallService` | ➕ InvokeAzureSpeechServicesAsync, InterpretVoiceResponseAsync, SaveVoiceResponseAsync | Services/CallService.cs |
| `QuestionService` | ➕ PlayVoiceQuestionAsync (overload) | Services/QuestionService.cs |
| `CallSessionState` | ➕ IsVoiceOnlyMode, GetCampaignResponseMode | Services/CallSessionState.cs |

### 4.3: Test Files

```
tests/CallistraAgent.Functions.Tests/

NEW:
  - Unit/Services/VoiceResponseInterpreterTests.cs (intent mapping logic)
  - Integration/CallEventWebhookFunctionTests.cs (voice event handling)
  - Fixtures/VoiceResponseFixtures.cs (mock Azure Speech responses)

EXTENDED:
  - Unit/Services/CallServiceTests.cs (add voice method tests)
```

---

## Part 5: Running Tests Locally (5 minutes)

### 5.1: Unit Tests

```bash
cd tests/CallistraAgent.Functions.Tests
dotnet test --filter "Category=Voice" --verbosity normal
```

### 5.2: Integration Tests (with local Azurite)

```bash
# Terminal 1: Start Azurite (Azure Storage emulator)
azurite --silent --location ./azurite-data

# Terminal 2: Start local SQL Server (if using Docker)
docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=YourPassword \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:latest

# Terminal 3: Run tests
dotnet test --filter "Category=Integration" --verbosity normal
```

### 5.3: Run Azure Function Locally

```bash
cd src/CallistraAgent.Functions
func start

# Function will be available at:
# POST http://localhost:7071/api/calls/events (webhook endpoint)
# GET http://localhost:7071/api/calls/status/{callSessionId}
```

---

## Part 6: Testing Voice Recognition Locally (10 minutes)

### 6.1: Mock Azure Speech Services (Development)

Use test fixtures to simulate voice recognition without calling real Azure:

```csharp
// tests/CallistraAgent.Functions.Tests/Fixtures/VoiceResponseFixtures.cs
var mockSpeechResult = new SpeechData
{
    Text = "yes",
    Confidence = 0.92m,
    Language = "en-US",
    AudioDurationMs = 1250.5m
};

// Use in test
var interpreter = new VoiceResponseInterpreter();
var intent = interpreter.InterpretVoiceResponse(mockSpeechResult.Text);
Assert.Equal("yes", intent);
```

### 6.2: Send Test Webhook Events

```bash
# Test DTMF-only event (existing behavior)
curl -X POST http://localhost:7071/api/calls/events \
  -H "Content-Type: application/cloudevents+json" \
  -d @test-events/dtmf-recognize-completed.json

# Test voice-only event (new)
curl -X POST http://localhost:7071/api/calls/events \
  -H "Content-Type: application/cloudevents+json" \
  -d @test-events/voice-recognize-completed.json

# Test recognize failed event
curl -X POST http://localhost:7071/api/calls/events \
  -H "Content-Type: application/cloudevents+json" \
  -d @test-events/recognize-failed.json
```

**Sample event files** in `tests/CallistraAgent.Functions.Tests/TestData/`:
- `dtmf-recognize-completed.json` - User pressed "1"
- `voice-recognize-completed.json` - User said "yes" (confidence 0.92)
- `recognize-failed.json` - User did not speak (timeout)

---

## Part 7: Code Style & Standards Checklist

Before submitting PR, verify:

- ✅ **TDD**: Did you write tests FIRST? Do they fail initially?
- ✅ **Coverage**: Do new tests cover 80%+ of code? (Run `dotnet test /p:CollectCoverage=true`)
- ✅ **C# Style**: 
  - PascalCase classes/methods, camelCase fields
  - Max 100 characters per line
  - Methods <30 lines
  - XML documentation on public APIs
  - No hardcoded credentials
- ✅ **Regression**: Did you extend services (new methods) vs. modify existing methods?
  - Example: ✅ `SaveVoiceResponseAsync()` new method
  - Example: ❌ AVOID modifying `SaveCallResponseAsync()` signature
- ✅ **Logging**: Include context in error logs (call ID, timestamp, no PII)
- ✅ **Commit Messages**: Atomic, descriptive ("Add VoiceResponseInterpreter" not "fix stuff")

---

## Part 8: Development Workflow

### Daily Workflow

1. **Start Function Locally**:
   ```bash
   cd src/CallistraAgent.Functions
   func start
   ```

2. **Write Tests First**:
   ```bash
   # In tests directory
   dotnet test --watch
   ```

3. **Implement Code** (Red-Green-Refactor):
   - RED: Tests fail (prove implementation doesn't exist)
   - GREEN: Write minimal code to pass tests
   - REFACTOR: Improve code quality while keeping tests green

4. **Commit Changes**:
   ```bash
   git add .
   git commit -m "Add: VoiceResponseInterpreter with intent mapping tests"
   ```

### Pre-PR Checklist

```bash
# 1. Run all tests locally
dotnet test

# 2. Check code coverage
dotnet test /p:CollectCoverage=true /p:CoverageThreshold=80

# 3. Lint C# code
dotnet format --verify-no-changes

# 4. Verify no hardcoded secrets
git log -p | grep -i "password\|key\|secret" || echo "✓ No secrets found"

# 5. Update CHANGELOG
# - Document new methods, breaking changes, deprecations

# 6. Create PR with description
git push origin 002-voice-response
# Open GitHub PR with:
#   - Summary of changes
#   - Reference to spec (specs/002-voice-response/spec.md)
#   - Test coverage percentage
#   - Any regression prevention notes
```

---

## Part 9: Common Tasks

### Testing Intent Mapping

```csharp
// Test: "yes", "yeah", "affirmative" → "yes" intent
var interpreter = new VoiceResponseInterpreter();
Assert.Equal("yes", interpreter.InterpretVoiceResponse("yes"));
Assert.Equal("yes", interpreter.InterpretVoiceResponse("yeah"));
Assert.Equal("yes", interpreter.InterpretVoiceResponse("affirmative"));

// Test: "no", "nope", "negative" → "no" intent
Assert.Equal("no", interpreter.InterpretVoiceResponse("no"));
Assert.Equal("no", interpreter.InterpretVoiceResponse("nope"));
```

### Testing Confidence Threshold

```csharp
// Test: Confidence < 75% → Reprompt
var lowConfidence = new SpeechData { Text = "yes", Confidence = 0.60m };
var shouldReprompt = lowConfidence.ConfidenceScore < 0.75m;
Assert.True(shouldReprompt);

// Test: Confidence ≥ 75% → Accept
var highConfidence = new SpeechData { Text = "yes", Confidence = 0.92m };
var shouldAccept = highConfidence.ConfidenceScore >= 0.75m;
Assert.True(shouldAccept);
```

### Querying Voice Results

```csharp
// Find all voice responses for a campaign
var voiceResults = await context.VoiceRecognitionResults
    .Include(v => v.CallSession)
    .Where(v => v.CallSession.ResponseMode == "voice-only"
                && v.ProcessedAt > DateTime.UtcNow.AddDays(-1))
    .ToListAsync();

// Average confidence score
var avgConfidence = voiceResults
    .Average(v => v.ConfidenceScore);
```

---

## Part 10: Troubleshooting

| Issue | Solution |
|-------|----------|
| **"Speech Services endpoint not configured"** | Add `AzureCommunicationServices__SpeechServicesEndpoint` to local.settings.json |
| **"ConfidenceScore out of range"** | Verify Azure Speech Services returns 0.0-1.0 (some APIs return 0-100%) |
| **Tests fail with "Campaign not found"** | Ensure test data insertions run before voice tests |
| **Webhook events processed as DTMF instead of voice** | Check CallSession.ResponseMode is set correctly before calling webhook |
| **"Intent is not recognized"** | Verify VoiceResponseInterpreter.cs handles transcription (case-insensitive) |

---

## Next Steps

1. ✅ **Database Setup**: Apply migration script (002_AddVoiceSupport.sql)
2. ✅ **Configuration**: Add Speech Services credentials to local.settings.json
3. ✅ **Code Review**: Understand key new classes and extended functionality
4. ✅ **Tests**: Run unit + integration tests locally
5. ➜ **Implement**: Follow TDD workflow for assigned tasks
6. ➜ **Submit PR**: Reference spec + domain-review; ensure 80%+ coverage
7. ➜ **Deploy**: Test in staging before production rollout

---

## Resources

- **Feature Spec**: [specs/002-voice-response/spec.md](../spec.md)
- **Architecture Plan**: [specs/002-voice-response/plan.md](../plan.md)
- **Data Model**: [specs/002-voice-response/data-model.md](../data-model.md)
- **API Contracts**: [specs/002-voice-response/contracts/](../contracts/)
- **Test Cases**: [specs/002-voice-response/checklists/requirements.md](../checklists/requirements.md)
- **Azure Speech Services Docs**: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/
- **Azure Communication Services Docs**: https://learn.microsoft.com/en-us/azure/communication-services/

---

## Support

- **Questions**: Check feature spec (specs/002-voice-response/spec.md)
- **Architecture Decisions**: See domain-review.md and plan.md
- **API Format**: Consult contracts/ directory for OpenAPI specs
- **Database Help**: Run `SELECT * FROM INFORMATION_SCHEMA.TABLES` to verify schema

