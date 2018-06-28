namespace dotNES
{
    sealed partial class APU : Addressable
    {   
        public APU(Emulator emulator) : base(emulator, 0x0017)
        {
            InitializeMemoryMap();
        }
    }
}
