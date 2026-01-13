// =============================================================================
// PidController.cs
// =============================================================================
// A discrete-time PID (Proportional-Integral-Derivative) Controller implementation.
//
// PID control is a feedback control mechanism widely used in industrial control
// systems. It calculates an error value as the difference between a desired
// setpoint and a measured process variable and applies a correction based on:
//
//   Output = Kp × e(t) + Ki × ∫e(t)dt + Kd × de(t)/dt
//
// Where:
//   e(t)      = Setpoint - Measurement (Error)
//   Kp × e(t) = Proportional term (reacts to current error)
//   Ki × ∫e   = Integral term (eliminates steady-state error)
//   Kd × de/dt = Derivative term (predicts future error)
//
// Features:
//   - Anti-windup protection for integral term
//   - Configurable output limits for safety
//   - Reset functionality for controller state
//
// References:
//   - https://en.wikipedia.org/wiki/PID_controller
//   - K. J. Aström and T. Hägglund, "PID Controllers: Theory, Design, and Tuning"
// =============================================================================

using System;

namespace IGRF_Interface.Core.Algorithms
{
    /// <summary>
    /// Implements a discrete-time PID (Proportional-Integral-Derivative) controller
    /// with anti-windup protection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The PID controller is the most common feedback control algorithm used in
    /// industrial control systems. It continuously calculates an error value and
    /// applies a correction based on proportional, integral, and derivative terms.
    /// </para>
    ///
    /// <para><b>The Three Terms:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Term</term>
    ///     <description>Function</description>
    ///   </listheader>
    ///   <item>
    ///     <term><b>P (Proportional)</b></term>
    ///     <description>
    ///       Produces output proportional to current error.
    ///       Provides immediate response but cannot eliminate steady-state error alone.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><b>I (Integral)</b></term>
    ///     <description>
    ///       Accumulates past errors over time.
    ///       Eliminates steady-state error but can cause overshoot if too high.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term><b>D (Derivative)</b></term>
    ///     <description>
    ///       Responds to rate of error change.
    ///       Provides damping and reduces overshoot but amplifies noise.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para><b>Tuning Guidelines (Ziegler-Nichols):</b></para>
    /// <list type="number">
    ///   <item><description>Set Ki and Kd to 0</description></item>
    ///   <item><description>Increase Kp until system oscillates with constant amplitude (Ku)</description></item>
    ///   <item><description>Measure the oscillation period (Tu)</description></item>
    ///   <item><description>Apply: Kp = 0.6×Ku, Ki = 2×Kp/Tu, Kd = Kp×Tu/8</description></item>
    /// </list>
    ///
    /// <para><b>Manual Tuning Tips:</b></para>
    /// <list type="bullet">
    ///   <item><description>Start with Kp only, then add Ki to remove steady-state error</description></item>
    ///   <item><description>Add Kd to reduce overshoot and oscillation</description></item>
    ///   <item><description>Typical ratio: Kp : Ki : Kd ≈ 1 : 0.5 : 0.125</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Basic usage for position control:
    /// <code>
    /// var pid = new PidController
    /// {
    ///     Kp = 1.0,
    ///     Ki = 0.1,
    ///     Kd = 0.05,
    ///     MaxOutput = 100,
    ///     MinOutput = -100
    /// };
    ///
    /// // In control loop:
    /// double targetPosition = 50.0;
    /// double currentPosition = sensor.ReadPosition();
    /// double motorCommand = pid.Calculate(targetPosition, currentPosition);
    /// motor.SetSpeed(motorCommand);
    /// </code>
    /// </example>
    public class PidController
    {
        #region Gain Parameters

        /// <summary>
        /// Gets or sets the Proportional gain (Kp).
        /// </summary>
        /// <value>Default is 0. Typical range depends on system dynamics.</value>
        /// <remarks>
        /// <para>
        /// The proportional term produces an output proportional to the current error.
        /// It provides the primary driving force to reduce the error.
        /// </para>
        /// <para><b>Effects of Kp:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <b>Too Low:</b> Slow response, large steady-state error
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Too High:</b> Overshoot, oscillation, potential instability
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Optimal:</b> Fast response with acceptable overshoot
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// <b>Note:</b> P-only control cannot completely eliminate steady-state error
        /// in most systems. Add integral term (Ki) to eliminate residual error.
        /// </para>
        /// </remarks>
        public double Kp { get; set; }

        /// <summary>
        /// Gets or sets the Integral gain (Ki).
        /// </summary>
        /// <value>Default is 0. Typical range: 0 to Kp/2.</value>
        /// <remarks>
        /// <para>
        /// The integral term accumulates past errors and eliminates steady-state error.
        /// It continues to increase output as long as any error exists.
        /// </para>
        /// <para><b>Effects of Ki:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <b>Too Low:</b> Slow elimination of steady-state error
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Too High:</b> Overshoot, slow settling, integral windup
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Optimal:</b> Eliminates steady-state error without excessive overshoot
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// This implementation includes anti-windup protection to prevent the integral
        /// term from growing unbounded when the output is saturated.
        /// </para>
        /// </remarks>
        public double Ki { get; set; }

        /// <summary>
        /// Gets or sets the Derivative gain (Kd).
        /// </summary>
        /// <value>Default is 0. Typical range: 0 to Kp/8.</value>
        /// <remarks>
        /// <para>
        /// The derivative term responds to the rate of change of error, providing
        /// predictive control. It acts as a "brake" when the error is changing rapidly.
        /// </para>
        /// <para><b>Effects of Kd:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <b>Too Low:</b> May not adequately dampen oscillations
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Too High:</b> Amplifies sensor noise, causes jerky control
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Optimal:</b> Reduces overshoot and settling time
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// <b>Warning:</b> The derivative term is sensitive to noise. Consider using
        /// a low-pass filter on measurements (e.g., <see cref="KalmanFilter"/>) before
        /// applying derivative control.
        /// </para>
        /// </remarks>
        public double Kd { get; set; }

