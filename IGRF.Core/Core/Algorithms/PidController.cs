using System;

namespace IGRF_Interface.Core.Algorithms
{
    /// <summary>
    /// PID (Proportional-Integral-Derivative) Controller implementation
    /// </summary>
    public class PidController
    {
        /// <summary>Proportional gain</summary>
        public double Kp { get; set; }
        
        /// <summary>Integral gain</summary>
        public double Ki { get; set; }
        
        /// <summary>Derivative gain</summary>
        public double Kd { get; set; }

        /// <summary>Maximum output value</summary>
        public double MaxOutput { get; set; } = 100.0;
        
        /// <summary>Minimum output value</summary>
        public double MinOutput { get; set; } = -100.0;

        // Internal state variables
        private double _prevError;
        private double _integral;

        /// <summary>
        /// Calculate PID control output
        /// </summary>
        /// <param name="setpoint">Desired target value</param>
        /// <param name="measurement">Current measured value</param>
        /// <returns>Control output value (clamped to Min/MaxOutput)</returns>
        public double Calculate(double setpoint, double measurement)
        {
            double error = setpoint - measurement;

            // Proportional term
            double pOut = Kp * error;

            // Integral term with anti-windup
            _integral += error;

            // Anti-windup: prevent integral from growing too large
            double iOut = Ki * _integral;
            if (iOut > MaxOutput)
            {
                iOut = MaxOutput;
                _integral = Ki != 0 ? MaxOutput / Ki : 0;
            }
            else if (iOut < MinOutput)
            {
                iOut = MinOutput;
                _integral = Ki != 0 ? MinOutput / Ki : 0;
            }

            // Derivative term
            double derivative = error - _prevError;
            double dOut = Kd * derivative;

            // Combine all terms
            double output = pOut + iOut + dOut;

            // Clamp final output to safety limits
            if (output > MaxOutput) 
                output = MaxOutput;
            else if (output < MinOutput) 
                output = MinOutput;

            // Save error for next derivative calculation
            _prevError = error;
            return output;
        }

        /// <summary>
        /// Reset PID controller internal state
        /// </summary>
        public void Reset()
        {
            _prevError = 0;
            _integral = 0;
        }
    }
}