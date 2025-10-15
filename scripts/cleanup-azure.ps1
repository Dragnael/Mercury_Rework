# Mercury Azure Cleanup Script (PowerShell)

param(
    [string]$ResourceGroup = $env:AZURE_RESOURCE_GROUP ?? "mercury-live"
)

$ErrorActionPreference = "Stop"

# Colors
$Yellow = [ConsoleColor]::Yellow
$Green = [ConsoleColor]::Green
$Red = [ConsoleColor]::Red

Write-Host "Mercury Azure Cleanup Script" -ForegroundColor $Yellow
Write-Host "=============================" -ForegroundColor $Yellow
Write-Host "Resource Group: $ResourceGroup"
Write-Host ""

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Azure CLI is not installed" -ForegroundColor $Red
    Write-Host "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
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

# Check if resource group exists
Write-Host "Checking if resource group exists..." -ForegroundColor $Yellow
$exists = az group exists --name $ResourceGroup
if ($exists -eq "false") {
    Write-Host "Resource group '$ResourceGroup' does not exist. Nothing to clean up." -ForegroundColor $Green
    exit 0
}

# List resources in the group
Write-Host "Resources in '$ResourceGroup':" -ForegroundColor $Yellow
az resource list --resource-group $ResourceGroup --query "[].{Name:name, Type:type}" --output table
Write-Host ""

# Confirm deletion
Write-Host "WARNING: This will permanently delete all resources in '$ResourceGroup'" -ForegroundColor $Red
$confirmation = Read-Host "Are you sure you want to continue? (yes/no)"

if ($confirmation -ne "yes") {
    Write-Host "Cleanup cancelled." -ForegroundColor $Green
    exit 0
}

# Delete resource group
Write-Host "Deleting resource group '$ResourceGroup'..." -ForegroundColor $Yellow
az group delete `
    --name $ResourceGroup `
    --yes `
    --no-wait

Write-Host "✓ Resource group deletion initiated" -ForegroundColor $Green
Write-Host ""
Write-Host "The resource group is being deleted in the background."
Write-Host "This may take several minutes to complete."
Write-Host ""
Write-Host "Check deletion status with:"
Write-Host "  az group exists --name $ResourceGroup"
Write-Host ""
