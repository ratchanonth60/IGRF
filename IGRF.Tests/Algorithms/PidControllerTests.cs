#nullable enable

using IGRF_Interface.Core.Algorithms;

namespace IGRF.Tests.Algorithms;

/// <summary>
/// Unit tests for PidController class
/// </summary>
public class PidControllerTests
{
    [Fact]
    public void Constructor_DefaultValues_AreZero()
    {
        // Arrange & Act
        var pid = new PidController();

        // Assert
        Assert.Equal(0.0, pid.Kp);
        Assert.Equal(0.0, pid.Ki);
        Assert.Equal(0.0, pid.Kd);
        Assert.Equal(100.0, pid.MaxOutput);
        Assert.Equal(-100.0, pid.MinOutput);
    }

    [Fact]
    public void Calculate_ProportionalOnly_ReturnsProportionalError()
    {
        // Arrange
        var pid = new PidController { Kp = 2.0, Ki = 0.0, Kd = 0.0 };
        const double setpoint = 100.0;
        const double measurement = 90.0;
        const double expectedError = 10.0; // setpoint - measurement

        // Act
        double output = pid.Calculate(setpoint, measurement);

        // Assert - Output = Kp * error = 2.0 * 10.0 = 20.0
        Assert.Equal(expectedError * 2.0, output, precision: 5);
    }

    [Fact]
    public void Calculate_IntegralTerm_AccumulatesError()
    {
        // Arrange
        var pid = new PidController { Kp = 0.0, Ki = 1.0, Kd = 0.0 };
        const double setpoint = 100.0;
        const double measurement = 90.0;

        // Act - Call multiple times to accumulate integral
        pid.Calculate(setpoint, measurement);
        pid.Calculate(setpoint, measurement);
        double output = pid.Calculate(setpoint, measurement);

        // Assert - Integral should accumulate: Ki * (3 * error) = 1.0 * 30.0 = 30.0
        Assert.Equal(30.0, output, precision: 5);
    }

    [Fact]
    public void Calculate_DerivativeTerm_RespondsToErrorChange()
    {
        // Arrange
        var pid = new PidController { Kp = 0.0, Ki = 0.0, Kd = 1.0 };

        // Act - First call (no previous error)
        double output1 = pid.Calculate(setpoint: 100.0, measurement: 90.0); // error = 10
        double output2 = pid.Calculate(setpoint: 100.0, measurement: 85.0); // error = 15

        // Assert
        // First derivative = 10 - 0 = 10, so output1 = 10
        // Second derivative = 15 - 10 = 5, so output2 = 5
        Assert.Equal(10.0, output1, precision: 5);
        Assert.Equal(5.0, output2, precision: 5);
    }

    [Fact]
    public void Calculate_ClampsToMaxOutput()
    {
        // Arrange
        var pid = new PidController
        {
            Kp = 100.0,
            Ki = 0.0,
            Kd = 0.0,
            MaxOutput = 50.0,
            MinOutput = -50.0
        };

        // Act - Large error would produce output > MaxOutput
        double output = pid.Calculate(setpoint: 100.0, measurement: 0.0);

        // Assert - Should be clamped to MaxOutput
        Assert.Equal(50.0, output);
    }

    [Fact]
    public void Calculate_ClampsToMinOutput()
    {
        // Arrange
        var pid = new PidController
        {
            Kp = 100.0,
            Ki = 0.0,
            Kd = 0.0,
            MaxOutput = 50.0,
            MinOutput = -50.0
        };

        // Act - Negative large error
        double output = pid.Calculate(setpoint: 0.0, measurement: 100.0);

        // Assert - Should be clamped to MinOutput
        Assert.Equal(-50.0, output);
    }

