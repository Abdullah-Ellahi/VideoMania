using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Company.Function.Services;

public class VideoProcessingService
{
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string _tempDirectory;
    private static bool _ffmpegInitialized = false;

    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "VideoProcessing");
        Directory.CreateDirectory(_tempDirectory);
        
        InitializeFFmpegAsync().Wait();
    }

    private async Task InitializeFFmpegAsync()
    {
        if (_ffmpegInitialized) return;
        
        try
        {
            _logger.LogInformation("Downloading FFmpeg binaries...");
            var ffmpegPath = Path.Combine(_tempDirectory, "ffmpeg");
            Directory.CreateDirectory(ffmpegPath);
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegPath);
            FFmpeg.SetExecutablesPath(ffmpegPath);
            _ffmpegInitialized = true;
            _logger.LogInformation("FFmpeg initialized successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg binaries");
        }
    }

    /// <summary>
    /// Generate a thumbnail from the video at a specific timestamp
    /// </summary>
    public async Task<string> GenerateThumbnailAsync(string videoPath, string outputFileName, int secondsIntoVideo = 1)
    {
        _logger.LogInformation("Generating thumbnail for video: {videoPath}", videoPath);
        
        try
        {
            var thumbnailPath = Path.Combine(_tempDirectory, $"{Path.GetFileNameWithoutExtension(outputFileName)}_thumbnail.jpg");
            
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            
            if (videoStream == null)
            {
                _logger.LogError("No video stream found in: {videoPath}", videoPath);
                return null;
            }

            var conversion = await FFmpeg.Conversions.FromSnippet.Snapshot(videoPath, thumbnailPath, TimeSpan.FromSeconds(secondsIntoVideo));
            await conversion.Start();
            
            if (File.Exists(thumbnailPath))
            {
                var fileInfo = new FileInfo(thumbnailPath);
                _logger.LogInformation("✓ Thumbnail generated: {path} ({size} KB)", 
                    thumbnailPath, fileInfo.Length / 1024);
                return thumbnailPath;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for: {videoPath}", videoPath);
            return null;
        }
    }

    /// <summary>
    /// Resize video to specified dimensions
    /// </summary>
    public async Task<string> ResizeVideoAsync(string videoPath, string outputFileName, int width = 1280, int height = 720)
    {
        _logger.LogInformation("Resizing video: {videoPath} to {width}x{height}", videoPath, width, height);
        
        try
        {
            var resizedPath = Path.Combine(_tempDirectory, $"{Path.GetFileNameWithoutExtension(outputFileName)}_resized.mp4");
            
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault()?.SetSize(width, height);
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

            if (videoStream == null)
            {
                _logger.LogError("No video stream found in: {videoPath}", videoPath);
                return null;
            }

            var conversion = FFmpeg.Conversions.New()
                .AddStream(videoStream)
                .SetOutput(resizedPath);

            if (audioStream != null)
                conversion.AddStream(audioStream);

            await conversion.Start();
            
            if (File.Exists(resizedPath))
            {
                var originalSize = new FileInfo(videoPath).Length;
                var resizedSize = new FileInfo(resizedPath).Length;
                _logger.LogInformation("✓ Video resized: {path} (Original: {originalMB:F2} MB → Resized: {resizedMB:F2} MB)", 
                    resizedPath, 
                    originalSize / (1024.0 * 1024.0), 
                    resizedSize / (1024.0 * 1024.0));
                return resizedPath;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize video: {videoPath}", videoPath);
            return null;
        }
    }

    /// <summary>
    /// Get video metadata
    /// </summary>
    public async Task<VideoMetadata> GetVideoMetadataAsync(string videoPath)
    {
        _logger.LogInformation("Extracting metadata for: {videoPath}", videoPath);
        
        try
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(videoPath);
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

            var metadata = new VideoMetadata
            {
                FilePath = videoPath,
                FileSize = new FileInfo(videoPath).Length,
                Duration = mediaInfo.Duration.TotalSeconds,
                Width = videoStream?.Width ?? 0,
                Height = videoStream?.Height ?? 0,
                VideoCodec = videoStream?.Codec ?? "Unknown",
                AudioCodec = audioStream?.Codec ?? "Unknown",
                FrameRate = videoStream?.Framerate ?? 0,
                BitRate = mediaInfo.VideoStreams.FirstOrDefault()?.Bitrate ?? 0
            };
            
            _logger.LogInformation("✓ Metadata extracted - Duration: {duration:F2}s, Resolution: {width}x{height}, Codec: {codec}", 
                metadata.Duration, metadata.Width, metadata.Height, metadata.VideoCodec);
            
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract metadata for: {videoPath}", videoPath);
            return null;
        }
    }

    /// <summary>
    /// Save stream to temporary file
    /// </summary>
    public async Task<string> SaveStreamToTempFileAsync(Stream stream, string fileName)
    {
        var tempFilePath = Path.Combine(_tempDirectory, fileName);
        
        _logger.LogInformation("Saving stream to temporary file: {path}", tempFilePath);
        
        using (var fileStream = File.Create(tempFilePath))
        {
            stream.Position = 0;
            await stream.CopyToAsync(fileStream);
        }
        
        var fileInfo = new FileInfo(tempFilePath);
        _logger.LogInformation("✓ Stream saved: {path} ({size:F2} MB)", 
            tempFilePath, fileInfo.Length / (1024.0 * 1024.0));
        
        return tempFilePath;
    }

    /// <summary>
    /// Clean up temporary files
    /// </summary>
    public void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("✓ Cleaned up: {path}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup: {path}", filePath);
        }
    }
}

public class VideoMetadata
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public double Duration { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string VideoCodec { get; set; } = string.Empty;
    public string AudioCodec { get; set; } = string.Empty;
    public double FrameRate { get; set; }
    public long BitRate { get; set; }
}
