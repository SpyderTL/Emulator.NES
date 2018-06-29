using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace dotNES.Controllers
{
	public class NES001Controller : INESController
	{
		public Buttons State = Buttons.None;

		private int serialData;
		private bool strobing;

		public void Write(byte val)
		{
			serialData = (int)State;
			strobing = (val & 0x01) != 0;
		}

		public byte Read()
		{
			byte ret = (byte)(serialData & 0x01);

			if (!strobing)
				serialData >>= 1;

			return ret;
		}

		[Flags]
		public enum Buttons
		{
			None = 0,
			A = 1,
			B = 2,
			Select = 4,
			Start = 8,
			Up = 16,
			Down = 32,
			Left = 64,
			Right = 128
		}
	}
}
