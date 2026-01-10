# Dev Tunnels Setup Guide

This guide explains how to set up Microsoft Dev Tunnels for local development with Azure Communication Services webhooks.

## Overview

Azure Communication Services (ACS) needs to send webhook events to your locally running Azure Functions. Dev Tunnels creates a secure tunnel from the public internet to your local development machine, enabling ACS to reach your webhooks.

## Prerequisites

- Windows 10/11 or Windows Server
- Microsoft account or GitHub account for authentication
- Azure Functions running locally on port 7071

## Installation

### Option 1: Windows Package Manager (Recommended)

```powershell
winget install Microsoft.devtunnel
```

### Option 2: PowerShell Script

```powershell
Invoke-WebRequest -Uri https://aka.ms/TunnelsCliDownload/win-x64 -OutFile devtunnel.exe
```

Then add `devtunnel.exe` to your PATH or use `.\devtunnel.exe` for commands.

## Setup Steps

### 1. Verify Installation

```powershell
devtunnel --version
```

Expected output:
```
Tunnel CLI version: 1.0.1516+7e996fe917
Tunnel service URI: https://global.rel.tunnels.api.visualstudio.com/
...
```

### 2. Login with GitHub (Recommended for Corporate Networks)

**⚠️ IMPORTANT**: Use the `-g` flag for GitHub authentication if you encounter OAuth errors:

```powershell
devtunnel user login -g
```

This opens a browser window for GitHub authentication.

**Alternative**: If `-g` fails, try device code flow:

```powershell
devtunnel user login -d
```

This provides a code to enter at https://microsoft.com/devicelogin

### 3. Create the Tunnel

```powershell
devtunnel host -p 7071 --allow-anonymous
```

**Flags explained**:
- `-p 7071`: Port where Azure Functions runs locally
- `--allow-anonymous`: **CRITICAL** - Allows Azure Communication Services to send webhook events without authentication

### 4. Copy the Tunnel URL

After running the command, you'll see output like:

```
Hosting port: 7071
Connect via browser: https://abc123-7071.devtunnels.ms
Inspect network activity: https://abc123-7071-inspect.devtunnels.ms

Ready to accept connections for tunnel: your-tunnel-name
```

**Copy the first URL** (ending in `.devtunnels.ms`) - this is your public webhook endpoint.

### 5. Update Configuration

Edit `src/CallistraAgent.Functions/local.settings.json`:

```json
{
  "Values": {
    "AzureCommunicationServices__CallbackBaseUrl": "https://your-tunnel-id-7071.devtunnels.ms"
  }
}
```

Replace with your actual tunnel URL (no trailing slash).

### 6. Start Azure Functions

**IMPORTANT**: Keep the devtunnel command running in one terminal window.

Open a **new PowerShell terminal** and start Azure Functions:

```powershell
cd src/CallistraAgent.Functions
func start
```

### 7. Test the Setup

In a **third terminal**, test call initiation:

```powershell
curl -X POST http://localhost:7071/api/calls/initiate/1
```

Your phone should ring, and ACS webhook events will be sent to:
```
https://your-tunnel-id-7071.devtunnels.ms/api/calls/events
```

## Troubleshooting

### Error: "Request not permitted. Unauthorized tunnel creation access"

**Cause**: Not logged in or session expired.

**Solution**:
```powershell
devtunnel user login -g
```

### Error: OAuth/invalid_request during login

**Cause**: Standard OAuth flow blocked by corporate network or security policies.

**Solutions** (try in order):
1. Use GitHub authentication: `devtunnel user login -g`
2. Use device code flow: `devtunnel user login -d`
3. Clear cached credentials: `devtunnel user logout` then retry login

### Tunnel URL changes every time

**Cause**: Dev Tunnels generates a new tunnel ID by default.

**Solution**: Create a persistent tunnel (optional):

```powershell
# Create persistent tunnel
devtunnel create -a

# Note the tunnel ID from output, then host it:
devtunnel host <tunnel-id> -p 7071 --allow-anonymous
```

Update `local.settings.json` with the persistent URL once, and reuse the same `host` command.

### Webhook events not received

**Checklist**:
1. ✅ Devtunnel command still running?
2. ✅ Azure Functions running on port 7071?
3. ✅ `--allow-anonymous` flag used in host command?
4. ✅ `CallbackBaseUrl` in `local.settings.json` matches tunnel URL exactly?
5. ✅ Test the webhook endpoint directly:
   ```powershell
   curl https://your-tunnel-url.devtunnels.ms/api/calls/events
   ```

### Antivirus blocking devtunnel

**Cause**: Some antivirus software blocks tunneling tools.

**Solution**: Dev Tunnels is the official Microsoft solution. Add exception in antivirus or contact IT security with this documentation: https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/security

## Network Inspection

Dev Tunnels provides an inspection URL to debug webhook traffic:

```
https://your-tunnel-id-7071-inspect.devtunnels.ms
```

Open this URL in a browser to see:
- All incoming HTTP requests
- Request headers and body
- Response codes and timing

**Use this to verify ACS webhook events are reaching your endpoint.**

## Best Practices

### During Development

1. **Keep devtunnel running**: The tunnel closes when the command stops
2. **Use separate terminals**: One for devtunnel, one for Azure Functions
3. **Monitor the inspection URL**: Catch webhook issues quickly
4. **Restart Functions after config changes**: Changes to `local.settings.json` require restart

### For Team Development

1. **Each developer needs their own tunnel**: URLs are unique per developer
2. **Document the setup process**: Share this guide with your team
3. **Use persistent tunnels**: Reduces config updates when tunnel ID changes
4. **Add to .gitignore**: Never commit tunnel URLs to version control

### Security Considerations

- **`--allow-anonymous` is for local development only**: Never use in production
- **Tunnel URLs are temporary**: They expire when not in use
- **Dev Tunnels authenticates the developer**: Only you can create tunnels for your account
- **Monitor your tunnels**: Use `devtunnel list` to see active tunnels

## Production Deployment

**⚠️ Dev Tunnels are for local development only.**

For production:
1. Deploy Azure Functions to Azure App Service or Container Apps
2. Use the Azure-hosted URL (e.g., `https://callistra-agent.azurewebsites.net`) as `CallbackBaseUrl`
3. Configure ACS webhook URLs to point to production endpoint
4. Use managed identities for authentication between services

## References

- [Dev Tunnels Official Documentation](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/)
- [Dev Tunnels CLI Reference](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/cli-commands)
- [Dev Tunnels Security](https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/security)
- [Azure Communication Services Webhooks](https://learn.microsoft.com/en-us/azure/communication-services/concepts/call-automation/call-automation-webhook-events)

## Quick Reference

```powershell
# Login (GitHub - recommended for corporate networks)
devtunnel user login -g

# Start tunnel for port 7071
devtunnel host -p 7071 --allow-anonymous

# Check active tunnels
devtunnel list

# Logout
devtunnel user logout
```

## Support

- **Dev Tunnels Issues**: https://github.com/microsoft/dev-tunnels/issues
- **Azure Communication Services**: https://learn.microsoft.com/en-us/answers/tags/170/azure-communication-services