    [Fact]
    public void Calculate_AntiWindup_PreventsIntegralWindup()
    {
        // Arrange
        var pid = new PidController
        {
            Kp = 0.0,
            Ki = 10.0,
            Kd = 0.0,
            MaxOutput = 50.0,
            MinOutput = -50.0
        };

        // Act - Apply large error many times to saturate integral
        for (int i = 0; i < 100; i++)
        {
            pid.Calculate(setpoint: 100.0, measurement: 0.0); // error = 100
        }

        // Now apply opposite error
        double output = pid.Calculate(setpoint: 0.0, measurement: 100.0); // error = -100

        // Assert - Should not take forever to come back (anti-windup working)
        // Without anti-windup, integral would be 100*100*10 = 100000, and
        // output would still be clamped to 50 for many cycles
        Assert.True(output < 0, "Output should go negative after reverse error");
    }

    [Fact]
    public void Reset_ClearsInternalState()
    {
        // Arrange
        var pid = new PidController { Kp = 1.0, Ki = 1.0, Kd = 1.0 };

        // Build up some internal state
        pid.Calculate(100.0, 0.0);
        pid.Calculate(100.0, 0.0);
        pid.Calculate(100.0, 0.0);

        // Act
        pid.Reset();

        // Assert - After reset, should behave like fresh controller
        var freshPid = new PidController { Kp = 1.0, Ki = 1.0, Kd = 1.0 };

        double resetOutput = pid.Calculate(50.0, 40.0);
        double freshOutput = freshPid.Calculate(50.0, 40.0);

        Assert.Equal(freshOutput, resetOutput, precision: 5);
    }

    [Fact]
    public void Calculate_ZeroError_ReturnsZeroForProportional()
    {
        // Arrange
        var pid = new PidController { Kp = 100.0, Ki = 0.0, Kd = 0.0 };

        // Act
        double output = pid.Calculate(setpoint: 50.0, measurement: 50.0);

        // Assert
        Assert.Equal(0.0, output);
    }

    [Fact]
    public void Calculate_NegativeError_ReturnsNegativeOutput()
    {
        // Arrange
        var pid = new PidController { Kp = 2.0, Ki = 0.0, Kd = 0.0 };

        // Act
        double output = pid.Calculate(setpoint: 40.0, measurement: 50.0);

        // Assert - Error = 40 - 50 = -10, Output = 2 * -10 = -20
        Assert.Equal(-20.0, output, precision: 5);
    }

    [Theory]
    [InlineData(1.0, 0.0, 0.0, 10.0)]   // P only
    [InlineData(0.0, 1.0, 0.0, 10.0)]   // I only  
    [InlineData(0.0, 0.0, 1.0, 10.0)]   // D only
    [InlineData(1.0, 0.5, 0.1, 16.0)]   // PID combined (P:10 + I:5 + D:1 = 16)
    public void Calculate_VariousGainCombinations(double kp, double ki, double kd, double expectedFirstOutput)
    {
        // Arrange
        var pid = new PidController { Kp = kp, Ki = ki, Kd = kd };

        // Act - First calculation with error = 10
        double output = pid.Calculate(setpoint: 100.0, measurement: 90.0);

        // Assert
        Assert.Equal(expectedFirstOutput, output, precision: 5);
    }

    [Fact]
    public void Calculate_RealWorldScenario_ConvergesToSetpoint()
    {
        // Arrange - Simulate a simple system where output affects measurement
        var pid = new PidController
        {
            Kp = 0.5,
            Ki = 0.1,
            Kd = 0.05,
            MaxOutput = 100.0,
            MinOutput = -100.0
        };

        const double setpoint = 50.0;
        double measurement = 0.0;

        // Act - Simulate 100 iterations
        for (int i = 0; i < 100; i++)
        {
            double output = pid.Calculate(setpoint, measurement);
            // Simple system: measurement moves toward setpoint based on output
            measurement += output * 0.1;
        }

        // Assert - Should have converged close to setpoint (with some tolerance for oscillation)
        Assert.InRange(measurement, setpoint - 5.0, setpoint + 5.0);
    }
}
