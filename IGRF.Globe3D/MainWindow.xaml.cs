using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using IGRF.Globe3D.Services;

namespace IGRF.Globe3D
{
    public partial class MainWindow : Window
    {
        // Services
        private readonly MagneticDataService _magService;
        private readonly PipeClientService _pipeService;
        private readonly SatelliteLoader _satLoader;

        // Visual State
        private System.Windows.Threading.DispatcherTimer _animationTimer;
        private System.Windows.Media.Media3D.Model3D? _satelliteModel3D;

        // Demo Params
        private double _demoSatPhase = 0;
        private double _demoSatInclination = 51.6; // ISS approx
        private double _demoSatAltitude = 400; // km

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Services
            _magService = new MagneticDataService();
            _pipeService = new PipeClientService();
            _satLoader = new SatelliteLoader();

            // Setup animation timer (Demo mode initially)
            _animationTimer = new System.Windows.Threading.DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(50);
            _animationTimer.Tick += AnimationTimer_Tick;

            // Wire up Pipe Events
            _pipeService.OnStatusChanged += (status) =>
                Application.Current.Dispatcher.Invoke(() => ConnectionDebug.Text = status);

            _pipeService.OnDebugMessage += (msg) =>
                Application.Current.Dispatcher.Invoke(() => ConnectionDebug.Text = msg);

