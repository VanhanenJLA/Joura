using './main.bicep'

param namePrefix = 'joura'
param appServiceSkuName = 'F1'
param sqlDatabaseName = 'joura'
param sqlAdministratorLogin = 'jouraadmin'

// Fill these before deployment.
param sqlAdministratorPassword = ''
param allowedClientIp = ''
