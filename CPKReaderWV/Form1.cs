using Be.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CPKReaderWV
{
    public partial class Form1 : Form
    {
        public CPKReader cpk;
        public helper help;
        public string tempapath;

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "*.cpk|*.cpk";
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                status.Text = d.FileName;
                tempapath = d.FileName;
                cpk = new CPKReader(d.FileName);
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            rtb1.Text = cpk.PrintHeader();
            rtb2.Text = cpk.Print_SortedFileInfo();
            rtb3.Text = cpk.Print_Locations();
            rtb4.Text = cpk.Print_CompressedSectorToDecompressedSector();

            rbtd.Text = cpk.Print_DecompressedSectorToCompressedSector();
            rtb5.Text = cpk.Print_FileNameArrayOffsets();
            listBox2.Items.Clear();
            for (int x = 0; x < cpk.location.Length; x++)
            {
                int i = (int)cpk.location[x].index;
                int a = GetLocationIndex(i);
                listBox2.Items.Add("Location index: " + ((cpk.location[x].file)).ToString("d6") + ": Hash : " + cpk.fileHash[a] + " => " + cpk.fileNames[a]);
            }
            listBox3.Items.Clear();
            int count = 0;
            foreach (KeyValuePair<uint, uint> pair in cpk.fileOffsets)
                listBox3.Items.Add((count++) + ": Offset=0x" + pair.Key.ToString("X8") + " Size=" + pair.Value.ToString());
    
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            help = new helper();
            int n = listBox3.SelectedIndex;
            if (n == -1) return;
            KeyValuePair<uint, uint> pair = cpk.fileOffsets.ToArray()[n];
            cpk.cpkpath = tempapath;
            FileStream fs = new FileStream(cpk.cpkpath, FileMode.Open, FileAccess.Read);
            fs.Seek(pair.Key, 0);
            Console.WriteLine("DecompressedSize: "+ help.ReadU16(fs)+ " Flag: " + help.ReadU16(fs) + " CompressedSize: " + help.ReadU16(fs));
            byte[] buff = new byte[pair.Value];
            fs.Read(buff, 0, (int)pair.Value);
            fs.Close();
            byte[] tmp = { };
            try
            {
                tmp = DecompressZlib(buff);
                hb1.ByteProvider = new DynamicByteProvider(tmp);
            }
            catch { }
            if (tmp.Length < 1)
                hb1.ByteProvider = new DynamicByteProvider(buff);

        }


        public static byte[] DecompressZlib(byte[] input)
        {
            MemoryStream source = new MemoryStream(input);
            byte[] result = null;
            using (MemoryStream outStream = new MemoryStream())
            {
                using (InflaterInputStream inf = new InflaterInputStream(source))
                {
                    inf.CopyTo(outStream);
                }
                result = outStream.ToArray();
            }
            return result;
        }

        private void saveBlobToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Close();
        }


        public int GetLocationIndex(int i)
        {
            int a = -1;
            int tmp = i;
            {
                for (a = 0; a < cpk.fileinfo.Length; a++)
                {
                    tmp = i;
                    for (uint b = 0; b < cpk.fileinfo[a].nLocationCount; b++)
                    {
                        if (cpk.fileinfo[a].nLocationIndex == tmp)
                            return a;
                        tmp--;
                    }
                }
            }
            return a;
        }

    }
}
