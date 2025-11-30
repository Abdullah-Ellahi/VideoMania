# VideoMania - Serverless Video Streaming Platform

A cloud-native video sharing platform built on Microsoft Azure using only free-tier services. The application leverages Azure App Service, Azure Blob Storage, and Azure Cosmos DB to provide a scalable, serverless video streaming experience.

## 🎯 Project Overview

VideoMania is designed as a serverless video streaming platform with the following features:

- **User Management**: User registration and profiles
- **Video Upload**: Secure video upload with SAS token authentication
- **Video Streaming**: Stream videos with optimized storage
- **Comments & Engagement**: Users can comment on videos
- **Responsive UI**: Modern, user-friendly interface

## 🏗️ Architecture

### Azure Services Used

1. **Azure App Service (F1 - Free Tier)**

   - Hosts the ASP.NET Core web application
   - 60 compute minutes/day (free tier)
   - Auto-scaling capability

2. **Azure Blob Storage**

   - Stores video files
   - SAS token-based secure access
   - 5 GB free storage
   - CDN-ready

3. **Azure Cosmos DB (Free Tier)**
   - Stores user, video, and comment metadata
   - 1000 RU/s provisioned throughput
   - Global distribution ready

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- Azure CLI
- Azure Subscription (free tier available)
- Git

### Local Development

```bash
# Clone the repository
git clone https://github.com/Abdullah-Ellahi/VideoMania.git
cd VideoMania/D224396

# Restore dependencies
cd ICC.AzureAppService.Demo
dotnet restore

# Run locally
dotnet run

# Access the application
# http://localhost:5000
```

### Deploy to Azure

See [DEPLOYMENT.md](./DEPLOYMENT.md) for detailed deployment instructions.

#### Quick Deploy (Windows)

```bash
cd D224396
.\deploy.bat "videomania-appservice" "videomania-rg" "eastus"
```

#### Quick Deploy (Linux/macOS)

```bash
cd D224396
chmod +x deploy.sh
./deploy.sh "videomania-appservice" "videomania-rg" "eastus"
```

## 📋 Tasks Completed

### 1.1 Azure Setup & Base Resources ✅

- [x] App Service configuration (Free Plan - F1)
- [x] Storage Account setup with Blob container
- [x] Cosmos DB database creation with containers
- [x] appsettings.json with configuration templates
- [x] ARM template for Infrastructure as Code

### 2.1 Video Upload Flow ✅

- [x] **UI Component**: Enhanced file selection with drag-and-drop

  - Click-to-upload functionality
  - Drag-and-drop support
  - File preview
  - Progress tracking

- [x] **SAS Token Security**:

  - Backend endpoint `/api/getuploadSas` for token generation
  - 30-minute token validity
  - File type validation
  - Direct blob upload without server processing

- [x] **Upload Flow**:
  - Client requests SAS token from backend
  - Client uploads directly to Blob Storage
  - Backend registers metadata in Cosmos DB
  - User receives upload confirmation

### 3. Deployment to Azure ✅

- [x] Automated deployment scripts (Bash & Batch)
- [x] GitHub Actions CI/CD pipeline
- [x] ARM template for infrastructure
- [x] Publish profiles for Azure App Service
- [x] Comprehensive deployment documentation

## 📁 Project Structure

```
VideoMania/
├── D224396/
│   ├── ICC.AzureAppService.Demo/
│   │   ├── Pages/
│   │   │   ├── Videos/
│   │   │   │   ├── Upload.cshtml          (UI for video upload)
│   │   │   │   ├── Upload.cshtml.cs       (Code-behind with SAS flow)
│   │   │   │   ├── Index.cshtml           (Video list)
│   │   │   │   └── Details.cshtml         (Video details)
│   │   │   └── Api/
│   │   │       └── GetUploadSas.cshtml.cs (SAS token endpoint)
│   │   ├── Services/
│   │   │   ├── BlobStorageService.cs      (Blob operations & SAS)
│   │   │   └── CosmosDbService.cs         (Database operations)
│   │   ├── Models/
│   │   │   ├── User.cs
│   │   │   ├── Video.cs
│   │   │   └── Comment.cs
│   │   ├── appsettings.json               (Configuration)
│   │   └── Program.cs                     (Application entry point)
│   ├── arm-template.json                  (Infrastructure template)
│   ├── deploy.sh                          (Linux/macOS deployment)
│   ├── deploy.bat                         (Windows deployment)
│   ├── DEPLOYMENT.md                      (Deployment guide)
│   └── .github/workflows/
│       └── deploy-to-azure.yml            (CI/CD pipeline)
└── README.md                              (This file)
```

