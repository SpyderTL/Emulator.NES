using System;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace dotNES
{
    partial class APU
    {
		private const int bufferSize = 1024;

		private void InitializeAudio()
		{
			device = new XAudio2();
			device.StartEngine();

			master = new MasteringVoice(device);

			format = new WaveFormat(44100, 16, 1);

			source = new SourceVoice(device, format);

			source.BufferEnd += Source_BufferEnd;

			buffers = new AudioBuffer[2];
			pointers = new DataPointer[buffers.Length];

			for (var buffer = 0; buffer < buffers.Length; buffer++)
			{
				pointers[buffer] = new DataPointer(Utilities.AllocateClearedMemory(bufferSize), bufferSize);
				buffers[buffer] = new AudioBuffer(pointers[buffer]);

				source.SubmitSourceBuffer(buffers[buffer], null);
			}

			source.Start();
		}

		private double time = 0.0;
		private byte[] data = new byte[bufferSize];
		private Random random = new Random();
		private int[] timers = new int[5];

		internal void TickFromPPU()
		{
			for (var timer = 0; timer < timers.Length; timer++)
				if (timers[timer] > 0)
					timers[timer]--;
		}

		private void Source_BufferEnd(IntPtr obj)
		{
			for (int x = 0; x < data.Length; x += 2)
			{
				var value = 0d;
				var count = 0;

				if ((Registers[0x15] & 1) != 0)
				{
					var frequency = Registers[0x02] | ((Registers[0x03] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[0] > 0)
					{
						value += Math.Sin(time * Math.PI * frequency2 * 2.0);
						count++;
					}
				}

				if ((Registers[0x15] & 2) != 0)
				{
					var frequency = Registers[0x06] | ((Registers[0x07] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[1] > 0)
					{
						value += Math.Sin(time * Math.PI * frequency2 * 2.0);
						count++;
					}
				}

				if ((Registers[0x15] & 4) != 0)
				{
					var frequency = Registers[0x0a] | ((Registers[0x0b] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[2] > 0)
					{
						value += Math.Sin(time * Math.PI * frequency2 * 2.0);
						count++;
					}
				}

				if ((Registers[0x15] & 8) != 0)
				{
					var frequency = Registers[0x0e] | ((Registers[0x0f] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[3] > 0)
					{
						value += (random.NextDouble() * 2.0) - 1.0;
						count++;
					}
				}

				var value2 = (short)((value / count) * short.MaxValue);

				data[x] = (byte)(value2 & 0xff);
				data[x + 1] = (byte)(value2 >> 8);

				time += 1.0 / format.SampleRate;
			}

			pointers[bufferIndex].CopyFrom(data);

			source.SubmitSourceBuffer(buffers[bufferIndex], null);

			bufferIndex++;

			if (bufferIndex == buffers.Length)
				bufferIndex = 0;
		}
	}
}
