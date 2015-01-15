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

namespace 另一种文本
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        bool IsTblChk = false;
        private Dictionary<string, string> tblArr = new Dictionary<string, string>();

        private class Temp
        {
            public uint length { get; set; }
            public uint addr { get; set; }
        }

        public class F
        {
            public int start { get; set; }
            public int end { get; set; }
        }

        public List<F> filelist = new List<F>();

        uint baseOff = 0x20; 
        public MainWindow()
        {
            InitializeComponent();
            filelist.Add(new F() { start = 0x90e, end = 0x10a0 });
            filelist.Add(new F() { start = 0x1f94, end = 0x97b0 });
            filelist.Add(new F() { start = 0xe90, end = 0x1110 });
        }

        private void Window_DragEnter_1(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Link;
            }
        }

        private void Window_Drop_1(object sender, System.Windows.DragEventArgs e)
        {
            string[] files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (se, ar) =>
                {
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file) == ".bin")
                        {
                            try
                            {
                                Export(file);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message);
                            }
                        }
                    }
                };
            worker.RunWorkerAsync();
        }

        private void Export(string file)
        {
            if (!string.IsNullOrEmpty(file))
            {
                using (FileStream stream = new FileStream(file, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        reader.BaseStream.Seek(0x84,SeekOrigin.Begin);
                        uint count = SwapEndian(reader.ReadUInt32());
                        uint off = SwapEndian(reader.ReadUInt32()) + baseOff;
                        Dictionary<uint, uint> index = GetAddr(reader,off,count);
                        Dictionary<uint, string> text = GetText(reader, index);
                        WriteText(file,text);
                    }
                }
            }
        }

        private void WriteText(string path,Dictionary<uint, string> text)
        {
            path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".txt";
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    int i = 0;
                    foreach (var t in text)
                    {
                        writer.WriteLine("#### [{0:X}] {1} ####", t.Key, i);
                        writer.Write(t.Value.Replace("　", "\r\n"));
                        writer.WriteLine("{END}");
                        writer.WriteLine();
                        ++i;
                    }
                }
            }
        }

        private Dictionary<uint, string> GetText(BinaryReader reader, Dictionary<uint, uint> index)
        {
            Dictionary<uint, string> text = new Dictionary<uint, string>();
            foreach (var addr in index)
            {
                reader.BaseStream.Seek(addr.Value + baseOff, SeekOrigin.Begin);
                List<byte> buff = new List<byte>();
                while (true)
                {
                    byte val = reader.ReadByte();
                    if (val == 0)
                    {
                        break;
                    }
                    buff.Add(val);
                }
                string t = Encoding.GetEncoding(932).GetString(buff.ToArray());
                text.Add(addr.Key,t);
                buff.Clear();
            }
            return text;
        }

        private Dictionary<uint, uint> GetAddr(BinaryReader reader,uint off,uint count)
        {
            List<Temp> temp = new List<Temp>();
            Dictionary<uint, uint> array = new Dictionary<uint, uint>();
            reader.BaseStream.Seek(off, SeekOrigin.Begin);
            for (int i = 0; i < count; ++i)
            {
                byte[] buff = reader.ReadBytes(0x10);
                uint length = SwapEndian(BitConverter.ToUInt32(buff, 0x8));
                uint addr = SwapEndian(BitConverter.ToUInt32(buff, 0xC));
                temp.Add(new Temp() { length = length, addr = addr });
            }
            for (int i = 3; i < temp.Count; ++i)
            {
                reader.BaseStream.Seek(temp[i].addr + baseOff + 0x4, SeekOrigin.Begin);
                int j = 0x4;
                //uint length = (temp[i].length == 0x1a8) ? 0x58 : temp[i].length;
                uint length = temp[i].length;
                while (j < length)
                {
                    uint pos = Convert.ToUInt32(reader.BaseStream.Position);
                    uint addr = SwapEndian(reader.ReadUInt32());
                    array.Add(pos, addr);
                    j += 4;
                }
            }
            return array;
        }

        private uint SwapEndian(uint num)
        {
            return ((num & 0xFF000000) >> 24) |
                   ((num & 0x00FF0000) >> 8) |
                   ((num & 0x0000FF00) << 8) |
                   ((num & 0x000000FF) << 24);
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

        private void GroupBox_DragEnter_1(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Link;
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
                backgroundWorker.DoWork += (se,v)=>
                {
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file) == ".txt")
                        {
                            try
                            {
                                Import(file,source);
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show(ex.Message);
                            }
                        }
                    }
                };
                backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
                backgroundWorker.RunWorkerAsync();
            }
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
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
                        this.tblArr["8140"] = "\n";
                    }
                }
            }
        }

        private void Import(string file,string source)
        {
            int idx = 0;
            string fileName = Path.GetFileNameWithoutExtension(file);
            switch (fileName)
            {
                case "16":
                    idx = 0;
                    break;
                case "17":
                    idx = 1;
                    break;
                case "18":
                    idx = 2;
                    break;
                default:
                    throw new Exception("文件错误");
            }
            byte[] top, bottom;
            using (FileStream stream = new FileStream(source + "\\" + fileName + ".bin", FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    top = reader.ReadBytes(filelist[idx].start);
                    reader.BaseStream.Seek(filelist[idx].end, SeekOrigin.Begin);
                    bottom = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length - filelist[idx].end));
                }
            }
            Dictionary<string, string> list = new Dictionary<string, string>();
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.Default))
                {
                    string text = string.Empty;
                    string addr = string.Empty;
                    while (!streamReader.EndOfStream)
                    {
                        string val = streamReader.ReadLine();
                        if (!val.StartsWith("##"))
                        {
                            if (!string.IsNullOrEmpty(val))
                            {
                                if (!string.IsNullOrEmpty(text))
                                {
                                    text += "\n";
                                }
                                text += val;
                                if (text.EndsWith("{END}"))
                                {
                                    list.Add(addr,text.Replace("{END}", ""));
                                    text = string.Empty;
                                }
                            }
                        }
                        else
                        {
                            addr = val.Substring(val.IndexOf('[') + 1, val.IndexOf(']') - val.IndexOf('[') - 1);
                        }
                    }
                }
            }
            string foleder = "replace";
            if(!Directory.Exists(Path.GetDirectoryName(file) + "\\" + foleder))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file) + "\\" + foleder);
            }
            string path = Path.GetDirectoryName(file) + "\\" + foleder + "\\" + Path.GetFileNameWithoutExtension(file) + ".bin";
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    Dictionary<int, int> addr = new Dictionary<int, int>();
                    writer.Write(top);
                    foreach (var t in list)
                    {
                        if (IsTblChk)
                        {
                            int address, offset;
                            address = Convert.ToInt32(t.Key,16);
                            string tx = t.Value.Replace("\r\n", "\n");
                            List<byte> te = new List<byte>();
                            for (int i = 0; i < tx.Length; i++)
                            {
                                try
                                {
                                    var c = tx[i];
                                    string s = Convert.ToString(tx[i]);
                                    var kp = tblArr.First(k => k.Value == s);
                                    string code = kp.Key;
                                    if (Convert.ToUInt32(code, 16) < 255u)
                                    {
                                        te.Add(Convert.ToByte(code, 16));
                                    }
                                    else
                                    {
                                        te.Add(Convert.ToByte(code.Substring(0, 2),16));
                                        te.Add(Convert.ToByte(code.Substring(2),16));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //System.Windows.MessageBox.Show(string.Format("{0}中的 {1} 在码表中没有对应的编码", tx, tx[i]));
                                    te.Add(0x20);
                                }
                            }
                            offset = Convert.ToInt32(writer.BaseStream.Position) - 0x20;
                            addr.Add(address, offset);
                            writer.Write(te.ToArray());
                            writer.Write(Convert.ToByte(0));
                        }
                    }
                    writer.BaseStream.Seek(filelist[idx].end,SeekOrigin.Begin);
                    writer.Write(bottom);
                    foreach (var item in addr)
                    {
                        writer.BaseStream.Seek(item.Key, SeekOrigin.Begin);
                        writer.Write(SwapEndian(Convert.ToUInt32(item.Value)));
                    }
                }
            }
        }
    }
}
