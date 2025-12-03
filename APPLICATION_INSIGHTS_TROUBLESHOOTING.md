# Application Insights Troubleshooting Guide

## ✅ Issues Fixed

I've identified and fixed several issues that were preventing Application Insights from working properly in your VideoMania Azure Function:

### 1. **Missing NuGet Package**
**Problem:** The `Microsoft.Extensions.Logging.ApplicationInsights` package was missing.

**Solution:** Added the package to `VM.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.Logging.ApplicationInsights" Version="2.23.0" />
```

### 2. **Missing Cosmos DB Package**
**Problem:** The `Microsoft.Azure.Cosmos` package was missing, causing build errors.

**Solution:** Added the package to `VM.csproj`:
```xml
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.45.0" />
```

### 3. **Missing Using Statement**
**Problem:** `Program.cs` was missing the `Microsoft.Extensions.Logging` using statement.

**Solution:** Added the using statement to `Program.cs`.

### 4. **Project Structure Issues**
**Problem:** The project was trying to compile duplicate files from `CloudFunction` and `D224396` directories.

**Solution:** Updated `VM.csproj` to exclude these directories while keeping the required Services folder.

### 5. **Enhanced Logging Configuration**
**Problem:** No explicit logging configuration or startup diagnostics.

**Solution:** Enhanced `Program.cs` with:
- Explicit Application Insights logging configuration
- Startup diagnostic logging to verify configuration
- Minimum log level set to Information

---

## 🔧 Configuration Required

### **CRITICAL: Add Application Insights Connection String**

Application Insights **will not work** until you add the connection string to your `local.settings.json` file:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "APPLICATIONINSIGHTS_CONNECTION_STRING": "YOUR_CONNECTION_STRING_HERE",
    "COSMOS_DB_CONNECTION_STRING": "...",
    "BLOB_STORAGE_CONNECTION_STRING": "..."
  }
}
```

### **How to Get Your Connection String:**

1. Go to the [Azure Portal](https://portal.azure.com)
2. Navigate to your Application Insights resource
3. Click on **"Overview"** or **"Properties"**
4. Copy the **Connection String** (it should look like this):
   ```
   InstrumentationKey=xxxxx-xxxx-xxxx-xxxx-xxxxxxxxx;IngestionEndpoint=https://...;LiveEndpoint=https://...
   ```
5. Paste it into your `local.settings.json` file

---

## 🧪 Testing Application Insights

### **1. Run the Function Locally**

```bash
cd /Users/asani.io/Documents/abdullah_ellahi/VideoMania
func start
```

### **2. Check Startup Logs**

When the function starts, you should see one of these messages:

✅ **Success:**
```
✓ Application Insights is configured and ready.
```

⚠️ **Warning (Missing Connection String):**
```
⚠️ Application Insights connection string is NOT configured. Telemetry will not be sent.
```

### **3. Trigger the Function**

Upload a video file to your blob storage `videos` container to trigger the function.

### **4. Verify Telemetry in Azure Portal**

1. Go to your Application Insights resource in Azure Portal
2. Click on **"Live Metrics"** to see real-time telemetry
3. Click on **"Logs"** to query telemetry data
4. Click on **"Transaction search"** to see individual requests

---

## 📊 What Gets Logged to Application Insights

Your function now logs the following to Application Insights:

### **Traces (Logs)**
- All `_logger.LogInformation()` calls
- All `_logger.LogWarning()` calls
- All `_logger.LogError()` calls

### **Requests**
- Each function invocation
- Duration and success/failure status

### **Dependencies**
- Cosmos DB calls
- Blob Storage operations
- External API calls

### **Exceptions**
- All unhandled exceptions
- Exceptions logged via `_logger.LogError(ex, ...)`

---

## 🔍 Troubleshooting Tips

### **Issue: No telemetry appearing in Application Insights**

**Check:**
1. ✅ Connection string is correctly set in `local.settings.json`
2. ✅ Function is running (check console output)
3. ✅ Function is being triggered (upload a video file)
4. ⏱️ Wait 2-5 minutes for telemetry to appear in Azure Portal

### **Issue: "Application Insights connection string is NOT configured" warning**

**Solution:** Add the `APPLICATIONINSIGHTS_CONNECTION_STRING` to your `local.settings.json` file.

### **Issue: Build errors**

**Solution:** Run:
```bash
dotnet clean
dotnet restore
dotnet build
```

### **Issue: Function not triggering**

**Check:**
1. Blob storage connection string is correct
2. `videos` container exists
3. File being uploaded is a valid video format (.mp4, .avi, .mov, etc.)

---

## 📦 Updated Packages

Your `VM.csproj` now includes:

```xml
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.23.0" />
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.45.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="2.50.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.ApplicationInsights" Version="2.50.0" />
<PackageReference Include="Microsoft.Extensions.Logging.ApplicationInsights" Version="2.23.0" />
```

---

## 🚀 Deployment to Azure

When deploying to Azure, make sure to set the Application Insights connection string in your Function App settings:

```bash
az functionapp config appsettings set \
  --name YOUR_FUNCTION_APP_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=YOUR_CONNECTION_STRING"
```

Or set it in the Azure Portal:
1. Go to your Function App
2. Click **"Configuration"**
3. Add a new application setting:
   - **Name:** `APPLICATIONINSIGHTS_CONNECTION_STRING`
   - **Value:** Your connection string

---

## 📝 Summary

✅ **Fixed Issues:**
- Added missing NuGet packages
- Fixed project structure and build errors
- Enhanced logging configuration
- Added startup diagnostics

⚠️ **Action Required:**
- Add Application Insights connection string to `local.settings.json`
- Test the function locally
- Verify telemetry in Azure Portal

🎯 **Next Steps:**
1. Get your Application Insights connection string from Azure Portal
2. Add it to `local.settings.json`
3. Run `func start` to test locally
4. Upload a video file to trigger the function
5. Check Application Insights in Azure Portal for telemetry

---

## 📞 Need Help?

If you're still experiencing issues:
1. Check the function console output for errors
2. Verify all connection strings are correct
3. Check Azure Portal for any service outages
4. Review the Application Insights documentation: https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview
