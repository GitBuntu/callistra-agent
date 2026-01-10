# Quickstart: Test Your First Call

This guide walks you through testing the Callistra Agent with a real phone call.

## Prerequisites

✅ Database created with tables (see [QUICKSTART-DATABASE.md](CallistraAgent/QUICKSTART-DATABASE.md))  
✅ Azure Communication Services configured (connection string and phone number in `local.settings.json`)  
✅ Dev Tunnels installed and configured (see [DEVTUNNEL-SETUP.md](DEVTUNNEL-SETUP.md))

## Step-by-Step Test

### 1. Start Dev Tunnels

Open PowerShell Terminal #1:

```powershell
devtunnel user login -g
devtunnel host -p 7071 --allow-anonymous
```

**Copy the tunnel URL** from the output (e.g., `https://abc123-7071.devtunnels.ms`)

⚠️ **Keep this terminal running** throughout the test.

### 2. Update Configuration

Edit `src/CallistraAgent.Functions/local.settings.json` and paste your tunnel URL:

```json
{
  "Values": {
    "AzureCommunicationServices__CallbackBaseUrl": "https://your-tunnel-id-7071.devtunnels.ms"
  }
}
```

### 3. Insert Test Member with Your Phone Number

Open PowerShell Terminal #2:

```powershell
cd CallistraAgent
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; UPDATE Members SET PhoneNumber = '+1234567890', Status = 'Active' WHERE Id = 1;"
```

Replace `+1234567890` with your actual phone number in E.164 format (e.g., `+12025551234` for US numbers).

**Verify the update**:

```powershell
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; SELECT Id, FirstName, LastName, PhoneNumber, Status FROM Members WHERE Id = 1;"
```

### 4. Start Azure Functions

In the same Terminal #2:

```powershell
cd ..\src\CallistraAgent.Functions
func start
```

Wait for the output:
```
Functions:
        CallEventWebhookFunction: [POST] http://localhost:7071/api/calls/events
        CallStatusFunction: [GET] http://localhost:7071/api/calls/status/{callConnectionId}
        InitiateCallFunction: [POST] http://localhost:7071/api/calls/initiate/{memberId}
```

### 5. Initiate Test Call

Open PowerShell Terminal #3:

```powershell
curl -X POST http://localhost:7071/api/calls/initiate/1
```

**Expected Response** (202 Accepted):

```json
{
  "callSessionId": 1,
  "memberId": 1,
  "status": "Initiated",
  "startTime": "2026-01-10T16:42:00Z",
  "callbackUrl": "https://your-tunnel-id-7071.devtunnels.ms/api/calls/events"
}
```

### 6. Answer Your Phone

📱 **Your phone should ring** from the Azure Communication Services number (the one configured in `local.settings.json`).

**Answer the call** to verify the connection works.

### 7. Verify Call Session

While on the call (or after), check the call status:

```powershell
curl http://localhost:7071/api/calls/status/<callConnectionId>
```

Replace `<callConnectionId>` with the value from the Azure Functions logs or database.

**Expected Response**:

```json
{
  "id": 1,
  "memberId": 1,
  "callConnectionId": "acsCallConnection...",
  "status": "Connected",
  "startTime": "2026-01-10T16:42:00Z",
  "endTime": null,
  "durationSeconds": null,
  "member": {
    "id": 1,
    "fullName": "John Doe",
    "phoneNumber": "+15555551234"
  },
  "responses": []
}
```

### 8. Check Database

Verify the call session was created:

```powershell
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; SELECT Id, MemberId, CallConnectionId, Status, StartTime FROM CallSessions ORDER BY Id DESC;"
```

## What Should Happen

### ✅ Success Indicators

1. **Terminal #1** (devtunnel): Shows "Ready to accept connections"
2. **Terminal #2** (Functions): Shows logs:
   ```
   [2026-01-10T16:42:01] Executing HTTP request: POST /api/calls/initiate/1
   [2026-01-10T16:42:02] Call initiated successfully for member 1
   ```
3. **Your Phone**: Rings from ACS number
4. **After answering**: Terminal #2 shows webhook received:
   ```
   [2026-01-10T16:42:05] Executing HTTP request: POST /api/calls/events
   [2026-01-10T16:42:05] Processing event: CallConnected
   ```
5. **Database**: `CallSessions` table has new row with `Status = 'Connected'`

### ❌ Common Issues

#### Phone doesn't ring

**Check**:
1. Member phone number is correct and in E.164 format (`+1234567890`)
2. ACS phone number (`AzureCommunicationServices__PhoneNumber`) is correct
3. ACS connection string is valid
4. Azure Functions logs show "Call initiated successfully"

