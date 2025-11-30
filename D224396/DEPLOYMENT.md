# Azure Deployment Guide - VideoMania

This guide provides step-by-step instructions to deploy the VideoMania serverless video streaming platform to Microsoft Azure.

## Prerequisites

- **Azure Subscription**: An active Azure account
- **Azure CLI**: Install from https://learn.microsoft.com/en-us/cli/azure/install-azure-cli
- **.NET 8 SDK**: Install from https://dotnet.microsoft.com/download/dotnet/8.0
- **Git**: Version control system
- **PowerShell 5.1+** (Windows) or **Bash** (Linux/Mac)

## Deployment Options

### Option 1: Automated Deployment (Recommended)

#### Windows (PowerShell)

```powershell
cd C:\VideoMania\D224396
.\deploy.bat "videomania-appservice" "videomania-rg" "eastus"
```

#### Linux/macOS (Bash)

```bash
cd /path/to/VideoMania/D224396
chmod +x deploy.sh
./deploy.sh "videomania-appservice" "videomania-rg" "eastus"
```

### Option 2: Manual Deployment

#### Step 1: Login to Azure

```bash
az login
```

#### Step 2: Create a Resource Group

```bash
az group create \
  --name videomania-rg \
  --location eastus
```

#### Step 3: Deploy Azure Resources using ARM Template

```bash
az deployment group create \
  --resource-group videomania-rg \
  --template-file arm-template.json \
  --parameters appServiceName="videomania-appservice" \
               storageAccountName="videomaniastorage" \
               cosmosDbAccountName="videomania-cosmos" \
               location="eastus"
```

#### Step 4: Get Connection Strings

```bash
# Get Storage Account Connection String
az storage account show-connection-string \
  --name videomaniastorage \
  --resource-group videomania-rg \
  --query connectionString -o tsv

# Get Cosmos DB Connection String (Primary Connection String)
az cosmosdb keys list \
  --name videomania-cosmos \
  --resource-group videomania-rg \
  --type connection-strings
```

#### Step 5: Configure App Service Settings

```bash
az webapp config appsettings set \
  --name videomania-appservice \
  --resource-group videomania-rg \
  --settings BLOB_STORAGE_CONNECTION_STRING="<connection-string>" \
             COSMOS_DB_ACCOUNT="videomania-cosmos.documents.azure.com" \
             COSMOS_DB_KEY="<primary-key>" \
             CosmosDb__DatabaseName="videomania" \
             BlobStorage__SasTokenValidityMinutes="30"
```

#### Step 6: Build and Publish

```bash
cd ICC.AzureAppService.Demo
dotnet publish -c Release -o ./bin/Release/publish
```

#### Step 7: Deploy to App Service

```bash
az webapp up \
  --name videomania-appservice \
  --resource-group videomania-rg \
  --runtime "dotnet8"
```

### Option 3: CI/CD Pipeline (GitHub Actions)

1. **Add Secrets to GitHub Repository**:

   - Go to Settings → Secrets and variables → Actions
   - Add the following secrets:
     - `AZURE_APPSERVICE_PUBLISH_PROFILE`: Download from App Service
     - `BLOB_STORAGE_CONNECTION_STRING`: From Step 4
     - `COSMOS_DB_ACCOUNT`: Account name
     - `COSMOS_DB_KEY`: Primary key

2. **Trigger Deployment**:
   - Push code to the `main` branch, or
   - Manually trigger from Actions tab

## Configuration

### Environment Variables

The application requires these environment variables to be set in Azure App Service:

```
BLOB_STORAGE_CONNECTION_STRING=<your-connection-string>
COSMOS_DB_ACCOUNT=<your-account>.documents.azure.com
COSMOS_DB_KEY=<your-primary-key>
CosmosDb__DatabaseName=videomania
CosmosDb__UsersContainer=Users
CosmosDb__VideosContainer=Videos
CosmosDb__CommentsContainer=Comments
BlobStorage__ContainerName=videos
BlobStorage__SasTokenValidityMinutes=30
```

### Application Settings (appsettings.json)

The application reads from `appsettings.json` and environment variables. Environment variables override JSON settings.

## Verify Deployment

1. **Check Application Health**:

```bash
az webapp show --name videomania-appservice --resource-group videomania-rg
```

2. **View Application Logs**:

```bash
az webapp log tail --name videomania-appservice --resource-group videomania-rg
```

3. **Access the Application**:

```
https://videomania-appservice.azurewebsites.net
```

## API Endpoints

### Upload Video

- **POST** `/api/getuploadSas`
- **Body**: `{ "fileName": "video.mp4" }`
- **Response**: `{ "sasUri": "...", "blobName": "...", "expiresIn": 1800 }`

### Upload UI

- **GET** `/Videos/Upload`

### View Videos

- **GET** `/Videos`

### Video Details

- **GET** `/Videos/Details/{id}`

## Troubleshooting

### Connection String Issues

```bash
# Verify connection string format
az storage account show-connection-string --name videomaniastorage --resource-group videomania-rg
```

### Cosmos DB Configuration

```bash
# List Cosmos DB keys
az cosmosdb keys list --name videomania-cosmos --resource-group videomania-rg
```

### App Service Logs

```bash
# Enable detailed logging
az webapp config appsettings set \
  --name videomania-appservice \
  --resource-group videomania-rg \
  --settings LOGLEVEL="Debug"

# Stream logs
az webapp log tail --name videomania-appservice --resource-group videomania-rg
```

## Cost Optimization (Free Tier)

- **App Service**: F1 (Free) - 60 minutes/day
- **Cosmos DB**: Free tier - 1000 RU/s
- **Storage Account**: 5 GB free storage
- **Data Transfer**: Minimal free tier

**Note**: For production use, upgrade to paid tiers.

## Security Considerations

1. **SAS Tokens**: Limited to 30-minute validity
2. **Connection Strings**: Store in Azure Key Vault
3. **HTTPS**: Enforced by default
4. **CORS**: Configure as needed
5. **Authentication**: Implement user authentication

## Cleanup

To delete all resources and avoid charges:

```bash
az group delete --name videomania-rg --yes --no-wait
```

## Next Steps

1. Configure authentication (Azure AD B2C)
2. Set up custom domain
3. Configure CDN for video delivery
4. Implement monitoring and alerting
5. Set up backup and disaster recovery

## Support

For issues or questions, refer to:

- Azure Documentation: https://learn.microsoft.com/en-us/azure/
- .NET Documentation: https://learn.microsoft.com/en-us/dotnet/
- Repository Issues: Check GitHub issues page
