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
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using MessageBox = System.Windows.Forms.MessageBox;

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

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                originalImage = new BitmapImage(new Uri(fileName));
                Display_image.Source = originalImage;
                Display_image.Visibility = Visibility.Visible;
                ImageButton.Visibility = Visibility.Collapsed;
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

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (Display_image.Source != null)
            {
                SendButton.Visibility = Visibility.Collapsed;
                ResultButtonsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                Control controlWindow = new Control();
                controlWindow.Show(); 
            }
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            if (Display_image.Source != null)
            {
                Display_image.Visibility = Visibility.Collapsed;
                ImageButton.Visibility = Visibility.Visible;
                SendButton.Visibility = Visibility.Visible;
                ResultButtonsPanel.Visibility = Visibility.Collapsed;
                Display_image.Source = null;
            }
            else
            {
                Control controlWindow = new Control();
                controlWindow.Show();
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (Display_image.Source != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Imagen JPEG|*.jpg|Imagen PNG|*.png";
                if (saveFileDialog.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(saveFileDialog.FileName).ToLower();

                    BitmapEncoder encoder = null;
                    if (extension == ".jpg" || extension == ".jpeg")
                    {
                        encoder = new JpegBitmapEncoder();
                    }
                    else if (extension == ".png")
                    {
                        encoder = new PngBitmapEncoder();
                    }

                    if (encoder != null)
                    {
                        try
                        {
                            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)Display_image.Source));
                            using (FileStream fs = File.Open(saveFileDialog.FileName, FileMode.Create))
                            {
                                encoder.Save(fs);
                            }
                            System.Windows.MessageBox.Show("La imagen se guardó correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Ocurrió un error al guardar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Formato de archivo no compatible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No hay una imagen para guardar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
