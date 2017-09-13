﻿using System;
using System.Runtime.CompilerServices;

namespace dotNES
{
    abstract class Addressable
    {
        public delegate uint ReadDelegate(uint addr);

        public delegate void WriteDelegate(uint addr, byte val);

        protected readonly Emulator _emulator;
        protected readonly CPU.ReadDelegate[] _readMap;
        protected readonly CPU.WriteDelegate[] _writeMap;
        protected readonly uint _addressSize;
        protected Addressable(Emulator emulator, uint addressSpace)
        {
            _emulator = emulator;
            _addressSize = addressSpace;
            _readMap = new CPU.ReadDelegate[addressSpace];
            _writeMap = new CPU.WriteDelegate[addressSpace];
        }

        protected virtual void InitializeMemoryMap()
        {
            _readMap.Fill(addr => throw new NotImplementedException($"read from {addr:X4}"));

            // Some games write to addresses not mapped and expect to continue afterwards
            _writeMap.Fill((addr, val) => { });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadByte(uint addr)
        {
            addr &= _addressSize;
            return _readMap[addr](addr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(uint addr, uint val)
        {
            addr &= _addressSize;
            _writeMap[addr](addr, (byte)val);
        }

        public void MapReadHandler(uint start, uint end, CPU.ReadDelegate func)
        {
            for (uint i = start; i <= end; i++)
                _readMap[i] = func;
        }

        public void MapWriteHandler(uint start, uint end, CPU.WriteDelegate func)
        {
            for (uint i = start; i <= end; i++)
                _writeMap[i] = func;
        }
    }
}