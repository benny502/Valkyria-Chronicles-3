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
using System.Windows.Shapes;

namespace DDS图片导出
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        int index = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Grid_DragEnter_1(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        private void Grid_Drop_1(object sender, DragEventArgs e)
        {
            string fileName = (e.Data.GetData(DataFormats.FileDrop) as string[])[0];
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (se,ev) =>
                {
                    Dispatcher.Invoke(new Action(() => 
                    {
                        Info.Text = "开始导出";
                        Pb.Visibility = System.Windows.Visibility.Visible;
                    }));
                    List<byte> data = new List<byte>();
                    List<long> pos = new List<long>();
                    using (FileStream stream = File.Open(fileName, FileMode.Open))
                    {
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            while (reader.BaseStream.Position < reader.BaseStream.Length)
                            {
                                byte[] tag = reader.ReadBytes(4);
                                int tmp = BitConverter.ToInt32(tag, 0);
                                if (BitConverter.ToInt32(tag, 0) == 0x20534444)
                                {
                                    pos.Add(reader.BaseStream.Position - 4);
                                    double per = reader.BaseStream.Position * 100 / reader.BaseStream.Length;
                                    worker.ReportProgress(Convert.ToInt32(per));
                                    data.AddRange(tag);
                                    while (true)
                                    {
                                        byte[] buff = reader.ReadBytes(4);
                                        if (BitConverter.ToInt32(buff, 0) == 0x43464F45 || BitConverter.ToInt32(buff, 0) == 0x46535448 || BitConverter.ToInt32(buff, 0) == 0x58455448)
                                        {
                                            break;
                                        }
                                        data.AddRange(buff);
                                    }
                                    Write(data);
                                    data.Clear();
                                }
                            }
                            Writelog(pos);
                        }
                    }
                };
            worker.WorkerReportsProgress = true;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Pb.Value = e.ProgressPercentage;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("导出完成");
            Info.Text = "导出结束";
        }

        private void Writelog(List<long> pos)
        {
            using (StreamWriter writer = File.CreateText("list.txt"))
            {
                int j = 0;
                foreach (var p in pos)
                {
                    writer.WriteLine("{0} : {1:X}",j,p);
                    j++;
                }
            }
        }

        private void Write(List<byte> data)
        {
            if (!Directory.Exists("DDS"))
            {
                Directory.CreateDirectory("DDS");
            }
            string path = string.Format("DDS\\{0}.dds", index);
            File.WriteAllBytes(path, data.ToArray());
            index++;
        }
    }
}
