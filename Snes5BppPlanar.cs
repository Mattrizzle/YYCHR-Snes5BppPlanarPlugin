using System;
using CharactorLib.Common;
using CharactorLib.Format;
using System.Drawing;

namespace Snes5BppPlanar
{
    public class Snes5BppPlanarFormat : FormatBase
    {
        public Snes5BppPlanarFormat()
        {
            base.FormatText = "[8][8]";
            base.Name = "5BPP SNES Planar(NBA Jam/TE)";
            base.Extension = "smc,sfc,fig";
            base.Author = "Mattrizzle";
            base.Url = "https://github.com/Mattrizzle/YYCHR-Snes5BppPlanar-Plugin";
            // Flags
            base.Readonly = false;
            base.IsCompressed = false;
            base.EnableAdf = true;
            base.IsSupportMirror = true;
            base.IsSupportRotate = false;    
            // Settings
            base.ColorBit = 5;
            base.ColorNum = 32;
            base.CharWidth = 8;
            base.CharHeight = 8;
            // Settings for Image Convert
            base.Width = 128;
            base.Height = 128;
        }

        /* offsets into SNES bitplanes per tile */
        byte[] PlaneOffset = { 0x00, 0x01, 0x10, 0x11, 0x20 };

        /* bytemap: contains image data in YYCHR internal format */
        /* data: contains image data in source format (ROM) */

        /* convert from source format (ROM) to YYCHR graphics, one tile at a time */
        public override void ConvertMemToChr(Byte[] data, int addr, Bytemap bytemap, int px, int py)
        {
            for (int x = 0; x < CharWidth; x++)
            {
                for (int y = 0; y < CharHeight; y++)
                {
                    byte pixel = 0x00;

                    /* gather bits from bitplanes and combine into pixel */
                    for (int b = 0; b < ColorBit; b++)
                    {
                        byte tmp;
                        if (b == 4)
                        {
                            tmp = data[addr + PlaneOffset[b] + 1 * y];
                        }
                        else {
                            tmp = data[addr + PlaneOffset[b] + 2 * y];
                        }
                        tmp &= (byte)(1 << (CharWidth - 1 - x));
                        tmp >>= CharWidth - 1 - x;
                        tmp <<= b;
                        pixel |= tmp;
                    }

                    /* get pixel address for requested coordinates in YYCHR bitmap */
                    Point p = base.GetAdvancePixelPoint(px + x, py + y);
                    int bytemapAddr = bytemap.GetPointAddress(p.X, p.Y);

                    /* write pixel to YYCHR bitmap */
                    bytemap.Data[bytemapAddr] = pixel;
                }
            }
        }

        /* convert from YYCHR graphics to source format (ROM), one tile at a time */
        public override void ConvertChrToMem(Byte[] data, int addr, Bytemap bytemap, int px, int py)
        {
            for (int x = 0; x < CharWidth; x++)
            {
                /* mask to substitute pixel in ROM data */
                byte mask = (byte)~(1 << (CharWidth - 1 - x));
                for (int y = 0; y < CharHeight; y++)
                {
                    /* read pixel from YYCHR bitmap */
                    Point p = base.GetAdvancePixelPoint(px + x, py + y);
                    int bytemapAddr = bytemap.GetPointAddress(p.X, p.Y);
                    byte pixel = bytemap.Data[bytemapAddr];
                    for (int b = 0; b < ColorBit; b++)
                    {
                        /* scatter bits from pixel across ROM bitplanes */
                        int tileAddr;
                        if (b == 4)
                        {
                            tileAddr = addr + PlaneOffset[b] + 1 * y;
                        }
                        else {
                            tileAddr = addr + PlaneOffset[b] + 2 * y;
                        }
                        byte orig = (byte)(data[tileAddr] & mask);
                        byte bit = (byte)(pixel & (1 << b));
                        bit >>= b;
                        bit <<= (7 - x);
                        orig |= bit;
                        data[tileAddr] = orig;
                    }
                }
            }
        }
    }
}
