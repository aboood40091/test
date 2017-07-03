//                  == Credits ==
// -AddrLib: actual code
// -Exzap: modifying code to apply to Wii U textures
// -AboodXD: porting, code improvements and cleaning up
// -Wexos: translating Python code to C#
// From this algorithm: https://pastebin.com/VDvs7q8Y

using System;

namespace Cafe.Imaging
{
    public static class Swizzling
    {
        public const int m_Banks = 4;
        public const int m_BanksBitCount = 2;
        public const int m_Pipes = 2;
        public const int m_PipesBitCount = 1;
        public const int m_PipeInterleaveBytes = 256;
        public const int m_PipeInterLeaveBytesBitCount = 8;
        public const int m_RowSize = 2048;
        public const int m_SwapSize = 256;
        public const int m_SplitSize = 2048;
        public const int m_ChipFamily = 2;
        public const int MicroTilePixels = 8 * 8;        
        
        public static byte[] Swizzle(UInt32 Width, UInt32 Height, UInt32 Format, UInt32 TileMode, UInt32 SwizzleValue, UInt32 Pitch, byte[] Data)
        {
            byte[] Result = new byte[Data.Length];

            bool IsCompressed = false;
            for (int i = 0; i < BCn_Formats.Length; i++)
            {
                if (BCn_Formats[i] == Format)
                {
                    IsCompressed = true;
                    break;
                }
            }
            if (IsCompressed)
            {
                Width /= 4;
                Height /= 4;
            }

            for (UInt32 y = 0; y < Height; y++)
            {
                for (UInt32 x = 0; x < Width; x++)
                {
                    UInt32 bpp = SurfaceGetBitsPerPixel(Format);

                    UInt32 PipeSwizzle = (SwizzleValue >> 8) & 1;
                    UInt32 BankSwizzle = (SwizzleValue >> 9) & 3;

                    UInt32 Position = 0;
                    if (TileMode == 0 || TileMode == 1)
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordLinear(x, y, bpp, Pitch, Height);
                    }
                    else if (TileMode == 2 || TileMode == 3)
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordMicroTiled(x, y, bpp, Pitch, Height, TileMode);
                    }
                    else
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordMacroTiled(x, y, bpp, Pitch, Height, TileMode, PipeSwizzle, BankSwizzle);
                    }

                    bpp /= 8;
                    UInt32 NewPosition = (y * Width + x) * bpp;

