#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-mercury-live}"
LOCATION="${AZURE_LOCATION:-eastus}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo -e "${GREEN}Mercury Azure Deployment Script${NC}"
echo "=================================="
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
echo -e "${YELLOW}Checking Azure login status...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure${NC}"
    echo "Running: az login"
    az login
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo -e "${GREEN}Using subscription: $SUBSCRIPTION${NC}"
echo ""

# Create resource group
echo -e "${YELLOW}Creating resource group...${NC}"
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none

echo -e "${GREEN}✓ Resource group created${NC}"
echo ""

# Deploy infrastructure
echo -e "${YELLOW}Deploying infrastructure with Bicep...${NC}"
DEPLOYMENT_NAME="mercury-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --template-file "$PROJECT_ROOT/infra/main.bicep" \
    --parameters "$PROJECT_ROOT/infra/main.parameters.json" \
    --output none

echo -e "${GREEN}✓ Infrastructure deployed${NC}"
echo ""

# Get deployment outputs
echo -e "${YELLOW}Retrieving deployment outputs...${NC}"
OUTPUTS=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs \
    --output json)

SUBMISSION_FUNC_NAME=$(echo "$OUTPUTS" | jq -r '.submissionFuncName.value')
NOTIFICATION_FUNC_NAME=$(echo "$OUTPUTS" | jq -r '.notificationFuncName.value')
COSMOS_ENDPOINT=$(echo "$OUTPUTS" | jq -r '.cosmosDbEndpoint.value')
SERVICE_BUS_NAMESPACE=$(echo "$OUTPUTS" | jq -r '.serviceBusNamespace.value')

echo "Submission Function: $SUBMISSION_FUNC_NAME"
echo "Notification Function: $NOTIFICATION_FUNC_NAME"
echo "Cosmos DB Endpoint: $COSMOS_ENDPOINT"
echo "Service Bus Namespace: $SERVICE_BUS_NAMESPACE"
echo ""

# Build and publish functions
echo -e "${YELLOW}Building Submission Function...${NC}"
dotnet publish "$PROJECT_ROOT/Nodsoft.Mercury.Functions.Submission/Nodsoft.Mercury.Functions.Submission.csproj" \
    --configuration Release \
    --output "$PROJECT_ROOT/publish/submission"

echo -e "${GREEN}✓ Submission Function built${NC}"
echo ""

echo -e "${YELLOW}Building Notification Function...${NC}"
dotnet publish "$PROJECT_ROOT/Nodsoft.Mercury.Functions.Notification/Nodsoft.Mercury.Functions.Notification.csproj" \
    --configuration Release \
    --output "$PROJECT_ROOT/publish/notification"

echo -e "${GREEN}✓ Notification Function built${NC}"
echo ""

# Create deployment packages
echo -e "${YELLOW}Creating deployment packages...${NC}"
cd "$PROJECT_ROOT/publish/submission" && zip -r ../submission.zip . > /dev/null
cd "$PROJECT_ROOT/publish/notification" && zip -r ../notification.zip . > /dev/null
cd "$PROJECT_ROOT"

echo -e "${GREEN}✓ Deployment packages created${NC}"
echo ""

# Deploy functions
echo -e "${YELLOW}Deploying Submission Function...${NC}"
az functionapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$SUBMISSION_FUNC_NAME" \
    --src "$PROJECT_ROOT/publish/submission.zip" \
    --output none

echo -e "${GREEN}✓ Submission Function deployed${NC}"
echo ""

echo -e "${YELLOW}Deploying Notification Function...${NC}"
az functionapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$NOTIFICATION_FUNC_NAME" \
    --src "$PROJECT_ROOT/publish/notification.zip" \
    --output none

echo -e "${GREEN}✓ Notification Function deployed${NC}"
echo ""

# Cleanup
echo -e "${YELLOW}Cleaning up build artifacts...${NC}"
rm -rf "$PROJECT_ROOT/publish"
echo -e "${GREEN}✓ Build artifacts cleaned${NC}"
echo ""

# Display results
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Resource Group: $RESOURCE_GROUP"
echo ""
echo "Function Apps:"
echo "  - Submission: https://$SUBMISSION_FUNC_NAME.azurewebsites.net"
echo "  - Notification: https://$NOTIFICATION_FUNC_NAME.azurewebsites.net"
echo ""
echo "Next steps:"
echo "  1. Configure any environment-specific app settings"
echo "  2. Verify functions are running: az functionapp list --resource-group $RESOURCE_GROUP"
echo "  3. Monitor logs in Application Insights"
echo ""
