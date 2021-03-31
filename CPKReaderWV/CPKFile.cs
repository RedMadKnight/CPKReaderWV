using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPKReaderWV
{
    public class CPKFile
    {
        public struct HeaderStruct 
        {
            public uint MagicNumber; 
            public uint PackageVersion;
            public ulong DecompressedFileSize;
            public uint Flags;
            public uint FileCount;
            public uint LocationCount;
            public uint HeaderSector;
            public uint FileSizeBitCount;
            public uint FileLocationCountBitCount;
            public uint FileLocationIndexBitCount;
            public uint LocationBitCount;
            public uint CompSectorToDecomOffsetBitCount;
            public uint DecompSectorToCompSectorBitCount;
            public uint CRC;
            public uint Unknown;
            public uint ReadSectorSize;
            public uint CompSectorSize;
            
        }
        public struct FileInfo 
        {
            public ulong dwHash;
            public uint nSize;
            public uint nLocationCount;
            public uint nLocationIndex;
            public uint nLocationIndexOverride;
        }

        public struct Locations
        {
            public uint index;
            public ulong offset;
            public uint file;
        }

        public string CPKFilePath;
        public HeaderStruct Header;
        public FileInfo[] HashTable;
        public Locations[] Location;
        public helper help;
        public int Reverse = -1;
        public uint CurrentReadOffset = 64;
        public uint HeaderSize;
        public uint HeaderReadSectorCount;
        public uint CompSectorCount;
        public uint FirstSectorPosition;
    }
}
