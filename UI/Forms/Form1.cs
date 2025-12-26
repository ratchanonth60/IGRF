using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;

// Third-party Libraries
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;
using Geo;
using Geo.Geomagnetism;
using Geo.Geomagnetism.Models;
using Geo.Geodesy;

// Application namespaces
using IGRF_Interface.Core.Services;
using IGRF_Interface.Core.Models;
using IGRF_Interface.Core.Algorithms;
using IGRF_Interface.Infrastructure.Communication;
using IGRF_Interface.Infrastructure.Utilities;
using IGRF_Interface.UI.Visualization;

namespace IGRF_Interface.UI.Forms
{
    public partial class Form1 : Form
    {
        #region Constants & Configuration
        private const int CONTROLLER_BAUD_RATE = 9600;
        private const byte HEADER_BYTE = 0xA0;
        private const int SENDER_INTERVAL = 100; // ms
        private const int UI_REFRESH_RATE_MS = 50; // ms (20 Hz)
        #endregion

        #region Services & Managers
        private readonly SerialPortManager _sensorManager = new SerialPortManager();
        private readonly SensorService _sensorService = new SensorService();
        private readonly SatelliteService _satService = new SatelliteService();
        private readonly CalculationService _calcService = new CalculationService();

        private AppConfig _appConfig = new AppConfig();
        private string ConfigFilePath => Path.Combine(Application.StartupPath, "SystemConfig.json");

        private GraphManager _graphX, _graphY, _graphZ;
        #endregion

        #region PID Controllers & Setpoints
        private readonly PidController _pidX = new PidController();
        private readonly PidController _pidY = new PidController();
        private readonly PidController _pidZ = new PidController();

        private GeomagnetismCalculator _geomagnetismCalculator;

        // Filter moved to CalculationService - no need for duplicate instances here
        #endregion

        #region Hardware & Data Variables
        private SerialPort _controllerPort = new SerialPort();

        // Data Variables
        private double _magX_nT, _magY_nT, _magZ_nT;
        private double _setpointX, _setpointY, _setpointZ;
        private double _outputX, _outputY, _outputZ;
        private double[,] _intensityResults; // For Map

        // Visualization & Simulation
        private LineSeries _seriesSatTrack;
        private DateTime _lastUiUpdate = DateTime.MinValue;
        private double _simTimeOffset = 0;

        // Timers
        private readonly Timer _timerPidX = new Timer { Interval = 100 };
        private readonly Timer _timerPidY = new Timer { Interval = 100 };
        private readonly Timer _timerPidZ = new Timer { Interval = 100 };

        // Logging
        private bool _isLogging = false;
        private int _logCount = 0;
        private string _logFileName = "";
        #endregion

        public Form1()
        {
            InitializeComponent();
            InitializeSystem();
        }

