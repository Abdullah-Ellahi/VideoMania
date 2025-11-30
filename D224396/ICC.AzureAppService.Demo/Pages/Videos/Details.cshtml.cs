using ICC.AzureAppService.Demo.Models;
using ICC.AzureAppService.Demo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ICC.AzureAppService.Demo.Pages.Videos
{
    public class DetailsModel : PageModel
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobStorageService _blobService;

        [BindProperty(SupportsGet = true)]
        public string? id { get; set; }

        [BindProperty]
        public string CommentText { get; set; } = string.Empty;

        public Video? Video { get; set; }
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public DetailsModel(CosmosDbService cosmosService, BlobStorageService blobService)
        {
            _cosmosService = cosmosService;
            _blobService = blobService;
        }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(id) && _cosmosService != null)
            {
                try
                {
                    Video = await _cosmosService.GetVideoByIdAsync(id);
                    Comments = await _cosmosService.GetCommentsAsync(id);

                    if (Video != null && _blobService != null)
                    {
                        try
                        {
                            Video.Url = (await _blobService.GetReadSasUriAsync(Video.Url, TimeSpan.FromHours(24))).ToString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error generating SAS URI: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching video details: {ex.Message}");
                }
            }
        }

        // FIXED: Method name must match the handler exactly (case-sensitive)
        public async Task<IActionResult> OnPostAddCommentAsync()
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrWhiteSpace(CommentText) || _cosmosService == null)
            {
                return RedirectToPage(new { id = id });
            }

            try
            {
                var comment = new Comment
                {
                    id = Guid.NewGuid().ToString(),
                    VideoId = id,
                    UserId = "Guest",
                    Text = CommentText.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _cosmosService.AddItemAsync(comment, "Comments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding comment: {ex.Message}");
            }

            return RedirectToPage(new { id = id });
        }        // FIXED: Method name must match the handler exactly (case-sensitive)
        public async Task<IActionResult> OnPostDeleteVideoAsync()
        {
            if (string.IsNullOrEmpty(id) || _cosmosService == null)
            {
                return RedirectToPage("/Videos/Index");
            }

            try
            {
                var video = await _cosmosService.GetVideoByIdAsync(id);

                if (video != null)
                {
                    try
                    {
                        // Delete all comments associated with this video
                        var comments = await _cosmosService.GetCommentsAsync(id);
                        foreach (var comment in comments)
                        {
                            if (!string.IsNullOrEmpty(comment?.id))
                            {
                                await _cosmosService.DeleteCommentAsync(comment.id, comment.VideoId);
                            }
                        }

                        // Delete the blob from storage
                        if (_blobService != null && !string.IsNullOrEmpty(video.Url))
                        {
                            await _blobService.DeleteBlobAsync(video.Url);
                        }

                        // Delete the video from Cosmos DB (must be last)
                        await _cosmosService.DeleteVideoAsync(video.id, video.UserId);
                    }
                    catch (Exception ex)
                    {
                        // Log the error if needed
                        Console.WriteLine($"Error deleting video: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in delete video handler: {ex.Message}");
            }

            return RedirectToPage("/Videos/Index");
        }
    }
}