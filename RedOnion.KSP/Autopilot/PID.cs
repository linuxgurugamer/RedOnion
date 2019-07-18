using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RedOnion.KSP.Autopilot
{
	[Description("PID(R) regulator parameters.")]
	public class PidParams
	{
		public PidParams() { }
		public PidParams(PidParams copyFrom)
		{
			P = copyFrom.P;
			I = copyFrom.I;
			D = copyFrom.D;
			R = copyFrom.R;
			targetChangeLimit = copyFrom.targetChangeLimit;
			outputChangeLimit = copyFrom.outputChangeLimit;
			accumulatorLimit = copyFrom.accumulatorLimit;
		}
		public PidParams(
			double P = 1.0,
			double I = 0.0,
			double D = 0.0,
			double R = 0.0)
		{
			this.P = P;
			this.I = I;
			this.D = D;
			this.R = R;
		}

		[Description("Proportional factor (strength of direct control)")]
		public double P = 1.0;
		[Description("Integral factor (dynamic error-correction, causes oscillation as side-effect)")]
		public double I = 0.0;
		[Description("Derivative factor (dumpening - applied to output, reduces the oscillation)")]
		public double D = 0.0;
		[Description("Reduction factor for accumulator"
			+ " (dumpening - applied to accumulator used by integral factor,"
			+ " works well against both oscillation and windup)")]
		public double R = 0.0;

		[Description("Maximal abs(Target - previous Target) per second."
			+ " NaN or +Inf means no limit (which is default)."
			+ " This can make the output smoother (more human-like control)"
			+ " and help prevent oscillation after target change (windup).")]
		public double targetChangeLimit = double.PositiveInfinity;

		[Description("Maximal abs(output-input)"
			+ " and also abs(target-input) for integral and reduction factors)."
			+ " Helps preventing overshooting especially after change of Target (windup)."
			+ " NaN or +Inf means no limit (which is default)")]
		public double outputChangeLimit = double.PositiveInfinity;

		[Description("Limit of abs(accumulator) used by I and R factors."
			+ " Another anti-windup measure to prevent overshooting.")]
		public double accumulatorLimit = double.PositiveInfinity;
	}
	[Description("PID(R) regulator.")]
	public class PID
	{
		protected PidParams _param = new PidParams();
		protected double _stamp, _input, _target, _output, _accu;

		[Description("Proportional factor (strength of direct control)")]
		public double P { get => _param.P; set => _param.P = value; }
		[Description("Integral factor (dynamic error-correction, causes oscillation as side-effect)")]
		public double I { get => _param.I; set => _param.I = value; }
		[Description("Derivative factor (dumpening - applied to output, reduces the oscillation)")]
		public double D { get => _param.D; set => _param.D = value; }
		[Description("Reduction factor for accumulator"
			+ " (dumpening - applied to accumulator used by integral factor,"
			+ " works well against both oscillation and windup)")]
		public double R { get => _param.R; set => _param.R = value; }

		[Description("Maximal abs(Target - previous Target) per second."
			+ " NaN or +Inf means no limit (which is default)."
			+ " This can make the output smoother (more human-like control)"
			+ " and help prevent oscillation after target change (windup).")]
		public double targetChangeLimit
		{
			get => _param.targetChangeLimit;
			set => _param.targetChangeLimit = value;
		}
		[Description("Maximal abs(output-input)"
			+ " and also abs(target-input) for integral and reduction factors)."
			+ " Helps preventing overshooting especially after change of Target (windup)."
			+ " NaN or +Inf means no limit (which is default)")]
		public double outputChangeLimit
		{
			get => _param.outputChangeLimit;
			set => _param.outputChangeLimit = value;
		}
		[Description("Limit of abs(accumulator) used by I and R factors."
			+ " Another anti-windup measure to prevent overshooting.")]
		public double accumulatorLimit
		{
			get => _param.accumulatorLimit;
			set => _param.outputChangeLimit = value;
		}

		[Description("Feedback (true state - e.g. current pitch;"
			+ " error/difference if Target is NaN)")]
		public double input { get; set; } = double.NaN;

		[Description("Desired state (set point - e.g. desired/wanted pitch;"
			+ " NaN for pure error/difference mode, which is the default)."
			+ " The computed control signal is added to Input if Target is valid,"
			+ " use error/difference mode if you want to add it to Target.")]
		public double target { get; set; } = double.NaN;

		[Description("Last computed output value (control signal,"
			+ " call Update() after changing Input/Target)")]
		public double output => _output;
		[Description("Highest output allowed")]
		public double maxOutput { get; set; } = double.PositiveInfinity;
		[Description("Lowest output allowed")]
		public double minOutput { get; set; } = double.NegativeInfinity;

		public PID() => reset();
		[Description("Reset internal state of the regulator (won't change PIDR and limits)")]
		public void reset()
		{
			_stamp = double.NaN;
			_input = double.NaN;
			_target = double.NaN;
			_output = double.NaN;
			_accu = 0.0;
		}
		[Description("Reset accumulator to zero.")]
		public void resetAccu()
			=> _accu = 0.0;

		[Description("Update output according to time elapsed (and Input and Target)")]
		public double Update()
		{
			var now = Planetarium.GetUniversalTime();
			if (now != _stamp)
			{
				Update(now - _stamp);
				_stamp = now;
			}
			return _output;
		}
		[Description("Set input and update output according to time elapsed (provided as dt)")]
		public double Update(
			[Description("Time elapsed since last update (in seconds)")]
			double dt,
			[Description("New input/feedback")]
			double input)
		{
			this.input = input;
			return Update(dt);
		}
		[Description("Set input and target and update output according to time elapsed (provided as dt)")]
		public double Update(
			[Description("Time elapsed since last update (in seconds)")]
			double dt,
			[Description("New input/feedback")]
			double input,
			[Description("New target / desired state")]
			double target)
		{
			this.input = input;
			this.target = target;
			return Update(dt);
		}
		[Description("Update output according to time elapsed (provided as dt, using current Input and Target)")]
		public double Update(
			[Description("Time elapsed since last update (in seconds)")]
			double dt)
		{
			if (double.IsNaN(dt))
				_output = input;
			else
			{
				var targetLimit = targetChangeLimit * dt;
				_target = Math.Abs(target - _target) > targetLimit ? target < 0
					? _target - targetLimit
					: _target + targetLimit
					: target; // this accounts for any of Target, this.target or targetLimit being NaN
				double error = double.IsNaN(_target) ? input : _target - input;
				double result = 0;
				if (!double.IsNaN(error))
				{
					if (!double.IsNaN(P))
						result = P * error;
					if (!double.IsNaN(I))
						_accu += I * error * dt;
					if (!double.IsNaN(_input))
					{
						double change = input - _input;
						if (!double.IsNaN(D))
							result -= D * change / dt;
						if (!double.IsNaN(R))
							_accu -= R * change * dt;
					}
				}
				if (Math.Abs(_accu) > accumulatorLimit)
					_accu = _accu < 0 ? -accumulatorLimit : accumulatorLimit;
				result += _accu;
				var outputLimit = outputChangeLimit * dt;
				if (Math.Abs(result) > outputLimit)
					result = result < 0 ? -outputLimit : outputLimit;
				_output = double.IsNaN(_target) ? result : input + result;
			}
			_input = input;
			if (_output > maxOutput)
				_output = maxOutput;
			if (_output < minOutput)
				_output = minOutput;
			return _output;
		}
	}
}
