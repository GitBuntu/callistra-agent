# Callistra-Agent

## 📞 What It Does

Makes automated phone calls to healthcare members, asks questions, captures responses.

### The Basics
- Call people using Azure Communication Services
- Ask 3-5 healthcare questions
- Record their yes/no answers
- Save to database

### Stack
- **Azure Functions** (.NET 8) - 3 HTTP endpoints
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
- **InitiateCall** - Starts the call
- **HandleCallConnected** - Plays questions when connected
- **HandleResponse** - Captures answers

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for local database)
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
# Start SQL Server container
cd db
docker compose up -d

# Verify connection
cd ../backend/src/CallAgent.Api
dotnet ef database update
```

### 3. Build the Project
```bash
# Using VS Code task (recommended)
# Press Ctrl+Shift+P → "Tasks: Run Task" → "build (functions)"

# Or manually
cd backend/src/CallAgent.Api
dotnet build
```

### 4. Run Locally
```bash
# Using VS Code task
# Press Ctrl+Shift+P → "Tasks: Run Task" → "func: 4"

# Or manually
cd backend/src/CallAgent.Api/bin/Debug/net8.0
func host start
```

The API will be available at `http://localhost:7071`

## 🔧 Development Workflow

### Key Commands
- **Build**: `dotnet build` or VS Code task "build (functions)"
- **Run**: `func host start` or VS Code task "func: 4"
- **Database**: `docker compose up` from `/db` directory
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
backend/src/CallAgent.Api/
├── Data/                    # Entity Framework context and migrations
├── Functions/              # Azure Functions endpoints
├── Models/                 # DTOs, entities, and configuration
├── Repositories/           # Data access layer
├── Services/               # Business logic layer
└── appsettings.json        # Configuration

tests/                      # Unit and integration tests
db/                        # Database setup and seed data
docs/                      # Documentation
frontend/                  # Web interface (future)
```

## 📡 API Endpoints

### Health & System
- `GET /api/health` - System health check
- `GET /api/system/health` - Detailed system status

### Member Management
- `GET /api/members` - List members
- `GET /api/members/{id}` - Get member details
- `POST /api/members` - Create new member
- `PUT /api/members/{id}` - Update member

### Call Session Management
- `GET /api/callsessions` - List call sessions
- `GET /api/callsessions/{id}` - Get call session details
- `POST /api/callsessions` - Initiate new call session
- `POST /api/callsessions/{id}/complete` - Complete call session

### Speech Processing
- `POST /api/speech/recognize` - Speech-to-text recognition
- `POST /api/speech/synthesize` - Text-to-speech synthesis

### ACS Integration (Phase 4.1)
- `POST /api/acs/events/{memberId}/{callSessionId}` - Handle call events
- `POST /api/acs/question/{memberId}/{callSessionId}/{questionNumber}` - Process questions with speech recognition

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
  "CallAgent": {
    "Database": {
      "ConnectionString": "Server=localhost,1433;Database=CallAgent;User Id=sa;Password=CallAgent123!;"
    },
    "FeatureFlags": {
      "IsCallRecordingEnabled": true,
      "IsAcsIntegrationEnabled": true,
      "IsSpeechServicesEnabled": true
    }
  },
  "SpeechServices": {
    "SubscriptionKey": "your-cognitive-services-key",
    "Region": "eastus"
  },
  "CommunicationServices": {
    "ConnectionString": "your-acs-connection-string"
  }
}
```

### Feature Flags
- `IsCallRecordingEnabled`: Enable/disable call recording
- `IsAcsIntegrationEnabled`: Enable Azure Communication Services
- `IsTwilioIntegrationEnabled`: Enable Twilio integration
- `IsSpeechServicesEnabled`: Enable Azure Speech Services for voice recognition

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
- Database: `docker compose up` in `/db`
- Functions: `func host start` in `/backend/src/CallAgent.Api/bin/Debug/net8.0`
- Frontend: `npm start` in `/frontend` (future)

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

