// =============================================================================
// KalmanFilter.cs
// =============================================================================
// A 1D (scalar) Kalman Filter implementation for sensor noise reduction.
// 
// The Kalman Filter is an optimal recursive algorithm that estimates the state
// of a linear dynamic system from noisy measurements. It is widely used in:
//   - Sensor fusion and noise filtering
//   - Navigation and tracking systems
//   - Signal processing applications
//
// Mathematical Model:
//   State Equation:       x(k) = A * x(k-1) + B * u(k) + w(k)
//   Measurement Equation: z(k) = H * x(k) + v(k)
//
// Where:
//   x = State estimate
//   A = State transition matrix (how state evolves over time)
//   B = Control input matrix (effect of control signal)
//   u = Control input
//   H = Measurement matrix (relationship between state and measurement)
//   z = Measurement
//   w = Process noise (Q covariance)
//   v = Measurement noise (R covariance)
//
// References:
//   - R. E. Kalman (1960). "A New Approach to Linear Filtering and Prediction Problems"
//   - https://en.wikipedia.org/wiki/Kalman_filter
// =============================================================================

using System;

namespace IGRF_Interface.Core.Algorithms
{
    /// <summary>
    /// Implements a 1D (scalar) Kalman Filter for optimal state estimation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Kalman Filter provides optimal estimates of unknown variables by recursively
    /// processing noisy sensor measurements. It is particularly effective when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Sensor readings are noisy and need smoothing</description></item>
    ///   <item><description>Real-time filtering is required</description></item>
    ///   <item><description>System dynamics can be modeled linearly</description></item>
    /// </list>
    /// <para><b>Algorithm Steps:</b></para>
    /// <list type="number">
    ///   <item><description><b>Predict:</b> Project the state and error covariance ahead</description></item>
    ///   <item><description><b>Update:</b> Compute Kalman gain and correct the prediction with measurement</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Basic usage for filtering magnetic field sensor data:
    /// <code>
    /// // Initialize filter with first measurement
    /// var kalman = new KalmanFilter(initialState: 50000.0, q: 1, r: 100);
    /// 
    /// // In your sensor reading loop:
    /// double rawMeasurement = sensor.ReadMagneticField();
    /// double filteredValue = kalman.Filter(rawMeasurement);
    /// </code>
    /// </example>
    public class KalmanFilter
    {
        #region Configuration Parameters (System Model)

