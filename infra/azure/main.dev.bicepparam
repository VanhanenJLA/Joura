using './main.bicep'

param env = 'dev'
param projectName = 'joura'
param appServiceSkuName = 'F1'
param sqlAdministratorLogin = 'admin'

// Fill these before deployment.
param sqlAdministratorPassword = ''
param allowedClientIp = ''
