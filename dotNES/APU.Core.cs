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
			//master.SetVolume(0.1f);

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
			//for (var timer = 0; timer < timers.Length; timer++)
			//	if (timers[timer] > 0)
			//		timers[timer]--;

			if (timers[0] > 0 &&
				(Registers[0x00] & 0x20) == 0)
				timers[0]--;

			if (timers[1] > 0 &&
				(Registers[0x04] & 0x20) == 0)
				timers[1]--;

			if (timers[2] > 0 &&
				(Registers[0x08] & 0x80) == 0)
				timers[2]--;

			if (timers[3] > 0 &&
				(Registers[0x0c] & 0x20) == 0)
				timers[3]--;

			if (timers[4] > 0 &&
				(Registers[0x08] & 0x80) == 0)
				timers[4]--;
		}

		private void Source_BufferEnd(IntPtr obj)
		{
			for (int x = 0; x < data.Length; x += 2)
			{
				var pulse = 0d;
				var pulse2 = 0d;
				var triangle = 0d;
				var noise = 0d;
				var delta = 0d;

				if ((Registers[0x15] & 1) != 0)
				{
					var frequency = Registers[0x02] | ((Registers[0x03] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[0] > 0)
						pulse = Waves.Square(time, frequency2, 0);
				}

				if ((Registers[0x15] & 2) != 0)
				{
					var frequency = Registers[0x06] | ((Registers[0x07] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (frequency >= 8 && timers[1] > 0)
						pulse2 = Waves.Square(time, frequency2, 0);
				}

				if ((Registers[0x15] & 4) != 0)
				{
					var frequency = Registers[0x0a] | ((Registers[0x0b] & 0x07) << 8);

					var frequency2 = 1789773.0 / (32 * (frequency + 1));

					if (timers[2] > 0 && timers[4] > 0)
						triangle = Waves.Triangle(time, frequency2, 0);
				}

				if ((Registers[0x15] & 8) != 0)
				{
					var frequency = Registers[0x0e] | ((Registers[0x0f] & 0x07) << 8);

					var frequency2 = 1789773.0 / (16 * (frequency + 1));

					if (timers[3] > 0)
						noise = (random.NextDouble() * 2.0) - 1.0;
				}

				var value2 = (short)((((pulse + pulse2) * 0.0752) + (triangle * 0.0851) + (noise * 0.0494) + (delta * 0.0335)) * short.MaxValue);

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
