using System;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace dotNES
{
    sealed partial class APU : Addressable
    {
		private XAudio2 device;
		private MasteringVoice master;
		private WaveFormat format;
		private SourceVoice source;
		private AudioBuffer[] buffers;
		private DataPointer[] pointers;
		private int bufferIndex;

		public APU(Emulator emulator) : base(emulator, 0x0017)
        {
            InitializeMemoryMap();

			InitializeAudio();
		}
	}
}
