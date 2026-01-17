# Azurite Setup for Local Development

Azure Functions require Azure Storage for internal operations (queues, blobs, state management). For local development, use **Azurite** - the official Azure Storage Emulator.

## Why Azurite?

Your `local.settings.json` contains:
```json
"AzureWebJobsStorage": "UseDevelopmentStorage=true"
```

This tells Azure Functions to use a local storage emulator instead of a real Azure Storage account. Azurite provides this emulator.

---

## Option 1: VS Code Extension (Recommended) ⭐

**Easiest setup for VS Code users**

### Installation

1. Open VS Code
2. Press **Ctrl+Shift+P** (Command Palette)
3. Type: `Extensions: Install Extensions`
4. Search for: **Azurite**
5. Install the extension by **Microsoft**

OR click "Install Azurite" when VS Code prompts you on F5 debug.

### Usage

**Start Azurite:**
1. Press **Ctrl+Shift+P**
2. Type: `Azurite: Start`
3. Press Enter

**Stop Azurite:**
1. Press **Ctrl+Shift+P**
2. Type: `Azurite: Close`

**Status:** Look for Azurite status in the bottom status bar.

### Auto-Start (Optional)

Add to VS Code `settings.json`:
```json
{
  "azurite.silent": true,
  "azurite.location": "${workspaceFolder}/.azurite"
}
```

---

## Option 2: npm Global Install

**Good for command-line users or CI/CD**

### Installation

```bash
npm install -g azurite
```

### Usage

**Start Azurite (default ports):**
```bash
azurite
```

**Start with custom location:**
```bash
azurite --location ./azurite-data
```

**Silent mode (no console output):**
```bash
azurite --silent
```

**Stop:** Press **Ctrl+C** in the terminal

---

## Option 3: Docker

**Best for isolated environments or multiple projects**

### Run Azurite Container

```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

### Run with Persistent Storage

```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  -v ${PWD}/azurite-data:/data \
  mcr.microsoft.com/azure-storage/azurite
```

### Docker Compose

Add to `docker-compose.yml`:
```yaml
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"
      - "10001:10001"
      - "10002:10002"
    volumes:
      - ./azurite-data:/data
```

Start with:
```bash
docker-compose up -d azurite
```

---

## Ports Used by Azurite

- **10000**: Blob service
- **10001**: Queue service
- **10002**: Table service

Azure Functions primarily uses the Blob and Queue services.

---

## Troubleshooting

### "Failed to verify AzureWebJobsStorage" error
- Azurite is not running
- Solution: Start Azurite using one of the options above

### Port already in use
- Another instance of Azurite is running
- Solution: Stop other instances or use custom ports:
  ```bash
  azurite --blobPort 10100 --queuePort 10101 --tablePort 10102
  ```

### Data persistence
- By default, Azurite stores data in memory (lost on restart)
- For persistent data, specify a location:
  ```bash
  azurite --location ./azurite-data
  ```

---

## Debugging Workflow

1. **Start Azurite** (choose one method above)
2. **Press F5** in VS Code to start debugging
3. Set breakpoints in your Functions code
4. **Make API calls** to test your functions

---

## Production Note

⚠️ **Azurite is for local development only**

In production Azure environments, use a real Azure Storage account:
```json
"AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=..."
```

Store production connection strings in **Azure Key Vault** or **App Configuration**.
