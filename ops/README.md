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

Infrastructure is deployed manually with Bicep. Application releases are handled separately by the GitHub Actions workflow in `.github/workflows/ci-cd.yml`.

Create the resource group:

```bash
az group create --name rg-joura-dev --location westeurope
```

Deploy with inline parameters:

```bash
az deployment group create \
  --resource-group rg-joura-dev \
  --template-file ops/main.bicep \
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
  --parameters ops/main.dev.bicepparam \
  --template-file ops/main.bicep \
  --parameters sqlAdministratorPassword='<strong-password>'
```

## Application CI/CD

The `CI/CD` GitHub Actions workflow runs for pull requests and pushes to `main`. It checks formatting, builds the solution, and runs the Playwright UI suite against a temporary PostgreSQL database. A successful push to `main` is published and deployed to the provisioned Azure Web App.

Configure these repository Actions settings before the first deployment:

- Variable `AZURE_WEBAPP_NAME`: the `webAppName` output from the Bicep deployment.
- Secret `AZURE_WEBAPP_PUBLISH_PROFILE`: the publish profile downloaded from that Azure Web App.

The deployment job uses the GitHub environment named `production`. Add environment protection rules in GitHub if deployments should require a separate approval.

The application and its demo data are initialized on first startup. SQL Server currently uses `EnsureCreated()`, so schema upgrades are not yet a migration stage in the pipeline.

## Main Branch Protection

For the Full Stack Open CI/CD exercise, configure the `main` branch ruleset in GitHub to:

- require a pull request before merging
- require at least one approval
- require the `Build and test` status check
- prevent bypassing the rule, including by administrators

Repository rules and collaborator invitations are GitHub settings and cannot be represented by this workflow file. Add `mluukkai` as a collaborator, open the exercise pull request, and request their review as required by exercise 11.21.

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