## 🔐 Security Features

### SAS Token Authentication

- **Time-limited Access**: Tokens expire after 30 minutes
- **Limited Permissions**: Tokens only grant Write/Create access
- **Per-file Tokens**: Each upload gets a unique token
- **No Server Upload**: Reduces server bandwidth and processing

### Data Protection

- **HTTPS Enforcement**: All communications encrypted
- **Storage Security**: Blob storage with private containers
- **Database Security**: Cosmos DB with connection string protection

## 🌐 API Endpoints

### SAS Token Generation

```
POST /api/getuploadSas
Content-Type: application/json

Request:
{
  "fileName": "my-video.mp4"
}

Response:
{
  "sasUri": "https://videomaniastorage.blob.core.windows.net/videos/...",
  "blobName": "unique-id_my-video.mp4",
  "expiresIn": 1800
}
```

### Video Management

```
GET  /Videos                 - List all videos
GET  /Videos/Upload          - Upload page
POST /Videos/Upload          - Submit video upload
GET  /Videos/Details/{id}    - View video details
```

## ⚙️ Configuration

### Environment Variables

```env
BLOB_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;...
COSMOS_DB_ACCOUNT=videomania-cosmos.documents.azure.com
COSMOS_DB_KEY=your-primary-key
CosmosDb__DatabaseName=videomania
CosmosDb__UsersContainer=Users
CosmosDb__VideosContainer=Videos
CosmosDb__CommentsContainer=Comments
BlobStorage__ContainerName=videos
BlobStorage__SasTokenValidityMinutes=30
```

## 📊 Database Schema

### Cosmos DB Collections

**Users Container**

```json
{
  "id": "user-guid",
  "username": "user@example.com",
  "email": "user@example.com",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**Videos Container**

```json
{
  "id": "video-guid",
  "userId": "user-guid",
  "title": "Video Title",
  "description": "Video Description",
  "url": "unique-id_filename.mp4",
  "uploadedAt": "2024-01-01T00:00:00Z"
}
```

**Comments Container**

```json
{
  "id": "comment-guid",
  "videoId": "video-guid",
  "userId": "user-guid",
  "text": "Comment text",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

## 🧪 Testing

### Local Testing

```bash
dotnet test
```

### Azure Testing

```bash
# Check application health
curl https://videomania-appservice.azurewebsites.net/health

# Test SAS token endpoint
curl -X POST https://videomania-appservice.azurewebsites.net/api/getuploadSas \
  -H "Content-Type: application/json" \
  -d '{"fileName": "test.mp4"}'
```

## 📈 Performance & Scalability

- **Serverless**: No server management
- **Auto-scaling**: App Service scales automatically
- **Global Reach**: Cosmos DB provides multi-region support
- **CDN Ready**: Blob storage integrates with Azure CDN
- **Cost Efficient**: Free tier covers development and small deployments

## 🛠️ Maintenance

### Regular Tasks

- Monitor application logs
- Review storage usage
- Check Cosmos DB quotas
- Update dependencies

### Backup Strategy

- Automated Cosmos DB backups
- Blob storage versioning
- Git repository backups

## 📚 Documentation

- [Deployment Guide](./D224396/DEPLOYMENT.md) - Complete deployment instructions
- [Azure Documentation](https://learn.microsoft.com/en-us/azure/)
- [.NET Documentation](https://learn.microsoft.com/en-us/dotnet/)

## 🤝 Contributing

Contributions are welcome! Please submit pull requests with:

- Clear commit messages
- Tests for new features
- Updated documentation

## 📝 License

This project is part of a cloud computing course assignment.

## 👤 Authors

- **Hassan** - Azure Setup & Base Resources (Task 1.1)
- **Hassan** - Video Upload Flow (Task 2.1)

## 🔗 Resources

- [Azure Free Account](https://azure.microsoft.com/free)
- [Azure App Service](https://learn.microsoft.com/en-us/azure/app-service/)
- [Azure Blob Storage](https://learn.microsoft.com/en-us/azure/storage/blobs/)
- [Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/)
- [SAS Token Documentation](https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview)

---

**Last Updated**: November 2024
**Version**: 1.0.0
**Status**: Ready for Deployment