        #region Initialization
        private void InitializeSystem()
        {
            try
            {
                // Init Math Models
                _geomagnetismCalculator = new GeomagnetismCalculator(Spheroid.Wgs84, new List<IGeomagneticModel> { new Wmm2025() });

                // Init Graphs
                _graphX = new GraphManager(plotViewX, "PID X-Axis");
                _graphY = new GraphManager(plotViewY, "PID Y-Axis");
                _graphZ = new GraphManager(plotViewZ, "PID Z-Axis");

                // Init Events
                _sensorManager.OnPacketReceived += HandleSensorPacket;
                _sensorManager.OnError += (msg) => 
                {
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => MessageBox.Show($"Serial Error: {msg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                    else
                        MessageBox.Show($"Serial Error: {msg}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };

                timerSender.Interval = SENDER_INTERVAL;
                timerSender.Tick += timerSender_Tick;


                // Bind PID Logic (Return value to Output Variable)
                ConfigureAxisTimer(_timerPidX, _pidX, () => _setpointX, () => _magX_nT, (val) => _outputX = val, textSysX2, _graphX, "X");
                ConfigureAxisTimer(_timerPidY, _pidY, () => _setpointY, () => _magY_nT, (val) => _outputY = val, textSysY2, _graphY, "Y");
                ConfigureAxisTimer(_timerPidZ, _pidZ, () => _setpointZ, () => _magZ_nT, (val) => _outputZ = val, textSysZ2, _graphZ, "Z");

                // Load Config
                LoadSystemConfig();

                // Set Button Defaults
                UpdateButtonState(ConnectSensor, false); // Assuming button name is ConnectSensor
                UpdateButtonState(button1, false); // Assuming button name is buttonConnectController (change to actual name)
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup Error: {ex.Message}");
            }
        }
        private void InitializeSatellitePresets()
        {
            cboSatelliteList.Items.Clear();
            cboSatelliteList.Items.Add(new SatelliteInfo { Name = "-- Manual --" });
            cboSatelliteList.Items.Add(new SatelliteInfo { Name = "ISS (ZARYA)", Line1 = "1 25544U 98067A   24030.10147156  .00014904  00000-0  27473-3 0  9998", Line2 = "2 25544  51.6414 284.5574 0002475 176.3471 287.7672 15.49357173436989" });
            cboSatelliteList.SelectedIndex = 0;

            // Add Event Handler manually
            cboSatelliteList.SelectedIndexChanged += cboSatelliteList_SelectedIndexChanged_1;
        }

        /// <summary>
        /// Configure PID timer for a single axis (eliminates duplicate code for X/Y/Z)
        /// </summary>
        private void ConfigureAxisTimer(Timer timer, PidController pid, Func<double> getSetpoint, Func<double> getMeasurement,
            Action<double> setOutput, Control displayControl, GraphManager graph, string axisName)
        {
            timer.Tick += (s, e) =>
            {
                try
                {
                    double output = RunPidLogic(pid, getSetpoint(), getMeasurement(), displayControl, graph);
                    setOutput(output);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PID {axisName} Error: {ex.Message}");
                    timer.Stop();
                }
            };
        }

        private void UpdateErrorStyle(TextBox tb)
        {
            if (double.TryParse(tb.Text, out double val))
            {
                if (Math.Abs(val) > 5.0) { tb.BackColor = Color.Red; tb.ForeColor = Color.White; }
                else { tb.BackColor = Color.LightGreen; tb.ForeColor = Color.Black; }
            }
        }

        private void ValidateDoubleInput(TextBox tb)
        {
            tb.BackColor = double.TryParse(tb.Text, out _) ? Color.White : Color.LightPink;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            UpdatePortList();
            InitializeSatellitePresets();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop timers & Disconnect
            _timerPidX.Stop(); _timerPidY.Stop(); _timerPidZ.Stop();
            SatPosTimer.Stop(); timerSender.Stop(); timer_sensor.Stop();

            _sensorManager?.Disconnect();
            if (_controllerPort != null && _controllerPort.IsOpen)
            {
                try { _controllerPort.Close(); } catch { }
                _controllerPort.Dispose();
            }
            base.OnFormClosing(e);
        }
        #endregion

        #region Sensor Data Handling (High Frequency)
        private void HandleSensorPacket(byte[] packet)
        {
            // 1. Convert Packet to Raw Data
            var rawData = _sensorService.ProcessData(packet);

            // 2. Process Data (Filter & Calculate Error) using Service
            var processed = _calcService.ProcessSensorData(rawData, _setpointX, _setpointY, _setpointZ);

            // Update Global Variables for Logging/PID
            _magX_nT = processed.MagX;
            _magY_nT = processed.MagY;
            _magZ_nT = processed.MagZ;

            // 3. UI Throttling
            if ((DateTime.Now - _lastUiUpdate).TotalMilliseconds < UI_REFRESH_RATE_MS) return;
            _lastUiUpdate = DateTime.Now;

            // 4. Update UI
            if (this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(() => UpdateSensorUI(processed)));
            }

            // 5. Log Data
            SaveLogData();
        }

        /// <summary>
        /// Update sensor UI elements with processed data
        /// </summary>
        private void UpdateSensorUI(ProcessedData processed)
        {
            textSensorX2.Text = processed.MagX.ToString("F2");
            textSensorY2.Text = processed.MagY.ToString("F2");
            textSensorZ2.Text = processed.MagZ.ToString("F2");

            textBoxErrorX.Text = processed.ErrorX.ToString("F2");
            textBoxErrorY.Text = processed.ErrorY.ToString("F2");
            textBoxErrorZ.Text = processed.ErrorZ.ToString("F2");

            textBoxErrorX_per.Text = processed.ErrorPerX.ToString("F2");
            textBoxErrorY_per.Text = processed.ErrorPerY.ToString("F2");
            textBoxErrorZ_per.Text = processed.ErrorPerZ.ToString("F2");

            // Graph update handled by PID logic if running, or manually here if stopped
            if (!_timerPidX.Enabled) _graphX.Update(_setpointX, processed.MagX);
            if (!_timerPidY.Enabled) _graphY.Update(_setpointY, processed.MagY);
            if (!_timerPidZ.Enabled) _graphZ.Update(_setpointZ, processed.MagZ);
        }

        private void SaveLogData()
        {
            if (!_isLogging) return;

            try
            {
                string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string dataLine = $"{timeStamp},{_magX_nT:F2},{_magY_nT:F2},{_magZ_nT:F2}," +
                                  $"{_setpointX:F2},{_setpointY:F2},{_setpointZ:F2}," +
                                  $"{textBoxErrorX.Text},{textBoxErrorY.Text},{textBoxErrorZ.Text}," +
                                  $"{_outputX:F2},{_outputY:F2},{_outputZ:F2}," +
                                  $"{KpX.Text},{KiX.Text},{KdX.Text}," +
                                  $"{KpY.Text},{KiY.Text},{KdY.Text}," +
                                  $"{KpZ.Text},{KiZ.Text},{KdZ.Text}\n";

                File.AppendAllText(_logFileName, dataLine);
                _logCount++;

                if (Count_label != null)
                {
                    this.BeginInvoke(new Action(() => Count_label.Text = _logCount.ToString()));
                }
            }
            catch { }
        }
        #endregion

        #region Connection Management (Async + Toggle)
        private void UpdateButtonState(Button btn, bool isConnected)
        {
            if (btn == null) return;
            if (isConnected)
            {
                btn.Text = "⏹ Disconnect";
                btn.BackColor = Color.Salmon;
                btn.ForeColor = Color.White;
            }
            else
            {
                btn.Text = "▶ Connect";
                btn.BackColor = Color.LightGreen;
                btn.ForeColor = Color.Black;
            }
            btn.Enabled = true;
        }

        private async void ConnectSensor_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button; // Use the clicked button

            // 1. If already connected -> Disconnect
            if (_sensorManager.IsOpen)
            {
                try
                {
                    btn.Enabled = false;
                    timer_sensor.Stop();
                    await Task.Run(() => _sensorManager.Disconnect());
                    UpdateButtonState(btn, false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Disconnect Error: {ex.Message}");
                    btn.Enabled = true;
                }
                return;
            }

            // 2. If not connected -> Connect
            try
            {
                string portName = ExtractPortName(cboSensorPort.Text);
                if (string.IsNullOrEmpty(portName))
                {
                    MessageBox.Show("Please select a port.");
                    return;
                }

                btn.Enabled = false;
                btn.Text = "Connecting...";
                btn.BackColor = Color.LightYellow;

                await Task.Run(() => _sensorManager.Connect(portName));

                timer_sensor.Start();
                UpdateButtonState(btn, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection Failed: {ex.Message}");
                UpdateButtonState(btn, false);
            }
        }

        private async void ConnectController_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            // 1. Disconnect
            if (_controllerPort.IsOpen)
            {
                try
                {
                    timerSender.Stop(); // Stop sending
                    _controllerPort.Close();
                    UpdateButtonState(btn, false);
                    MessageBox.Show("Controller Disconnected.");
                }
                catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
                return;
            }

            // 2. Connect
            try
            {
                string portName = ExtractPortName(cboControllerPort.Text);
                if (string.IsNullOrEmpty(portName))
                {
                    MessageBox.Show("Please select a port.");
                    return;
                }

                btn.Enabled = false;
                btn.Text = "Connecting...";

                await Task.Run(() =>
                {
                    _controllerPort.PortName = portName;
                    _controllerPort.BaudRate = CONTROLLER_BAUD_RATE;
                    _controllerPort.Parity = Parity.None;
                    _controllerPort.StopBits = StopBits.One;
                    _controllerPort.Handshake = Handshake.None;
                    _controllerPort.DataBits = 8;
                    _controllerPort.DtrEnable = true;
                    _controllerPort.Open();
                });

                // Note: timerSender.Start() is not called here (waiting for Start PID button)
                UpdateButtonState(btn, true);
                MessageBox.Show($"Controller Connected: {portName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Controller Error: {ex.Message}");
                UpdateButtonState(btn, false);
            }
        }

        private async void UpdatePortList()
        {
            cboSensorPort.Items.Clear();
            cboControllerPort.Items.Clear();
            btnRefreshPorts.Enabled = false;

            try
            {
                var ports = await Task.Run(() =>
                {
                    var list = new List<string>();
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                        {
                            foreach (var item in searcher.Get()) list.Add(item["Caption"]?.ToString() ?? "");
                        }
                    }
                    catch { /* WMI fallback */ }

                    if (list.Count == 0) list.AddRange(SerialPort.GetPortNames());
                    return list.Distinct().OrderBy(x => x).ToList();
                });

                foreach (var p in ports)
                {
                    cboSensorPort.Items.Add(p);
                    cboControllerPort.Items.Add(p);
                }

                if (cboSensorPort.Items.Count > 0) cboSensorPort.SelectedIndex = 0;
                if (cboControllerPort.Items.Count > 0) cboControllerPort.SelectedIndex = 0;
            }
            finally { btnRefreshPorts.Enabled = true; }
        }
        #endregion

        #region Data Transmission (Optimized + CRC)
        // Buffer Reuse to prevent GC pressure
        private readonly byte[] _txBuffer = new byte[15];

        private void timerSender_Tick(object sender, EventArgs e)
        {
            if (_controllerPort == null || !_controllerPort.IsOpen) return;

            // Header
            _txBuffer[0] = HEADER_BYTE; // 0xA0

            // Write Floats to Buffer (optimized helper)
            CrcUtils.WriteFloat(_txBuffer, 1, (float)_outputX);
            CrcUtils.WriteFloat(_txBuffer, 5, (float)_outputY);
            CrcUtils.WriteFloat(_txBuffer, 9, (float)_outputZ);

            // CRC Calculation (Zero Allocation)
            // Calculate over first 13 bytes (Header + 3 Floats)
            ushort crc = CrcUtils.CalculateCrc(_txBuffer, 13);
            
            // Start Little Endian
            _txBuffer[13] = (byte)(crc & 0xFF);
            _txBuffer[14] = (byte)(crc >> 8);

            // Debug text (Allocates string, but necessary for UI)
            if (debug_label_x.Visible) // Only update if visible
                debug_label_x.Text = "TX: " + BitConverter.ToString(_txBuffer);

            try
            {
                _controllerPort.Write(_txBuffer, 0, _txBuffer.Length);
            }
            catch { }
        }
        #endregion

        #region Satellite & Map Logic (Async Loading)
        private async void btnLoadMapData_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                ofd.Title = "Select Geomagnetic Grid Data";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // Lock button to prevent re-clicking
                    if (btn != null)
                    {
                        btn.Enabled = false;
                        btn.Text = "Processing...";
                    }

                    try
                    {
                        // =========================================================
                        // Part 1: Background Calculation (Background Thread)
                        // =========================================================
                        await Task.Run(() =>
                        {
                            string[] lines = File.ReadAllLines(ofd.FileName);
                            int fullRows = lines.Length;

                            // [Tuning] Reduce resolution slightly for speed (very important for Contour)
                            // step = 1 (highest detail, slow), step = 2 (4x faster), step = 3 (very fast)
                            int step = 2;

                            int newRows = 180 / step;
                            int newCols = 360 / step;

                            // Create Array
                            var intensityData = new double[newCols, newRows];
                            var latData = new double[newRows];
                            var lonData = new double[newCols];

                            // Use Parallel Loop for faster String -> Double conversion
                            Parallel.For(0, newRows, i =>
                            {
                                int originalLatIndex = i * step;
                                if (originalLatIndex >= fullRows) return;

                                string line = lines[originalLatIndex];
                                var parts = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                                for (int j = 0; j < newCols; j++)
                                {
                                    int originalLonIndex = j * step;
                                    if (originalLonIndex >= parts.Length) break;

                                    if (double.TryParse(parts[originalLonIndex], out double val))
                                    {
                                        intensityData[j, i] = val;
                                    }

                                    // Create X-axis (a bit redundant but Parallel is fast)
                                    lonData[j] = -180 + originalLonIndex;
                                }
                                // Create Y-axis
                                latData[i] = -90 + originalLatIndex;
                            });

                            // Pass values back to Global variable (Data only)
                            // Lat/Lon axes will be recreated on the fly or passed via other variables
                            // For simplicity, we will recreate axes in the UI Thread
                            this._intensityResults = intensityData;
                        });

                        // =========================================================
                        // Part 2: Draw Graph (UI Thread)
                        // =========================================================
                        if (btn != null) btn.Text = "Drawing...";

                        if (plotViewInt2.Model == null)
                        {
                            plotViewInt2.Model = new PlotModel { Title = "Geomagnetic Field Map" };
                            plotViewInt2.Model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Longitude" });
                            plotViewInt2.Model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Latitude" });
                        }

                        // 1. Clear old maps (remove both Contour and HeatMap if present)
                        var oldMaps = plotViewInt2.Model.Series
                            .Where(s => s is ContourSeries || s is HeatMapSeries).ToList();
                        foreach (var map in oldMaps) plotViewInt2.Model.Series.Remove(map);

                        // 2. Remove old color axes (if any)
                        var oldAxes = plotViewInt2.Model.Axes.OfType<LinearColorAxis>().ToList();
                        foreach (var axis in oldAxes) plotViewInt2.Model.Axes.Remove(axis);

                        // 3. Create new data axes, matching the reduced Step
                        int drawStep = 2; // Must match above
                        int drawRows = 180 / drawStep;
                        int drawCols = 360 / drawStep;
                        double[] lats = new double[drawRows];
                        double[] lons = new double[drawCols];
                        for (int i = 0; i < drawRows; i++) lats[i] = -90 + (i * drawStep);
                        for (int j = 0; j < drawCols; j++) lons[j] = -180 + (j * drawStep);

                        // 4. Create ContourSeries
                        var cs = new ContourSeries
                        {
                            Title = "Intensity",
                            ColumnCoordinates = lons,
                            RowCoordinates = lats,
                            Data = _intensityResults,

                            // [Config] Set lines to be aesthetic and not cluttered
                            ContourLevelStep = 2000,   // Draw lines every 2000 nT (if number is small, lines will be too dense and freeze)
                            LabelStep = 2,             // Label every 2 lines
                            StrokeThickness = 1.0,     // Line thickness
                            LineStyle = LineStyle.Solid,
                            Color = OxyColors.Automatic // Let OxyPlot choose line color or use OxyColors.Black
                        };

                        // 5. Insert at the bottom (Index 0)
                        plotViewInt2.Model.Series.Insert(0, cs);

                        // 6. Check if satellite track exists, if missing, add it back
                        if (_seriesSatTrack != null && !plotViewInt2.Model.Series.Contains(_seriesSatTrack))
                        {
                            plotViewInt2.Model.Series.Add(_seriesSatTrack);
                        }

                        plotViewInt2.Model.InvalidatePlot(true);
                        MessageBox.Show("Map Loaded Successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                    finally
                    {
                        if (btn != null)
                        {
                            btn.Text = "Load Map Data";
                            btn.Enabled = true;
                        }
                    }
                }
            }
        }