        #endregion

        #region Output Limits

        /// <summary>
        /// Gets or sets the maximum output value (upper saturation limit).
        /// </summary>
        /// <value>Default is 100.0.</value>
        /// <remarks>
        /// The output will be clamped to this maximum value for safety.
        /// This also triggers anti-windup when the integral term would exceed this limit.
        /// </remarks>
        public double MaxOutput { get; set; } = 100.0;

        /// <summary>
        /// Gets or sets the minimum output value (lower saturation limit).
        /// </summary>
        /// <value>Default is -100.0.</value>
        /// <remarks>
        /// The output will be clamped to this minimum value for safety.
        /// This also triggers anti-windup when the integral term would fall below this limit.
        /// </remarks>
        public double MinOutput { get; set; } = -100.0;

        #endregion

        #region Internal State

        /// <summary>
        /// Stores the previous error for derivative calculation.
        /// </summary>
        private double _prevError;

        /// <summary>
        /// Accumulated integral of the error.
        /// </summary>
        private double _integral;

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the PID control output based on the current setpoint and measurement.
        /// </summary>
        /// <param name="setpoint">
        /// The desired target value that the controller should achieve.
        /// </param>
        /// <param name="measurement">
        /// The current measured value from the sensor/process.
        /// </param>
        /// <returns>
        /// The control output value, clamped between <see cref="MinOutput"/> and <see cref="MaxOutput"/>.
        /// </returns>
        /// <remarks>
        /// <para><b>Calculation Steps:</b></para>
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       <b>Error:</b> e = setpoint - measurement
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Proportional:</b> P_out = Kp × e
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Integral:</b> I_out = Ki × Σe (with anti-windup)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Derivative:</b> D_out = Kd × (e - e_prev)
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Output:</b> clamp(P_out + I_out + D_out, MinOutput, MaxOutput)
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// <b>Anti-Windup:</b> When the integral term would exceed the output limits,
        /// it is clamped and the integral accumulator is adjusted to prevent windup.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var pid = new PidController { Kp = 2.0, Ki = 0.5, Kd = 0.1 };
        ///
        /// // Control loop (e.g., called every 100ms)
        /// while (running)
        /// {
        ///     double target = 100.0;  // Desired position
        ///     double current = sensor.Read();
        ///     double output = pid.Calculate(target, current);
        ///     actuator.SetOutput(output);
        ///
        ///     Thread.Sleep(100);
        /// }
        /// </code>
        /// </example>
        public double Calculate(double setpoint, double measurement)
        {
            // ═══════════════════════════════════════════════════════════════
            // STEP 1: Calculate Error
            // ═══════════════════════════════════════════════════════════════
            double error = setpoint - measurement;

            // ═══════════════════════════════════════════════════════════════
            // STEP 2: Proportional Term
            // ═══════════════════════════════════════════════════════════════
            // P_out = Kp × error
            // Immediate response proportional to current error
            double pOut = Kp * error;

            // ═══════════════════════════════════════════════════════════════
            // STEP 3: Integral Term (with Anti-Windup)
            // ═══════════════════════════════════════════════════════════════
            // Accumulate error for integral calculation
            _integral += error;

            // Calculate integral output
            double iOut = Ki * _integral;

            // Anti-windup: Clamp integral output and back-calculate accumulator
            // This prevents the integral term from growing unbounded when output is saturated
            if (iOut > MaxOutput)
            {
                iOut = MaxOutput;
                // Back-calculate integral to prevent windup
                _integral = Ki != 0 ? MaxOutput / Ki : 0;
            }
            else if (iOut < MinOutput)
            {
                iOut = MinOutput;
                // Back-calculate integral to prevent windup
                _integral = Ki != 0 ? MinOutput / Ki : 0;
            }

            // ═══════════════════════════════════════════════════════════════
            // STEP 4: Derivative Term
            // ═══════════════════════════════════════════════════════════════
            // D_out = Kd × (error - previous_error)
            // Responds to rate of change of error
            double derivative = error - _prevError;
            double dOut = Kd * derivative;

            // ═══════════════════════════════════════════════════════════════
            // STEP 5: Combine Terms and Clamp Output
            // ═══════════════════════════════════════════════════════════════
            double output = pOut + iOut + dOut;

            // Clamp final output to safety limits
            if (output > MaxOutput)
            {
                output = MaxOutput;
            }
            else if (output < MinOutput)
            {
                output = MinOutput;
            }

            // Save error for next derivative calculation
            _prevError = error;

            return output;
        }

        /// <summary>
        /// Resets the PID controller's internal state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method clears the integral accumulator and previous error,
        /// effectively restarting the controller from a clean state.
        /// </para>
        /// <para><b>Use Cases:</b></para>
        /// <list type="bullet">
        ///   <item><description>When switching to a new setpoint that is significantly different</description></item>
        ///   <item><description>After a system fault or emergency stop</description></item>
        ///   <item><description>When transitioning between manual and automatic control</description></item>
        ///   <item><description>When the controlled system has been physically moved/reset</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Reset controller after emergency stop
        /// if (emergencyStopCleared)
        /// {
        ///     pid.Reset();
        ///     Console.WriteLine("PID controller reset after emergency stop");
        /// }
        /// </code>
        /// </example>
        public void Reset()
        {
            _prevError = 0;
            _integral = 0;
        }

        #endregion
    }
}

