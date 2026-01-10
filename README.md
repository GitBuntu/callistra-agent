# Callistra-Agent Call Agent API

## 📞 Overview

**Callistra-Agent** is an intelligent healthcare outreach call center platform that automates member engagement through **Azure Speech Services** and **Azure Communication Services**. It enables organizations to conduct large-scale, interactive voice outreach campaigns with natural language understanding and real-time response capture.

### 🎯 Project Goals
- Automate healthcare program member outreach at scale
- Provide interactive voice response (IVR) with **natural speech recognition** powered by Azure Cognitive Services
- Capture detailed responses and engagement metrics from each call
- Ensure data compliance and auditability for healthcare scenarios
- Enable flexible, configurable call flows for different programs and use cases

### ⚡ Core Capabilities
Built with .NET 8, Azure Functions, Entity Framework Core, and SQL Server, featuring:

- **Azure Speech Services Integration**: Real-time speech-to-text and text-to-speech capabilities for natural voice interactions
- **Outbound Call Management**: Automated healthcare outreach to program members
- **Interactive Voice Response (IVR)**: Dynamic question flows with speech recognition and intelligent response processing
- **Multi-Channel Support**: Local harness for testing, Twilio, and Azure Communication Services integration
- **Real-time Call Tracking**: Comprehensive call session and response logging for compliance and analytics
- **Healthcare Program Management**: Member enrollment, segmentation, and program-specific call flows
- **Enterprise Security**: Feature flags, role-based configuration, and audit logging
- **Scalable Architecture**: Repository pattern, Unit of Work, and dependency injection for maintainability

## 🏗️ Architecture

### Core Components
- **Members**: Healthcare program participants to be contacted
- **CallSessions**: Individual call attempts with status tracking
- **CallResponses**: Q&A interactions captured during calls

### Technology Stack
- **Backend**: Azure Functions v4 (.NET 8)
- **Voice & Speech**: 
  - **Azure Speech Services** (Cognitive Services) for speech-to-text and text-to-speech
  - **Azure Communication Services** (Call Automation) for outbound calling and PSTN integration
- **Database**: SQL Server with Entity Framework Core
- **Configuration**: Options pattern with feature flags
- **Testing**: xUnit for unit tests, integration tests

### Design Patterns
- Repository + Unit of Work Pattern
- Dependency Injection
- Configuration-Driven Features
- Structured Logging

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

