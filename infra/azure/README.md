# Azure Bicep

This folder contains a resource group-scoped Bicep deployment for Joura on Azure App Service plus Azure SQL Database.

## What it deploys

- Windows App Service plan on `F1` by default
- Windows Web App with production app settings
- Azure SQL logical server
- Azure SQL Database in serverless General Purpose
- SQL firewall rule allowing Azure services
- Optional SQL firewall rule for one client IP

## Free-tier intent

The template is tuned toward the Azure free offers that were current on April 5, 2026:

- App Service `F1` for the web app
- Azure SQL Database serverless with `useFreeLimit: true`
- `freeLimitExhaustionBehavior: AutoPause` so the database stops instead of billing over the free allowance

That is still only a small demo/dev footprint. The current Joura app is Blazor Server and Azure App Service Free remains a constrained target for that runtime.

## Important app note

The template sets:

- `DatabaseProvider=SqlServer`
- `ConnectionStrings__SqlServer=<generated Azure SQL connection string>`

Current Joura application code now supports provider selection. For `SqlServer`, startup currently uses `EnsureCreated()` rather than a dedicated SQL Server migration set, so the first deployment can create schema automatically but future cross-provider schema evolution should move to separate SQL Server migrations.

## Deploy

Create the resource group:

```bash
az group create --name rg-joura-demo --location westeurope
```

Deploy with inline parameters:

```bash
az deployment group create \
  --resource-group rg-joura-demo \
  --template-file infra/azure/main.bicep \
  --parameters \
    namePrefix=joura \
    webAppName=joura-demo-<unique-suffix> \
    sqlAdministratorLogin=jouraadmin \
    sqlAdministratorPassword='<strong-password>' \
    allowedClientIp='<your-public-ip>'
```

Or deploy with the parameter file:

```bash
az deployment group create \
  --resource-group rg-joura-demo \
  --parameters infra/azure/main.demo.bicepparam \
  --template-file infra/azure/main.bicep \
  --parameters \
    webAppName=joura-demo-<unique-suffix> \
    sqlAdministratorPassword='<strong-password>'
```

## Tear down

Delete the resource group:

```bash
az group delete --name rg-joura-demo --yes --no-wait
```