        private void SatPosTimer_Tick(object sender, EventArgs e)
        {
            if (TimeSpeed.Value != 0) _simTimeOffset += TimeSpeed.Value;
            var currentTime = DateTime.UtcNow.AddSeconds(_simTimeOffset);

            if (TimeSim_label != null) TimeSim_label.Text = currentTime.ToString("yyyy-MM-dd HH:mm:ss");

            var satPos = _satService.CalculatePosition(currentTime);

            if (_seriesSatTrack != null && plotViewInt2.Model != null)
            {
                _seriesSatTrack.Points.Clear();
                _seriesSatTrack.Points.Add(new DataPoint(satPos.Lon, satPos.Lat));
                plotViewInt2.Model.InvalidatePlot(true);
            }

            tbxSatLat.Text = satPos.Lat.ToString("F3");
            tbxSatLon.Text = satPos.Lon.ToString("F3");
            tbxSatHeight.Text = satPos.Alt.ToString("F3");
            tbxX.Text = satPos.X.ToString("F3");
            tbxY.Text = satPos.Y.ToString("F3");
            tbxZ.Text = satPos.Z.ToString("F3");

            var mag = _geomagnetismCalculator.TryCalculate(new Geo.Coordinate(satPos.Lat, satPos.Lon), currentTime);
            _setpointX = mag.X; _setpointY = mag.Y; _setpointZ = mag.Z;

            textSetpointXMag.Text = _setpointX.ToString("F3");
            textSetpointYMag.Text = _setpointY.ToString("F3");
            textSetpointZMag.Text = _setpointZ.ToString("F3");
            tbxSatInt.Text = mag.TotalIntensity.ToString("F3");
        }
        #endregion

