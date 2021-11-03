using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Drawing;
using RecognitionCoreLibrary;
using YOLOv4MLNet.DataStructures;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;

namespace RecognitionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static CancellationTokenSource source = new CancellationTokenSource();
        static CancellationToken token = source.Token;

        List<BitmapImage> items = new List<BitmapImage>();
        string imageFolder = "";
        string[] filenames = new string[0];

        public MainWindow()
        {
            InitializeComponent();
        }

        private BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }

        private void Button_Open(object sender, RoutedEventArgs e)
        {
            // При повторном открытии директории программа не работает, так как Listbox не обновляется. Можно ли это как-нибудь исправить? Я практически уверен, что выбрал нелучший способ привязки
            // и обновления Listbox, но с другими способами ничего не работало.
            var dlg = new CommonOpenFileDialog();
            dlg.InitialDirectory = "C:\\Users\\murad\\Desktop";
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                items.Clear();
                listBox_Images.Items.Refresh();
                var filepaths = Directory.GetFiles(dlg.FileName, "*", SearchOption.TopDirectoryOnly).ToArray();
                filenames = filepaths.Select(path => Path.GetFileName(path)).ToArray();
                int n = filepaths.Length;
                listBox_Images.ItemsSource = items;
                for (int i = 0; i < n; ++i)
                {
                    items.Add(new BitmapImage(new Uri(filepaths[i])));
                }
                listBox_Images.Items.Refresh();

                imageFolder = dlg.FileName;
            }
        }

        private void Button_Stop(object sender, RoutedEventArgs e)
        {
            source.Cancel();
        }

        private async void Button_Start(object sender, RoutedEventArgs e)
        {
            var recognitionResult = new ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>>();

            var task1 = Task.Factory.StartNew(() => RecognitionCore.Recognise(imageFolder, recognitionResult, token), TaskCreationOptions.LongRunning);
            var task2 = Task.Factory.StartNew(() =>
            {
                while (task1.Status == TaskStatus.Running)
                {
                    while (recognitionResult.TryDequeue(out Tuple<string, IReadOnlyList<YoloV4Result>> result))
                    {
                        string name = result.Item1;
                        var bitmap = new Bitmap(Image.FromFile(Path.Combine(imageFolder, name)));
                        using var g = Graphics.FromImage(bitmap);
                        foreach (var res in result.Item2)
                        {
                            var x1 = res.BBox[0];
                            var y1 = res.BBox[1];
                            var x2 = res.BBox[2];
                            var y2 = res.BBox[3];
                            g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                            using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                            {
                                g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                            }

                            g.DrawString(res.Label, new Font("Arial", 12),
                                         Brushes.Blue, new PointF(x1, y1));
                        }
                        items[Array.FindIndex(filenames, val => val.Equals(name))] = Bitmap2BitmapImage(bitmap);
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            listBox_Images.Items.Refresh();
                        }));
                    }
                }
            });
            await Task.WhenAll(task1, task2);
        }
    }
}
