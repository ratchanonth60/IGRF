using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace IGRF.Globe3D.Services
{
    public class SatelliteLoader
    {
        public static Model3D? LoadModel()
        {
            try
            {
                // Look for common 3D formats
                string[] files =
                {
                    @"D:\ratchanonth\IGRF Interface Demo1.1\IGRF.Globe3D\assets\3d\zarya\zarya.obj",
                    @"D:\ratchanonth\IGRF Interface Demo1.1\IGRF.Globe3D\assets\Satellite.obj",
                    @"D:\ratchanonth\IGRF Interface Demo1.1\IGRF.Globe3D\assets\Satellite.stl",
                    "Satellite.obj",
                    "Satellite.stl",
                    "assets/Satellite.obj",
                };

                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        var importer = new ModelImporter();
                        var model = importer.Load(Path.GetFullPath(file));
                        return model;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading satellite model: {ex.Message}");
            }
            return null;
        }
    }
}
