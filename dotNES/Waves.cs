using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotNES
{
	public static class Waves
	{
		private static readonly double TwoPi = Math.PI * 2.0;

		public static double Sine(double time, double frequency, double phase)
		{
			return Math.Sin(((time * frequency) + phase) * TwoPi);
		}

		public static int Square(double time, double frequency, double phase)
		{
			return Math.Sign(Sine(time, frequency, phase));
		}

		public static double Triangle(double time, double frequency, double phase)
		{
			var t = (time * frequency) + phase;

			return 1.0 - (4.0 * Math.Abs(Math.Round(t - 0.25) - (t - 0.25)));
		}

		public static double Sawtooth(double time, double frequency, double phase)
		{
			var t = (time * frequency) + phase;

			return 2.0 * (t - Math.Floor(t + 0.5));
		}
	}
}
