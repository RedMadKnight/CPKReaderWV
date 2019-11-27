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
            rtb2.Text = cpk.PrintFileInfoBlock();
            rtb3.Text = cpk.PrintLocationBlock();
            rtb4.Text = cpk.PrintBlock3();
            
            hb3.ByteProvider = new DynamicByteProvider(cpk.block4);
            rtb5.Text = cpk.PrintBlock5();
            listBox2.Items.Clear();

            for(int x=0;x<cpk.header.FileCount;x++)
                listBox2.Items.Add((x+1).ToString("d6") + ": Hash : "+ cpk.fileHash[x] +" => "+ cpk.fileNames[x]);
            listBox3.Items.Clear();
            int count = 0;
            foreach (KeyValuePair<uint, uint> pair in cpk.fileOffsets)
                listBox3.Items.Add((count++) + ": Offset=0x" + pair.Key.ToString("X8") + " Size=0x" + pair.Value.ToString("X8"));
            //files are cut in blocks, after some FF FF FF FF goes 11-bit something, then goes the file and then unused FF-bits again
        }

        private void listBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            int n = listBox3.SelectedIndex;
            if (n == -1) return;
            KeyValuePair<uint, uint> pair = cpk.fileOffsets.ToArray()[n];
            cpk.cpkpath = tempapath;
            FileStream fs = new FileStream(cpk.cpkpath, FileMode.Open, FileAccess.Read);
            fs.Seek(pair.Key + 6, 0); //6 = 3 * 2-bytes <= must correct, not true if filecount <> LocationCount
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
    }
}
