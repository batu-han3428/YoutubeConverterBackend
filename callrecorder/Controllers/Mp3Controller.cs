using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace callrecorder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Mp3Controller : ControllerBase
    {
        private readonly YoutubeClient _youtubeClient;

        public Mp3Controller()
        {
            _youtubeClient = new YoutubeClient();
        }

        public class Mp3
        {
            public string VideoUrl { get; set; }
        }

        string GetYoutubeVideoId(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                var uri = new Uri(url);
                var host = uri.Host;
                var path = uri.AbsolutePath;

                if (host.Contains("youtube.com"))
                {
                    var query = uri.Query;
                    var queryParams = System.Web.HttpUtility.ParseQueryString(query);
                    return queryParams["v"];
                }
                else if (host.Contains("youtu.be"))
                {
                    return path.Substring(1);
                }
            }
            return null;
        }

        [HttpPost("ConvertVideoToMp3")]
        public async Task<IActionResult> ConvertVideoToMp3(Mp3 data)
        {
            try
            {
                string videoId = GetYoutubeVideoId(data.VideoUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    return NotFound("Video stream not found");
                }
                var video = await _youtubeClient.Videos.GetAsync(videoId);
                var streamInfoSet = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                var videoStreamInfo = streamInfoSet.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (videoStreamInfo != null)
                {
                    var videoStream = await _youtubeClient.Videos.Streams.GetAsync(videoStreamInfo);
                    var memoryStream = new MemoryStream();

                    await videoStream.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    var videoFilePath = "video.mp4";
                    await System.IO.File.WriteAllBytesAsync(videoFilePath, memoryStream.ToArray());

                    var mp3FilePath = "song.mp3";
                    var ffmpegProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{videoFilePath}\" -vn -acodec libmp3lame -ab 128k \"{mp3FilePath}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });

                    await ffmpegProcess.WaitForExitAsync();

                    var file = TagLib.File.Create(mp3FilePath);
                    file.Tag.Title = video.Title;
                    file.Save();

                    var mp3Bytes = await System.IO.File.ReadAllBytesAsync(mp3FilePath);

                    System.IO.File.Delete(videoFilePath);
                    System.IO.File.Delete(mp3FilePath);

                    return File(mp3Bytes, "audio/mpeg", $"{Guid.NewGuid()}.mp3");
                }
                else
                {
                    return NotFound("Video stream not found");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}
