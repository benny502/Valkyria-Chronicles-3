using System;
using System.Collections.Generic;
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
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace 战场的女武神
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {

        private const int START_OFF = 0x3C800;

        private List<string> log = null;

        public Window1()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string fileName = DataCVM.Text;
            if (!string.IsNullOrEmpty(fileName))
            {
                BackgroundWorker Unpack = new BackgroundWorker();
                Unpack.DoWork += new DoWorkEventHandler(Unpack_DoWork);
                Unpack.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Unpack_RunWorkerCompleted);
                Unpack.RunWorkerAsync(fileName);
            }
            else
            {
                System.Windows.MessageBox.Show("请先输入包的地址");
            }
        }

        void dlg_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataCVM.Text = (sender as OpenFileDialog).FileName;
        }

        void Unpack_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            System.Windows.MessageBox.Show("解包完成！");
        }

        void Unpack_DoWork(object sender, DoWorkEventArgs e)
        {
            string fileName = (e.Argument as string);
            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {

                            byte[] buffer = null;
                            int i = 0;
                            reader.BaseStream.Seek(START_OFF, SeekOrigin.Begin);
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                byte[] tag = reader.ReadBytes(4);
                                int length1 = reader.ReadInt32();
                                int length2 = reader.ReadInt32();
                                int length = length1 + length2;
                                length = ((length + 0x800) / 0x800) * 0x800;
                                reader.BaseStream.Seek(-0xC, SeekOrigin.Current);
                                if (reader.BaseStream.Position + length > reader.BaseStream.Length)
                                {
                                    break;
                                }
                                buffer = reader.ReadBytes(length);
                                if (buffer != null)
                                {
                                    string outfile = string.Format("{0}.bin", i);
                                    WriteFile(outfile, buffer);
                                    i++;
                                    //Dispatcher.Invoke(new Action(() =>
                                    //    {
                                    //        progress.Text = string.Format("已完成:{0}/3660", i);
                                    //    }));
                                    Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => 
                                    {
                                        progress.Text = string.Format("已完成:{0}/3660", i);
                                    }));
                                }
                                buffer = null;
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
            }
        }

        private void WriteFile(string outfile, byte[] buffer)
        {
            string path = Environment.CurrentDirectory + "\\pack\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            try
            {
                using (FileStream stream = new FileStream(path + outfile, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(buffer);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string CVMPath = DataCVM.Text;
            if (!string.IsNullOrEmpty(CVMPath))
            {
                FolderBrowserDialog dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Pack(CVMPath, dlg.SelectedPath);
                }
            }
        }

        private void Pack(string CVMPath, string path)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s,e) =>
            {
                GetFileInfo(CVMPath,path);

            };
            worker.RunWorkerAsync();
            
        }


        private CVMFile ParseFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            byte[] tag = reader.ReadBytes(4);
                            int length1 = reader.ReadInt32();
                            int length2 = reader.ReadInt32();
                            int length = length1 + length2;
                            length = ((length + 0x800) / 0x800) * 0x800;
                            reader.BaseStream.Seek(0, SeekOrigin.Begin);
                            byte[] data = reader.ReadBytes(length);
                            return new CVMFile() { tag = tag, length = length, data = data };
                        }
                    }
                }
                catch (Exception ex)
                {
                    string fileName = System.IO.Path.GetFileName(path);
                    log.Add(string.Format("读取{0}出错:{1}", fileName, ex.Message));
                }
            }
            return null;
        }

        private void GetFileInfo(string CVMPath,string path)
        {
            if (!string.IsNullOrEmpty(CVMPath))
            {
                try
                {
                    using (FileStream stream = new FileStream(CVMPath, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            log = new List<string>();
                            FileStream outStream = new FileStream("Data.封包.CVM", FileMode.Create);
                            BinaryWriter writer = new BinaryWriter(outStream);
                            int i = 0;
                            //reader.BaseStream.Seek(START_OFF, SeekOrigin.Begin);
                            byte[] header = reader.ReadBytes(START_OFF);
                            writer.Write(header);
                            writer.Flush();
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                byte[] tag = reader.ReadBytes(4);
                                int length1 = reader.ReadInt32();
                                int length2 = reader.ReadInt32();
                                int length = length1 + length2;
                                length = ((length + 0x800) / 0x800) * 0x800;
                                reader.BaseStream.Seek(-0xC, SeekOrigin.Current);
                                if (reader.BaseStream.Position + length > reader.BaseStream.Length)
                                {
                                    break;
                                }
                                byte[] buffer = reader.ReadBytes(length);
                                string p = path + "\\" + string.Format("{0}.bin", i);
                                CVMFile file = ParseFile(p);
                                if (file != null && ArrayEquals(file.tag, tag) && file.length.Equals(length))
                                {
                                    writer.Write(file.data);
                                    writer.Flush();
                                }
                                else
                                {
                                    //写日志，用原数据代替
                                    string fileName = i + ".bin";
                                    log.Add(string.Format("{0}文件未找到，或与原文件长度或标示不符", fileName));
                                    writer.Write(buffer);
                                    writer.Flush();
                                }
                                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,new Action(
                                    () =>
                                    {
                                        progress.Text = string.Format("已完成:{0}/3660", i);
                                    }));
                                i++;
                            }
                            writer.Close();
                            outStream.Close();
                            writeLog();
                            System.Windows.Forms.MessageBox.Show("封包完成");
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Add(string.Format("错误：", ex.Message));
                }
            }
            else
            {
                System.Windows.MessageBox.Show("请先选择原始文件");
            }
        }

        private void writeLog()
        {
            if (log.Count > 0)
            {
                using (FileStream stream = new FileStream("Vlog.txt", FileMode.Append))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("======={0}=======", DateTime.Now);
                        foreach (var l in log)
                        {
                            writer.WriteLine(l);
                        }
                        writer.WriteLine();
                        writer.Flush();
                    }
                }
            }
        }

        private bool ArrayEquals(byte[] p, byte[] tag)
        {
            if (p.Length == tag.Length)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    if (p[i] != tag[i])
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "CVM|*.cvm";
            dlg.FileOk += new System.ComponentModel.CancelEventHandler(dlg_FileOk);
            dlg.ShowDialog();
        }
    }
}

public class CVMFile
{
    public int index { get; set; }
    public byte[] tag { get; set; }
    public int length { get; set; }
    public byte[] data { get; set; }
}
