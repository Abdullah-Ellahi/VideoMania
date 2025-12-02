# Manual Event Grid Setup via REST API
# Use this if Azure Portal keeps failing

$subscriptionId = "ad707f0f-bc06-4e81-a549-d1e4ae4833d7"
$resourceGroup = "videomania-dev"
$storageAccount = "videomaniadev98e1"
$webhookUrl = "https://videomania-fvdqg9abbqbsbsgb.canadacentral-01.azurewebsites.net/runtime/webhooks/blobs?functionName=VideoTrigger"

# Get access token (you'll need to login first)
$token = (Get-AzAccessToken).Token

$body = @{
    properties = @{
        destination = @{
            endpointType = "WebHook"
            properties = @{
                endpointUrl = $webhookUrl
            }
        }
        filter = @{
            includedEventTypes = @("Microsoft.Storage.BlobCreated")
            subjectBeginsWith = "/blobServices/default/containers/samples-workitems/"
        }
    }
} | ConvertTo-Json -Depth 10

$uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Storage/storageAccounts/$storageAccount/providers/Microsoft.EventGrid/eventSubscriptions/videomania-blob-events?api-version=2021-12-01"

Invoke-RestMethod -Uri $uri -Method Put -Headers @{Authorization="Bearer $token"} -Body $body -ContentType "application/json"
