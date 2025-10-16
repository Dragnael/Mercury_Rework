@description('The location for all resources')
param location string = resourceGroup().location

@description('The name prefix for all resources')
param namePrefix string = 'mercury'

@description('The environment name (e.g., dev, prod)')
param environment string = 'live'

// Variables
var uniqueSuffix = uniqueString(resourceGroup().id)
var cosmosDbAccountName = 'cosmos-${namePrefix}-${environment}-${uniqueSuffix}'
var serviceBusNamespaceName = 'mq-${namePrefix}-${environment}-${uniqueSuffix}'
var storageAccountName = toLower(substring('${namePrefix}${environment}${uniqueSuffix}', 0, 24))
var appInsightsName = 'appin-${namePrefix}-${environment}'
var logAnalyticsName = 'logs-${namePrefix}-${environment}'
var submissionFuncName = 'func-${namePrefix}-submission-${environment}-${uniqueSuffix}'
var notificationFuncName = 'func-${namePrefix}-notification-${environment}-${uniqueSuffix}'
var submissionPlanName = 'plan-${namePrefix}-submission-${environment}'
var notificationPlanName = 'plan-${namePrefix}-notification-${environment}'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

// CosmosDB Account (Serverless)
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

// CosmosDB Database
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosDbAccount
  name: 'forms-db'
  properties: {
    resource: {
      id: 'forms-db'
    }
  }
}

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

// Service Bus Queue
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBusNamespace
  name: 'submissions'
  properties: {
    lockDuration: 'PT5M'
    requiresDuplicateDetection: false
    requiresSession: false
    //     defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 10
  }
}

// App Service Plan for Submission Function (Flex Consumption)
resource submissionPlan 'Microsoft.Web/serverfarms@2024-11-01' = {
  name: submissionPlanName
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// App Service Plan for Notification Function (Flex Consumption)
resource notificationPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: notificationPlanName
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// Submission Function App
resource submissionFunc 'Microsoft.Web/sites@2023-12-01' = {
  name: submissionFuncName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: submissionPlan.id
    functionAppConfig: {
      runtime: {
				name: 'dotnet-isolated'
				version: '10.0' 
			}
    }
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__forms-db'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'ConnectionStrings__submissions-queue'
          value: '${serviceBusNamespace.name}.servicebus.windows.net'
        }
      ]
      cors: {
        allowedOrigins: ['*']
      }
      use32BitWorkerProcess: false
    }
    httpsOnly: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Notification Function App
resource notificationFunc 'Microsoft.Web/sites@2023-12-01' = {
  name: notificationFuncName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: notificationPlan.id
		functionAppConfig: {
			runtime: {
				name: 'dotnet-isolated'
				version: '10.0' 
			}
		}
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${az.environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ConnectionStrings__forms-db'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'ConnectionStrings__notifications-mq'
          value: '${serviceBusNamespace.name}.servicebus.windows.net'
        }
      ]
      use32BitWorkerProcess: false
    }
    httpsOnly: true
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Assign Cosmos DB Data Contributor role to Submission Function
resource submissionCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosDbAccount
  name: guid(cosmosDbAccount.id, submissionFunc.id, 'CosmosDBDataContributor')
  properties: {
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDbAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: submissionFunc.identity.principalId
    scope: cosmosDbAccount.id
  }
}

// Assign Cosmos DB Data Contributor role to Notification Function
resource notificationCosmosRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosDbAccount
  name: guid(cosmosDbAccount.id, notificationFunc.id, 'CosmosDBDataContributor')
  properties: {
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDbAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    principalId: notificationFunc.identity.principalId
    scope: cosmosDbAccount.id
  }
}

// Assign Service Bus Data Owner role to Submission Function
resource submissionServiceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, submissionFunc.id, 'ServiceBusDataOwner')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '090c5cfd-751d-490a-894a-3ce6f1109419'
    )
    principalId: submissionFunc.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Assign Service Bus Data Owner role to Notification Function
resource notificationServiceBusRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(serviceBusNamespace.id, notificationFunc.id, 'ServiceBusDataOwner')
  scope: serviceBusNamespace
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '090c5cfd-751d-490a-894a-3ce6f1109419'
    )
    principalId: notificationFunc.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output submissionFuncName string = submissionFunc.name
output notificationFuncName string = notificationFunc.name
output cosmosDbEndpoint string = cosmosDbAccount.properties.documentEndpoint
output serviceBusNamespace string = '${serviceBusNamespace.name}.servicebus.windows.net'
output appInsightsConnectionString string = appInsights.properties.ConnectionString
