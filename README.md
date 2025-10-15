# Mercury

A .NET Aspire-based form submission and notification system powered by Azure Functions, Cosmos DB, and Service Bus.

## Overview

Mercury is a serverless application built with .NET Aspire that handles form submissions and notifications. It consists of two Azure Functions that work together to process and notify about form submissions.

### Architecture

```
┌─────────────────┐        ┌──────────────────┐
│   Submission    │        │   Notification   │
│    Function     │◄──────►│     Function     │
│  (HTTP Trigger) │        │ (Queue Trigger)  │
└────────┬────────┘        └────────┬─────────┘
         │                          │
         ▼                          ▼
    ┌─────────────────────────────────────┐
    │     Cosmos DB (Serverless)          │
    │          forms-db                   │
    └─────────────────────────────────────┘
                     │
                     ▼
    ┌─────────────────────────────────────┐
    │     Service Bus (Standard)          │
    │      submissions queue              │
    └─────────────────────────────────────┘
```

## Features

- **Form Submission Processing**: HTTP-triggered function to handle form submissions
- **Notification System**: Queue-triggered function for processing notifications
- **Serverless Database**: Cosmos DB with serverless billing
- **Message Queue**: Azure Service Bus for reliable message processing
- **Monitoring**: Application Insights for telemetry and logging
- **Secure**: Managed Identity for all Azure service authentication

## Technology Stack

- **.NET 10.0**: Latest .NET runtime
- **Azure Functions V4**: Isolated worker model
- **Azure Cosmos DB**: NoSQL serverless database
- **Azure Service Bus**: Message queue
- **Application Insights**: Monitoring and telemetry
- **.NET Aspire**: Cloud-native orchestration

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure subscription](https://azure.microsoft.com/free/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for local development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/Nodsoft/Mercury.git
   cd Mercury
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run with Aspire**
   ```bash
   dotnet run --project Nodsoft.Mercury.AppHost
   ```

4. **Access the Aspire Dashboard**
   - Open browser to `http://localhost:15888`
   - View running services, logs, and traces

### Azure Deployment

See **[DEPLOYMENT.md](DEPLOYMENT.md)** for comprehensive deployment guide.

#### Quick Deploy

**Using GitHub Actions:**
1. Configure GitHub secrets (see [DEPLOYMENT.md](DEPLOYMENT.md#prerequisites))
2. Push to `main` branch or trigger workflow manually

**Using Scripts:**
```bash
# Bash (Linux/macOS/WSL)
./scripts/deploy-azure.sh

# PowerShell (Windows)
.\scripts\deploy-azure.ps1
```

## Project Structure

```
Mercury/
├── .github/
│   └── workflows/              # CI/CD workflows
├── infra/                      # Infrastructure as Code (Bicep)
├── scripts/                    # Deployment and utility scripts
├── Nodsoft.Mercury.AppHost/    # Aspire orchestration
├── Nodsoft.Mercury.Functions.Submission/
│   └── Functions/              # HTTP-triggered functions
├── Nodsoft.Mercury.Functions.Notification/
│   └── NotificationFunctions.cs # Queue-triggered functions
├── Nodsoft.Mercury.Data/       # Data access layer
├── Nodsoft.Mercury.Models/     # Shared models
└── Nodsoft.Mercury.ServiceDefaults/ # Aspire service defaults
```

## Configuration

### Local Development

The AppHost configures all services with emulators:
- Cosmos DB emulator
- Service Bus emulator
- Azure Storage emulator

### Production

Environment variables are configured automatically during deployment:
- `ConnectionStrings__forms-db`: Cosmos DB endpoint
- `ConnectionStrings__submissions-queue`: Service Bus namespace
- `ConnectionStrings__notifications-mq`: Service Bus namespace

All connections use **Managed Identity** (no connection strings with keys).

## Monitoring

### Application Insights

Monitor your application in Azure Portal:
1. Navigate to Application Insights resource: `mercury-ai-live`
2. View:
   - Live Metrics
   - Failures and exceptions
   - Performance metrics
   - Custom logs

### Function Logs

```bash
# View live logs
az functionapp log tail \
  --resource-group mercury-live \
  --name <function-name>
```

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

### Linting

Infrastructure validation:
```bash
bicep build infra/main.bicep
```

Shell script linting:
```bash
shellcheck scripts/*.sh
```

## CI/CD

### Workflows

- **[deploy-azure.yml](.github/workflows/deploy-azure.yml)**: Main deployment pipeline
- **[validate-infra.yml](.github/workflows/validate-infra.yml)**: Infrastructure validation
- **[deploy-aspire.yml](.github/workflows/deploy-aspire.yml)**: Aspire-based deployment (future)

### Deployment Process

1. Code pushed to `main`
2. Infrastructure deployed via Bicep
3. Functions built and packaged
4. Functions deployed to Azure
5. Configuration applied

## Cleanup

Remove all Azure resources:

```bash
# Bash
./scripts/cleanup-azure.sh

# PowerShell
.\scripts\cleanup-azure.ps1

# Azure CLI
az group delete --name mercury-live --yes
```

## Documentation

- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Complete deployment guide
- **[infra/README.md](infra/README.md)**: Infrastructure documentation
- **[.github/DEPLOYMENT_SUMMARY.md](.github/DEPLOYMENT_SUMMARY.md)**: Quick reference

## Troubleshooting

### Common Issues

**Build Errors**: Ensure .NET 10.0 SDK is installed
```bash
dotnet --list-sdks
```

**Deployment Failures**: Check GitHub Actions logs and Azure Portal

**Function Issues**: Review Application Insights for errors

See [DEPLOYMENT.md](DEPLOYMENT.md#troubleshooting) for detailed troubleshooting.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

[Add your license here]

## Support

- GitHub Issues: Report bugs and feature requests
- Documentation: See [DEPLOYMENT.md](DEPLOYMENT.md)
- Azure Support: [Azure Support Portal](https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade)

## Acknowledgments

Built with:
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Functions](https://azure.microsoft.com/services/functions/)
- [Azure Cosmos DB](https://azure.microsoft.com/services/cosmos-db/)
- [Azure Service Bus](https://azure.microsoft.com/services/service-bus/)
