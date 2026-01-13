#nullable enable

using IGRF_Interface.Core.Algorithms;

namespace IGRF.Tests.Algorithms;

/// <summary>
/// Unit tests for KalmanFilter class
/// </summary>
public class KalmanFilterTests
{
    [Fact]
    public void Constructor_SetsInitialState()
    {
        // Arrange & Act
        var filter = new KalmanFilter(initialState: 100.0, initialCovariance: 5.0, q: 2.0, r: 50.0);

        // Assert
        Assert.Equal(100.0, filter.State);
        Assert.Equal(5.0, filter.Covariance);
        Assert.Equal(2.0, filter.Q);
        Assert.Equal(50.0, filter.R);
    }

    [Fact]
    public void Filter_WithConstantInput_ConvergesToInput()
    {
        // Arrange
        var filter = new KalmanFilter(initialState: 0.0, q: 1.0, r: 10.0);
        const double constantValue = 50.0;

        // Act - Run filter with constant value multiple times
        double result = 0;
        for (int i = 0; i < 100; i++)
        {
            result = filter.Filter(constantValue);
        }

        // Assert - Should converge close to the constant value
        Assert.InRange(result, 49.0, 51.0);
    }

    [Fact]
    public void Filter_ReducesNoiseVariance()
    {
        // Arrange
        var filter = new KalmanFilter(initialState: 0.0, q: 0.1, r: 100.0);
        var random = new Random(42); // Fixed seed for reproducibility
        const double trueValue = 100.0;
        const double noiseStdDev = 10.0;

        var rawValues = new List<double>();
        var filteredValues = new List<double>();

        // Act - Generate noisy measurements and filter them
        for (int i = 0; i < 200; i++)
        {
            double noise = random.NextDouble() * 2 * noiseStdDev - noiseStdDev;
            double measurement = trueValue + noise;
            rawValues.Add(measurement);
            filteredValues.Add(filter.Filter(measurement));
        }

        // Calculate variance of raw vs filtered (second half, after convergence)
        var rawVariance = CalculateVariance(rawValues.Skip(100).ToList());
        var filteredVariance = CalculateVariance(filteredValues.Skip(100).ToList());

        // Assert - Filtered variance should be significantly less than raw
        Assert.True(filteredVariance < rawVariance, 
            $"Filtered variance ({filteredVariance:F2}) should be less than raw ({rawVariance:F2})");
    }

    [Fact]
    public void Filter_HighR_ProducesSmootherOutput()
    {
        // Arrange
        var filterLowR = new KalmanFilter(initialState: 0.0, q: 1.0, r: 10.0);
        var filterHighR = new KalmanFilter(initialState: 0.0, q: 1.0, r: 1000.0);

        var measurements = new[] { 10.0, 20.0, 15.0, 25.0, 12.0 };

        // Act
        var resultsLowR = measurements.Select(m => filterLowR.Filter(m)).ToList();
        var resultsHighR = measurements.Select(m => filterHighR.Filter(m)).ToList();

        // Calculate "jerkiness" (sum of absolute differences)
        double jerkLowR = CalculateJerk(resultsLowR);
        double jerkHighR = CalculateJerk(resultsHighR);

        // Assert - High R should produce smoother (less jerky) output
        Assert.True(jerkHighR < jerkLowR,
            $"High R jerk ({jerkHighR:F2}) should be less than Low R ({jerkLowR:F2})");
    }

    [Fact]
    public void Filter_WithControlInput_AffectsState()
    {
        // Arrange
        var filter = new KalmanFilter(initialState: 0.0, q: 1.0, r: 100.0);
        const double controlInput = 5.0;

        // Act
        double result1 = filter.Filter(measurement: 0.0, controlInput: 0.0);
        var filterWithControl = new KalmanFilter(initialState: 0.0, q: 1.0, r: 100.0);
        double result2 = filterWithControl.Filter(measurement: 0.0, controlInput: controlInput);

        // Assert - Control input should increase the state
        Assert.True(result2 > result1,
            $"With control input ({result2:F2}) should be greater than without ({result1:F2})");
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        var filter = new KalmanFilter(initialState: 0.0);
        
        // Run some filtering
        filter.Filter(100.0);
        filter.Filter(100.0);
        
        // Act
        filter.Reset(initialState: 50.0, initialCovariance: 10.0);

        // Assert
        Assert.Equal(50.0, filter.State);
        Assert.Equal(10.0, filter.Covariance);
    }

    [Fact]
    public void Covariance_DecreasesOverTime()
    {
        // Arrange
        var filter = new KalmanFilter(initialState: 0.0, initialCovariance: 100.0, q: 0.1, r: 10.0);
        double initialCovariance = filter.Covariance;

        // Act - Run filter multiple times
        for (int i = 0; i < 50; i++)
        {
            filter.Filter(50.0);
        }

        // Assert - Covariance should decrease (filter becomes more confident)
        Assert.True(filter.Covariance < initialCovariance,
            $"Final covariance ({filter.Covariance:F2}) should be less than initial ({initialCovariance:F2})");
    }

    private static double CalculateVariance(List<double> values)
    {
        double mean = values.Average();
        return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
    }

    private static double CalculateJerk(List<double> values)
    {
        double jerk = 0;
        for (int i = 1; i < values.Count; i++)
        {
            jerk += Math.Abs(values[i] - values[i - 1]);
        }
        return jerk;
    }
}
