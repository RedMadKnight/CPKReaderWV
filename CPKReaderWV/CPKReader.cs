using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPKReaderWV
{
    public class CPKReader
    {
        public string cpkpath;
        public helper help;
        public CPKFile cpkfile;
        public uint fileSize;
        public CPKFile.HeaderStruct header;
        public CPKFile.FileInfo[] fileinfo;
        public byte[] BFileInfo;
        public byte[] block2;
        public byte[] block3;
        public byte[] block4;
        public uint[] block5;
        public string[] fileNames;
        public string[] fileHash;
        public Dictionary<uint, uint> fileOffsets;
        

        public CPKReader(string path)
        {
            help = new helper();
            cpkfile = new CPKFile();
            cpkfile.CPKFilePath = path;
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.End);
            fileSize = (uint)fs.Position;
            fs.Seek(0, 0);
            ReadHeader(fs);
            SortedFileInfo(fs);
            Locations(fs);
            CompressedSectorToDecompressedSector(fs);
            DecompressedSectorToCompressedSector(fs);
            FileNameArrayOffsets(fs);
            ReadFileNames(fs);
            ReadSectors(fs);
            fs.Close();
        }

        public void ReadSectors(Stream s)
        {
            uint pos = (uint)s.Position & 0xFFFF0000;
            if ((s.Position % 0x10000) != 0)
               pos += 0x10000;
            uint sector = 1;
            uint next_sector = pos+0x4000;
            fileOffsets = new Dictionary<uint, uint>();
            s.Seek(pos, 0);
            //skip first empty record
            help.ReadU16(s); help.ReadU16(s); s.Seek(help.ReadU16(s), SeekOrigin.Current);
            while (true)
            {
                pos = (uint)s.Position;
                if(pos+0xf > next_sector)
                    {
                    Console.WriteLine("Sector : "+sector);
                    pos = next_sector;
                    next_sector += 0x4000;
                    s.Seek(pos, 0);
                    sector++;
                    if (next_sector > s.Length)
                        break;
                }
                ushort Size = help.ReadU16(s); 
                ushort flag = help.ReadU16(s); 
                ushort ComSectorSize = help.ReadU16(s);
                if (ComSectorSize == 0)
                    break;
                fileOffsets.Add(pos, ComSectorSize);
                s.Seek(ComSectorSize, SeekOrigin.Current);
            }
        }

        public void ReadHeader(Stream s)
        {
            header = new CPKFile.HeaderStruct();
            header.MagicNumber = help.ReadU32(s);
            header.PackageVersion = help.ReadU32(s);
            header.DecompressedFileSize = help.ReadU64(s);
            header.Flags = help.ReadU32(s);
            header.FileCount = help.ReadU32(s);
            header.LocationCount = help.ReadU32(s);
            header.HeaderSector = help.ReadU32(s);
            header.FileSizeBitCount = help.ReadU32(s);
            header.FileLocationCountBitCount = help.ReadU32(s);
            header.FileLocationIndexBitCount = help.ReadU32(s);
            header.LocationBitCount = help.ReadU32(s);
            header.CompSectorToDecomOffsetBitCount = help.ReadU32(s);
            header.DecompSectorToCompSectorBitCount = help.ReadU32(s);
            header.CRC = help.ReadU32(s);
            cpkfile.CurrentReadOffset = help.ReadU32(s); //start at 0 offset
            if (header.Flags == (uint)CPKArchive.CPK_FLAG_COMPRESSED)
            {
                cpkfile.HeaderSize = header.HeaderSector;
                cpkfile.HeaderReadSectorCount = (header.HeaderSector + (uint)CPKArchiveSizes.CPK_READ_SECTOR_SIZE - 1) / (uint)CPKArchiveSizes.CPK_READ_SECTOR_SIZE;
            }
            else
            {
                cpkfile.HeaderSize = (uint)CPKArchiveSizes.CPK_READ_SECTOR_SIZE * header.HeaderSector;
                cpkfile.HeaderReadSectorCount = header.HeaderSector;
            }
            cpkfile.CompSectorCount = ((uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE + fileSize - 1 - (uint)CPKArchiveSizes.CPK_READ_SECTOR_SIZE * header.HeaderSector) / (uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE;

        }

        public string PrintHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MagicNumber    : " + header.MagicNumber.ToString("X8"));
            sb.AppendLine("PackageVersion  : " + header.PackageVersion.ToString());
            sb.AppendLine("DecompressedFileSize : " + header.DecompressedFileSize.ToString());
            sb.AppendLine("Flags : " + header.Flags.ToString());
            sb.AppendLine("FileCount : " + header.FileCount.ToString());
            sb.AppendLine("LocationCount : " + header.LocationCount.ToString());
            sb.AppendLine("HeaderSector : " + header.HeaderSector.ToString());
            sb.AppendLine("FileSizeBitCount : " + header.FileSizeBitCount.ToString());
            sb.AppendLine("FileLocationCountBitCount : " + header.FileLocationCountBitCount.ToString());
            sb.AppendLine("FileLocationIndexBitCount : " + header.FileLocationIndexBitCount.ToString());
            sb.AppendLine("LocationBitCount : " + header.LocationBitCount.ToString());
            sb.AppendLine("CompSectorToDecomOffsetBitCount : " + header.CompSectorToDecomOffsetBitCount.ToString());
            sb.AppendLine("DecompSectorToCompSectorBitCount : " + header.DecompSectorToCompSectorBitCount.ToString());
            sb.AppendLine("CRC : " + header.CRC.ToString(""));
            return sb.ToString();
        }

        public void SortedFileInfo(Stream s)
        {
            uint size = 64;
            size += header.FileSizeBitCount;
            size += header.FileLocationCountBitCount;
            size += header.FileLocationIndexBitCount;
            size *= header.FileCount;
            size += 7;
            size /= 8;
            BFileInfo = new byte[size];
            s.Read(BFileInfo, 0, (int)size);
        }

        public string Print_SortedFileInfo()
        {
            fileinfo = new CPKFile.FileInfo[header.FileCount];
            StringBuilder sb = new StringBuilder();
            uint pos = 0; 
            for (int i = 0; i < header.FileCount; i++)
            {
                sb.Append((i).ToString("d6") + " : ");
                ulong u1 = help.ReadBits(BFileInfo, pos, 64);
                fileinfo[i].dwHash = u1;
                pos += 64;
                ulong u2 = help.ReadBits(BFileInfo, pos, header.FileSizeBitCount);
                fileinfo[i].nSize = (uint)u2;
                pos += header.FileSizeBitCount;
                ulong u3 = help.ReadBits(BFileInfo, pos, header.FileLocationCountBitCount);
                fileinfo[i].nLocationCount = (uint)u3;
                pos += header.FileLocationCountBitCount;
                ulong u4 = help.ReadBits(BFileInfo, pos, header.FileLocationIndexBitCount);
                fileinfo[i].nLocationIndex = (uint)u4;
                pos += header.FileLocationIndexBitCount;
                sb.Append("Hash: " + u1.ToString("X16") + " Size: " + u2.ToString() + " LocationCount: " + u3.ToString() + " LocationIndex: " + u4.ToString("d6"));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void Locations(Stream s)
        {
            uint size = header.LocationBitCount * header.LocationCount;
            size += 7;
            size /= 8;
            block2 = new byte[size];
            s.Read(block2, 0, (int)size);
        }

        public string Print_Locations()
        {
            uint[] index = new uint[header.LocationCount];
            ulong[] offset = new ulong[header.LocationCount];
            StringBuilder sb = new StringBuilder();
            uint pos = 0;
            sb.AppendLine("Locations sorted by offset!");
            sb.AppendLine("Index => Offset");
            for (int i = 0; i < header.LocationCount; i++)
            {
                offset[i] = help.ReadBits(block2, pos, header.LocationBitCount);
                pos += header.LocationBitCount;
                index[i] = (uint)i;
            }
            Array.Sort(offset, index);
            for (int i = 0; i < header.LocationCount; i++)
                sb.AppendLine((index[i]).ToString("d4") + " : " + offset[i].ToString("X8"));
            return sb.ToString();
        }

        public void CompressedSectorToDecompressedSector(Stream s)
        {
           uint size = cpkfile.CompSectorCount * header.LocationBitCount;
            size += 7;
            size /= 8;
            block3 = new byte[size];
            s.Read(block3, 0, (int)size);
        }

        public string Print_CompressedSectorToDecompressedSector()
        {
            StringBuilder sb = new StringBuilder();
            uint pos = 0;
            for (int i = 0; i < cpkfile.CompSectorCount; i++)
            {
                sb.Append((i).ToString("d6") + " : ");
                ulong u1 = help.ReadBits(block3, pos, header.LocationBitCount);
                pos += header.LocationBitCount;
                sb.Append("0x" + u1.ToString("X8"));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void DecompressedSectorToCompressedSector(Stream s)
        {
            uint size = help.GetHighestBit(cpkfile.CompSectorCount) * (((uint)header.DecompressedFileSize + (uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE - 1) / (uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE);
            size += 7;
            size /= 8;
            block4 = new byte[size];
            s.Read(block4, 0, (int)size);
        }


        public string Print_DecompressedSectorToCompressedSector()
        {
            StringBuilder sb = new StringBuilder();
            uint sizet = (((uint)header.DecompressedFileSize + (uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE - 1) / (uint)CPKArchiveSizes.CPK_COMP_SECTOR_SIZE);
            uint pos = 0;
            for (int i = 0; i < sizet; i++)
            {
                sb.Append((i).ToString("d6") + " : ");
                ulong u1 = help.ReadBits(block4, pos, help.GetHighestBit(cpkfile.CompSectorCount));
                pos += help.GetHighestBit(cpkfile.CompSectorCount);
                sb.Append(u1.ToString("d6"));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void FileNameArrayOffsets(Stream s)
        {
            block5 = new uint[header.FileCount];
            for (int i = 0; i < header.FileCount; i++)
                block5[i] = help.ReadU32(s);
        }

        public string Print_FileNameArrayOffsets()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < header.FileCount; i++)
                sb.AppendLine((i).ToString("d6") + ": 0x" + block5[i].ToString("X8"));
            return sb.ToString();
        }

        public void ReadFileNames(Stream s)
        {
            long pos = s.Position;
            fileNames = new string[header.FileCount];
            fileHash = new string[header.FileCount];
            for (int i = 0; i < header.FileCount; i++)
            {
                s.Seek(pos + block5[i], 0);
                fileNames[i] = help.ReadString(s);
                fileHash[i] = help.GetFileHash(fileNames[i]).ToString("X16");
            }
        }
    }
}
