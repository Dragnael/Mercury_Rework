#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-mercury-live}"

echo -e "${YELLOW}Mercury Azure Cleanup Script${NC}"
echo "============================="
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure${NC}"
    echo "Running: az login"
    az login
fi

SUBSCRIPTION=$(az account show --query name -o tsv)
echo -e "${GREEN}Using subscription: $SUBSCRIPTION${NC}"
echo ""

# Check if resource group exists
echo -e "${YELLOW}Checking if resource group exists...${NC}"
if ! az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
    echo -e "${GREEN}Resource group '$RESOURCE_GROUP' does not exist. Nothing to clean up.${NC}"
    exit 0
fi

# List resources in the group
echo -e "${YELLOW}Resources in '$RESOURCE_GROUP':${NC}"
az resource list --resource-group "$RESOURCE_GROUP" --query "[].{Name:name, Type:type}" --output table
echo ""

# Confirm deletion
echo -e "${RED}WARNING: This will permanently delete all resources in '$RESOURCE_GROUP'${NC}"
read -p "Are you sure you want to continue? (yes/no): " -r
echo

if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${GREEN}Cleanup cancelled.${NC}"
    exit 0
fi

# Delete resource group
echo -e "${YELLOW}Deleting resource group '$RESOURCE_GROUP'...${NC}"
az group delete \
    --name "$RESOURCE_GROUP" \
    --yes \
    --no-wait

echo -e "${GREEN}✓ Resource group deletion initiated${NC}"
echo ""
echo "The resource group is being deleted in the background."
echo "This may take several minutes to complete."
echo ""
echo "Check deletion status with:"
echo "  az group exists --name $RESOURCE_GROUP"
echo ""