**Debug**:
```powershell
# Check member data
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; SELECT * FROM Members WHERE Id = 1;"

# Check Functions logs for errors
# Look in Terminal #2 for error messages
```

#### Webhook events not received

**Check**:
1. Terminal #1 (devtunnel) is still running
2. `--allow-anonymous` flag was used in devtunnel host command
3. `CallbackBaseUrl` in `local.settings.json` matches devtunnel URL exactly (no trailing slash)

**Debug**:
```powershell
# Test webhook endpoint directly
curl https://your-tunnel-url.devtunnels.ms/api/calls/events

# Check devtunnel inspection URL (shown in Terminal #1 output)
# Open in browser: https://your-tunnel-id-7071-inspect.eun1.devtunnels.ms
```

#### Database errors

**Error**: "Could not save changes because the target table has database triggers"

**Solution**: This was fixed in `CallistraAgentDbContext.cs`. Restart Azure Functions:
```powershell
# In Terminal #2, press Ctrl+C then:
func start
```

**Error**: "Cannot insert duplicate key in object 'dbo.CallSessions'"

**Cause**: Active call already exists for this member.

**Solution**: Wait for previous call to end, or test with a different member:
```powershell
curl -X POST http://localhost:7071/api/calls/initiate/2
```

## Test Scenarios

### Scenario 1: Complete Call Flow

1. Initiate call → Phone rings
2. Answer call → Status changes to "Connected"
3. Hang up → Status changes to "Disconnected", `EndTime` set

**Verify in database**:
```sql
SELECT Id, Status, StartTime, EndTime,
       DATEDIFF(SECOND, StartTime, EndTime) AS DurationSeconds
FROM CallSessions
WHERE Id = 1;
```

### Scenario 2: No Answer

1. Initiate call → Phone rings
2. Let it ring (don't answer)
3. After timeout → Status changes to "NoAnswer"

### Scenario 3: Multiple Calls

1. Make first call, answer, hang up
2. Make second call to same member
3. Both calls should be in database with different IDs

**Verify**:
```sql
SELECT Id, MemberId, Status, StartTime
FROM CallSessions
WHERE MemberId = 1
ORDER BY StartTime DESC;
```

## Next Steps

After confirming User Story 1 (call initiation) works:

1. **Run unit tests** (Phase 3 remaining tasks T066-T071)
2. **Implement User Story 2**: Healthcare questions with TTS (Phase 4)
3. **Implement User Story 3**: Response capture with DTMF (Phase 5)

See [tasks.md](specs/001-minimal-call-agent/tasks.md) for the complete task list.

## Cleanup

### Stop Services

1. **Terminal #2** (Functions): Press `Ctrl+C`
2. **Terminal #1** (devtunnel): Press `Ctrl+C`

### Clear Test Data (Optional)

```powershell
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; DELETE FROM CallSessions; DELETE FROM Members;"
```

Then re-run `insert-test-data.sql` to reset.

## Monitoring During Development

### Watch Azure Functions Logs

Terminal #2 shows detailed execution logs. Look for:
- `Executing HTTP request`: API calls received
- `Call initiated successfully`: ACS call started
- `Processing event: CallConnected`: Webhook received
- Any exception stack traces

### Monitor Devtunnel Traffic

Open the inspection URL in a browser (shown in Terminal #1):
```
https://your-tunnel-id-7071-inspect.devtunnels.ms
```

This shows:
- All HTTP requests to your tunnel
- Request/response headers
- Timing information
- Payload bodies

### Query Database

Keep a SQL query window open:
```powershell
sqlcmd -S localhost -E -C -Q "USE CallistraAgent; SELECT TOP 10 * FROM CallSessions ORDER BY Id DESC;"
```

Run after each test to verify data persistence.

## Tips for Effective Testing

1. **Use separate terminals**: One for devtunnel, one for Functions, one for commands
2. **Keep devtunnel running**: The tunnel closes when the process stops
3. **Restart Functions after config changes**: `local.settings.json` changes require restart
4. **Check all three places**: Logs (Terminal #2), Database (SQL), Network (devtunnel inspection)
5. **Test incrementally**: Verify each step works before moving to the next

## Support

- **Dev Tunnels**: See [DEVTUNNEL-SETUP.md](DEVTUNNEL-SETUP.md)
- **Database**: See [CallistraAgent/QUICKSTART-DATABASE.md](CallistraAgent/QUICKSTART-DATABASE.md)
- **Azure Communication Services**: https://learn.microsoft.com/en-us/azure/communication-services/