        #region PID Controls & Start Logic
        private double RunPidLogic(PidController pid, double sp, double pv, Control display, GraphManager graph)
        {
            double result = pid.Calculate(sp, pv);
            if (display.InvokeRequired) display.BeginInvoke(new Action(() => display.Text = result.ToString("F2")));
            else display.Text = result.ToString("F2");
            graph.Update(sp, pv);
            return result;
        }

        private void UpdatePidParams(PidController pid, string kp, string ki, string kd)
        {
            if (double.TryParse(kp, out double p)) pid.Kp = p;
            if (double.TryParse(ki, out double i)) pid.Ki = i;
            if (double.TryParse(kd, out double d)) pid.Kd = d;
        }

        /// <summary>
        /// Consolidated Start/Stop handler for PID control axes
        /// </summary>
        private void TogglePidAxis(Timer timer, PidController pid, TextBox kpBox, TextBox kiBox, TextBox kdBox, 
            Button btn, string axisName)
        {
            if (timer.Enabled)
            {
                // Stop PID
                timer.Stop();
                if (btn != null)
                {
                    btn.Text = $"Start PID {axisName}";
                    btn.BackColor = Color.LightGreen;
                }
            }
            else
            {
                // Start PID
                UpdatePidParams(pid, kpBox.Text, kiBox.Text, kdBox.Text);
                timer.Start();
                
                // Start data sender if controller connected
                if (!timerSender.Enabled && _controllerPort.IsOpen)
                {
                    timerSender.Start();
                }
                
                if (btn != null)
                {
                    btn.Text = $"Pause PID {axisName}";
                    btn.BackColor = Color.Salmon;
                }
            }
        }

