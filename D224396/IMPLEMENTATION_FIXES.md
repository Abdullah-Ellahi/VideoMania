# Delete Video & Comments Functionality - Implementation Fixes

## Summary of Changes

I've aligned the **delete video** and **comments functionality** with the **working upload video approach**. All three operations now follow the same pattern:

1. **Server-side validation** in Controllers
2. **Consistent API response format** with `success` field
3. **Proper error handling** and logging
4. **Frontend JavaScript** checking for `data.success === true`

---

## Files Updated

### 1. **Controllers/UploadController.cs**
- Added `success` field to all responses
- Added logging indicators (🔴, ✅, ❌)
- Improved comments explaining partition key usage

**Key Changes:**
```csharp
// All responses now include success field
return Ok(new 
{ 
    success = true,
    message = $"Video '{video.title}' uploaded successfully!",
    videoId = video.id,
    blobName = blobName
});

return BadRequest(new { success = false, error = "Title is required" });
```

---

### 2. **Controllers/CommentsController.cs**
- Added `success` field to all responses
- Improved validation messages
- Added explicit partition key documentation

**Key Changes:**
```csharp
// AddComment endpoint now returns consistent response
return Ok(new 
{ 
    success = true,
    message = "Comment added successfully",
    commentId = comment.id,
    videoId = request.videoId
});

// DeleteComment endpoint now returns consistent response
return Ok(new 
{ 
    success = true,
    message = "Comment deleted successfully",
    commentId,
    videoId
});
```

---

### 3. **Controllers/VideoController.cs**
- Maintains proper error handling
- Returns `success = true` on successful deletion
- Added detailed logging with emoji indicators
- Clear documentation of partition key usage

**Key Features:**
- ✅ Validates video exists before deletion
- ✅ Deletes all associated comments first
- ✅ Deletes blob from storage
- ✅ Deletes video from Cosmos DB last
- ✅ Handles errors gracefully

---

### 4. **Services/CosmosDbService.cs**
- Enhanced `DeleteVideoAsync()` with detailed logging
- Enhanced `DeleteCommentAsync()` with detailed logging
- Proper error handling with try-catch blocks

**Updated Methods:**
```csharp
public async Task DeleteVideoAsync(string id, string userId)
{
    Console.WriteLine($"[CosmosDbService] DeleteVideoAsync called: id={id}, userId={userId}");
    try
    {
        var response = await VideosContainer.DeleteItemAsync<Video>(id, new PartitionKey(userId));
        Console.WriteLine($"[CosmosDbService] ✅ Video deleted successfully, RequestCharge: {response.RequestCharge}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CosmosDbService] ❌ Error deleting video: {ex.Message}");
        throw;
    }
}

public async Task DeleteCommentAsync(string commentId, string videoId)
{
    Console.WriteLine($"[CosmosDbService] DeleteCommentAsync called: commentId={commentId}, videoId={videoId}");
    try
    {
        var response = await CommentsContainer.DeleteItemAsync<Comment>(commentId, new PartitionKey(videoId));
        Console.WriteLine($"[CosmosDbService] ✅ Comment deleted successfully, RequestCharge: {response.RequestCharge}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CosmosDbService] ❌ Error deleting comment: {ex.Message}");
        throw;
    }
}
```

---

### 5. **Pages/Videos/Details.cshtml (JavaScript)**
- Updated `handleDeleteVideo()` to check `data.success === true`
- Updated `handleAddComment()` to check `data.success === true`
- Updated `handleDeleteComment()` to check `data.success === true`
- Removed unused variable references

**Updated JavaScript Functions:**
```javascript
async function handleDeleteVideo() {
    try {
        const response = await fetch(`/api/videos/${videoId}`, {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' }
        });

        const data = await response.json();

        if (response.ok && data.success) {  // ← Check both status AND success flag
            alert('✅ Video deleted successfully!');
            window.location.href = '/Videos';
        } else {
            alert('❌ Error: ' + (data.error || 'Failed to delete video'));
        }
    } catch (error) {
        console.error('❌ Delete error:', error);
        alert('❌ Error deleting video: ' + error.message);
    }
}

async function handleAddComment(event) {
    // ... validation ...
    const response = await fetch('/api/comments/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            videoId: videoId,
            commentText: commentText,
            userId: null
        })
    });

    const data = await response.json();

    if (response.ok && data.success) {  // ← Check both status AND success flag
        window.location.reload();
    } else {
        alert('❌ Error: ' + (data.error || 'Failed to add comment'));
    }
}

async function handleDeleteComment(commentId) {
    const response = await fetch(`/api/comments/${commentId}?videoId=${videoId}`, {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' }
    });

    const data = await response.json();

    if (response.ok && data.success) {  // ← Check both status AND success flag
        window.location.reload();
    } else {
        alert('❌ Error: ' + (data.error || 'Failed to delete comment'));
    }
}
```

---

## Key Implementation Points

### Partition Keys Used
```
Videos Container:
  - Partition Key: /userId
  - Example: DeleteVideoAsync(videoId, userId)

Comments Container:
  - Partition Key: /videoId
  - Example: DeleteCommentAsync(commentId, videoId)

Users Container:
  - Partition Key: /id
```

### API Response Format (Consistent Across All Endpoints)
```json
// Success Response
{
    "success": true,
    "message": "Operation completed successfully",
    "videoId": "...",
    "commentId": "..."
}

// Error Response
{
    "success": false,
    "error": "Detailed error message"
}
```

### Request/Response Flow

#### Upload Video
1. POST `/api/getuploadSas` with multipart form data
2. Server validates file (size, type)
3. Server uploads blob to Azure Storage
4. Server saves metadata to Cosmos DB with userId as partition key
5. Returns `{ success: true, videoId, blobName }`

#### Add Comment
1. POST `/api/comments/add` with JSON body `{ videoId, commentText, userId }`
2. Server verifies video exists
3. Server creates comment with videoId as partition key
4. Returns `{ success: true, commentId, videoId }`

#### Delete Comment
1. DELETE `/api/comments/{commentId}?videoId={videoId}`
2. Server verifies video exists
3. Server deletes comment using videoId as partition key
4. Returns `{ success: true, commentId, videoId }`

#### Delete Video
1. DELETE `/api/videos/{videoId}`
2. Server fetches video (to get userId for partition key)
3. Server deletes all associated comments
4. Server deletes blob from Azure Storage
5. Server deletes video using userId as partition key
6. Returns `{ success: true, videoId }`

---

## Debugging & Logging

All operations now include detailed logging with visual indicators:

```
🔴 = Operation started
✅ = Operation successful
❌ = Operation failed
```

Example console output:
```
[CosmosDbService] DeleteVideoAsync called: id=abc123, userId=TestUser
[CosmosDbService] ✅ Video deleted successfully, RequestCharge: 10.5
```

---

## Testing Checklist

- [ ] Upload a video successfully
- [ ] Add a comment to the video
- [ ] Delete the comment (verify it's removed from UI)
- [ ] Delete the video (verify it redirects to Videos list)
- [ ] Verify blob is deleted from Azure Storage
- [ ] Verify all records deleted from Cosmos DB
- [ ] Check browser console for any errors
- [ ] Check application logs for proper logging

---

## Migration Notes

These changes **do not break existing functionality**:
- Upload still works the same way
- The response format is backward compatible (adding `success` field)
- All error handling is preserved
- Partition key usage is now clearly documented

