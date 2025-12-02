# Setup Event Grid for Blob Trigger on Flex Consumption
# This script upgrades storage account and creates Event Grid subscription

$storageAccountName = "videomaniadev98e1"
$resourceGroup = "videomania-dev"
$functionAppName = "videomania20251126526"
$containerName = "samples-workitems"

Write-Host "Step 1: Upgrading storage account to General Purpose v2..." -ForegroundColor Yellow

# Upgrade storage account from v1 to v2
az storage account update `
    --name $storageAccountName `
    --resource-group $resourceGroup `
    --set kind=StorageV2

Write-Host "✓ Storage account upgraded to v2" -ForegroundColor Green

Write-Host "`nStep 2: Getting storage account resource ID..." -ForegroundColor Yellow
$storageAccountId = az storage account show `
    --name $storageAccountName `
    --resource-group $resourceGroup `
    --query id -o tsv

Write-Host "✓ Storage ID: $storageAccountId" -ForegroundColor Green

Write-Host "`nStep 3: Creating Event Grid subscription..." -ForegroundColor Yellow

# Create Event Grid subscription for blob created events
az eventgrid event-subscription create `
    --name "videomania-blob-events" `
    --source-resource-id $storageAccountId `
    --endpoint-type webhook `
    --endpoint "https://videomania-fvdqg9abbqbsbsgb.canadacentral-01.azurewebsites.net/runtime/webhooks/blobs?functionName=VideoTrigger" `
    --included-event-types Microsoft.Storage.BlobCreated `
    --subject-begins-with "/blobServices/default/containers/$containerName/"

Write-Host "Event Grid subscription created!" -ForegroundColor Green
Write-Host ""
Write-Host "Setup complete! Upload a video to the container to test." -ForegroundColor Cyan

