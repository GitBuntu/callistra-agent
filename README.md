# Callistra-Agent

## 📞 What It Does

Makes automated phone calls to healthcare members, asks questions, captures responses.

### The Basics
- Call people using Azure Communication Services
- Ask 3 healthcare questions
- Record their yes/no answers
- Save to database

### Stack
- **Azure Functions** (.NET 9) - 3 HTTP endpoints
- **Azure Communication Services** - Makes the calls
- **SQL Server** - Stores responses
- **Entity Framework Core** - Database access

That's it.

## Quick Start

See [SIMPLE-IMPLEMENTATION.md](SIMPLE-IMPLEMENTATION.md) for the minimalist approach.

## Architecture

Three tables:
- **Members** - Who to call (name, phone, program)
- **CallSessions** - Call tracking (status, time)
- **CallResponses** - Q&A data (question, response)

Three functions:
- **InitiateCall** - Starts the call (POST /api/calls/initiate/{memberId})
- **CallEvents** - Webhook handler for call events (POST /api/calls/events)
- **CallStatus** - Query call session and responses (GET /api/calls/status/{callConnectionId})

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [SQL Server 2025](https://www.microsoft.com/sql-server/sql-server-downloads) (local installation)
- [Git](https://git-scm.com/)
- [Visual Studio Code](https://code.visualstudio.com/) (recommended)

## 🛠️ Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/GitBuntu/callistra-agent.git
cd callistra-agent
```

### 2. Database Setup
```bash
# Ensure SQL Server is running locally
# Create database using EF Core migrations
cd src/CallAgent.Functions
dotnet ef database update
```

### 3. Build the Project
```bash
# Using VS Code task (recommended)
# Press Ctrl+Shift+P → "Tasks: Run Task" → "build (functions)"

# Or manually
cd src/CallAgent.Functions
dotnet build
```

### 4. Run Locally
```bash
# Using VS Code task
# Press Ctrl+Shift+P → "Tasks: Run Task" → "func: 4"

# Or manually
cd src/CallAgent.Functions/bin/Debug/net9.0
func host start
```

The API will be available at `http://localhost:7071`

## 🔧 Development Workflow

### Key Commands
- **Build**: `dotnet build` or VS Code task "build (functions)"
- **Run**: `func host start` or VS Code task "func: 4"
- **Database**: Managed by local SQL Server installation
- **Publish**: `dotnet publish --configuration Release` or VS Code task "publish (functions)"

### Speech Services Configuration
Azure Speech Services requires configuration in `appsettings.json`:
```json
{
  "SpeechServices": {
    "SubscriptionKey": "your-speech-key",
    "Region": "eastus"
  }
}
```

### Project Structure
```
src/CallAgent.Functions/
├── Data/
│   ├── CallAgentDbContext.cs
│   ├── Entities/           # Member, CallSession, CallResponse
│   └── Migrations/         # EF Core migrations
├── Functions/              # Azure Functions HTTP triggers
│   ├── CallInitiation.cs   # POST /api/calls/initiate/{memberId}
│   ├── CallEvents.cs       # POST /api/calls/events
│   └── CallStatus.cs       # GET /api/calls/status/{callConnectionId}
├── Services/               # Business logic (CallService)
├── Models/                 # DTOs and configuration
├── Configuration/          # AcsOptions
├── host.json
└── local.settings.json

tests/CallAgent.Functions.Tests/
├── Unit/                   # Unit tests
└── Integration/            # Integration tests
```

## 📡 API Endpoints

### Core Endpoints (MVP)
- `POST /api/calls/initiate/{memberId}` - Initiate outbound call to member
- `POST /api/calls/events` - Webhook for Azure Communication Services call events
- `GET /api/calls/status/{callConnectionId}` - Query call session status and responses

### Future Endpoints (Out of Scope for MVP)
- Member Management (CRUD operations)
- Health checks
- Advanced speech processing

## 🧪 Testing

### Unit Tests
```bash
cd tests/Unit
dotnet test
```

### Integration Tests
```bash
cd tests/Integration
dotnet test
```

### API Tests
```bash
cd tests/api
# Use REST Client extension in VS Code or tools like Postman
```

## 🔐 Configuration

### Environment Variables
```json
{
  "ConnectionStrings": {
    "CallistraAgentDb": "Server=localhost;Database=CallistraAgent;Integrated Security=true;TrustServerCertificate=true;"
  },
  "AzureCommunicationServices": {
    "ConnectionString": "endpoint=https://your-acs-resource.communication.azure.com/;accesskey=your-key",
    "SourcePhoneNumber": "+18005551234",
    "CallbackBaseUrl": "https://your-function-app.azurewebsites.net"
  }
}
```

### Configuration Notes
- Database connection uses integrated security for local development
- Azure Communication Services connection string from Azure Portal
- Callback URL must be publicly accessible (use ngrok or Azure Dev Tunnels for local testing)

## 🚀 Deployment

### Azure Deployment
1. Create Azure resources:
   - Azure Function App
   - Azure SQL Database
   - Azure Communication Services (optional)

2. Configure application settings in Azure

3. Deploy using Azure CLI:
```bash
az functionapp deployment source config-zip \
  --resource-group <rg> \
  --name <function-app> \
  --src <zip-file>
```

### Local Development
- Database: Local SQL Server 2025 instance
- Functions: `func host start` in `/src/CallAgent.Functions/bin/Debug/net9.0`
- Webhook testing: Use ngrok or Azure Dev Tunnels for public callback URL

### Development Guidelines
- Follow existing code patterns and architecture
- Add unit tests for new functionality
- Update documentation as needed
- Use feature flags for new features
- Follow conventional commit messages

## 📚 Documentation

- [Configuration Management](./docs/configuration-management.md)
- [Configuration Troubleshooting](./docs/configuration-troubleshooting.md)
- [API Documentation](./tests/api/README.md)
- [Developer Guide](./.github/copilot-instructions.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Azure Functions team for the excellent serverless platform
- Azure Communication Services for enterprise voice capabilities
- .NET community for the robust framework

---

**Built with ❤️ for healthcare outreach programs**