        // [Logic: Start PID + Start Sending Data]
        private void StartX_Click(object sender, EventArgs e) => 
            TogglePidAxis(_timerPidX, _pidX, KpX, KiX, KdX, sender as Button, "X");
       
        private void TuningXkp_Click(object sender, EventArgs e) => UpdatePidParams(_pidX, KpX.Text, KiX.Text, KdX.Text);
        private void ResetKpX_Click(object sender, EventArgs e) => KpX.Text = "0";

        private void StartY_Click(object sender, EventArgs e) => 
            TogglePidAxis(_timerPidY, _pidY, KpY, KiY, KdY, sender as Button, "Y");

        private void TuningYkp_Click(object sender, EventArgs e) => UpdatePidParams(_pidY, KpY.Text, KiY.Text, KdY.Text);
        private void ResetKpY_Click(object sender, EventArgs e) => KpY.Text = "0";

        private void StartZ_Click(object sender, EventArgs e) => 
            TogglePidAxis(_timerPidZ, _pidZ, KpZ, KiZ, KdZ, sender as Button, "Z");

        private void TuningZkp_Click(object sender, EventArgs e) => UpdatePidParams(_pidZ, KpZ.Text, KiZ.Text, KdZ.Text);
        private void ResetKpZ_Click(object sender, EventArgs e) => KpZ.Text = "0";
        #endregion

        #region Config & Helpers
        private void SaveSystemConfig()
        {
            try
            {
                double.TryParse(KpX.Text, out double kpx); _appConfig.PidX.Kp = kpx;
                double.TryParse(KiX.Text, out double kix); _appConfig.PidX.Ki = kix;
                double.TryParse(KdX.Text, out double kdx); _appConfig.PidX.Kd = kdx;

                double.TryParse(KpY.Text, out double kpy); _appConfig.PidY.Kp = kpy;
                double.TryParse(KiY.Text, out double kiy); _appConfig.PidY.Ki = kiy;
                double.TryParse(KdY.Text, out double kdy); _appConfig.PidY.Kd = kdy;

                double.TryParse(KpZ.Text, out double kpz); _appConfig.PidZ.Kp = kpz;
                double.TryParse(KiZ.Text, out double kiz); _appConfig.PidZ.Ki = kiz;
                double.TryParse(KdZ.Text, out double kdz); _appConfig.PidZ.Kd = kdz;

                AppConfig.Save(_appConfig, ConfigFilePath);
                MessageBox.Show($"Saved to: {ConfigFilePath}");
            }
            catch (Exception ex) { MessageBox.Show("Save Error: " + ex.Message); }
        }

        private void LoadSystemConfig()
        {
            try
            {
                _appConfig = AppConfig.Load(ConfigFilePath);
                KpX.Text = _appConfig.PidX.Kp.ToString(); KiX.Text = _appConfig.PidX.Ki.ToString(); KdX.Text = _appConfig.PidX.Kd.ToString();
                KpY.Text = _appConfig.PidY.Kp.ToString(); KiY.Text = _appConfig.PidY.Ki.ToString(); KdY.Text = _appConfig.PidY.Kd.ToString();
                KpZ.Text = _appConfig.PidZ.Kp.ToString(); KiZ.Text = _appConfig.PidZ.Ki.ToString(); KdZ.Text = _appConfig.PidZ.Kd.ToString();
            }
            catch { }
        }

