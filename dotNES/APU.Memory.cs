using System;
using System.Runtime.CompilerServices;

namespace dotNES
{
    partial class APU
    {
        public void WriteRegister(uint reg, byte val)
        {
			Console.WriteLine($"{reg:X4} = {val:X2}");

			Registers[reg] = val;

			return;

			//throw new NotImplementedException($"{reg:X4} = {val:X2}");
		}

        public byte ReadRegister(uint reg)
        {
			Console.WriteLine(reg.ToString("X2"));

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
