# Simple Implementation Guide

## The Minimal Viable Approach

Stop overengineering. Here's what you actually need to build a healthcare call agent:

---

## Step 1: Make a Phone Call (2 hours)

### What you need:
1. Azure Communication Services resource
2. A phone number 
3. Basic call initiation code

```bash
# Install one package
dotnet add package Azure.Communication.CallAutomation
```

**Configuration (appsettings.json):**
```json
{
  "AzureCommunicationServices": {
    "ConnectionString": "your-connection-string",
    "PhoneNumber": "+1234567890"
  }
}
```

**Code:**
```csharp
// That's it. Just call someone.
var client = new CallAutomationClient(connectionString);
var callInvite = new CallInvite(new PhoneNumberIdentifier(toPhoneNumber), 
                                 new PhoneNumberIdentifier(fromPhoneNumber));
await client.CreateCallAsync(callInvite, new Uri(callbackUrl));
```

**Done.** You can make calls now.

---

## Step 2: Play a Question (1 hour)

When the call connects, play text:

```csharp
[FunctionName("CallConnected")]
public async Task<IActionResult> OnCallConnected([HttpTrigger] HttpRequest req)
{
    var callEvent = CallAutomationEventParser.Parse(await req.ReadAsStringAsync());
    
    if (callEvent is CallConnected connected)
    {
        var callConnection = client.GetCallConnection(connected.CallConnectionId);
        await callConnection.GetCallMedia().PlayAsync(
            new TextSource("Are you enrolled in the diabetes program?"));
    }
    
    return new OkResult();
}
```

**Done.** Your call talks now.

---

## Step 3: Capture Response (1 hour)

Get yes/no answers:

```csharp
var recognizeOptions = new CallMediaRecognizeDtmfOptions(targetParticipant, maxTonesToCollect: 1)
{
    Prompt = new TextSource("Press 1 for yes, 2 for no"),
    InterToneTimeout = TimeSpan.FromSeconds(10)
};

await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
```

**Save it:**
```csharp
[FunctionName("ResponseReceived")]
public async Task<IActionResult> OnResponse([HttpTrigger] HttpRequest req)
{
    var recognizeEvent = CallAutomationEventParser.Parse(await req.ReadAsStringAsync());
    
    if (recognizeEvent is RecognizeCompleted completed)
    {
        var response = completed.CollectedTones.FirstOrDefault();
        await SaveToDatabase(response); // Your simple database call
    }
    
    return new OkResult();
}
```

**Done.** You're capturing responses.

---

## Step 4: Multiple Questions (30 minutes)

Loop through questions:

```csharp
var questions = new[]
{
    "Are you enrolled in the diabetes program?",
    "Do you need assistance with your medication?",
    "Would you like a follow-up call?"
};

foreach (var question in questions)
{
    await AskQuestionAndWaitForResponse(question);
}
```

**Done.** That's your call flow.

---

## What You DON'T Need

❌ Complex service interfaces with 10+ methods  
❌ Separate projects for "harness" testing  
❌ Elaborate event handler architecture  
❌ Multiple layers of abstraction  
❌ Retry logic on day one  
❌ Comprehensive logging frameworks  
❌ Unit tests before it works  
❌ Speech services initially (use text-to-speech from ACS first)  
❌ Webhook validation schemes  
❌ Rate limiting  
❌ Azure Dev Tunnels setup guides  

---

## Actual File Structure

```
src/
├── Functions/
│   ├── CallOperations.cs          # Make calls (200 lines)
│   └── CallEvents.cs               # Handle events (150 lines)
├── Services/
│   └── CallService.cs              # Business logic (100 lines)
└── Data/
    └── CallRepository.cs           # Database access (50 lines)
```

**Total: ~500 lines of actual code.**

Not 5000+ lines across 14 files with multiple services, interfaces, implementations, validators, handlers, engines, and orchestrators.

---

## Database

Three tables. That's it.

```sql
CREATE TABLE Members (Id, Name, Phone, Program);
CREATE TABLE CallSessions (Id, MemberId, Status, StartTime);
CREATE TABLE CallResponses (Id, CallSessionId, Question, Response);
```

---

## The 1-Day Implementation Plan

**Morning (4 hours):**
1. Set up ACS resource (30 min)
2. Buy phone number (15 min)
3. Create Azure Function with call initiation (1 hour)
4. Test that phone rings (15 min)
5. Add CallConnected event handler (1 hour)
6. Play first question (1 hour)

**Afternoon (4 hours):**
1. Add DTMF response capture (1 hour)
2. Save response to database (30 min)
3. Add 2 more questions (30 min)
4. Test full flow (1 hour)
5. Deploy and call real phone (1 hour)

**Done. Shipped. Working.**

---

## When to Add Complexity

Only add these **after** the basic version is working and you have real users:

- Azure Speech Services (when DTMF isn't good enough)
- Retry logic (when calls are failing)
- Complex validation (when you have bad data)
- Comprehensive tests (when you have bugs to prevent)
- Monitoring/logging (when you need to debug production)

---

## Reality Check

The original Phase 4.1 checklist has:
- ✅ 30+ checkboxes
- ✅ 4 major implementation steps
- ✅ Multiple sub-steps per step
- ✅ 8-10 hours per step
- ✅ Dependencies and prerequisites
- ✅ Acceptance criteria
- ✅ Implementation notes
- ✅ Multiple test phases
- ✅ Webhook validation schemes
- ✅ Event routing architectures

**You need: 3 Azure Function endpoints and 3 database tables.**

Build the simple thing first. Add complexity when you actually need it.

---

## Getting Started Right Now

```bash
# 1. Create the project
func init CallAgent --dotnet

# 2. Add one package
dotnet add package Azure.Communication.CallAutomation

# 3. Create one function
func new --name CallOperations --template "HTTP trigger"

# 4. Write 50 lines of code to make a call

# 5. Deploy and test
func azure functionapp publish your-function-app

# Done.
```

Stop planning. Start building. Ship something today.
