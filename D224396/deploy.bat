@echo off
REM VideoMania Deployment Script to Microsoft Azure
REM This script deploys the application to Azure App Service

setlocal enabledelayedexpansion

REM Color codes equivalent (using echo messages)
echo.
echo ========================================
echo VideoMania Azure Deployment
echo ========================================
echo.

REM Check prerequisites
echo Checking prerequisites...

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: dotnet CLI is not installed
    exit /b 1
)

where az >nul 2>nul
if errorlevel 1 (
    echo ERROR: Azure CLI is not installed
    exit /b 1
)

echo Prerequisites verified

REM Configuration
set APP_SERVICE_NAME=%1
if "!APP_SERVICE_NAME!"=="" set APP_SERVICE_NAME=videomania-appservice

set RESOURCE_GROUP=%2
if "!RESOURCE_GROUP!"=="" set RESOURCE_GROUP=videomania-rg

set LOCATION=%3
if "!LOCATION!"=="" set LOCATION=eastus

set STORAGE_ACCOUNT_NAME=videomaniastorage
set COSMOS_DB_ACCOUNT_NAME=videomania-cosmos

echo Deployment Configuration:
echo   App Service: !APP_SERVICE_NAME!
echo   Resource Group: !RESOURCE_GROUP!
echo   Location: !LOCATION!
echo   Storage Account: !STORAGE_ACCOUNT_NAME!
echo   Cosmos DB Account: !COSMOS_DB_ACCOUNT_NAME!
echo.

REM Step 1: Login to Azure
echo Step 1: Logging in to Azure...
call az login
if errorlevel 1 (
    echo ERROR: Failed to login to Azure
    exit /b 1
)

REM Step 2: Create Resource Group
echo Step 2: Creating Resource Group...
call az group create --name "!RESOURCE_GROUP!" --location "!LOCATION!"

REM Step 3: Deploy ARM Template
echo Step 3: Deploying Azure Resources...
call az deployment group create ^
  --resource-group "!RESOURCE_GROUP!" ^
  --template-file arm-template.json ^
  --parameters appServiceName="!APP_SERVICE_NAME!" ^
               storageAccountName="!STORAGE_ACCOUNT_NAME!" ^
               cosmosDbAccountName="!COSMOS_DB_ACCOUNT_NAME!" ^
               location="!LOCATION!"

if errorlevel 1 (
    echo ERROR: Failed to deploy ARM template
    exit /b 1
)

REM Step 4: Get connection strings
echo Step 4: Retrieving connection strings...
for /f %%i in ('az storage account show-connection-string --name "!STORAGE_ACCOUNT_NAME!" --resource-group "!RESOURCE_GROUP!" --query connectionString -o tsv') do set STORAGE_CONNECTION_STRING=%%i

echo Step 5: Configuring App Service settings...

REM Set environment variables for the app
call az webapp config appsettings set ^
  --name "!APP_SERVICE_NAME!" ^
  --resource-group "!RESOURCE_GROUP!" ^
  --settings BLOB_STORAGE_CONNECTION_STRING="!STORAGE_CONNECTION_STRING!" ^
             COSMOS_DB_ACCOUNT="!COSMOS_DB_ACCOUNT_NAME!.documents.azure.com" ^
             WEBSITE_WEBDEPLOY_USE_SCM=true

echo Step 6: Building the application...
cd ICC.AzureAppService.Demo
call dotnet publish -c Release -o ./bin/Release/publish
if errorlevel 1 (
    echo ERROR: Failed to publish application
    exit /b 1
)

echo Step 7: Deploying to Azure App Service...
call az webapp up --name "!APP_SERVICE_NAME!" --resource-group "!RESOURCE_GROUP!" --runtime "dotnet8"
if errorlevel 1 (
    echo ERROR: Failed to deploy to App Service
    exit /b 1
)

echo.
echo ========================================
echo Deployment completed successfully!
echo ========================================
echo Application URL: https://!APP_SERVICE_NAME!.azurewebsites.net
echo.

endlocal
