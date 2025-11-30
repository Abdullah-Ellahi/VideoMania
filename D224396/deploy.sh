#!/bin/bash

# VideoMania Deployment Script to Microsoft Azure
# This script deploys the application to Azure App Service

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}VideoMania Azure Deployment${NC}"
echo -e "${YELLOW}========================================${NC}"

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ dotnet CLI is not installed${NC}"
    exit 1
fi

if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI is not installed${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Prerequisites verified${NC}"

# Configuration
APP_SERVICE_NAME=${1:-"videomania-appservice"}
RESOURCE_GROUP=${2:-"videomania-rg"}
LOCATION=${3:-"eastus"}
STORAGE_ACCOUNT_NAME="videomaniastorage"
COSMOS_DB_ACCOUNT_NAME="videomania-cosmos"

echo -e "${YELLOW}Deployment Configuration:${NC}"
echo "  App Service: $APP_SERVICE_NAME"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Storage Account: $STORAGE_ACCOUNT_NAME"
echo "  Cosmos DB Account: $COSMOS_DB_ACCOUNT_NAME"

# Step 1: Login to Azure
echo -e "${YELLOW}Step 1: Logging in to Azure...${NC}"
az login

# Step 2: Create Resource Group
echo -e "${YELLOW}Step 2: Creating Resource Group...${NC}"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" || true

# Step 3: Deploy ARM Template
echo -e "${YELLOW}Step 3: Deploying Azure Resources...${NC}"
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file arm-template.json \
  --parameters appServiceName="$APP_SERVICE_NAME" \
               storageAccountName="$STORAGE_ACCOUNT_NAME" \
               cosmosDbAccountName="$COSMOS_DB_ACCOUNT_NAME" \
               location="$LOCATION"

# Step 4: Get connection strings
echo -e "${YELLOW}Step 4: Retrieving connection strings...${NC}"

STORAGE_CONNECTION_STRING=$(az storage account show-connection-string \
  --name "$STORAGE_ACCOUNT_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query connectionString -o tsv)

echo -e "${YELLOW}Step 5: Configuring App Service settings...${NC}"

# Set environment variables for the app
az webapp config appsettings set \
  --name "$APP_SERVICE_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --settings BLOB_STORAGE_CONNECTION_STRING="$STORAGE_CONNECTION_STRING" \
             COSMOS_DB_ACCOUNT="$(echo $COSMOS_DB_ACCOUNT_NAME | tr '[:upper:]' '[:lower:]').documents.azure.com" \
             WEBSITE_WEBDEPLOY_USE_SCM=true

echo -e "${YELLOW}Step 6: Building the application...${NC}"
cd ICC.AzureAppService.Demo
dotnet publish -c Release -o ./bin/Release/publish

echo -e "${YELLOW}Step 7: Deploying to Azure App Service...${NC}"
cd ./bin/Release/publish
az webapp up --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" --runtime "dotnet8"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}✓ Deployment completed successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Application URL: https://${APP_SERVICE_NAME}.azurewebsites.net${NC}"
