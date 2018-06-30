using System;
using System.Runtime.CompilerServices;

namespace dotNES
{
	partial class APU
	{
		private static readonly int[] timerValues = new int[]
		{
			10,
			254,
			20,
			2,
			40,
			4,
			80,
			6,
			160,
			8,
			60,
			10,
			14,
			12,
			26,
			14,

			12,
			16,
			24,
			18,
			48,
			20,
			96,
			22,
			192,
			24,
			72,
			26,
			16,
			28,
			32,
			30
		};

		public void WriteRegister(uint reg, byte val)
		{
			//Console.WriteLine($"{reg:X4} = {val:X2}");

			Registers[reg] = val;

			switch (reg)
			{
				case 0x0003:
					timers[0] = timerValues[val >> 3] * 5000;
					break;

				case 0x0007:
					timers[1] = timerValues[val >> 3] * 5000;
					break;

				case 0x0008:
					timers[4] = val & 0x7f * 5000;
					break;

				case 0x000b:
					timers[2] = timerValues[val >> 3] * 5000;
					break;

				case 0x000f:
					timers[3] = timerValues[val >> 3] * 5000;
					break;
			}

			return;

			//throw new NotImplementedException($"{reg:X4} = {val:X2}");
		}

		public byte ReadRegister(uint reg)
		{
			//Console.WriteLine(reg.ToString("X2"));

			return Registers[reg];

			//throw new NotImplementedException(reg.ToString("X2"));

			//return 0;
		}

		protected override void InitializeMemoryMap()
		{
			base.InitializeMemoryMap();

			_emulator.Mapper.InitializeMemoryMap(this);
		}
	}
}
