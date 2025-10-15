# Mercury - CI/CD Deployment Guide

This guide explains how to deploy the Mercury Aspire stack to Azure using the provided CI/CD infrastructure.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Deployment Methods](#deployment-methods)
- [Configuration](#configuration)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

## Overview

Mercury is deployed to Azure as a .NET Aspire application with the following components:

- **Two Azure Functions (Flex Consumption)**:
  - Submission Function: Handles form submissions
  - Notification Function: Processes notifications
  
- **Azure Cosmos DB (Serverless)**: NoSQL database for forms data

- **Azure Service Bus**: Message queue for submissions

- **Application Insights**: Monitoring and telemetry

All resources are deployed to a single resource group: **`mercury-live`**

## Prerequisites

### For Automated Deployment (GitHub Actions)

1. **Azure Subscription**: Active Azure subscription
2. **Azure Service Principal**: For GitHub Actions authentication
3. **GitHub Repository Secrets**: Configured authentication secrets

### For Manual Deployment

1. **Azure CLI**: [Install Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
2. **.NET SDK**: .NET 10.0 or later
3. **Azure Subscription**: Active Azure subscription with appropriate permissions

## Quick Start

### 1. Set Up Azure Service Principal

Create a service principal for GitHub Actions:

```bash
az ad sp create-for-rbac --name "mercury-github-actions" \
  --role contributor \
  --scopes /subscriptions/{your-subscription-id} \
  --json-auth
```

This command outputs JSON credentials. You'll need the following values:
- `clientId`
- `tenantId`
- `subscriptionId`

### 2. Configure GitHub Secrets

Add these secrets to your GitHub repository (Settings → Secrets and variables → Actions):

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `AZURE_CLIENT_ID` | Service principal client ID | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `abcdef01-2345-6789-abcd-ef0123456789` |

### 3. Deploy

#### Option A: Automatic Deployment (Recommended)

Push to the `main` branch or manually trigger the workflow:

1. Go to **Actions** tab in your GitHub repository
2. Select **"Deploy to Azure"** workflow
3. Click **"Run workflow"**
4. Monitor the deployment progress

#### Option B: Manual Deployment

**Using Bash (Linux/macOS):**
```bash
cd scripts
./deploy-azure.sh
```

**Using PowerShell (Windows):**
```powershell
cd scripts
.\deploy-azure.ps1
```

## Deployment Methods

### Method 1: GitHub Actions (Recommended)

The automated CI/CD pipeline handles everything:

**Workflow: `.github/workflows/deploy-azure.yml`**
- Triggers on push to `main` branch
- Can be manually triggered
- Deploys infrastructure and applications
- Configures all settings automatically

**Steps:**
1. Checkout code
2. Setup .NET (10.0 preview)
3. Login to Azure
4. Deploy infrastructure (Bicep)
5. Build Function Apps
6. Deploy Function Apps
7. Configure app settings

### Method 2: Manual Script Deployment

Use the provided scripts for local or manual deployment:

**Bash Script (`scripts/deploy-azure.sh`):**
- Full deployment automation
- Interactive with status updates
- Cross-platform (Linux/macOS/WSL)

**PowerShell Script (`scripts/deploy-azure.ps1`):**
- Windows-native deployment
- Same functionality as Bash script
- Colorful output and progress tracking

### Method 3: Azure Developer CLI (azd)

Future support for `azd` deployment:

```bash
# Initialize (one-time)
azd init

# Deploy
azd up
```

> **Note**: Full `azd` integration is planned for Aspire 9.x when deployment tools mature.

## Configuration

### Environment Variables

The deployment uses these environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `AZURE_RESOURCE_GROUP` | `mercury-live` | Target resource group name |
| `AZURE_LOCATION` | `eastus` | Azure region for deployment |

Override defaults by setting environment variables before running scripts:

```bash
export AZURE_RESOURCE_GROUP="mercury-staging"
export AZURE_LOCATION="westus2"
./scripts/deploy-azure.sh
```

### Infrastructure Parameters

Customize deployment in `infra/main.parameters.json`:

```json
{
  "parameters": {
    "namePrefix": {
      "value": "mercury"
    },
    "environment": {
      "value": "live"
    }
  }
}
```

### Azure Function Settings

Function Apps are configured with:

**Submission Function:**
- `ConnectionStrings__forms-db`: Cosmos DB endpoint
- `ConnectionStrings__submissions-queue`: Service Bus namespace

**Notification Function:**
- `ConnectionStrings__forms-db`: Cosmos DB endpoint  
- `ConnectionStrings__notifications-mq`: Service Bus namespace

All connections use **Managed Identity** for authentication (no keys required).

## Monitoring

### Application Insights

Monitor your application:

1. Navigate to Azure Portal
2. Open the Application Insights resource: `mercury-ai-live`
3. View:
   - **Live Metrics**: Real-time telemetry
   - **Failures**: Exceptions and errors
   - **Performance**: Request/response times
   - **Logs**: Custom application logs

### Function App Logs

**Azure Portal:**
1. Navigate to Function App
2. Go to **Log stream** for live logs
3. Use **Monitor** for historical data

**Azure CLI:**
```bash
# Tail logs
az functionapp log tail \
  --resource-group mercury-live \
  --name <function-app-name>

# View deployment logs
az functionapp log deployment list \
  --resource-group mercury-live \
  --name <function-app-name>
```

### Cosmos DB Metrics

1. Open Cosmos DB account in Azure Portal
2. Navigate to **Metrics**
3. Monitor:
   - Request units (RUs) consumed
   - Storage usage
   - Throttled requests

## Troubleshooting

### Common Issues

#### 1. Build Failures - .NET SDK Version

**Problem**: `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`

**Solution**: Ensure .NET 10.0 preview SDK is installed:
```bash
# Check installed SDKs
dotnet --list-sdks

# Install .NET 10 preview if needed
# Follow: https://dotnet.microsoft.com/download/dotnet/10.0
```

#### 2. Azure Login Failures

**Problem**: GitHub Actions fails with authentication error

**Solution**: 
- Verify GitHub secrets are correctly configured
- Ensure service principal has `Contributor` role
- Check if service principal hasn't expired

#### 3. Resource Name Conflicts

**Problem**: Deployment fails with "resource name already exists"

**Solution**: Resource names include a unique suffix. If still failing:
- Delete the existing resource group: `az group delete --name mercury-live`
- Or change `namePrefix` in `infra/main.parameters.json`

#### 4. Function App Not Starting

**Problem**: Function app deployed but not responding

**Solution**:
1. Check Application Insights for errors
2. Verify Managed Identity permissions:
   ```bash
   az functionapp identity show \
     --resource-group mercury-live \
     --name <function-name>
   ```
3. Ensure connection strings are configured
4. Review function logs for startup errors

#### 5. Cosmos DB Connection Issues

**Problem**: Functions can't connect to Cosmos DB

**Solution**:
1. Verify Managed Identity has Cosmos DB Data Contributor role
2. Check connection string format (should be endpoint only, not full connection string)
3. Ensure firewall rules allow Azure services

### Getting Help

1. **Check Logs**: Application Insights and Function App logs
2. **GitHub Actions**: Review workflow run logs
3. **Azure Status**: Check [Azure status page](https://status.azure.com/)
4. **Documentation**: 
   - [.NET Aspire Docs](https://learn.microsoft.com/dotnet/aspire/)
   - [Azure Functions Docs](https://learn.microsoft.com/azure/azure-functions/)

## Cleanup

To remove all deployed resources:

```bash
az group delete --name mercury-live --yes --no-wait
```

**Warning**: This permanently deletes all resources and data.

## Advanced Topics

### Multi-Environment Deployment

Deploy to multiple environments:

```bash
# Staging
export AZURE_RESOURCE_GROUP="mercury-staging"
./scripts/deploy-azure.sh

# Production
export AZURE_RESOURCE_GROUP="mercury-production"
./scripts/deploy-azure.sh
```

### Custom Domain Configuration

Add custom domains to Function Apps:

```bash
az functionapp config hostname add \
  --resource-group mercury-live \
  --name <function-name> \
  --hostname <custom-domain>
```

### Scaling Configuration

Modify Function App scaling in `infra/main.bicep`:

```bicep
properties: {
  functionAppScaleLimit: 10  // Max instances
  minimumElasticInstanceCount: 1  // Always warm instances
}
```

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│                  GitHub Actions                      │
│              (CI/CD Workflow)                        │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────┐
│           Azure Resource Group                       │
│              (mercury-live)                          │
│                                                      │
│  ┌──────────────────┐      ┌──────────────────┐   │
│  │  Submission      │      │  Notification    │   │
│  │  Function        │◄────►│  Function        │   │
│  │  (Flex)          │      │  (Flex)          │   │
│  └────────┬─────────┘      └─────────┬────────┘   │
│           │                           │             │
│           ▼                           ▼             │
│  ┌──────────────────────────────────────────┐     │
│  │         Cosmos DB (Serverless)            │     │
│  │              forms-db                      │     │
│  └──────────────────────────────────────────┘     │
│                                                      │
│  ┌──────────────────────────────────────────┐     │
│  │         Service Bus Namespace             │     │
│  │         └─ submissions queue              │     │
│  └──────────────────────────────────────────┘     │
│                                                      │
│  ┌──────────────────────────────────────────┐     │
│  │      Application Insights                 │     │
│  │      (Monitoring & Telemetry)             │     │
│  └──────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────┘
```

## References

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Azure Functions Flex Consumption](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan)
- [Cosmos DB Serverless](https://learn.microsoft.com/azure/cosmos-db/serverless)
- [Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/)
- [Bicep Templates](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