        /// <summary>
        /// Gets or sets the state transition factor (A matrix in 1D).
        /// </summary>
        /// <value>Default is 1.0 (assumes constant state model).</value>
        /// <remarks>
        /// <para>
        /// The state transition factor models how the true state evolves over time.
        /// For most sensor filtering applications where the underlying value changes slowly,
        /// a value of 1.0 is appropriate (random walk model).
        /// </para>
        /// <para><b>Examples:</b></para>
        /// <list type="bullet">
        ///   <item><description>A = 1.0: State remains constant (most common)</description></item>
        ///   <item><description>A = 0.9: State decays toward zero over time</description></item>
        ///   <item><description>A = 1.1: State grows over time</description></item>
        /// </list>
        /// </remarks>
        public double A { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the measurement factor (H matrix in 1D).
        /// </summary>
        /// <value>Default is 1.0 (direct measurement of state).</value>
        /// <remarks>
        /// The measurement factor relates the true state to the measurement.
        /// A value of 1.0 means the sensor directly measures the state without scaling.
        /// </remarks>
        public double H { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the process noise covariance (Q).
        /// </summary>
        /// <value>Default is 1.0. Typical range: 0.001 to 10.</value>
        /// <remarks>
        /// <para>
        /// Process noise represents uncertainty in the system model itself.
        /// It accounts for unmodeled dynamics and disturbances.
        /// </para>
        /// <para><b>Tuning Guidelines:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <b>Higher Q:</b> Filter responds faster to changes but is more sensitive to noise.
        ///       Use when the true value changes rapidly.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Lower Q:</b> Filter responds slower but provides smoother output.
        ///       Use when the true value changes slowly or is nearly constant.
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public double Q { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the measurement noise covariance (R).
        /// </summary>
        /// <value>Default is 100.0. Typical range: 1 to 10000.</value>
        /// <remarks>
        /// <para>
        /// Measurement noise represents the variance of the sensor readings.
        /// It can often be estimated from sensor datasheets or calibration.
        /// </para>
        /// <para><b>Tuning Guidelines:</b></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <b>Higher R:</b> Less trust in measurements, more smoothing.
        ///       Use for noisy sensors or when you trust the model more.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Lower R:</b> More trust in measurements, faster response.
        ///       Use for accurate sensors.
        ///     </description>
        ///   </item>
        /// </list>
        /// <para><b>Rule of Thumb:</b> R/Q ratio determines filter behavior. Higher ratio = smoother output.</para>
        /// </remarks>
        public double R { get; set; } = 100.0;

        /// <summary>
        /// Gets or sets the measurement noise covariance (alias for <see cref="R"/>).
        /// </summary>
        /// <remarks>
        /// This property provides backward compatibility. Use <see cref="R"/> for new code.
        /// </remarks>
        [Obsolete("Use property 'R' instead. This will be removed in a future version.")]
        public double R_Val
        {
            get => R;
            set => R = value;
        }

        #endregion

        #region State Variables

        /// <summary>
        /// Gets the current estimated state value (x̂).
        /// </summary>
        /// <value>The filtered/estimated value after processing measurements.</value>
        /// <remarks>
        /// This is the primary output of the Kalman Filter - a smoothed estimate
        /// of the true underlying value based on all measurements processed so far.
        /// </remarks>
        public double State { get; private set; }

        /// <summary>
        /// Gets the current error covariance (P).
        /// </summary>
        /// <value>The estimated uncertainty/variance of the state estimate.</value>
        /// <remarks>
        /// <para>
        /// The error covariance represents how confident the filter is in its estimate.
        /// </para>
        /// <list type="bullet">
        ///   <item><description>High P: Large uncertainty, filter will trust new measurements more</description></item>
        ///   <item><description>Low P: Filter is confident, new measurements have less effect</description></item>
        /// </list>
        /// <para>
        /// P typically converges to a steady-state value after several iterations.
        /// </para>
        /// </remarks>
        public double Covariance { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="KalmanFilter"/> class.
        /// </summary>
        /// <param name="initialState">
        /// The initial state estimate (x₀). Typically set to the first sensor measurement.
        /// </param>
        /// <param name="initialCovariance">
        /// The initial error covariance (P₀). Use higher values (e.g., 10) if uncertain
        /// about initial state, lower values (e.g., 1) if confident. Default is 1.
        /// </param>
        /// <param name="q">
        /// Process noise covariance (Q). Controls filter responsiveness. Default is 1.
        /// </param>
        /// <param name="r">
        /// Measurement noise covariance (R). Controls smoothing amount. Default is 100.
        /// </param>
        /// <example>
        /// <code>
        /// // High confidence in model, noisy sensor:
        /// var filter = new KalmanFilter(initialState: 50000, q: 0.1, r: 500);
        /// 
        /// // Rapidly changing value, accurate sensor:
        /// var filter = new KalmanFilter(initialState: 0, q: 10, r: 10);
        /// </code>
        /// </example>
        public KalmanFilter(double initialState, double initialCovariance = 1.0, double q = 1.0, double r = 100.0)
        {
            State = initialState;
            Covariance = initialCovariance;
            Q = q;
            R = r;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes a new measurement and returns the filtered state estimate.
        /// </summary>
        /// <param name="measurement">
        /// The raw sensor measurement (z). This noisy value will be filtered.
        /// </param>
        /// <param name="controlInput">
        /// Optional control input (u). Use when you have a known external input
        /// affecting the state. Default is 0 (no control input).
        /// </param>
        /// <returns>
        /// The filtered state estimate. This value is also stored in <see cref="State"/>.
        /// </returns>
        /// <remarks>
        /// <para><b>Algorithm Steps:</b></para>
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       <b>Predict (Time Update):</b>
        ///       <para>x_pred = A × x + u</para>
        ///       <para>P_pred = A × P × A + Q</para>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <b>Update (Measurement Update):</b>
        ///       <para>K = P_pred × H / (H × P_pred × H + R)</para>
        ///       <para>x = x_pred + K × (z - H × x_pred)</para>
        ///       <para>P = (1 - K × H) × P_pred</para>
        ///     </description>
        ///   </item>
        /// </list>
        /// <para>
        /// Where K is the Kalman Gain, which optimally balances the prediction
        /// and measurement based on their respective uncertainties.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var filter = new KalmanFilter(initialState: rawSensorData[0]);
        /// 
        /// foreach (var rawValue in rawSensorData)
        /// {
        ///     double filtered = filter.Filter(rawValue);
        ///     Console.WriteLine($"Raw: {rawValue:F2}, Filtered: {filtered:F2}");
        /// }
        /// </code>
        /// </example>
        public double Filter(double measurement, double controlInput = 0.0)
        {
            // ═══════════════════════════════════════════════════════════════
            // STEP 1: Time Update (Prediction)
            // ═══════════════════════════════════════════════════════════════
            // Predict the next state based on the system model
            // x_pred = A * x + B * u  (B is assumed to be 1 for control input)
            double x_pred = (A * State) + controlInput;

            // Predict the error covariance
            // P_pred = A * P * A' + Q  (A' = A for scalar case)
            double p_pred = (A * Covariance * A) + Q;

            // ═══════════════════════════════════════════════════════════════
            // STEP 2: Measurement Update (Correction)
            // ═══════════════════════════════════════════════════════════════
            // Calculate the Kalman Gain
            // K = P_pred * H' / (H * P_pred * H' + R)
            // Higher K means more trust in measurement, lower K means more trust in prediction
            double K = (p_pred * H) / ((H * p_pred * H) + R);

            // Update the state estimate with the measurement
            // x = x_pred + K * (z - H * x_pred)
            // The term (z - H * x_pred) is called the "innovation" or "residual"
            State = x_pred + K * (measurement - (H * x_pred));

            // Update the error covariance
            // P = (1 - K * H) * P_pred
            Covariance = (1.0 - (K * H)) * p_pred;

            return State;
        }

        /// <summary>
        /// Resets the filter to a new initial state.
        /// </summary>
        /// <param name="initialState">
        /// The new initial state estimate. Typically set to a known value
        /// or the latest sensor measurement.
        /// </param>
        /// <param name="initialCovariance">
        /// The new initial error covariance. Use higher values if uncertain
        /// about the initial state. Default is 1.
        /// </param>
        /// <remarks>
        /// Use this method when:
        /// <list type="bullet">
        ///   <item><description>The sensor is reconnected or recalibrated</description></item>
        ///   <item><description>A discontinuity is detected in the data</description></item>
        ///   <item><description>The system is restarted</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Reset when sensor is recalibrated
        /// if (sensorRecalibrated)
        /// {
        ///     double newReading = sensor.Read();
        ///     filter.Reset(initialState: newReading, initialCovariance: 5);
        /// }
        /// </code>
        /// </example>
        public void Reset(double initialState, double initialCovariance = 1.0)
        {
            State = initialState;
            Covariance = initialCovariance;
        }

        #endregion
    }
}