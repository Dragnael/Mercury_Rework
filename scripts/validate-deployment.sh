#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-mercury-live}"

echo -e "${BLUE}Mercury Azure Deployment Validation${NC}"
echo "====================================="
echo "Resource Group: $RESOURCE_GROUP"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}Not logged in to Azure${NC}"
    exit 1
fi

# Check if resource group exists
echo -e "${YELLOW}Checking resource group...${NC}"
if ! az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
    echo -e "${RED}✗ Resource group '$RESOURCE_GROUP' does not exist${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Resource group exists${NC}"
echo ""

# Get all resources
echo -e "${YELLOW}Validating resources...${NC}"
RESOURCES=$(az resource list --resource-group "$RESOURCE_GROUP" --query "[].{Name:name, Type:type, Status:provisioningState}" --output json)

# Function to check resource
check_resource() {
    local type=$1
    local display_name=$2
    local count
    
    count=$(echo "$RESOURCES" | jq -r "[.[] | select(.Type | contains(\"$type\"))] | length")
    
    if [ "$count" -gt 0 ]; then
        echo -e "${GREEN}✓ $display_name found ($count)${NC}"
        echo "$RESOURCES" | jq -r ".[] | select(.Type | contains(\"$type\")) | \"  - \" + .Name + \" (\" + .Status + \")\""
        return 0
    else
        echo -e "${RED}✗ $display_name not found${NC}"
        return 1
    fi
}

# Validate each resource type
echo ""
check_resource "Microsoft.Web/sites" "Function Apps"
echo ""
check_resource "Microsoft.DocumentDB/databaseAccounts" "Cosmos DB Account"
echo ""
check_resource "Microsoft.ServiceBus/namespaces" "Service Bus Namespace"
echo ""
check_resource "Microsoft.Storage/storageAccounts" "Storage Account"
echo ""
check_resource "Microsoft.Insights/components" "Application Insights"
echo ""

# Get Function App URLs
echo -e "${YELLOW}Function App Endpoints:${NC}"
FUNC_APPS=$(az functionapp list --resource-group "$RESOURCE_GROUP" --query "[].{Name:name, State:state, Url:defaultHostName}" --output json)

echo "$FUNC_APPS" | jq -r '.[] | "  \(.Name):"'
echo "$FUNC_APPS" | jq -r '.[] | "    State: \(.State)"'
echo "$FUNC_APPS" | jq -r '.[] | "    URL: https://\(.Url)"'
echo ""

# Check Function App health
echo -e "${YELLOW}Checking Function App health...${NC}"
for func_name in $(echo "$FUNC_APPS" | jq -r '.[].Name'); do
    url="https://$(echo "$FUNC_APPS" | jq -r ".[] | select(.Name==\"$func_name\") | .Url")"
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "200\|401\|404"; then
        echo -e "${GREEN}✓ $func_name is responding${NC}"
    else
        echo -e "${YELLOW}⚠ $func_name may not be fully started yet${NC}"
    fi
done
echo ""

# Check Cosmos DB
echo -e "${YELLOW}Checking Cosmos DB...${NC}"
COSMOS_ACCOUNT=$(az cosmosdb list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)
if [ -n "$COSMOS_ACCOUNT" ]; then
    COSMOS_STATUS=$(az cosmosdb show --resource-group "$RESOURCE_GROUP" --name "$COSMOS_ACCOUNT" --query "provisioningState" -o tsv)
    COSMOS_ENDPOINT=$(az cosmosdb show --resource-group "$RESOURCE_GROUP" --name "$COSMOS_ACCOUNT" --query "documentEndpoint" -o tsv)
    
    echo "  Account: $COSMOS_ACCOUNT"
    echo "  Status: $COSMOS_STATUS"
    echo "  Endpoint: $COSMOS_ENDPOINT"
    
    # Check for databases
    DB_COUNT=$(az cosmosdb sql database list --account-name "$COSMOS_ACCOUNT" --resource-group "$RESOURCE_GROUP" --query "length(@)" -o tsv)
    echo "  Databases: $DB_COUNT"
    
    if [ "$DB_COUNT" -gt 0 ]; then
        echo -e "${GREEN}✓ Cosmos DB is configured${NC}"
    else
        echo -e "${YELLOW}⚠ No databases found in Cosmos DB${NC}"
    fi
fi
echo ""

# Check Service Bus
echo -e "${YELLOW}Checking Service Bus...${NC}"
SB_NAMESPACE=$(az servicebus namespace list --resource-group "$RESOURCE_GROUP" --query "[0].name" -o tsv)
if [ -n "$SB_NAMESPACE" ]; then
    SB_STATUS=$(az servicebus namespace show --resource-group "$RESOURCE_GROUP" --name "$SB_NAMESPACE" --query "provisioningState" -o tsv)
    
    echo "  Namespace: $SB_NAMESPACE"
    echo "  Status: $SB_STATUS"
    
    # Check for queues
    QUEUE_COUNT=$(az servicebus queue list --namespace-name "$SB_NAMESPACE" --resource-group "$RESOURCE_GROUP" --query "length(@)" -o tsv)
    echo "  Queues: $QUEUE_COUNT"
    
    if [ "$QUEUE_COUNT" -gt 0 ]; then
        az servicebus queue list --namespace-name "$SB_NAMESPACE" --resource-group "$RESOURCE_GROUP" --query "[].name" -o tsv | while read queue; do
            echo "    - $queue"
        done
        echo -e "${GREEN}✓ Service Bus is configured${NC}"
    else
        echo -e "${YELLOW}⚠ No queues found in Service Bus${NC}"
    fi
fi
echo ""

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Validation Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Resource Group: $RESOURCE_GROUP"
echo "Total Resources: $(echo "$RESOURCES" | jq 'length')"
echo ""
echo "Next steps:"
echo "  1. Test Function App endpoints"
echo "  2. Verify Application Insights data"
echo "  3. Check function logs for any errors"
echo "  4. Monitor Cosmos DB and Service Bus metrics"
echo ""
