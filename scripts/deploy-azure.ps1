# Mercury Azure Deployment Script (PowerShell)
# Requires: Azure CLI, .NET SDK

param(
    [string]$ResourceGroup = $env:AZURE_RESOURCE_GROUP ?? "mercury-live",
    [string]$Location = $env:AZURE_LOCATION ?? "eastus"
)

$ErrorActionPreference = "Stop"

# Colors
$Green = [ConsoleColor]::Green
$Yellow = [ConsoleColor]::Yellow
$Red = [ConsoleColor]::Red

Write-Host "Mercury Azure Deployment Script" -ForegroundColor $Green
Write-Host "==================================" -ForegroundColor $Green
Write-Host "Resource Group: $ResourceGroup"
Write-Host "Location: $Location"
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Azure CLI is not installed" -ForegroundColor $Red
    Write-Host "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
Write-Host "Checking Azure login status..." -ForegroundColor $Yellow
try {
    $null = az account show 2>&1
} catch {
    Write-Host "Not logged in to Azure" -ForegroundColor $Red
    Write-Host "Running: az login"
    az login
}

$Subscription = az account show --query name -o tsv
Write-Host "Using subscription: $Subscription" -ForegroundColor $Green
Write-Host ""

# Create resource group
Write-Host "Creating resource group..." -ForegroundColor $Yellow
az group create `
    --name $ResourceGroup `
    --location $Location `
    --output none

Write-Host "✓ Resource group created" -ForegroundColor $Green
Write-Host ""

# Deploy infrastructure
Write-Host "Deploying infrastructure with Bicep..." -ForegroundColor $Yellow
$DeploymentName = "mercury-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

az deployment group create `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --template-file "$ProjectRoot/infra/main.bicep" `
    --parameters "$ProjectRoot/infra/main.parameters.json" `
    --output none

Write-Host "✓ Infrastructure deployed" -ForegroundColor $Green
Write-Host ""

# Get deployment outputs
Write-Host "Retrieving deployment outputs..." -ForegroundColor $Yellow
$Outputs = az deployment group show `
    --resource-group $ResourceGroup `
    --name $DeploymentName `
    --query properties.outputs `
    --output json | ConvertFrom-Json

$SubmissionFuncName = $Outputs.submissionFuncName.value
$NotificationFuncName = $Outputs.notificationFuncName.value
$CosmosEndpoint = $Outputs.cosmosDbEndpoint.value
$ServiceBusNamespace = $Outputs.serviceBusNamespace.value

Write-Host "Submission Function: $SubmissionFuncName"
Write-Host "Notification Function: $NotificationFuncName"
Write-Host "Cosmos DB Endpoint: $CosmosEndpoint"
Write-Host "Service Bus Namespace: $ServiceBusNamespace"
Write-Host ""

# Build and publish functions
Write-Host "Building Submission Function..." -ForegroundColor $Yellow
dotnet publish "$ProjectRoot/Nodsoft.Mercury.Functions.Submission/Nodsoft.Mercury.Functions.Submission.csproj" `
    --configuration Release `
    --output "$ProjectRoot/publish/submission"

Write-Host "✓ Submission Function built" -ForegroundColor $Green
Write-Host ""

Write-Host "Building Notification Function..." -ForegroundColor $Yellow
dotnet publish "$ProjectRoot/Nodsoft.Mercury.Functions.Notification/Nodsoft.Mercury.Functions.Notification.csproj" `
    --configuration Release `
    --output "$ProjectRoot/publish/notification"

Write-Host "✓ Notification Function built" -ForegroundColor $Green
Write-Host ""

# Create deployment packages
Write-Host "Creating deployment packages..." -ForegroundColor $Yellow
Compress-Archive -Path "$ProjectRoot/publish/submission/*" -DestinationPath "$ProjectRoot/publish/submission.zip" -Force
Compress-Archive -Path "$ProjectRoot/publish/notification/*" -DestinationPath "$ProjectRoot/publish/notification.zip" -Force

Write-Host "✓ Deployment packages created" -ForegroundColor $Green
Write-Host ""

# Deploy functions
Write-Host "Deploying Submission Function..." -ForegroundColor $Yellow
az functionapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $SubmissionFuncName `
    --src "$ProjectRoot/publish/submission.zip" `
    --output none

Write-Host "✓ Submission Function deployed" -ForegroundColor $Green
Write-Host ""

Write-Host "Deploying Notification Function..." -ForegroundColor $Yellow
az functionapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $NotificationFuncName `
    --src "$ProjectRoot/publish/notification.zip" `
    --output none

Write-Host "✓ Notification Function deployed" -ForegroundColor $Green
Write-Host ""

# Cleanup
Write-Host "Cleaning up build artifacts..." -ForegroundColor $Yellow
Remove-Item -Path "$ProjectRoot/publish" -Recurse -Force
Write-Host "✓ Build artifacts cleaned" -ForegroundColor $Green
Write-Host ""

# Display results
Write-Host "========================================" -ForegroundColor $Green
Write-Host "Deployment Complete!" -ForegroundColor $Green
Write-Host "========================================" -ForegroundColor $Green
Write-Host ""
Write-Host "Resource Group: $ResourceGroup"
Write-Host ""
Write-Host "Function Apps:"
Write-Host "  - Submission: https://$SubmissionFuncName.azurewebsites.net"
Write-Host "  - Notification: https://$NotificationFuncName.azurewebsites.net"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Configure any environment-specific app settings"
Write-Host "  2. Verify functions are running: az functionapp list --resource-group $ResourceGroup"
Write-Host "  3. Monitor logs in Application Insights"
Write-Host ""
