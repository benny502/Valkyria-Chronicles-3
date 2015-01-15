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

namespace DDS图片导出
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            Array files = e.Data.GetData(DataFormats.FileDrop) as Array;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s,ev) => 
            { 
                foreach (string file in files)
                {
                    GetDDS(file);
                }
            };
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted); 
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("导出完成！");
        }

        private void GetDDS(string file)
        {
            try
            {
                using (FileStream stream = new FileStream(file,FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        UInt32 tag = SwapEndian(reader.ReadUInt32());
                        UInt32 fileLength = reader.ReadUInt32() - 0x60;
                        reader.BaseStream.Seek(0x60, SeekOrigin.Begin);
                        UInt32 DDSTag = SwapEndian(reader.ReadUInt32());
                        if (DDSTag == 0x44445320)
                        {
                            reader.BaseStream.Seek(0x60, SeekOrigin.Begin);
                            byte[] content = reader.ReadBytes(Convert.ToInt32(fileLength));
                            Write(file,content);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void Write(string file,byte[] content)
        {
            string filename = Path.GetFileNameWithoutExtension(file) + ".dds";
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(content);
                    writer.Flush();
                }
            }
        }

        private UInt32 SwapEndian(UInt32 n)
        {
            return ((n & 0xFF000000) >> 24) |
                    ((n & 0x00FF0000) >> 8) |
                    ((n & 0x0000FF00) << 8) |
                    ((n & 0x000000FF) << 24);
        }
    }
}
