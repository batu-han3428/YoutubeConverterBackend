using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;

namespace callrecorder.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallRecordingController : ControllerBase
    {
        private static string _recordingFilePath;
        private static WaveFileWriter _waveWriter;
        private static WaveInEvent _waveIn;

        [HttpPost("start")]
        public async Task<IActionResult> StartRecording()
        {
            try
            {
                _recordingFilePath = Path.Combine(Path.GetTempPath(), "call_recording.wav");

                _waveIn = new WaveInEvent();
                _waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
                _waveIn.DataAvailable += WaveInDataAvailable;

                _waveWriter = new WaveFileWriter(_recordingFilePath, _waveIn.WaveFormat);
                _waveIn.StartRecording();

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopRecording()
        {
            try
            {
                if (string.IsNullOrEmpty(_recordingFilePath) || !System.IO.File.Exists(_recordingFilePath))
                {
                    return BadRequest("Recording file not found.");
                }

                _waveIn.StopRecording();
                _waveWriter.Dispose();

                _recordingFilePath = null;

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        private void WaveInDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while writing audio data: {ex.Message}");
            }
        }
    }
}
