using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace IGRF.Globe3D.Services
{
    public class PipeClientService : IDisposable
    {
        private const string PipeName = "IGRF_Globe_Pipe";
        private bool _isRunning = false;

        public event Action<double, double, double>? OnSatellitePositionReceived;
        public event Action<string>? OnStatusChanged;
        public event Action<string>? OnDebugMessage;

        public void Start()
        {
            if (_isRunning)
                return;
            _isRunning = true;
            Task.Run(StartPipeClientAsync);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        private async Task StartPipeClientAsync()
        {
            while (_isRunning)
            {
                try
                {
                    using (
                        var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.In)
                    )
                    {
                        OnStatusChanged?.Invoke("Pipe: Connecting...");
                        await pipeClient.ConnectAsync(2000);
                        OnStatusChanged?.Invoke("Pipe: Connected! Waiting data...");

                        using (var reader = new StreamReader(pipeClient))
                        {
                            while (pipeClient.IsConnected && _isRunning)
                            {
                                string? line = await reader.ReadLineAsync();
                                if (line != null)
                                {
                                    OnDebugMessage?.Invoke($"Rx: {line}");

                                    if (line.StartsWith("POS:"))
                                    {
                                        ParsePosition(line);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Retry connection
                    if (_isRunning)
                        await Task.Delay(1000);
                }
            }
        }

        private void ParsePosition(string line)
        {
            try
            {
                var parts = line.Substring(4).Split(',');
                if (
                    parts.Length == 3
                    && double.TryParse(parts[0], out double lat)
                    && double.TryParse(parts[1], out double lon)
                    && double.TryParse(parts[2], out double alt)
                )
                {
                    OnSatellitePositionReceived?.Invoke(lat, lon, alt);
                }
            }
            catch { }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