            _pipeService.OnSatellitePositionReceived += (lat, lon, alt) =>
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_animationTimer.IsEnabled) _animationTimer.Stop();
                    UpdateSatellitePosition(lat, lon, alt);
                });

            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. Load Magnetic Data
            string basePath = @"D:\ratchanonth\IGRF Interface Demo1.1\magnetic";
            _magService.LoadMagneticDataFiles(basePath);

            CreateMagneticFieldLines();
            UpdateInclinationVisualization();

            // 2. Create demo visuals
            CreateOrbitVisualization();
            CreateStarfield();

            // 3. Load Earth Texture & Satellite Model
            LoadEarthTexture();
            LoadSatelliteModel();

            // 4. Start services
            _animationTimer.Start();
            _pipeService.Start();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _pipeService.Stop();
        }

        private void LoadSatelliteModel()
        {
            var model = SatelliteLoader.LoadModel();
            if (model != null)
            {
                _satelliteModel3D = model;
                SatelliteModelContainer.Content = model;

                // Hide the default sphere marker if model loaded
                SatelliteMarker.Radius = 0;
                SatelliteMarker.Fill = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void LoadEarthTexture()
        {
            try
            {
                // Prioritize the user's asset path
                string[] files = {
                    @"D:\ratchanonth\IGRF Interface Demo1.1\IGRF.Globe3D\assets\Earth.jpg",
                    "Earth.jpg", "assets/Earth.jpg", "../assets/Earth.jpg"
                };

                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        var imgBrush = new ImageBrush(new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.GetFullPath(file))));
                        var matGroup = EarthSphere.Material as MaterialGroup;
                        if (matGroup != null)
                        {
                            var diffMat = matGroup.Children[0] as DiffuseMaterial;
                            if (diffMat != null) diffMat.Brush = imgBrush;
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        private void CreateStarfield()
        {
            if (StarfieldContainer == null) return;
            StarfieldContainer.Children.Clear();

            var points = new Point3DCollection();
            var rand = new Random();
            int starCount = 1500;
            double distance = 100; // Far away background

            for (int i = 0; i < starCount; i++)
            {
                // Random point on sphere
                double u = rand.NextDouble();
                double v = rand.NextDouble();
                double theta = 2 * Math.PI * u;
                double phi = Math.Acos(2 * v - 1);

                double x = distance * Math.Sin(phi) * Math.Cos(theta);
                double y = distance * Math.Sin(phi) * Math.Sin(theta);
                double z = distance * Math.Cos(phi);

                points.Add(new Point3D(x, y, z));
            }

            var stars = new PointsVisual3D
            {
                Points = points,
                Size = 1.5,
                Color = Colors.White
            };
            StarfieldContainer.Children.Add(stars);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            _demoSatPhase += 1.0; // Speed of satellite
            if (_demoSatPhase >= 360) _demoSatPhase -= 360;

            UpdateDemoSatellitePosition();
        }

        private void UpdateDemoSatellitePosition()
        {
            // Calculate position for the main demo satellite
            var pos = CalculateOrbitPosition(6371 + _demoSatAltitude, _demoSatInclination, 45, _demoSatPhase);

            // Update marker
            SatelliteMarker.Center = pos;
            SatelliteLabel.Position = new Point3D(pos.X, pos.Y + 0.5, pos.Z);

            // Update label text with geo-coordinates
            double r = Math.Sqrt(pos.X * pos.X + pos.Y * pos.Y + pos.Z * pos.Z);
            double lat = Math.Asin(pos.Y / r) * 180 / Math.PI;
            double lon = Math.Atan2(pos.Z, pos.X) * 180 / Math.PI;

            // Get magnetic data
            string inclText = "";
            if (_magService.InclinationData != null)
            {
                int latIdx = (int)Math.Round(lat + 90);
                int lonIdx = (int)Math.Round(lon + 180);
                // Clamp
                latIdx = Math.Max(0, Math.Min(_magService.InclinationData.GetLength(0) - 1, latIdx));
                lonIdx = Math.Max(0, Math.Min(_magService.InclinationData.GetLength(1) - 1, lonIdx));

                if (latIdx >= 0 && lonIdx >= 0)
                {
                    double incl = _magService.InclinationData[latIdx, lonIdx];
                    inclText = $"\nMag.Inc: {incl:F1}°";
                }
            }

            SatelliteLabel.Text = $"DEMO SAT\nLat: {lat:F1}°\nLon: {lon:F1}°{inclText}";
        }

        private void UpdateSatellitePosition(double lat, double lon, double altKm)
        {
            // Cartesian Conversion (Y-North system used in this app)
            double scale = 6.371 / 6371.0;

            // Exaggerate altitude for visualization
            double visualAlt = altKm * 3.0;
            if (visualAlt < 200) visualAlt = 200; // Minimum visual offset

            double r = 6.371 + (visualAlt * scale);

            double latRad = lat * Math.PI / 180.0;
            double lonRad = lon * Math.PI / 180.0;

            double x = r * Math.Cos(latRad) * Math.Cos(lonRad);
            double y = r * Math.Sin(latRad); // Y is Up/North
            double z = r * Math.Cos(latRad) * Math.Sin(lonRad);

            // Update Marker (Sphere)
            SatelliteMarker.Center = new Point3D(x, y, z);

            // Update 3D Model (if loaded)
            if (_satelliteModel3D != null)
            {
                // Optimization: reuse transforms if they exist
                if (SatelliteModelContainer.Transform is not Transform3DGroup existingGroup)
                {
                    var group = new Transform3DGroup();
                    var modelScale = new ScaleTransform3D(0.05, 0.05, 0.05);
                    var translate = new TranslateTransform3D(x, y, z);

                    group.Children.Add(modelScale);
                    group.Children.Add(translate);
                    SatelliteModelContainer.Transform = group;
                }
                else
                {
                    // Update existing transforms directy to avoid GC pressure
                    if (existingGroup.Children.Count >= 2 &&
                        existingGroup.Children[1] is TranslateTransform3D translate)
                    {
                        translate.OffsetX = x;
                        translate.OffsetY = y;
                        translate.OffsetZ = z;
                    }
                }
            }

            // Update Label
            var labelMsg = $"LIVE SAT\nLat: {lat:F2}°\nLon: {lon:F2}°";

            // Get magnetic data if available
            if (_magService.InclinationData != null)
            {
                int latIdx = (int)Math.Round(lat + 90);
                int lonIdx = (int)Math.Round(lon + 180);
                latIdx = Math.Max(0, Math.Min(_magService.InclinationData.GetLength(0) - 1, latIdx));
                lonIdx = Math.Max(0, Math.Min(_magService.InclinationData.GetLength(1) - 1, lonIdx));
                if (latIdx >= 0 && lonIdx >= 0)
                {
                    double incl = _magService.InclinationData[latIdx, lonIdx];
                    labelMsg += $"\nMag.Inc: {incl:F1}°";
                }
            }

            SatelliteLabel.Text = labelMsg;

            // Position label
            Vector3D posVec = new Vector3D(x, y, z);
            posVec.Normalize();
            Point3D labelPos = SatelliteMarker.Center + (posVec * 0.8);
            SatelliteLabel.Position = labelPos;

            // Camera Tracking Logic
            if (TrackSatelliteCheck.IsChecked == true && Viewport3D.Camera != null)
            {
                double cameraDist = r + 15.0;
                Point3D newCamPos = new Point3D(posVec.X * cameraDist, posVec.Y * cameraDist, posVec.Z * cameraDist);

                Viewport3D.Camera.Position = newCamPos;
                Viewport3D.Camera.LookDirection = new Vector3D(-newCamPos.X, -newCamPos.Y, -newCamPos.Z);
                Viewport3D.Camera.UpDirection = new Vector3D(0, 1, 0);
            }
        }

        private void CreateOrbitVisualization()
        {
            OrbitContainer.Children.Clear();

            // 1. Main Demo Satellite Orbit (Cyan)
            var mainOrbit = CreateOrbitPath(6371 + _demoSatAltitude, _demoSatInclination, 45, Colors.Cyan, 0.05);
            OrbitContainer.Children.Add(mainOrbit);

            // 2. Some background constellations
            Color dimColor = Color.FromArgb(100, 200, 200, 200);

            // Polar constellation
            OrbitContainer.Children.Add(CreateOrbitPath(6371 + 700, 90, 0, dimColor, 0.02));
            OrbitContainer.Children.Add(CreateOrbitPath(6371 + 700, 90, 90, dimColor, 0.02));

            // Equatorial
            OrbitContainer.Children.Add(CreateOrbitPath(6371 + 800, 0, 0, dimColor, 0.02));

            // Inclined
            OrbitContainer.Children.Add(CreateOrbitPath(6371 + 600, 45, 0, dimColor, 0.02));
            OrbitContainer.Children.Add(CreateOrbitPath(6371 + 600, -45, 120, dimColor, 0.02));
        }

        private Visual3D CreateOrbitPath(double radius, double inclinationDeg, double raanDeg, Color color, double thickness)
        {
            var points = new Point3DCollection();
            double incRad = inclinationDeg * Math.PI / 180;
            double raanRad = raanDeg * Math.PI / 180;

            // Generate circle points
            for (int i = 0; i <= 360; i += 2)
            {
                double theta = i * Math.PI / 180;
                points.Add(CalculatePositionFromKepler(radius, incRad, raanRad, theta));
            }

            var tube = new TubeVisual3D
            {
                Path = points,
                Diameter = thickness,
                ThetaDiv = 4,
                Fill = new SolidColorBrush(color)
            };

            return tube;
        }

        private Point3D CalculateOrbitPosition(double radius, double incDeg, double raanDeg, double phaseDeg)
        {
            return CalculatePositionFromKepler(radius, incDeg * Math.PI / 180, raanDeg * Math.PI / 180, phaseDeg * Math.PI / 180);
        }

        private Point3D CalculatePositionFromKepler(double r, double i, double omega, double u)
        {
            double x_std = r * (Math.Cos(omega) * Math.Cos(u) - Math.Sin(omega) * Math.Sin(u) * Math.Cos(i));
            double y_std = r * (Math.Sin(omega) * Math.Cos(u) + Math.Cos(omega) * Math.Sin(u) * Math.Cos(i));
            double z_std = r * (Math.Sin(u) * Math.Sin(i));

            // Map to WPF coords (Y is North/Up in our case, Z is depth)
            return new Point3D(x_std, z_std, y_std);
        }

        private void UpdateInclinationVisualization()
        {
            if (_magService.InclinationData == null) return;

            InclinationContainer.Children.Clear();

            int rows = _magService.InclinationData.GetLength(0);
            int cols = _magService.InclinationData.GetLength(1);

            int stepLat = 10;
            int stepLon = 15;
            double earthRadius = 6.371;
            double offset = 0.02;

            for (int r = 0; r < rows; r += stepLat)
            {
                for (int c = 0; c < cols; c += stepLon)
                {
                    double lat = -90 + r;
                    double lon = -180 + c;
                    double incl = _magService.InclinationData[r, c];

                    double latRad = lat * Math.PI / 180;
                    double lonRad = lon * Math.PI / 180;
                    double radius = earthRadius + offset;

                    double x = radius * Math.Cos(latRad) * Math.Cos(lonRad);
                    double z = radius * Math.Cos(latRad) * Math.Sin(lonRad);
                    double y = radius * Math.Sin(latRad);

                    Color color = GetInclinationColor(incl);

                    var point = new SphereVisual3D
                    {
                        Center = new Point3D(x, y, z),
                        Radius = 0.08,
                        Fill = new SolidColorBrush(color)
                    };
                    InclinationContainer.Children.Add(point);
                }
            }
        }

        private Color GetInclinationColor(double inclination)
        {
            double normalized = (inclination + 90) / 180.0;
            if (normalized < 0.5)
            {
                double t = normalized * 2;
                return Color.FromRgb((byte)0, (byte)(255 * t), (byte)(255 * (1 - t)));
            }
            else
            {
                double t = (normalized - 0.5) * 2;
                return Color.FromRgb((byte)(255 * t), (byte)(255 * (1 - t)), (byte)0);
            }
        }

        private void CreateMagneticFieldLines()
        {
            MagneticFieldLinesContainer.Children.Clear();
            for (int i = 0; i < 12; i++)
            {
                double azimuth = i * 30 * Math.PI / 180;
                var fieldLine = CreateDipoleFieldLine(azimuth, Colors.Cyan);
                if (fieldLine != null) MagneticFieldLinesContainer.Children.Add(fieldLine);
            }
        }

        private Visual3D? CreateDipoleFieldLine(double azimuth, Color color)
        {
            var points = new Point3DCollection();
            for (double theta = -80 * Math.PI / 180; theta <= 80 * Math.PI / 180; theta += 0.05)
            {
                double L = 12;
                double r = L * Math.Cos(theta) * Math.Cos(theta);
                if (r < 6.5) continue;

                double x = r * Math.Cos(theta) * Math.Cos(azimuth);
                double y = r * Math.Sin(theta);
                double z = r * Math.Cos(theta) * Math.Sin(azimuth);

                points.Add(new Point3D(x, y, z));
            }

            if (points.Count < 2) return null;

            return new TubeVisual3D
            {
                Path = points,
                Diameter = 0.08,
                ThetaDiv = 6,
                Fill = new SolidColorBrush(color) { Opacity = 0.8 }
            };
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            if (Viewport3D.Camera != null)
            {
                Viewport3D.Camera.Position = new Point3D(0, 0, 25);
                Viewport3D.Camera.LookDirection = new Vector3D(0, 0, -25);
                Viewport3D.Camera.UpDirection = new Vector3D(0, 1, 0);
            }
        }

        private void ShowFieldLines_Changed(object sender, RoutedEventArgs e)
        {
            if (MagneticFieldLinesContainer != null)
            {
                MagneticFieldLinesContainer.Children.Clear();
                if (ShowFieldLinesCheck.IsChecked == true) CreateMagneticFieldLines();
            }
        }

        private void ShowInclination_Changed(object sender, RoutedEventArgs e)
        {
            if (InclinationContainer != null)
            {
                InclinationContainer.Children.Clear();
                if (ShowInclinationCheck.IsChecked == true) UpdateInclinationVisualization();
            }
        }
    }
}