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
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using Microsoft.Win32;
using System.IO;
using System.Windows.Threading;


namespace PDI_Mosaico
{
    public partial class MainWindow : Window
    {
        BitmapImage originalImage;
        BitmapImage mosaicImage;
        BitmapImage rowImage;
        BitmapImage[] images;
        string selectedFolderName;

        public MainWindow()
        {
            InitializeComponent();
        }

        string[] loadingMessages = new string[]
        {
            "Cargando, por favor espera...",
            "Preparando todo para ti...",
            "Estamos trabajando en ello...",
            "Casi listo, solo un momento más...",
            "Procesando tu solicitud...",
            "Esto no tomará mucho tiempo...",
            "Todo está en marcha, paciencia...",
            "Los detalles se están afinando...",
            "Gracias por tu paciencia...",
            "¡Estamos en ello! Ya casi...",
            "Finalizando los últimos detalles...",
            "Tu espera será recompensada...",
            "La magia está sucediendo...",
            "Todo estará listo en breve...",
            "Estamos casi allí...",
            "Asegurando todo para ti...",
            "Conectando los puntos...",
            "Ajustando los últimos parámetros...",
            "Estamos en la recta final...",
            "¡Gracias por esperar, ya casi terminamos!"
};


        private void LoadImageBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                originalImage = new BitmapImage(new Uri(fileName));
                img_display_original.Source = originalImage;
                originalImage = ReduceResolution(originalImage, 85, 85);
                
