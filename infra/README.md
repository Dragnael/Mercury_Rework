# Azure Deployment for Mercury

This directory contains the infrastructure-as-code (IaC) templates and CI/CD workflows for deploying the Mercury Aspire stack to Azure.

## Architecture

The deployment creates the following Azure resources in the `mercury-live` resource group:

- **Azure Functions (Flex Consumption)**:
  - `func-mercury-submission-live-{uniqueid}`: Handles form submissions
  - `func-mercury-notification-live-{uniqueid}`: Handles notifications

- **Azure Cosmos DB (Serverless)**:
  - Account: `cosmos-mercury-live-{uniqueid}`
  - Database: `forms-db`

- **Azure Service Bus**:
  - Namespace: `mq-mercury-live-{uniqueid}`
  - Queue: `submissions`

- **Supporting Services**:
  - Application Insights for monitoring
  - Log Analytics Workspace
  - Storage Account for Function Apps

## Prerequisites

Before deploying, you need to:

1. **Azure Subscription**: Have an active Azure subscription

2. **Azure Service Principal**: Create a service principal with Contributor role:
   ```bash
   az ad sp create-for-rbac --name "mercury-github-actions" \
     --role contributor \
     --scopes /subscriptions/{subscription-id} \
     --sdk-auth
   ```

3. **GitHub Secrets**: Configure the following secrets in your GitHub repository:
   - `AZURE_CLIENT_ID`: The client ID from the service principal
   - `AZURE_TENANT_ID`: Your Azure tenant ID
   - `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID

## Deployment

### Automatic Deployment (CI/CD)

The deployment is automatically triggered on:
- Push to the `main` branch
- Manual trigger via GitHub Actions

The workflow:
1. Creates the `mercury-live` resource group in France Central
2. Deploys infrastructure using Bicep templates
3. Builds and publishes both Function Apps
4. Deploys Function Apps to Azure
5. Configures connection strings and app settings

### Manual Deployment

You can also deploy manually using Azure CLI:

```bash
# Login to Azure
az login

# Create resource group
az group create --name mercury-live --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group mercury-live \
  --template-file ./infra/main.bicep \
  --parameters ./infra/main.parameters.json

# Build and publish functions
dotnet publish ./Nodsoft.Mercury.Functions.Submission/Nodsoft.Mercury.Functions.Submission.csproj \
  --configuration Release \
  --output ./publish/submission

dotnet publish ./Nodsoft.Mercury.Functions.Notification/Nodsoft.Mercury.Functions.Notification.csproj \
  --configuration Release \
  --output ./publish/notification

# Deploy functions
az functionapp deployment source config-zip \
  --resource-group mercury-live \
  --name <submission-func-name> \
  --src ./publish/submission.zip

az functionapp deployment source config-zip \
  --resource-group mercury-live \
  --name <notification-func-name> \
  --src ./publish/notification.zip
```

## Infrastructure Details

### Azure Functions - Flex Consumption Plan

Both Function Apps use the Flex Consumption plan which provides:
- Pay-per-execution pricing
- Automatic scaling
- .NET 10.0 isolated worker runtime
- Linux-based hosting

### Cosmos DB - Serverless

The Cosmos DB account is configured with:
- Serverless capacity mode (pay-per-operation)
- Session consistency level
- Single region deployment
- NoSQL API

### Authentication

The Function Apps use Managed Identity to authenticate to Azure services:
- **Cosmos DB**: Assigned "Cosmos DB Built-in Data Contributor" role
- **Service Bus**: Assigned "Azure Service Bus Data Owner" role

This eliminates the need for connection strings with keys.

## Monitoring

Application Insights is configured for both Function Apps, providing:
- Request/response telemetry
- Exception tracking
- Performance metrics
- Custom application logging

Access metrics through Azure Portal:
1. Navigate to the Application Insights resource
2. View metrics, logs, and performance data

## Troubleshooting

### Build Failures

If you encounter .NET SDK version issues:
- The project targets .NET 10.0 (preview)
- Ensure GitHub Actions uses the correct .NET SDK version
- Update the workflow's `DOTNET_VERSION` if needed

### Deployment Failures

Check the GitHub Actions logs for detailed error messages. Common issues:

1. **Missing Secrets**: Ensure all required secrets are configured
2. **Permission Issues**: Verify the service principal has Contributor role
3. **Resource Naming**: Azure resource names must be globally unique

### Function App Issues

1. **Check Application Insights**: View logs and exceptions
2. **Verify Settings**: Ensure connection strings are correctly configured
3. **Review Function Logs**: Use `az functionapp log tail` to view live logs

## Cleanup

To remove all deployed resources:

```bash
az group delete --name mercury-live --yes --no-wait
```

## Support

For issues or questions:
- Review GitHub Actions workflow logs
- Check Azure Portal for resource status
- Consult Azure Functions and Aspire documentation
