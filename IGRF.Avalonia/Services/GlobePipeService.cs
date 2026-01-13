using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace IGRF.Avalonia.Services
{
    public class GlobePipeService : IDisposable
    {
        private const string PipeName = "IGRF_Globe_Pipe";
        private NamedPipeServerStream? _pipeServer;
        private StreamWriter? _writer;
        private bool _isRunning;

        public event EventHandler<string>? ConnectionStatusChanged;

        public GlobePipeService()
        {
            _isRunning = true;
            Task.Run(StartPipeServerAsync);
        }

        private async Task StartPipeServerAsync()
        {
            while (_isRunning)
            {
                try
                {
                    ConnectionStatusChanged?.Invoke(this, "Listening...");

                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.Out,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous
                    );

                    await _pipeServer.WaitForConnectionAsync();

                    ConnectionStatusChanged?.Invoke(this, "Connected");
                    _writer = new StreamWriter(_pipeServer) { AutoFlush = true };

                    // Keep connection open until broken
                    while (_pipeServer.IsConnected && _isRunning)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    ConnectionStatusChanged?.Invoke(this, $"Error: {ex.Message}");
                    await Task.Delay(2000); // Retry delay
                }
                finally
                {
                    DisposePipe();
                }
            }
        }

        public async Task SendSatellitePositionAsync(double lat, double lon, double altKm)
        {
            if (_writer != null && _pipeServer != null && _pipeServer.IsConnected)
            {
                try
                {
                    // Format: "POS:lat,lon,alt"
                    string message = $"POS:{lat:F6},{lon:F6},{altKm:F3}";
                    await _writer.WriteLineAsync(message);
                }
                catch
                {
                    // Ignore send errors, pipe will reconnect
                }
            }
        }

        private void DisposePipe()
        {
            try
            {
                _writer?.Dispose();
                _writer = null;
                _pipeServer?.Dispose();
                _pipeServer = null;
            }
            catch { }
        }

        public void Dispose()
        {
            _isRunning = false;
            DisposePipe();
        }
    }
}
