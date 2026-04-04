@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short prefix used for resource naming.')
@minLength(3)
@maxLength(12)
param namePrefix string = 'joura'

@description('Globally unique Web App name.')
param webAppName string = '${namePrefix}-${uniqueString(subscription().id, resourceGroup().id, 'web')}'

@description('App Service plan name.')
param appServicePlanName string = '${namePrefix}-plan'

@description('App Service SKU. F1 is the free tier and best suited only for demo/dev usage.')
@allowed([
  'F1'
  'B1'
])
param appServiceSkuName string = 'F1'

@description('Logical SQL server name. Must be globally unique.')
param sqlServerName string = '${namePrefix}-${uniqueString(subscription().id, resourceGroup().id, 'sql')}'

@description('Database name for Joura.')
param sqlDatabaseName string = 'joura'

@description('SQL administrator login.')
@minLength(1)
param sqlAdministratorLogin string

@description('SQL administrator password.')
@secure()
param sqlAdministratorPassword string

@description('Optional client IPv4 address allowed through the SQL firewall, for example 203.0.113.10. Leave blank to skip.')
param allowedClientIp string = ''

@description('Tag values applied to all resources.')
param tags object = {
  app: 'Joura'
  environment: 'demo'
  managedBy: 'bicep'
}

var isFreePlan = appServiceSkuName == 'F1'
var sqlConnectionString = 'Server=tcp:${sqlServer.name}.${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdministratorLogin};Password=${sqlAdministratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'windows'
  sku: {
    name: appServiceSkuName
  }
  tags: tags
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      use32BitWorkerProcess: true
      alwaysOn: !isFreePlan
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'DatabaseProvider'
          value: 'SqlServer'
        }
        {
          name: 'ConnectionStrings__SqlServer'
          value: sqlConnectionString
        }
      ]
    }
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
    version: '12.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    createMode: 'Default'
    freeLimitExhaustionBehavior: 'AutoPause'
    maxSizeBytes: 34359738368
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
    useFreeLimit: true
    zoneRedundant: false
  }
}

resource azureServicesFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource clientFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01' = if (!empty(allowedClientIp)) {
  parent: sqlServer
  name: 'AllowNamedClientIp'
  properties: {
    startIpAddress: allowedClientIp
    endIpAddress: allowedClientIp
  }
}

output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
