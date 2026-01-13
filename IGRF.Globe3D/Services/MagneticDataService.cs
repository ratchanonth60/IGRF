using System;
using System.IO;

namespace IGRF.Globe3D.Services
{
    public class MagneticDataService
    {
        public double[,]? InclinationData { get; private set; }
        public double[,]? DeclinationData { get; private set; }
        public double[,]? IntensityData { get; private set; }

        public void LoadMagneticDataFiles(string basePath)
        {
            try
            {
                string incPath = Path.Combine(basePath, "inclinationData.txt");
                string decPath = Path.Combine(basePath, "declinationData.txt");
                string intPath = Path.Combine(basePath, "intensityData.txt");

                if (File.Exists(incPath))
                    InclinationData = LoadGridData(incPath);

                if (File.Exists(decPath))
                    DeclinationData = LoadGridData(decPath);

                if (File.Exists(intPath))
                    IntensityData = LoadGridData(intPath);

                System.Diagnostics.Debug.WriteLine(
                    $"Loaded IGRF data: Inc={InclinationData != null}, Dec={DeclinationData != null}, Int={IntensityData != null}"
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading magnetic data: {ex.Message}");
            }
        }

        private double[,] LoadGridData(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            int rows = lines.Length;

            string[] firstCols = lines[0]
                .Split(new[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int cols = firstCols.Length;
            if (firstCols[cols - 1] == "_")
                cols--;

            var data = new double[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                string[] parts = lines[r]
                    .Split(new[] { ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int c = 0; c < cols && c < parts.Length; c++)
                {
                    if (parts[c] != "_" && double.TryParse(parts[c], out double val))
                    {
                        data[r, c] = val;
                    }
                }
            }
            return data;
        }
    }
}