                    // In Python: Result[Pos_:Pos_ + bpp] = Data[Pos:Pos + bpp];
                    for (int i = 0; i < bpp; i++)
                    {
                        if (Position + i < Data.Length && NewPosition + i < Data.Length)
                        {
                            Result[Position + i] = Data[NewPosition + i];
                        }
                    }
                }
            }

            return Result;
        }
        public static byte[] Deswizzle(UInt32 Width, UInt32 Height, UInt32 Format, UInt32 TileMode, UInt32 SwizzleValue, UInt32 Pitch, byte[] Data)
        {
            byte[] Result = new byte[Data.Length];

            bool IsCompressed = false;
            for (int i = 0; i < BCn_Formats.Length; i++)
            {
                if (BCn_Formats[i] == Format)
                {
                    IsCompressed = true;
                    break;
                }
            }
            if (IsCompressed)
            {
                Width /= 4;
                Height /= 4;
            }

            for (UInt32 y = 0; y < Height; y++)
            {
                for (UInt32 x = 0; x < Width; x++)
                {
                    UInt32 bpp = SurfaceGetBitsPerPixel(Format);

                    UInt32 PipeSwizzle = (SwizzleValue >> 8) & 1;
                    UInt32 BankSwizzle = (SwizzleValue >> 9) & 3;

                    UInt32 Position = 0;
                    if (TileMode == 0 || TileMode == 1)
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordLinear(x, y, bpp, Pitch, Height);
                    }
                    else if (TileMode == 2 || TileMode == 3)
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordMicroTiled(x, y, bpp, Pitch, Height, TileMode);
                    }
                    else
                    {
                        Position = AddrLib_computeSurfaceAddrFromCoordMacroTiled(x, y, bpp, Pitch, Height, TileMode, PipeSwizzle, BankSwizzle);
                    }

                    bpp /= 8;
                    UInt32 NewPosition = (y * Width + x) * bpp;

                    // In Python: Result[Pos_:Pos_ + bpp] = Data[Pos:Pos + bpp];
                    for (int i = 0; i < bpp; i++)
                    {
                        if (NewPosition + i < Data.Length && Position + i < Data.Length)
                        {
                            Result[NewPosition + i] = Data[Position + i];
                        }
                    }
                }
            }

            return Result;
        }
                
        private static UInt32 AddrLib_computeSurfaceAddrFromCoordLinear(UInt32 x, UInt32 y, UInt32 bpp, UInt32 Pitch, UInt32 Height)
        {
            UInt32 RowOffset = y * Pitch;
            UInt32 PixOffset = x;

            UInt32 Addr = (RowOffset + PixOffset) * bpp;
            Addr /= 8;

            return Addr;
        }
        private static UInt32 AddrLib_computeSurfaceAddrFromCoordMicroTiled(UInt32 x, UInt32 y, UInt32 bpp, UInt32 Pitch, UInt32 Height, UInt32 TileMode)
        {
            UInt32 MicroTileThickness = 1;

            if (TileMode == 3)
            {
                MicroTileThickness = 4;
            }

            UInt32 MicroTileBytes = (MicroTilePixels * MicroTileThickness * bpp + 7) / 8;
            UInt32 MicroTilesPerRow = Pitch >> 3;
            UInt32 MicroTileIndexX = x >> 3;
            UInt32 MicroTileIndexY = y >> 3;

            UInt32 MicroTileOffset = MicroTileBytes * (MicroTileIndexX + MicroTileIndexY * MicroTilesPerRow);

            UInt32 PixelIndex = ComputePixelIndexWithinMicroTile(x, y, bpp, TileMode);

            UInt32 PixelOffset = bpp * PixelIndex;
            PixelOffset >>= 3;

            return PixelOffset + MicroTileOffset;
        }
        private static UInt32 AddrLib_computeSurfaceAddrFromCoordMacroTiled(UInt32 x, UInt32 y, UInt32 bpp, UInt32 Pitch, UInt32 Height, UInt32 TileMode, UInt32 PipeSwizzle, UInt32 BankSwizzle)
        {
            UInt32 NumPipes = m_Pipes;
            UInt32 NumBanks = m_Banks;
            UInt32 NumGroupBits = m_PipeInterLeaveBytesBitCount;
            UInt32 NumPipeBits = m_PipesBitCount;
            UInt32 NumBankBits = m_BanksBitCount;

            UInt32 MicroTileThickness = ComputeSurfaceThickness(TileMode);

            UInt32 MicroTileBits = bpp * (MicroTileThickness * MicroTilePixels);
            UInt32 MicroTileBytes = (MicroTileBits + 7) / 8;

            UInt32 PixelIndex = ComputePixelIndexWithinMicroTile(x, y, bpp, TileMode);
            UInt32 PixelOffset = bpp * PixelIndex;

            UInt32 ElemOffset = PixelOffset;
            UInt32 BytesPerSample = MicroTileBytes;

            UInt32 SamplesPerSlice = 0;
            UInt32 NumSampleSplits = 0;
            UInt32 NumSamples = 0;
            UInt32 SampleSlice = 0;

            if (MicroTileBytes <= m_SplitSize)
            {
                SamplesPerSlice = 1;
                NumSampleSplits = 1;
                NumSamples = 0;
                SampleSlice = 0;
            }
            else
            {
                SamplesPerSlice = m_SplitSize / BytesPerSample;
                NumSampleSplits = Math.Max(1, 1 / SamplesPerSlice);
                NumSamples = SamplesPerSlice;
                SampleSlice = ElemOffset / (MicroTileBits / NumSampleSplits);
                ElemOffset %= MicroTileBits / NumSampleSplits;
            }

            ElemOffset += 7;
            ElemOffset /= 8;

            UInt32 Pipe = ComputePipeFromCoordWoRotation(x, y);
            UInt32 Bank = ComputeBankFromCoordWoRotation(x, y);

            UInt32 BankPipe = Pipe + NumPipes * Bank;
            UInt32 Rotation = ComputeSurfaceRotationFromTileMode(TileMode);

            UInt32 Swizzle = PipeSwizzle + NumPipes * BankSwizzle;

            BankPipe ^= NumPipes * SampleSlice * ((NumBanks >> 1) + 1) ^ Swizzle;
            BankPipe %= NumPipes * NumBanks;
            Pipe = BankPipe % NumPipes;
            Bank = BankPipe / NumPipes;

            UInt32 SliceBytes = (Height * Pitch * MicroTileThickness * bpp * NumSamples + 7) / 8;
            UInt32 SliceOffset = SliceBytes * (SampleSlice / MicroTileThickness);

            UInt32 MacroTilePitch = 8 * m_Banks;
            UInt32 MacroTileHeight = 8 * m_Pipes;

            if (TileMode == 5 || TileMode == 9)
            {
                MacroTilePitch >>= 1;
                MacroTileHeight *= 2;
            }
            else if (TileMode == 6 || TileMode == 10)
            {
                MacroTilePitch >>= 2;
                MacroTileHeight *= 4;
            }

            UInt32 MacroTilesPerRow = Pitch / MacroTilePitch;
            UInt32 MacroTileBytes = (NumSamples * MicroTileThickness * bpp * MacroTileHeight * MacroTilePitch + 7) / 8;
            UInt32 MacroTileIndexX = x / MacroTilePitch;
            UInt32 MacroTileIndexY = y / MacroTileHeight;
            UInt32 MacroTileOffset = MacroTileOffset = (MacroTileIndexX + MacroTilesPerRow * (MacroTileIndexY)) * MacroTileBytes;

            if (TileMode == 8 || TileMode == 9 || TileMode == 10 || TileMode == 11 || TileMode == 14 || TileMode == 16)
            {
                byte[] BankSwapOrder = new byte[] { 0, 1, 3, 2, 6, 7, 5, 4, 0, 0 };
                UInt32 BankSwapWidth = ComputeSurfaceBankSwappedWidth(TileMode, bpp, Pitch);
                UInt32 SwapIndex = MacroTilePitch * MacroTileIndexX / BankSwapWidth;
                Bank ^= BankSwapOrder[SwapIndex & (m_Banks - 1)];
            }

            UInt32 GroupMask = ((1u << (int)NumGroupBits) - 1);
            int NumSwizzleBits = (int)(NumBankBits + NumPipeBits);

            UInt32 TotalOffset = (ElemOffset + ((MacroTileOffset + SliceOffset) >> NumSwizzleBits));

            UInt32 OffsetHigh = (TotalOffset & ~(GroupMask)) << NumSwizzleBits;
            UInt32 OffsetLow = GroupMask & TotalOffset;

            UInt32 PipeBits = Pipe << (int)NumGroupBits;
            UInt32 BankBits = Bank << (int)(NumPipeBits + NumGroupBits);

            return BankBits | PipeBits | OffsetLow | OffsetHigh;
        }

        private static UInt32 ComputePixelIndexWithinMicroTile(UInt32 x, UInt32 y, UInt32 bpp, UInt32 TileMode, UInt32 Z = 0)
        {
            UInt32 PixelBit0 = 0;
            UInt32 PixelBit1 = 0;
            UInt32 PixelBit2 = 0;
            UInt32 PixelBit3 = 0;
            UInt32 PixelBit4 = 0;
            UInt32 PixelBit5 = 0;
            UInt32 PixelBit6 = 0;
            UInt32 PixelBit7 = 0;
            UInt32 PixelBit8 = 0;

            UInt32 Thickness = ComputeSurfaceThickness(TileMode);

            if (bpp == 0x08)
            {
                PixelBit0 = x & 1;
                PixelBit1 = (x & 2) >> 1;
                PixelBit2 = (x & 4) >> 2;
                PixelBit3 = (y & 2) >> 1;
                PixelBit4 = (y & 1);
                PixelBit5 = (y & 4) >> 2;
            }
            else if (bpp == 0x10)
            {
                PixelBit0 = x & 1;
                PixelBit1 = (x & 2) >> 1;
                PixelBit2 = (x & 4) >> 2;
                PixelBit3 = y & 1;
                PixelBit4 = (y & 2) >> 1;
                PixelBit5 = (y & 4) >> 2;
            }
            else if (bpp == 0x20 || bpp == 0x60)
            {
                PixelBit0 = x & 1;
                PixelBit1 = (x & 2) >> 1;
                PixelBit2 = y & 1;
                PixelBit3 = (x & 4) >> 2;
                PixelBit4 = (y & 2) >> 1;
                PixelBit5 = (y & 4) >> 2;
            }
            else if (bpp == 0x40)
            {
                PixelBit0 = x & 1;
                PixelBit1 = y & 1;
                PixelBit2 = (x & 2) >> 1;
                PixelBit3 = (x & 4) >> 2;
                PixelBit4 = (y & 2) >> 1;
                PixelBit5 = (y & 4) >> 2;
            }
            else if (bpp == 0x80)
            {
                PixelBit0 = y & 1;
                PixelBit1 = x & 1;
                PixelBit2 = (x & 2) >> 1;
                PixelBit3 = (x & 4) >> 2;
                PixelBit4 = (y & 2) >> 1;
                PixelBit5 = (y & 4) >> 2;
            }
            else
            {
                PixelBit0 = x & 1;
                PixelBit1 = (x & 2) >> 1;
                PixelBit2 = y & 1;
                PixelBit3 = (x & 4) >> 2;
                PixelBit4 = (y & 2) >> 1;
                PixelBit5 = (y & 4) >> 2;
            }

            if (Thickness > 1)
            {
                PixelBit6 = Z & 1;
                PixelBit7 = (Z & 2) >> 1;
            }
            if (Thickness == 8)
            {
                PixelBit8 = (Z & 4) >> 2;
            }

            return ((PixelBit8 << 8) | (PixelBit7 << 7) | (PixelBit6 << 6) | 32 * PixelBit5 | 16 * PixelBit4 | 8 * PixelBit3 | 4 * PixelBit2 | PixelBit0 | 2 * PixelBit1);
        }
        private static UInt32 ComputeSurfaceThickness(UInt32 TileMode)
        {
            UInt32 Thickness = 1;

            if (TileMode == 3 || TileMode == 7 || TileMode == 11 || TileMode == 13 || TileMode == 15)
            {
                Thickness = 4;
            }
            else if (TileMode == 16 || TileMode == 17)
            {
                Thickness = 8;
            }

            return Thickness;
        }
        private static UInt32 SurfaceGetBitsPerPixel(UInt32 Format)
        {
            UInt32 hwFormat = Format & 0x3F;
            return FormatHwInfo[hwFormat * 4];
        }
        private static UInt32 ComputePipeFromCoordWoRotation(UInt32 x, UInt32 y)
        {
            return ((y >> 3) ^ (x >> 3)) & 1;
        }
        private static UInt32 ComputeBankFromCoordWoRotation(UInt32 x, UInt32 y)
        {
            UInt32 NumPipes = m_Pipes;
            UInt32 NumBanks = m_Banks;
            UInt32 Bank = 0;

            if (NumBanks == 4)
            {
                UInt32 BankBit0 = ((y / (16 * NumPipes)) ^ (x >> 3)) & 1;
                Bank = BankBit0 | 2 * (((y / (8 * NumPipes)) ^ (x >> 4)) & 1);
            }
            else if (NumBanks == 8)
            {
                UInt32 BankBit0a = ((y / (32 * NumPipes)) ^ (x >> 3)) & 1;
                Bank = BankBit0a | 2 * (((y / (32 * NumPipes)) ^ (y / (16 * NumPipes) ^ (x >> 4))) & 1) | 4 * (((y / (8 * NumPipes)) ^ (x >> 5)) & 1);
            }

            return Bank;
        }
        private static UInt32 ComputeSurfaceRotationFromTileMode(UInt32 TileMode)
        {
            UInt32 Pipes = m_Pipes;
            UInt32 Result = 0;

            if (TileMode >= 4 && TileMode <= 11)
            {
                Result = Pipes * ((m_Banks >> 1) - 1);
            }
            else if (TileMode >= 12 && TileMode <= 15)
            {
                // This syntax part...
                // if not pipes < 4
                if (Pipes >= 4)
                {
                    Result = (Pipes >> 1) - 1;
                }
                else
                {
                    Result = 1;
                }
            }

            return Result;
        }
        private static UInt32 ComputeSurfaceBankSwappedWidth(UInt32 TileMode, UInt32 bpp, UInt32 Pitch, UInt32 NumSamples = 1)
        {
            if (IsBankSwappedTileMode(TileMode) == 0)
            {
                return 0;
            }

            UInt32 NumBanks = m_Banks;
            UInt32 NumPipes = m_Pipes;
            UInt32 SwapSize = m_SwapSize;
            UInt32 RowSize = m_RowSize;
            UInt32 SplitSize = m_SplitSize;
            UInt32 GroupSize = m_PipeInterleaveBytes;
            UInt32 BytesPerSample = 8 * bpp;

            UInt32 SamplesPerTile = 0;
            UInt32 SlicesPerTile = 0;

            try
            {
                SamplesPerTile = SplitSize / BytesPerSample;
                SlicesPerTile = Math.Max(1, NumSamples / SamplesPerTile);
            }
            catch (DivideByZeroException)
            {
                SlicesPerTile = 1;
            }

            if (IsThickMacroTiled(TileMode) != 0)
            {
                NumSamples = 4;
            }

            UInt32 BytesPerTileSlice = NumSamples * BytesPerSample / SlicesPerTile;

            UInt32 Factor = ComputeMacroTileAspectRatio(TileMode);
            UInt32 SwapTiles = Math.Max(1, (SwapSize >> 1) / bpp);

            UInt32 SwapWidth = SwapTiles * 8 * NumBanks;
            UInt32 HeightBytes = NumSamples * Factor * NumPipes * bpp / SlicesPerTile;
            UInt32 SwapMax = NumPipes * NumBanks * RowSize / HeightBytes;
            UInt32 SwapMin = GroupSize * 8 * NumBanks / BytesPerTileSlice;

            UInt32 BankSwapWidth = Math.Min(SwapMax, Math.Max(SwapMin, SwapWidth));

            while (BankSwapWidth >= (2 * Pitch))
            {
                BankSwapWidth >>= 1;
            }

            return BankSwapWidth;
        }
        private static UInt32 IsBankSwappedTileMode(UInt32 TileMode)
        {
            UInt32 BankSwapped = 0;
            if (TileMode == 8 || TileMode == 9 || TileMode == 10 || TileMode == 11 || TileMode == 14 || TileMode == 15)
            {
                BankSwapped = 1;
            }
            return BankSwapped;
        }
        private static UInt32 IsThickMacroTiled(UInt32 TileMode)
        {
            UInt32 ThickMacroTile = 0;
            if (TileMode == 7 || TileMode == 11 || TileMode == 13 || TileMode == 15)
            {
                ThickMacroTile = 1;
            }
            return ThickMacroTile;
        }
        private static UInt32 ComputeMacroTileAspectRatio(UInt32 TileMode)
        {
            UInt32 Ratio = 1;

            if (TileMode == 8 || TileMode == 12 || TileMode == 14)
            {
                Ratio = 1;
            }
            else if (TileMode == 5 || TileMode == 9)
            {
                Ratio = 2;
            }
            else if (TileMode == 6 || TileMode == 10)
            {
                Ratio = 4;
            }
            return Ratio;
        }

        public static readonly UInt32[] BCn_Formats = { 0x31, 0x431, 0x32, 0x432, 0x33, 0x433, 0x34, 0x234, 0x35, 0x235 };
        public static readonly byte[] FormatHwInfo =
        {
            0x00, 0x00, 0x00, 0x01, 0x08, 0x03, 0x00, 0x01, 0x08, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01, 0x10, 0x07, 0x00, 0x00, 0x10, 0x03, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01,
            0x10, 0x0B, 0x00, 0x01, 0x10, 0x01, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01,
            0x10, 0x03, 0x00, 0x01, 0x20, 0x03, 0x00, 0x00, 0x20, 0x07, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00,
            0x20, 0x03, 0x00, 0x01, 0x20, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x20, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01,
            0x40, 0x05, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00,
            0x40, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x80, 0x03, 0x00, 0x00, 0x80, 0x03, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x10, 0x01, 0x00, 0x00,
            0x10, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x60, 0x01, 0x00, 0x00,
            0x60, 0x01, 0x00, 0x00, 0x40, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01,
            0x40, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
    }
}
