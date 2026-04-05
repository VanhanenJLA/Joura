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
az group create --name rg-joura-dev --location westeurope
```

Deploy with inline parameters:

```bash
az deployment group create \
  --resource-group rg-joura-dev \
  --template-file ops/azure/main.bicep \
  --parameters \
    env=dev \
    projectName=joura \
    sqlAdministratorLogin=jouraadmin \
    sqlAdministratorPassword='<strong-password>' \
    allowedClientIp='<your-public-ip>'
```

Or deploy with the parameter file:

```bash
az deployment group create \
  --resource-group rg-joura-dev \
  --parameters ops/azure/main.dev.bicepparam \
  --template-file ops/azure/main.bicep \
  --parameters sqlAdministratorPassword='<strong-password>'
```

## Naming

The deployment derives names automatically:

- App Service plan: `asp-<projectName>-<env>`
- Web app: `app-<projectName>-<env>-<suffix>`
- SQL server: `sql-<projectName>-<env>-<suffix>`
- SQL database: `sqldb-<projectName>-<env>`

The suffix is generated from subscription ID, resource group ID, project name, and environment so names stay stable for the same target resource group.

## Tear down

Delete the resource group:

```bash
az group delete --name rg-joura-dev --yes --no-wait
```