        private string ExtractPortName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var parts = input.Split(':');
            var port = parts[0].Trim();
            if (!port.StartsWith("COM") && input.Contains("(COM"))
            {
                int idx = input.IndexOf("(COM");
                return input.Substring(idx + 1, 4).TrimEnd(')');
            }
            return port;
        }

        // ==========================================
        // 10. Buttons & UI Events
        // ==========================================
        private void btnCalMagnati_Click(object sender, EventArgs e)
        {
            double lat = 0, lon = 0;

            // 1. รับค่าละติจูด/ลองจิจูดจากกล่องข้อความ
            if (double.TryParse(tbxLat.Text, out lat) && double.TryParse(tbxLon.Text, out lon))
            {
                try
                {
                    // 2. ใช้เครื่องคิดเลขตัวกลางที่ประกาศไว้แล้ว (ไม่ต้อง new Wmm2020 ใหม่ทุกรอบ)
                    // _geomagnetismCalculator ถูกสร้างใน InitializeSystem() แล้ว

                    Geo.Coordinate coordinate = new Geo.Coordinate(lat, lon);

                    // คำนวณค่าสนามแม่เหล็ก
                    GeomagnetismResult result = _geomagnetismCalculator.TryCalculate(coordinate, DateTime.UtcNow);

                    // 3. แสดงผลลัพธ์
                    tbxCal.Clear();
                    tbxCal.Text += "--- Manual Calculation ---" + Environment.NewLine;
                    tbxCal.Text += "Model: WMM2025" + Environment.NewLine; 
                    tbxCal.Text += "Declination: " + result.Declination.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Inclination: " + result.Inclination.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Horizontal Intensity: " + result.HorizontalIntensity.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Total Intensity: " + result.TotalIntensity.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "X: " + result.X.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Y: " + result.Y.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Z: " + result.Z.ToString("F4") + Environment.NewLine;
                    tbxCal.Text += "Time: " + DateTime.UtcNow.ToString() + Environment.NewLine;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Calculation Error: " + ex.Message);
                }
            }
            else
            {
                // แจ้งเตือนถ้าใส่เลขผิด
                tbxLon.Text = "Invalid Input";
                tbxLat.Text = "Invalid Input";
                MessageBox.Show("Please enter valid Latitude and Longitude numbers.");
            }
        }
        private void TimeSim_label_Click(object sender, EventArgs e) { if (!string.IsNullOrEmpty(TimeSim_label.Text)) Clipboard.SetText(TimeSim_label.Text); }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                _satService.SetTLE(tbx4.Text, tbx5.Text, tbx6.Text);
                if (_seriesSatTrack == null)
                {
                    if (plotViewInt2.Model == null) plotViewInt2.Model = new PlotModel { Title = "Map" };
                    _seriesSatTrack = new LineSeries { MarkerType = MarkerType.Circle, Color = OxyColors.Blue, StrokeThickness = 0 };
                    plotViewInt2.Model.Series.Add(_seriesSatTrack);
                }
                SatPosTimer.Start();
                if (Track != null) Track.Text = "Stop Tracking";
                MessageBox.Show("Tracking Started");
            }
            catch (Exception ex) { MessageBox.Show("TLE Error: " + ex.Message); }
        }

        private void Write_Btn_Click(object sender, EventArgs e)
        {
            if (!_isLogging)
            {
                string name = Name_Txb.Text.Trim();
                if (string.IsNullOrEmpty(name)) name = $"DataLog_{DateTime.Now:yyyyMMdd_HHmmss}";
                if (!name.EndsWith(".csv")) name += ".csv";
                _logFileName = name;

                try
                {
                    if (!File.Exists(_logFileName)) File.WriteAllText(_logFileName, "Timestamp,MagX,MagY,MagZ,SetX,SetY,SetZ,ErrX,ErrY,ErrZ,OutX,OutY,OutZ,KpX,KiX,KdX,KpY,KiY,KdY,KpZ,KiZ,KdZ\n");
                    _isLogging = true; _logCount = 0;
                    Write_Btn.Text = "STOP Saving"; Write_Btn.BackColor = Color.Salmon;
                    MessageBox.Show($"Started logging to: {_logFileName}");
                }
                catch (Exception ex) { MessageBox.Show($"File Error: {ex.Message}"); }
            }
            else
            {
                _isLogging = false;
                Write_Btn.Text = "Write / Start"; Write_Btn.BackColor = Color.LightGray;
                MessageBox.Show($"Logging Stopped. Total: {_logCount} rows.");
            }
        }

        private void ZerorizeBtn_Click(object sender, EventArgs e)
        {
            // 1. ส่งคำสั่ง Reset ไปที่ Hardware (ตามโค้ดเดิม)
            if (_sensorManager.IsOpen)
            {
                try
                {
                    // ส่งคำสั่ง Zerorize (Toggle)
                    _sensorManager.Write(new byte[] { 0x2A, 0x30, 0x30, 0x5A, 0x4E, 0x0D }); // 4E = ON/Toggle
                    Console.WriteLine("Zero Toggle Sent.");
                }
                catch (Exception ex) { MessageBox.Show("Error sending command: " + ex.Message); }
            }
            else
            {
                MessageBox.Show("Please connect sensor first");
                return;
            }

            // 2. อัปเดตค่า Reference ใน Software (ถ้าต้องการ Software Zero ด้วย)
            // _sensorService.SetZero(_magX_nT, _magY_nT, _magZ_nT); 
            // ^ เปิดบรรทัดบนนี้ถ้าต้องการให้โปรแกรมจำค่าปัจจุบันเป็น 0 ด้วย (นอกเหนือจาก Hardware Reset)
        }

        private void SendBtn_Click_2(object sender, EventArgs e)
        {
            if (_sensorManager.IsOpen) { try { _sensorManager.Write(new byte[] { 0xAA, 0xBB, 0xCC }); } catch { } }
        }

        private void capture_Click(object sender, EventArgs e)
        {
            try
            {
                var exporter = new PngExporter { Width = 1000, Height = 600 };
                exporter.ExportToFile(plotViewX.Model, $"GraphX_{DateTime.Now:HHmmss}.png");
                MessageBox.Show("Graph Captured!");
            }
            catch (Exception ex) { MessageBox.Show("Capture Error: " + ex.Message); }
        }

        
        private void btnGen_Click(object sender, EventArgs e) { /* Optional: Reset Model */ }

        // Mapped UI Events (Boilerplate)
        private void saveX2_Click(object sender, EventArgs e) => SaveSystemConfig();
        private void saveY2_Click(object sender, EventArgs e) => SaveSystemConfig();
        private void saveZ2_Click(object sender, EventArgs e) => SaveSystemConfig();
        private void readX2_Click(object sender, EventArgs e) => LoadSystemConfig();
        private void readY2_Click(object sender, EventArgs e) => LoadSystemConfig();
        private void readZ2_Click(object sender, EventArgs e) => LoadSystemConfig();
        private void btnRefreshPorts_Click(object sender, EventArgs e) => UpdatePortList();
        private void timer1_Tick(object sender, EventArgs e) => textBoxTime.Text = DateTime.UtcNow.ToString();
        private void button2_Click(object sender, EventArgs e) => timerUTC.Enabled = !timerUTC.Enabled;
        private void SatPosReset_Click(object sender, EventArgs e) { _simTimeOffset = 0; TimeSpeed.Value = 0; textSpeed.Text = "0"; }
        private void SetSpeed_Btn_Click(object sender, EventArgs e) { if (int.TryParse(textSpeed.Text, out int v)) TimeSpeed.Value = v; }
        private void TimeSpeed_Scroll(object sender, EventArgs e) => textSpeed.Text = TimeSpeed.Value.ToString();
        private void textBoxErrorX_per_TextChanged(object sender, EventArgs e) => UpdateErrorStyle(sender as TextBox);
        private void textBoxErrorY_per_TextChanged(object sender, EventArgs e) => UpdateErrorStyle(sender as TextBox);
        private void textBoxErrorZ_per_TextChanged(object sender, EventArgs e) => UpdateErrorStyle(sender as TextBox);
        private void KiX_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KdX_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KpX_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KpY_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KiY_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KdY_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KpZ_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KiZ_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void KdZ_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void tbxSatLat_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void tbxX_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void tbxY_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void tbxZ_TextChanged(object sender, EventArgs e) => ValidateDoubleInput(sender as TextBox);
        private void tbx4_TextChanged(object sender, EventArgs e) { }
        private void tbx5_TextChanged(object sender, EventArgs e) { }
        private void timer_sensor_Tick(object sender, EventArgs e) { if (_sensorManager.IsOpen && !_sensorManager.IsSensorReady) try { _sensorManager.Write(new byte[] { 0x2A, 0x30, 0x30, 0x57, 0x45, 0x0D }); } catch { } }
        private void cboControllerPort_SelectedIndexChanged(object sender, EventArgs e) { }
        private void btnMasterReset_Click(object sender, EventArgs e) { _graphX?.Clear(); _graphY?.Clear(); _graphZ?.Clear(); _pidX.Reset(); _pidY.Reset(); _pidZ.Reset(); MessageBox.Show("System Reset!"); }
        private void Track_Click(object sender, EventArgs e) { if (SatPosTimer.Enabled) { SatPosTimer.Stop(); Track.Text = "Start Tracking"; } else { SatPosTimer.Start(); Track.Text = "Stop Tracking"; } }
        private void cboSatelliteList_SelectedIndexChanged_1(object sender, EventArgs e) { if (cboSatelliteList.SelectedItem is SatelliteInfo sat && !string.IsNullOrEmpty(sat.Line1)) { tbx4.Text = sat.Name; tbx5.Text = sat.Line1; tbx6.Text = sat.Line2; } }
        private void btnTimeNow_Click(object sender, EventArgs e) => timerUTC.Enabled = !timerUTC.Enabled;
        private void buttonSetKFR_Click(object sender, EventArgs e)
        {
            try
            {
                // ตรวจสอบว่ากดปุ่มไหน และอัปเดตค่า R ของ Filter แกนนั้น

                // แกน X
                if (sender == buttonSetKFR_X && double.TryParse(textBoxKFR_X.Text, out double rx))
                {
                    // อัปเดตค่า R เข้าไปใน Filter ตัวจริงทันที
                    _calcService.FilterX.R_Val = rx;
                    MessageBox.Show($"X Filter R updated to: {rx}");
                }
                // แกน Y
                else if (sender == buttonSetKFR_Y && double.TryParse(textBoxKFR_Y.Text, out double ry))
                {
                    _calcService.FilterY.R_Val = ry;
                    MessageBox.Show($"Y Filter R updated to: {ry}");
                }
                // แกน Z
                else if (sender == buttonSetKFR_Z && double.TryParse(textBoxKFR_Z.Text, out double rz))
                {
                    _calcService.FilterZ.R_Val = rz;
                    MessageBox.Show($"Z Filter R updated to: {rz}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid Input: " + ex.Message);
            }
        }
        private void TuningXKi_Click(object sender, EventArgs e) => UpdatePidParams(_pidX, KpX.Text, KiX.Text, KdX.Text);
        private void TuningXkd_Click(object sender, EventArgs e) => UpdatePidParams(_pidX, KpX.Text, KiX.Text, KdX.Text);
        private void ResetKiX_Click(object sender, EventArgs e) => KiX.Text = "0";
        private void ResetKdX_Click(object sender, EventArgs e) => KdX.Text = "0";
        private void TuningYki_Click(object sender, EventArgs e) => UpdatePidParams(_pidY, KpY.Text, KiY.Text, KdY.Text);
        private void TuningYkd_Click(object sender, EventArgs e) => UpdatePidParams(_pidY, KpY.Text, KiY.Text, KdY.Text);
        private void ResetKiY_Click(object sender, EventArgs e) => KiY.Text = "0";
        private void ResetKdY_Click(object sender, EventArgs e) => KdY.Text = "0";
        private void TuningZki_Click(object sender, EventArgs e) => UpdatePidParams(_pidZ, KpZ.Text, KiZ.Text, KdZ.Text);
        private void TuningZkd_Click(object sender, EventArgs e) => UpdatePidParams(_pidZ, KpZ.Text, KiZ.Text, KdZ.Text);
        private void ResetKiZ_Click(object sender, EventArgs e) => KiZ.Text = "0";
        private void ResetKdZ_Click(object sender, EventArgs e) => KdZ.Text = "0";
        private void buttonSetTarget_Click(object sender, EventArgs e)
        {
            if (double.TryParse(textBoxSetpointX.Text, out double val))
            {
                // 1. อัปเดตค่าเป้าหมาย
                _setpointX = val;
                _pidX.MinOutput = double.Parse(textLowerBoundX.Text);
                _pidX.MaxOutput = double.Parse(textUpperBoundX.Text);
                // 2. โชว์ค่าที่ตั้งไว้
                textSetpointXMag.Text = _setpointX.ToString("F3");

                // 3. [สำคัญ] หยุดโหมดติดตามดาวเทียม (Manual Override)
                // ต้องหยุด SatPosTimer ไม่งั้นเดี๋ยวค่าจากดาวเทียมจะมาทับค่าที่เราตั้งเอง
                if (SatPosTimer.Enabled)
                {
                    SatPosTimer.Stop();
                    if (Track != null) Track.Text = "Start Tracking"; // คืนชื่อปุ่ม
                }
            }
        }

        // ปุ่ม Set Target แกน Y
        private void buttonSetTargetY_Click(object sender, EventArgs e)
        {
            
            if (double.TryParse(textBoxSetpointY.Text, out double val))
            {
                _setpointY = val;
                _pidY.MinOutput = double.Parse(textLowerBoundY.Text);
                _pidY.MaxOutput = double.Parse(textUpperBoundY.Text);
                textSetpointYMag.Text = _setpointY.ToString("F3");


                // หยุด Tracking เหมือนกัน
                if (SatPosTimer.Enabled)
                {
                    SatPosTimer.Stop();
                    if (Track != null) Track.Text = "Start Tracking";
                }
            }
        }

        private void textSysY2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textLowerBoundX_TextChanged(object sender, EventArgs e)
        {

        }

        private void textSensorX2_TextChanged(object sender, EventArgs e)
        {

        }

        // ปุ่ม Set Target แกน Z
        private void buttonSetTargetZ_Click(object sender, EventArgs e)
        {
            
            if (double.TryParse(textBoxSetpointZ.Text, out double val))
            {
                _setpointZ = val;
                _pidZ.MinOutput = double.Parse(textLowerBoundZ.Text);
                _pidZ.MaxOutput = double.Parse(textUpperBoundZ.Text);
                textSetpointZMag.Text = _setpointZ.ToString("F3");

                // หยุด Tracking เหมือนกัน
                if (SatPosTimer.Enabled)
                {
                    SatPosTimer.Stop();
                    if (Track != null) Track.Text = "Start Tracking";
                }
            }
        }

        // Empty event handler stubs (required by Form Designer)
        private void debug_label_x_Click(object sender, EventArgs e) { }
        private void tracktimer_Tick(object sender, EventArgs e) { }
        private void Avg_timer_Tick(object sender, EventArgs e) { }
        private void timerRefreshUI_Tick(object sender, EventArgs e) { }
        private void timerX_Tick(object sender, EventArgs e) { }
        private void timerY_Tick(object sender, EventArgs e) { }
        private void timerZ_Tick(object sender, EventArgs e) { }
        #endregion
    }
}