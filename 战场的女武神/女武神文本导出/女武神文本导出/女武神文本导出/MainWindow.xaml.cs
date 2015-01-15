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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace 女武神文本导出
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsTblChk = false;
        private Dictionary<string, string> tblArr = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        private void Window_Drop_1(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                foreach (string name in files)
                {
                    Export(name);
                }
            };
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("导出结束");
        }

        private void Export(string name)
        {
            try
            {
                using (FileStream stream = new FileStream(name, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        UInt32 tag = reader.ReadUInt32();
                        if (tag == 0x4150544D)
                        {
                            int text_offset = -1;
                            Dictionary<int, int> index = GetIndex(reader, out text_offset);
                            if (text_offset > 0)
                            {
                                List<string> text = new List<string>();
                                foreach (var addr in index)
                                {
                                    reader.BaseStream.Seek(addr.Key + text_offset, SeekOrigin.Begin);
                                    int length = reader.ReadInt32();
                                    byte[] buff = reader.ReadBytes(length);
                                    string str = Encoding.GetEncoding(932).GetString(buff);
                                    text.Add(str);
                                }
                                WriteText(name, text);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private Dictionary<int, int> GetIndex(BinaryReader reader, out int text_offset)
        {
            Dictionary<int, int> index = new Dictionary<int, int>();
            int pos = -1; //地址在索引中的位置
            int k_offset = -1; //真实索引地址
            int address_format_length = -1; //单个索引长度
            List<int> indexArray = GetIndexArray(reader, out k_offset, out pos, out address_format_length);
            if (indexArray.Count > 0)
            {
                foreach (var k in indexArray)
                {
                    reader.BaseStream.Seek(k * 4 + k_offset, SeekOrigin.Begin);
                    byte[] buff = reader.ReadBytes(address_format_length * 4);
                    int position = k * 4 + k_offset + pos;
                    index.Add(BitConverter.ToInt32(buff, pos), position);
                }
                text_offset = Convert.ToInt32(reader.BaseStream.Position);
                return index;
            }
            text_offset = -1;
            return index;
        }

        private List<int> GetIndexArray(BinaryReader reader, out int k_offset, out int pos, out int address_format_length)
        {
            List<int> indexArray = new List<int>();
            reader.BaseStream.Seek(0x28, SeekOrigin.Begin);
            address_format_length = reader.ReadInt32();
            int count = reader.ReadInt32();
            byte[] address_format = reader.ReadBytes(address_format_length * 4);
            pos = SearchIndex(address_format);
            for (int i = 0; i < count; ++i)
            {
                indexArray.Add(reader.ReadInt32());
            }
            k_offset = Convert.ToInt32(reader.BaseStream.Position);
            return indexArray;
        }

        private void WriteText(string name, List<string> text)
        {
            string filename = Path.GetFileNameWithoutExtension(name) + ".txt";
            if (text != null)
            {
                using (FileStream stream = new FileStream(filename, FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        int i = 0;
                        foreach (var str in text)
                        {
                            writer.WriteLine("#### {0} ####", i);
                            writer.Write(str.Replace("\n","\r\n"));
                            writer.WriteLine("{END}");
                            writer.WriteLine();
                            i++;
                        }
                        writer.Flush();
                    }
                }
            }
        }

        private int SearchIndex(byte[] address_format)
        {
            if(address_format != null)
            {
                for (int i = 0; i < address_format.Length; i += 4)
                {
                    int b = BitConverter.ToInt32(address_format, i);
                    if (b == 1)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "选择原文件目录";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Source.Text = dlg.SelectedPath;
            }
        }

        private void GroupBox_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
                e.Handled = true;
            }
        }

        private void GroupBox_Drop_1(object sender, DragEventArgs e)
        {
            string source = Source.Text;
            if(string.IsNullOrEmpty(source))
            {
                MessageBox.Show("请先选择原文件路径");
                return;
            }
            if (IsTblChk == true)
            {
                string tblStr = tbl.Text;
                if (string.IsNullOrEmpty(tblStr))
                {
                    MessageBox.Show("请先选择码表路径");
                    return;
                }
                GetTbl(tblStr);
            }
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, ev) =>
            {
                foreach (var file in files)
                {
                    if (Path.GetExtension(file) == ".txt")
                    {
                        PackTxt(source, file);
                    }
                }
            };
            worker.RunWorkerAsync();
        }

        private void GetTbl(string tblStr)
        {
            tblArr.Clear();
            if (!string.IsNullOrEmpty(tblStr))
            {
                using (FileStream stream = new FileStream(tblStr, FileMode.Open))
                {
                    using (StreamReader reader = new StreamReader(stream,Encoding.Unicode))
                    {
                        while (!reader.EndOfStream)
                        {
                            string item = reader.ReadLine();
                            if (!string.IsNullOrEmpty(item))
                            {
                                string[] buff = item.Split('=');
                                if (!string.IsNullOrEmpty(buff[1]))
                                {
                                    tblArr.Add(buff[0], buff[1]);
                                }
                            }
                        }
                        tblArr.Add("0A", "\n");
                    }
                }
            }
        }

        private void PackTxt(string source, string file)
        {
            try
            {
                string sourcefile = source + "\\" + Path.GetFileNameWithoutExtension(file) + ".bin";
                byte[] head, end;
                Dictionary<int, int> list;
                List<string> text;
                using (FileStream stream = new FileStream(sourcefile, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        UInt32 tag = reader.ReadUInt32();
                        if (tag != 0x4150544D)
                        {
                            throw new Exception("原文件格式不对");
                        }
                        int text_offset = -1;
                        list = GetIndex(reader,out text_offset);
                        head = GetHead(reader, text_offset);
                        end = GetEnd(reader);
                    }
                }
                text = ReadText(file);
                MakeFile(file, head, end, text, list);

            }
            catch (Exception ex)
            {
            }
        }

        private void MakeFile(string file, byte[] head, byte[] end, List<string> text, Dictionary<int,int> list)
        {
            List<int> index = new List<int>();
            string path = "女武神文本替换\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += Path.GetFileNameWithoutExtension(file) + ".bin";
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    byte z = 0x0;
                    int pos = 0;
                    writer.Write(head);
                    foreach (var str in text)
                    {
                        index.Add(pos);
                        byte[] buff;
                        if (IsTblChk)
                        {
                            List<byte> tmp = new List<byte>();
                            foreach (char c in str)
                            {
                                try
                                {
                                    var pair = tblArr.First(t => t.Value.Equals(Convert.ToString(c)));
                                    if (Convert.ToUInt32(pair.Key, 16) < 0xFF)
                                    {
                                        tmp.Add(Convert.ToByte(pair.Key, 16));
                                    }
                                    else
                                    {
                                        string first = pair.Key.Substring(0, 2);
                                        tmp.Add(Convert.ToByte(first, 16));
                                        string second = pair.Key.Substring(2);
                                        tmp.Add(Convert.ToByte(second, 16));
                                    }
                                }
                                catch(Exception)
                                {
                                    MessageBox.Show(string.Format("字库中没有{0}子符", c));
                                    tmp.Add(0x20);
                                }
                            }
                            buff = tmp.ToArray();
                        }
                        else
                        {
                            buff = Encoding.GetEncoding(932).GetBytes(str);
                        }
                        writer.Write(buff.Length);
                        pos += 4;
                        writer.Write(buff);
                        pos += buff.Length;
                        int zero = ((buff.Length / 4) + 1) * 4 - buff.Length;
                        for (int i = 0; i < zero; ++i)
                        {
                            writer.Write(z);
                            pos++;
                        }
                    }
                    int position = ((Convert.ToInt32(writer.BaseStream.Position) / 0x10) + 1) * 0x10;
                    writer.BaseStream.Seek(position, SeekOrigin.Begin);
                    writer.Write(end);
                    int eight = ((Convert.ToInt32(writer.BaseStream.Position) / 0x800) + 1) * 0x800 - Convert.ToInt32(writer.BaseStream.Position);
                    for (int i = 0; i < eight; ++i)
                    {
                        writer.Write(z);
                    }
                    ReplaceIndex(writer, index, list);
                }
            }
        }

        private void ReplaceIndex(BinaryWriter writer, List<int> index, Dictionary<int, int> list)
        {
            int i = 0;
            foreach (var l in list)
            {
                writer.BaseStream.Seek(l.Value,SeekOrigin.Begin);
                writer.Write(index[i]);
                ++i;
            }
        }

        private byte[] GetHead(BinaryReader reader, int text_offset)
        {
            reader.BaseStream.Seek(0x0, SeekOrigin.Begin);
            return reader.ReadBytes(text_offset);
        }

        private List<string> ReadText(string file)
        {
            List<string> text = new List<string>();
            using (FileStream stream = new FileStream(file, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string str = string.Empty;
                    while (!reader.EndOfStream)
                    {
                        string tmp = reader.ReadLine();
                        if (tmp.StartsWith("##"))
                        {

                        }
                        else if (string.IsNullOrEmpty(tmp))
                        {

                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(str))
                            {
                                str += "\n";
                            }
                            str += tmp;
                            if (str.EndsWith("{END}"))
                            {
                                text.Add(str.Replace("{END}", ""));
                                str = string.Empty;
                            }
                        }
                    }
                }
            }
            return text;
        }

        private byte[] GetEnd(BinaryReader reader)
        {
            reader.BaseStream.Seek(0x4, SeekOrigin.Begin);
            UInt32 file_end = reader.ReadUInt32();
            UInt32 end_off = reader.ReadUInt32();
            reader.BaseStream.Seek(0x14, SeekOrigin.Begin);
            UInt32 text_end = reader.ReadUInt32();
            UInt32 length = file_end - (text_end + end_off) + 0x10;
            reader.BaseStream.Seek(text_end + end_off, SeekOrigin.Begin);
            return reader.ReadBytes(Convert.ToInt32(length));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.Title = "选择码表文件";
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbl.Text = dlg.FileName;
            }
        }

        private void CheckBox_Click_1(object sender, RoutedEventArgs e)
        {
            IsTblChk = ((sender as CheckBox).IsChecked == true);
            tbl.IsEnabled = IsTblChk;
            tblBtn.IsEnabled = IsTblChk;
        }

    }
}