                LoadImageBtn.Visibility = Visibility.Collapsed;
                RetryLoadImageBtn.Visibility = Visibility.Visible;
            }
        }

        private void btnLoadImages_Click(object sender, RoutedEventArgs e)
        {
            images = null;
            images = LoadImagesFromFolder();
            images = ResizeImagesToSameSize(images);
            imagesItemsControl.ItemsSource = images;
            btnLoadImages.Visibility = Visibility.Collapsed;
            RetrybtnLoadImages.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(selectedFolderName))
            {
                FolderNameTextBlock.Text = $"Carpeta: {System.IO.Path.GetFileName(selectedFolderName)}";
            }
        }


        private BitmapImage[] LoadImagesFromFolder()
        {
            var imagesList = new List<BitmapImage>();

            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFolderName = fbd.SelectedPath;

                var imageFiles = Directory.GetFiles(selectedFolderName, "*.*")
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

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (img_display_original.Source != null)
            {
                SendButton.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;
                SendButton.Visibility = Visibility.Collapsed;

                await Task.Run(() =>
                {
                    GenerateMosaicWithProgress(originalImage);
                });

                Dispatcher.Invoke(() =>
                {
                    img_display_result.Source = mosaicImage;
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    SendButton.IsEnabled = true;
                    ResultButtonsPanel.Visibility = Visibility.Visible;
                });
            }
            else
            {
                Control controlWindow = new Control();
                controlWindow.Show();
            }
        }




        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            if (img_display_original.Source != null)
            {
                LoadImageBtn.Visibility = Visibility.Visible;
                SendButton.Visibility = Visibility.Visible;
                ResultButtonsPanel.Visibility = Visibility.Collapsed;
                RetryLoadImageBtn.Visibility = Visibility.Collapsed;
                LoadImageBtn.Visibility = Visibility.Visible;
                RetrybtnLoadImages.Visibility = Visibility.Collapsed;
                btnLoadImages.Visibility = Visibility.Visible;
                img_display_original.Source = null;
                img_display_result.Source = null;
            }
            else
            {
                var controlWindow = new Control();
                controlWindow.Show();
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (img_display_result.Source != null)
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
                            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)img_display_result.Source));
                            using (FileStream fs = File.Open(saveFileDialog.FileName, FileMode.Create))
                            {
                                encoder.Save(fs);
                            }
                            MessageBox.Show("La imagen se guardó correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ocurrió un error al guardar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Formato de archivo no compatible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("No hay una imagen para guardar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        



        private BitmapImage ReduceResolution(BitmapImage originalImage, int newWidth, int newHeight)
        {
            Bitmap bitmap = BitmapImage2Bitmap(originalImage);
            Bitmap resizedBitmap = new Bitmap(bitmap, newWidth, newHeight);

            return ToBitmapImage(resizedBitmap);
        }

        private BitmapImage[] ResizeImagesToSameSize(BitmapImage[] images)
        {
            if (images.Length == 0)
            {
                return images;
            }

            for (int i = 1; i < images.Length; i++)
            {
                images[i] = ResizeImage(images[i], images[0].PixelWidth, images[0].PixelHeight);
            }

            return images;
        }

        private BitmapImage ResizeImage(BitmapImage image, int targetWidth, int targetHeight)
        {
            Bitmap bitmap = BitmapImage2Bitmap(image);
            Bitmap resizedBitmap = new Bitmap(targetWidth, targetHeight);

            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {
                graphics.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
            }

            return ToBitmapImage(resizedBitmap);
        }


        private void GenerateMosaicWithProgress(BitmapImage img)
        {
            Bitmap bitmapAux = BitmapImage2Bitmap(img);
            int totalSteps = (bitmapAux.Width / 2) * (bitmapAux.Height / 2);
            int currentStep = 0;
            int messageIndex = 0;

            double messageChangeValue = Math.Ceiling(totalSteps / (double)loadingMessages.Length);

            Dispatcher.Invoke(() =>
            {
                lblLoadingMessage.Content = loadingMessages[messageIndex];
            });

            for (int f = 0; f < bitmapAux.Width; f += 2)
            {
                for (int c = 0; c < bitmapAux.Height; c += 2)
                {
                    System.Drawing.Color p = bitmapAux.GetPixel(c, f);
                    int randomImg = new Random().Next(0, images.Length);
                    BitmapImage resizedImage = ResizeImage(images[randomImg], 70, 70);
                    AddToRow(ApplyColorFilter(resizedImage, p));

                    currentStep++;

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = (double)currentStep / totalSteps * 100;
                        ProgressText.Text = $"{(int)((double)currentStep / totalSteps * 100)}% completado";

                        if (currentStep > messageChangeValue * (messageIndex + 1) && messageIndex < loadingMessages.Length - 1)
                        {
                            messageIndex++;
                            lblLoadingMessage.Content = loadingMessages[messageIndex];
                        }
                    });
                }
                AddNewRow(rowImage);
            }

            Dispatcher.Invoke(() =>
            {
                lblLoadingMessage.Content = "";
            });
        }

        void AddToRow(BitmapImage img)
        {
            try
            {
                Bitmap bitmapAux = BitmapImage2Bitmap(img);
                Bitmap bitmapRow = rowImage != null ? BitmapImage2Bitmap(rowImage) : new Bitmap(1, bitmapAux.Height);

                int combinedWidth = bitmapRow.Width + bitmapAux.Width;
                int maxHeight = Math.Max(bitmapRow.Height, bitmapAux.Height);
                Bitmap bitmapResult = new Bitmap(combinedWidth, maxHeight);

                using (Graphics g = Graphics.FromImage(bitmapResult))
                {
                    g.DrawImage(bitmapRow, 0, 0);
                    g.DrawImage(bitmapAux, bitmapRow.Width, 0);
                }

                rowImage = ToBitmapImage(bitmapResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar la imagen a la fila: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void AddNewRow(BitmapImage img)
        {
            try
            {
                Bitmap bitmapAux = BitmapImage2Bitmap(img);
                Bitmap bitmapRow = mosaicImage != null ? BitmapImage2Bitmap(mosaicImage) : new Bitmap(bitmapAux.Width, 1);
                
                int maxWidth = Math.Max(bitmapRow.Width, bitmapAux.Width);
                int combinedHeight = bitmapRow.Height + bitmapAux.Height;
                Bitmap bitmapResult = new Bitmap(maxWidth, combinedHeight);

                using (Graphics g = Graphics.FromImage(bitmapResult))
                {
                    g.DrawImage(bitmapRow, 0, 0);
                    g.DrawImage(bitmapAux, 0, bitmapRow.Height);
                }

                mosaicImage = ToBitmapImage(bitmapResult);
                rowImage = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar una nueva fila: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        BitmapImage ApplyColorFilter(BitmapImage img, System.Drawing.Color color)
        {
            Bitmap bitmapAux = BitmapImage2Bitmap(img);
            Bitmap bitmapResult = new Bitmap(bitmapAux.Width, bitmapAux.Height);
            double NewColorOpacity = 0.5;//Cambiar la opacidad para aplicar más, o menos color
            for (int x = 0; x < bitmapAux.Width; x++)
            {
                for (int y = 0; y < bitmapAux.Height; y++)
                {
                    System.Drawing.Color p = bitmapAux.GetPixel(x, y);

                    int grayValue = (int)(p.R * 0.3 + p.G * 0.59 + p.B * 0.11);

                    int r = (int)(grayValue * (1 - NewColorOpacity) + color.R * NewColorOpacity);
                    int g = (int)(grayValue * (1 - NewColorOpacity) + color.G * NewColorOpacity);
                    int b = (int)(grayValue * (1 - NewColorOpacity) + color.B * NewColorOpacity);

                    bitmapResult.SetPixel(x, y, System.Drawing.Color.FromArgb(r, g, b));
                }
            }

            return ToBitmapImage(bitmapResult);
        }

        private Bitmap BitmapImage2Bitmap(BitmapImage imgOrg)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(imgOrg));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        public static BitmapImage ToBitmapImage(System.Drawing.Image bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void RetryLoadImageBtn_Click(object sender, RoutedEventArgs e)
        {
            RetryLoadImageBtn.Visibility = Visibility.Collapsed;
            LoadImageBtn.Visibility = Visibility.Visible;
            img_display_original.Source = null;
        }

        private void RetrybtnLoadImages_Click(object sender, RoutedEventArgs e)
        {
            RetrybtnLoadImages.Visibility = Visibility.Collapsed;
            btnLoadImages.Visibility = Visibility.Visible;
        }
    }
}
