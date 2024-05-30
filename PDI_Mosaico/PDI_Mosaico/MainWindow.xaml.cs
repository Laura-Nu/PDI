using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using Microsoft.Win32;
using System.Windows.Forms;
using System.IO;

namespace PDI_Mosaico
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage originalImage;
        BitmapImage mosaicoImage;
        BitmapImage uploadedImage;
        BitmapImage resultImage;
        BitmapImage[] images;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnUploadImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                originalImage = new BitmapImage(new Uri(fileName));
            }
        }

        private void btnLoadImages_Click(object sender, RoutedEventArgs e)
        {
            images = LoadImagesFromFolder();

            if (images.Length > 0)
            {
                //imgDisplay.Source = images[1];
            }
        }


        private BitmapImage[] LoadImagesFromFolder()
        {
            var imagesList = new List<BitmapImage>();

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = fbd.SelectedPath;

                var imageFiles = Directory.GetFiles(folderPath, "*.*")
                                          .Where(file => file.EndsWith(".jpg") ||
                                                         file.EndsWith(".jpeg") ||
                                                         file.EndsWith(".png"))
                                          .ToArray();

                foreach (var file in imageFiles)
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(file);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze(); // To make it cross-thread accessible
                    imagesList.Add(bitmapImage);
                }
            }

            return imagesList.ToArray();
        }

    }
}
