﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Ntreev.Library.PsdParser
{
    public sealed class ChannelInfo
    {
        private CompressionType compressionType;
        private byte[] data;
        //public long dataStartPosition;

        private readonly ChannelType type;
        private readonly int size;
        private int height;
        private int width;

        internal ChannelInfo(PSDReader reader, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.type = (ChannelType)reader.ReadInt16();
            this.size = (int)reader.ReadUInt32();
        }

        internal void LoadImage(PSDReader reader, int bpp)
        {
            long position = reader.Position;
            if (this.size > 2)
            {
                //reader.Position = this.dataStartPosition;
                this.compressionType = (CompressionType)reader.ReadInt16();
                switch (this.compressionType)
                {
                    case CompressionType.RawData:
                        this.readData(reader, bpp, this.compressionType, null);
                        return;

                    case CompressionType.RLECompressed:
                    {
                        short[] rlePackLengths = new short[this.height * 2];
                        for (int i = 0; i < this.height; i++)
                        {
                            rlePackLengths[i] = reader.ReadInt16();
                        }
                        this.readData(reader, bpp, this.compressionType, rlePackLengths);
                        return;
                    }
                }
                throw new SystemException(string.Format("Unsupport compression type {0}", this.compressionType));
            }

            reader.Position = position + this.size;
        }

        

        private void readData(PSDReader reader, int bps, CompressionType compressionType, short[] rlePackLengths)
        {
            int unpackedLength = 0;
            if (bps == 1)
            {
                unpackedLength = (this.width + 7) >> 3;
            }
            else
            {
                unpackedLength = (this.width * bps) >> 3;
            }
            this.data = new byte[unpackedLength * this.height];
            switch (compressionType)
            {
                case CompressionType.RawData:
                    reader.Read(this.data, 0, this.data.Length);
                    break;

                case CompressionType.RLECompressed:
                    for (int i = 0; i < this.height; i++)
                    {
                        byte[] buffer = new byte[rlePackLengths[i]];
                        byte[] dst = new byte[unpackedLength];
                        reader.Read(buffer, 0, rlePackLengths[i]);
                        PSDUtil.decodeRLE(buffer, dst, rlePackLengths[i], unpackedLength);
                        for (int j = 0; j < unpackedLength; j++)
                        {
                            this.data[(i * unpackedLength) + j] = dst[j];
                        }
                    }
                    break;
            }
            int num14 = bps;
            switch (num14)
            {
                case 1:
                {
                    byte[] data = this.data;
                    byte[] buffer6 = new byte[this.width * this.height];
                    int height = this.height;
                    int width = this.width;
                    uint num7 = 0;
                    int index = 0;
                    int num11 = 0;
                    for (int k = 0; k < (height * ((width + 7) >> 3)); k++)
                    {
                        byte num12 = 0x80;
                        for (int m = 0; (m < 8) && (num7 < width); m++)
                        {
                            buffer6[num11++] = ((data[index] & num12) != 0) ? ((byte) 0) : ((byte) 1);
                            num12 = (byte) (num12 >> 1);
                            num7++;
                        }
                        if (num7 >= width)
                        {
                            num7 = 0;
                        }
                        index++;
                    }
                    this.data = buffer6;
                    break;
                }
                case 8:
                    break;

                default:
                    if (num14 == 0x10)
                    {
                        byte[] buffer3 = this.data;
                        byte[] buffer4 = new byte[this.width * this.height];
                        for (int n = 0; n < this.data.Length; n++)
                        {
                            this.data[n] = buffer3[n * 2];
                        }
                        this.data = buffer4;
                    }
                    return;
            }
        }

        //public void saveData(BinaryWriter bw)
        //{
        //    uint size = this.size;
        //}

        //public void saveHeader(BinaryWriter bw)
        //{
        //    bw.Write(reader.convert(this.id));
        //    bw.Write(reader.convert(this.size));
        //}

        public bool maskChannel
        {
            get
            {

                return this.type == ChannelType.Mask;
            }
        }

        public byte[] Data
        {
            get { return this.data; }
        }

        public ChannelType Type
        {
            get { return this.type; }
        }

        public int Size
        {
            get { return this.size; }
        }

        
    }
}

