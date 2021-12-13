using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.Net.Http;
using Contract;
using Newtonsoft.Json;

namespace RecognitionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource source = new CancellationTokenSource();
        private ImmutableList<BitmapImage> im_items;
        private string imageFolder = "";
        private string[] filenames = new string[0];
        HttpClient client = new HttpClient();

        private async void ShowDBContent()
        {
            try
            {
                string result = await client.GetStringAsync("https://localhost:5001/get-images");
                var images = JsonConvert.DeserializeObject<List<ProcessedImage>>(result);
                listView_Images.ItemsSource = images;

                var objectList = new List<RecognizedObject>();
                foreach (var img in images)
                {
                    objectList.AddRange(img.Objects);
                }
                listView_Objects.ItemsSource = objectList;
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message, "Serever Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            ShowDBContent();
        }

        private void DisableAllButtons()
        {
            Open_Button.IsEnabled = false;
            Start_Button.IsEnabled = false;
            Clear_Button.IsEnabled = false;
        }

        private void EnableAllButtons()
        {
            Open_Button.IsEnabled = true ;
            Start_Button.IsEnabled = true;
            Clear_Button.IsEnabled = true;
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

        private Bitmap ByteArrayToImage(byte[] data)
        {
            Bitmap bmp;
            using (var ms = new MemoryStream(data))
            {
                bmp = new Bitmap(ms);
                return bmp;
            }
        }

        private void Button_Open(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                InitialDirectory = "C:\\Users\\murad\\Desktop",
                IsFolderPicker = true
            };
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                im_items = ImmutableList.Create<BitmapImage>();
                var filepaths = Directory.GetFiles(dlg.FileName, "*", SearchOption.TopDirectoryOnly).ToArray();
                filenames = filepaths.Select(path => Path.GetFileName(path)).ToArray();
                int n = filepaths.Length;
                for (int i = 0; i < n; ++i)
                {
                    im_items = im_items.Add(new BitmapImage(new Uri(filepaths[i])));
                }
                listBox_Images.ItemsSource = im_items;

                imageFolder = dlg.FileName;
            }
        }

        private async void Button_Stop(object sender, RoutedEventArgs e)
        {
            try
            {
                await client.GetAsync("https://localhost:5001/stop");
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message, "Serever Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Button_Start(object sender, RoutedEventArgs e)
        {
            DisableAllButtons();
            try
            {
                var response = await client.GetAsync("https://localhost:5001/detect?path=" + imageFolder);
                var result = await response.Content.ReadAsStringAsync();
                var images = JsonConvert.DeserializeObject<ImmutableList<RequestImage>>(result);

                foreach (var image in images)
                {
                    string name = image.ImageClass;
                    int ind = Array.FindIndex(filenames, val => val.Equals(name));
                    im_items = im_items.RemoveAt(ind);
                    Bitmap bitmap = ByteArrayToImage(image.Bitmap);
                    im_items = im_items.Insert(ind, Bitmap2BitmapImage(bitmap));
                    listBox_Images.ItemsSource = im_items;
                }
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message, "Serever Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
     
            ShowDBContent();
            EnableAllButtons();
        }

        private async void Button_Clear(object sender, RoutedEventArgs e)
        {
            DisableAllButtons();

            try
            {
                await client.DeleteAsync("https://localhost:5001/clear");
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(ex.Message, "Serever Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ShowDBContent();
            EnableAllButtons();
        }
    }
}
