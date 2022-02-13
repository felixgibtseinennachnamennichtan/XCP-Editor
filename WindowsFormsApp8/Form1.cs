using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp8
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string FolderName;
        string FileName;
        string FileContent;
        string FileType;
        private void button1_Click(object sender, EventArgs e)
        {
            byte[] fileContent = null;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "C:\\Users\\Felix\\Downloads\\txtstuff";
                openFileDialog.Filter = "XCP files (*.xcp)|*.xcp";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    

                    //Read the contents of the file into a stream
                     fileContent= File.ReadAllBytes(openFileDialog.FileName);


                }
            }

            DecodeFile(fileContent);
            richTextBox2.Text = FolderName;
            richTextBox1.Text = FileName;
            richTextBox3.Text = FileContent;
        }
         void DecodeFile(byte[] fileContent) {
            int FileCounter; // thats a variable to keep track of where in the file i am
            byte[] foldernamelength = new byte[2];
            Array.Copy(fileContent,18,foldernamelength,0,2);
            string Foldernamelength =   Convert.ToChar(foldernamelength[0]).ToString() + Convert.ToChar(foldernamelength[1]).ToString();
            byte[] foldername = new byte[9];
            Array.Copy(fileContent, 20, foldername, 0, Int16.Parse(Foldernamelength, System.Globalization.NumberStyles.HexNumber) - 1);
            int i = 0;
            FolderName = "";
            foreach (byte b in foldername) {

                FolderName += Convert.ToChar(b);
                
            }
            FileCounter = 20 + Int16.Parse(Foldernamelength); // Update the point at which we are, dont wanna calc that everytime
                                                              // we can just copy the code from above and change the names to File
            byte[] filenamelength = new byte[2];
            Array.Copy(fileContent, FileCounter, filenamelength, 0, 2);
            string Filenamelength = Convert.ToChar(filenamelength[0]).ToString() + Convert.ToChar(filenamelength[1]).ToString();
            byte[] filename = new byte[9];
            Array.Copy(fileContent, FileCounter+2, filename, 0, Int16.Parse(Filenamelength, System.Globalization.NumberStyles.HexNumber) - 1);
             i = 0;
            FileName = "";
            foreach (byte b in filename)
            {

                FileName += Convert.ToChar(b);

            }
            //fourty bytes are trash +Filenamelength + 2
            FileCounter += 42 + Int16.Parse(Filenamelength);
            // Get the Length of the Data
            byte[] datalength = new byte[4];
            Array.Copy(fileContent, FileCounter, datalength, 0, 4);

            int DataLength = BitConverter.ToInt32(datalength,0);
            FileCounter += 4;

            //get the type of this File (GUQ is unlocked text file and GLQ is Locked textfile, the programm isnt supposed to read anything else)
            byte[] datatype = new byte[3];
            Array.Copy(fileContent, FileCounter, datatype, 0, 3);
             FileType = Convert.ToChar(datatype[0]).ToString() + Convert.ToChar(datatype[1]).ToString() + Convert.ToChar(datatype[2]).ToString();
            //technically The DataType Section is 13 bytes large, but if its anything larger than 3 then this program cant read it so why bother? 
            FileCounter += 13;
            //The Length is listed twices, but now as ASCII, so we need to skip 8 Bytes
            FileCounter += 8;
            //now we can just decode length bytes and call it a day
            //first up is the total length of the text +3
            //its little endian, so we might need to convert
            byte[] dataLen = new byte[4];
            Array.Copy(fileContent, FileCounter, dataLen, 0, 4);
            bool littleEndian = BitConverter.IsLittleEndian;
            UInt32 DataLen = BitConverter.ToUInt32(dataLen,0);
            if (!littleEndian) {
                UInt32 tmp = 0;
                tmp = DataLen >> 24;
                tmp = tmp | ((DataLen & 0xff0000) >> 8);
                tmp = tmp | ((DataLen & 0xff00) << 8);
                tmp = tmp | ((DataLen & 0xff) << 24);
                DataLen = tmp;
            }
            FileCounter += 13;
            //Now its just reading DataLen -3 Text from the file
            byte[] Data = new byte[DataLen-3];
            Array.Copy(fileContent, FileCounter, Data, 0, DataLen-3);
            FileContent = "";
            foreach (byte b in Data) {
                FileContent += Convert.ToChar(b);
            }

        }

        byte[] EncodeFile() {
            byte checksum = 0;
            List<byte> xcpFile = new List<byte>();
            //Add the Header (VCP.XDATA�5f4d4353)
            xcpFile.AddRange(new byte[] { 0x56, 0x43, 0x50, 0x2E, 0x58, 0x44, 0x41, 0x54, 0x41, 0x00, 0x35, 0x66, 0x34, 0x64, 0x34, 0x33, 0x35, 0x33 });

            foreach (byte b in new byte[] { 0x56, 0x43, 0x50, 0x2E, 0x58, 0x44, 0x41, 0x54, 0x41, 0x00 }) {
                checksum-=b;
            }
            checksum -= 0x5f;
            checksum -= 0x4d;
            checksum -= 0x43;
            checksum -= 0x53;
            //Add the Folderlength + the Schwarzenegger
            char[] Folderlength = (richTextBox2.Text.Length+1).ToString("X2").ToCharArray();
            foreach (byte b in BitConverter.GetBytes(richTextBox2.Text.Length + 1)) {
                checksum -= b;
            }
            foreach (char c in Folderlength) {
                xcpFile.Add(Convert.ToByte(c));
            }
            //Add the Foldername and the 0x00 Terminator
           char[] Folder =richTextBox2.Text.ToCharArray();
            foreach (char c in Folder) {
                xcpFile.Add(Convert.ToByte(c));
                checksum -= Convert.ToByte(c);
            }
            xcpFile.Add(0x00);
            
            //Add the Filenamelength + the Schwarzenegger
            char[] Filenamelength = (richTextBox1.Text.Length + 1).ToString("X2").ToCharArray();
            foreach (char c in Filenamelength)
            {
                xcpFile.Add(Convert.ToByte(c));
            }
            foreach (byte b in BitConverter.GetBytes(richTextBox1.Text.Length + 1))
            {
                checksum -= b;
            }
            //Add the Filename and the 0x00 Terminator
            char[] Filename = richTextBox1.Text.ToCharArray();
            foreach (char c in Filename)
            {
                xcpFile.Add(Convert.ToByte(c));
                checksum -= Convert.ToByte(c);
            }
            xcpFile.Add(0x00);
            //add 8 Bytes of 00000031 (but the num is a String!)
            char[] garbage = "00000031".ToCharArray();
            foreach (char c in garbage)
            {
                xcpFile.Add(Convert.ToByte(c));
            }
            checksum -= 0x31;
            // add the foldername again, but it has to be 16 bytes and if smaller padded with 0xff
           
            for (int i = 0; i < 16; i++)
            {
                if (i >= Folder.Length)
                {
                    xcpFile.Add(0xff);
                    checksum -= 0xff;
                }
                else {
                    xcpFile.Add(Convert.ToByte(Folder[i]));
                    checksum -= Convert.ToByte(Folder[i]);
                }
                
            }
            //same for filename
            for (int i = 0; i < 16; i++)
            {
                if (i >= Filename.Length)
                {
                    xcpFile.Add(0xff);
                    checksum -= 0xff;
                }
                else {
                    xcpFile.Add(Convert.ToByte(Filename[i]));
                    checksum -= Convert.ToByte(Filename[i]);
                }

            }
            
            //nextup is the length of the Datablock, so i just make it now and check it like that
            List<byte> DataBlock = new List<byte>();
            UInt32 length = (UInt32) richTextBox3.Text.Length + 3;
            DataBlock.AddRange(BitConverter.GetBytes(length));
            //9 Bytes of 0
            DataBlock.AddRange(new Byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            //now we have to add the actual text
            char[] data = richTextBox3.Text.ToCharArray();
            foreach (char c in data) {
                DataBlock.Add(Convert.ToByte(c));
            }
            // now the Terminator of the file
            DataBlock.Add(0x00);
            DataBlock.Add(0xff);
            //and a padding that makes the length value from before a multiple of 4
            for (; 0 != DataBlock.Count%4;) {
                DataBlock.Add(0x00);
            }
            //datablock done
            //now just add the length to the file
            UInt32 Length = (UInt32)DataBlock.Count;
            foreach (byte b in BitConverter.GetBytes(Length)) {
                checksum -= b;
            }
            xcpFile.AddRange(BitConverter.GetBytes(Length).Reverse());
            //add the Vartype padded in 0xff
            List<byte> vartype = new List<byte>();
            vartype.AddRange(new byte[] {(byte)'G',(byte)'U',(byte)'Q'});
            for (; vartype.Count < 13;) {
                vartype.Add(0xff);
            }
            foreach (byte b in vartype) {
                checksum -= b;
            }
            xcpFile.AddRange(vartype);
            char[] lenn = Length.ToString("x8").ToCharArray();
            foreach (byte b in BitConverter.GetBytes(Length)) {
                checksum -= b;
            }
            List<byte> len = new List<byte>();
            foreach (char c in lenn) {

                len.Add((byte)c);

            }
            xcpFile.AddRange(len);
            //add the DataBlock
            xcpFile.AddRange(DataBlock);
            foreach (byte b in DataBlock) {
                checksum -= b;
            }

            char[] chekksum = checksum.ToString("x2").ToCharArray();
            foreach (char c in chekksum) {
                xcpFile.Add((byte)c);
            }
            return xcpFile.ToArray();







        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog openFileDialog = new SaveFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "XCP files (*.xcp)|*.xcp";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    byte[] file = EncodeFile();
                    
                    File.WriteAllBytes(openFileDialog.FileName,file);


                }
            }
        }
    }

}
