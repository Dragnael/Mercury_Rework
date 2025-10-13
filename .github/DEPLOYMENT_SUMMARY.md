# Azure Deployment Setup - Quick Reference

## What Was Added

This PR adds comprehensive CI/CD infrastructure for deploying the Mercury Aspire stack to Azure.

### 📁 File Structure

```
.
├── .github/workflows/
│   ├── deploy-azure.yml          # Main deployment workflow
│   ├── deploy-aspire.yml          # Aspire-based deployment (future)
│   └── validate-infra.yml         # Infrastructure validation
├── infra/
│   ├── main.bicep                 # Infrastructure as Code (Bicep)
│   ├── main.parameters.json       # Deployment parameters
│   └── README.md                  # Infrastructure documentation
├── scripts/
│   ├── deploy-azure.sh            # Bash deployment script
│   ├── deploy-azure.ps1           # PowerShell deployment script
│   ├── cleanup-azure.sh           # Bash cleanup script
│   ├── cleanup-azure.ps1          # PowerShell cleanup script
│   └── validate-deployment.sh     # Deployment validation script
├── azure.yaml                     # Azure Developer CLI config
├── DEPLOYMENT.md                  # Comprehensive deployment guide
└── Nodsoft.Mercury.AppHost/
    └── appsettings.Production.json # Production settings
```

## 🚀 Quick Start

### Prerequisites
1. Azure subscription
2. GitHub repository secrets configured:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

### Deploy

**Option 1: GitHub Actions (Recommended)**
- Push to `main` branch or manually trigger "Deploy to Azure" workflow

**Option 2: Local Deployment**
```bash
# Bash
./scripts/deploy-azure.sh

# PowerShell
.\scripts\deploy-azure.ps1
```

## 📦 Azure Resources

All resources deployed to: **`mercury-live`** resource group

| Resource | Type | Configuration |
|----------|------|---------------|
| Submission Function | Azure Functions | Flex Consumption, .NET 10.0 Isolated |
| Notification Function | Azure Functions | Flex Consumption, .NET 10.0 Isolated |
| Cosmos DB | Database | Serverless, NoSQL API |
| Service Bus | Messaging | Standard tier, `submissions` queue |
| Application Insights | Monitoring | Integrated with Log Analytics |
| Storage Account | Storage | Standard LRS |

## 🔐 Security

- **Managed Identity**: Functions use system-assigned managed identity
- **RBAC**: Cosmos DB Data Contributor and Service Bus Data Owner roles assigned
- **No Keys**: Connection strings use managed identity authentication
- **HTTPS Only**: All endpoints enforce HTTPS

## 📊 Monitoring

View metrics and logs in:
- **Application Insights**: `mercury-ai-live`
- **Function App logs**: Live stream in Azure Portal
- **Cosmos DB metrics**: Request units and throttling

## 🧪 Validation

Validate deployment:
```bash
./scripts/validate-deployment.sh
```

Clean up resources:
```bash
./scripts/cleanup-azure.sh
```

## 📚 Documentation

- **[DEPLOYMENT.md](DEPLOYMENT.md)**: Complete deployment guide
- **[infra/README.md](infra/README.md)**: Infrastructure details
- **[Azure Aspire Docs](https://learn.microsoft.com/dotnet/aspire/)**

## ✅ What's Configured

- [x] Azure Functions (Flex Consumption) for both functions
- [x] Cosmos DB (Serverless)
- [x] Service Bus with submissions queue
- [x] Application Insights monitoring
- [x] Managed Identity authentication
- [x] Automated CI/CD pipeline
- [x] Infrastructure validation
- [x] Deployment scripts (cross-platform)
- [x] Comprehensive documentation

## 🔄 Workflow

1. **Code Push** → GitHub Actions triggered
2. **Infrastructure Deployed** → Bicep templates create resources
3. **Functions Built** → .NET 10.0 build
4. **Functions Deployed** → Zip deployment to Azure
5. **Settings Configured** → Connection strings and app settings

## 🛠️ Customization

Edit deployment parameters:
```json
// infra/main.parameters.json
{
  "namePrefix": "mercury",
  "environment": "live"
}
```

Override defaults:
```bash
export AZURE_RESOURCE_GROUP="mercury-staging"
export AZURE_LOCATION="westus2"
./scripts/deploy-azure.sh
```

## 🐛 Troubleshooting

Common issues and solutions in [DEPLOYMENT.md](DEPLOYMENT.md#troubleshooting)

## 📞 Support

- Review GitHub Actions logs
- Check Application Insights
- Consult [Azure Functions Docs](https://learn.microsoft.com/azure/azure-functions/)
