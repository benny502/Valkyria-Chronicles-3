using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace 战场的女武神3
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsTblChk = false;
        private Dictionary<string, string> tblArr = new Dictionary<string, string>();
        private List<string> log = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_DragEnter_1(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Move;
            }
        }
        private void Window_Drop_1(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += delegate(object s, DoWorkEventArgs ev)
            {
                //string[] files = files;
                for (int i = 0; i < files.Length; i++)
                {
                    string name = files[i];
                    this.Export(name);
                }
            };
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.worker_RunWorkerCompleted);
            backgroundWorker.RunWorkerAsync();
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (log.Count > 0)
            {
                StreamWriter writer;
                if (!File.Exists("log.txt"))
                {
                    writer = File.CreateText("log.txt");
                }
                else
                {
                    writer = File.AppendText("log.txt");
                }
                writer.WriteLine("========={0}-导出=========", DateTime.Now);
                foreach (var str in log)
                {
                    writer.WriteLine(str);
                }
                log.Clear();
                writer.Close();
            }
            System.Windows.MessageBox.Show("导出结束");
        }
        private void Export(string name)
        {
            try
            {
                using (FileStream fileStream = new FileStream(name, FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        uint num = binaryReader.ReadUInt32();
                        if (num == 1095783501u)
                        {
                            int num2 = -1;
                            Dictionary<int, int> index = this.GetIndex(binaryReader, out num2);
                            if (num2 > 0)
                            {
                                List<string> list = new List<string>();
                                foreach (KeyValuePair<int, int> current in index)
                                {
                                    binaryReader.BaseStream.Seek((long)(current.Key + num2), SeekOrigin.Begin);
                                    int count = binaryReader.ReadInt32();
                                    byte[] bytes = binaryReader.ReadBytes(count);
                                    string @string = Encoding.GetEncoding(932).GetString(bytes);
                                    list.Add(@string);
                                }
                                this.WriteText(name, list);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Add(string.Format("错误: {0}\t{1}", name, ex.Message));
            }
        }
        private Dictionary<int, int> GetIndex(BinaryReader reader, out int text_offset)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();
            int num = -1;
            int num2 = -1;
            int num3 = -1;
            List<int> indexArray = this.GetIndexArray(reader, out num2, out num, out num3);
            Dictionary<int, int> result;
            if (indexArray.Count > 0)
            {
                foreach (int current in indexArray)
                {
                    reader.BaseStream.Seek((long)(current * 4 + num2), SeekOrigin.Begin);
                    byte[] value = reader.ReadBytes(num3 * 4);
                    int value2 = current * 4 + num2 + num;
                    dictionary.Add(BitConverter.ToInt32(value, num), value2);
                }
                text_offset = Convert.ToInt32(reader.BaseStream.Position);
                result = dictionary;
            }
            else
            {
                text_offset = -1;
                result = dictionary;
            }
            return result;
        }
        private List<int> GetIndexArray(BinaryReader reader, out int k_offset, out int pos, out int address_format_length)
        {
            List<int> list = new List<int>();
            reader.BaseStream.Seek(40L, SeekOrigin.Begin);
            address_format_length = reader.ReadInt32();
            int num = reader.ReadInt32();
            byte[] address_format = reader.ReadBytes(address_format_length * 4);
            pos = this.SearchIndex(address_format);
            for (int i = 0; i < num; i++)
            {
                list.Add(reader.ReadInt32());
            }
            k_offset = Convert.ToInt32(reader.BaseStream.Position);
            return list;
        }
        private void WriteText(string name, List<string> text)
        {
            string path = Path.GetFileNameWithoutExtension(name) + ".txt";
            if (text != null)
            {
                using (FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream))
                    {
                        int num = 0;
                        foreach (string current in text)
                        {
                            streamWriter.WriteLine("#### {0} ####", num);
                            streamWriter.Write(current.Replace("\n", "\r\n"));
                            streamWriter.WriteLine("{END}");
                            streamWriter.WriteLine();
                            num++;
                        }
                        streamWriter.Flush();
                    }
                }
            }
        }
        private int SearchIndex(byte[] address_format)
        {
            int result;
            if (address_format != null)
            {
                for (int i = 0; i < address_format.Length; i += 4)
                {
                    int num = BitConverter.ToInt32(address_format, i);
                    if (num == 1)
                    {
                        result = i;
                        return result;
                    }
                }
            }
            result = -1;
            return result;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "选择原文件目录";
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Source.Text = folderBrowserDialog.SelectedPath;
            }
        }
        private void GroupBox_DragEnter_1(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Link;
                e.Handled = true;
            }
        }
        private void GroupBox_Drop_1(object sender, System.Windows.DragEventArgs e)
        {
            string source = this.Source.Text;
            if (string.IsNullOrEmpty(source))
            {
                System.Windows.MessageBox.Show("请先选择原文件路径");
            }
            else
            {
                if (this.IsTblChk)
                {
                    string text = this.tbl.Text;
                    if (string.IsNullOrEmpty(text))
                    {
                        System.Windows.MessageBox.Show("请先选择码表路径");
                        return;
                    }
                    this.GetTbl(text);
                }
                string[] files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += delegate(object s, DoWorkEventArgs ev)
                {
                    //string[] files = files;
                    for (int i = 0; i < files.Length; i++)
                    {
                        string text2 = files[i];
                        if (Path.GetExtension(text2) == ".txt")
                        {
                            this.PackTxt(source, text2);
                        }
                    }
                };
                backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
                backgroundWorker.RunWorkerAsync();
            }
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (log.Count > 0)
            {
                StreamWriter writer;
                if (!File.Exists("log.txt"))
                {
                    writer = File.CreateText("log.txt");
                }
                else
                {
                    writer = File.AppendText("log.txt");
                }
                writer.WriteLine("========={0}-导入=========", DateTime.Now);
                foreach (var str in log)
                {
                    writer.WriteLine(str);
                }
                log.Clear();
                writer.Close();
            }
            System.Windows.Forms.MessageBox.Show("导入完成");
        }
        private void GetTbl(string tblStr)
        {
            this.tblArr.Clear();
            if (!string.IsNullOrEmpty(tblStr))
            {
                using (FileStream fileStream = new FileStream(tblStr, FileMode.Open))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream, Encoding.Unicode))
                    {
                        while (!streamReader.EndOfStream)
                        {
                            string text = streamReader.ReadLine();
                            if (!string.IsNullOrEmpty(text))
                            {
                                string[] array = text.Split(new char[]
								{
									'='
								});
                                if (!string.IsNullOrEmpty(array[1]))
                                {
                                    this.tblArr.Add(array[0], array[1]);
                                }
                            }
                        }
                        this.tblArr.Add("0A", "\n");
                    }
                }
            }
        }
        private void PackTxt(string source, string file)
        {
            try
            {
                string path = source + "\\" + Path.GetFileNameWithoutExtension(file) + ".bin";
                Dictionary<int, int> index;
                byte[] head;
                byte[] end;
                long length;
                uint endAddr;
                using (FileStream fileStream = new FileStream(path, FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        uint num = binaryReader.ReadUInt32();
                        if (num != 1095783501u)
                        {
                            throw new Exception("原文件格式不对");
                        }
                        int text_offset = -1;
                        index = this.GetIndex(binaryReader, out text_offset);
                        head = this.GetHead(binaryReader, text_offset);
                        end = this.GetEnd(binaryReader);
                        length = binaryReader.BaseStream.Length;
                        binaryReader.BaseStream.Seek(4L, SeekOrigin.Begin);
                        uint filelength = binaryReader.ReadUInt32();
                        uint offset = binaryReader.ReadUInt32();
                        binaryReader.BaseStream.Seek(20L, SeekOrigin.Begin);
                        endAddr = binaryReader.ReadUInt32() + offset;
                    }
                }
                List<string> text = this.ReadText(file);
                this.MakeFile(file, head, end, text, index , length, endAddr);
            }
            catch (Exception ex)
            {
                log.Add(string.Format("错误: {0}\t{1}", file, ex.Message));
            }
        }
        private void MakeFile(string file, byte[] head, byte[] end, List<string> text, Dictionary<int, int> list, long fileLength, uint endAddr)
        {
            List<int> list2 = new List<int>();
            string text2 = "女武神文本替换\\";
            if (!Directory.Exists(text2))
            {
                Directory.CreateDirectory(text2);
            }
            text2 = text2 + Path.GetFileNameWithoutExtension(file) + ".bin";
            using (FileStream fileStream = new FileStream(text2, FileMode.Create))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    byte value = 0;
                    int num = 0;
                    binaryWriter.Write(head);
                    foreach (string current in text)
                    {
                        list2.Add(num);
                        byte[] array;
                        if (this.IsTblChk)
                        {
                            List<byte> list3 = new List<byte>();
                            string text3 = current.Replace("\r\n","\n");
                            for (int i = 0; i < text3.Length; i++)
                            {
                                char c = text3[i];
                                try
                                {
                                    KeyValuePair<string, string> keyValuePair = this.tblArr.First((KeyValuePair<string, string> t) => t.Value.Equals(Convert.ToString(c)));
                                    if (Convert.ToUInt32(keyValuePair.Key, 16) < 255u)
                                    {
                                        list3.Add(Convert.ToByte(keyValuePair.Key, 16));
                                    }
                                    else
                                    {
                                        string value2 = keyValuePair.Key.Substring(0, 2);
                                        list3.Add(Convert.ToByte(value2, 16));
                                        string value3 = keyValuePair.Key.Substring(2);
                                        list3.Add(Convert.ToByte(value3, 16));
                                    }
                                }
                                catch (Exception)
                                {
                                    System.Windows.MessageBox.Show(string.Format("{0}中的 {1} 在码表中没有对应的编码", current, c));
                                    list3.Add(32);
                                }
                            }
                            array = list3.ToArray();
                        }
                        else
                        {
                            array = Encoding.GetEncoding(932).GetBytes(current);
                        }
                        binaryWriter.Write(array.Length);
                        num += 4;
                        binaryWriter.Write(array);
                        num += array.Length;
                        int num2 = (array.Length / 4 + 1) * 4 - array.Length;
                        for (int j = 0; j < num2; j++)
                        {
                            binaryWriter.Write(value);
                            num++;
                        }
                    }
                    //int num3 = (Convert.ToInt32(binaryWriter.BaseStream.Position) / 16 + 1) * 16;
                    //binaryWriter.BaseStream.Seek((long)num3, SeekOrigin.Begin);
                    binaryWriter.BaseStream.Seek(endAddr, SeekOrigin.Begin);
                    //long num6 = binaryWriter.BaseStream.Position; 
                    binaryWriter.Write(end);
                    //int num4 = (Convert.ToInt32(binaryWriter.BaseStream.Position) / 2048 + 1) * 2048 - Convert.ToInt32(binaryWriter.BaseStream.Position);
                    long num5 = binaryWriter.BaseStream.Position;
                    long num4 = fileLength - num5;
                    for (int j = 0; j < num4; j++)
                    {
                        binaryWriter.Write(value);
                    }
                    this.ReplaceIndex(binaryWriter, list2, list);
                }
            }
        }
        private void ReplaceIndex(BinaryWriter writer, List<int> index, Dictionary<int, int> list)
        {
            int num = 0;
            foreach (KeyValuePair<int, int> current in list)
            {
                writer.BaseStream.Seek((long)current.Value, SeekOrigin.Begin);
                writer.Write(index[num]);
                num++;
            }
        }
        private byte[] GetHead(BinaryReader reader, int text_offset)
        {
            reader.BaseStream.Seek(0L, SeekOrigin.Begin);
            return reader.ReadBytes(text_offset);
        }
        private List<string> ReadText(string file)
        {
            List<string> list = new List<string>();
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                using (StreamReader streamReader = new StreamReader(fileStream,Encoding.Default))
                {
                    string text = string.Empty;
                    while (!streamReader.EndOfStream)
                    {
                        string text2 = streamReader.ReadLine();
                        if (!text2.StartsWith("##"))
                        {
                            if (!string.IsNullOrEmpty(text2))
                            {
                                if (!string.IsNullOrEmpty(text))
                                {
                                    text += "\n";
                                }
                                text += text2;
                                if (text.EndsWith("{END}"))
                                {
                                    list.Add(text.Replace("{END}", ""));
                                    text = string.Empty;
                                }
                            }
                        }
                    }
                }
            }
            return list;
        }
        private byte[] GetEnd(BinaryReader reader)
        {
            reader.BaseStream.Seek(4L, SeekOrigin.Begin);
            uint num = reader.ReadUInt32();
            uint num2 = reader.ReadUInt32();
            reader.BaseStream.Seek(20L, SeekOrigin.Begin);
            uint num3 = reader.ReadUInt32();
            uint value = num - (num3 + num2) + 16u;
            reader.BaseStream.Seek((long)((ulong)(num3 + num2)), SeekOrigin.Begin);
            return reader.ReadBytes(Convert.ToInt32(value));
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "选择码表文件";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.tbl.Text = openFileDialog.FileName;
            }
        }
        private void CheckBox_Click_1(object sender, RoutedEventArgs e)
        {
            this.IsTblChk = ((sender as System.Windows.Controls.CheckBox).IsChecked == true);
            this.tbl.IsEnabled = this.IsTblChk;
            this.tblBtn.IsEnabled = this.IsTblChk;
        }

    }
}
