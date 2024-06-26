﻿using System;
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
        int resolution = 0;

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
            originalImage = null;
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                originalImage = new BitmapImage(new Uri(fileName));
                img_display_original.Source = originalImage;
                originalImage = ReduceResolution(originalImage, 90, 90);
                
                LoadImageBtn.Visibility = Visibility.Collapsed;
                RetryLoadImageBtn.Visibility = Visibility.Visible;
            }
        }

        private void btnLoadImages_Click(object sender, RoutedEventArgs e)
        {
            images = null;
            images = LoadImagesFromFolder();
            imagesItemsControl.ItemsSource = images;
            images = ResizeImagesToSameSize(images);
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
            if (img_display_original.Source != null && images != null && resolution != 0)
            {
                SendButton.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;
                SendButton.Visibility = Visibility.Collapsed;
                RetryLoadImageBtn.Visibility = Visibility.Hidden;
                RetrybtnLoadImages.Visibility = Visibility.Hidden;
                stkOpciones.Visibility = Visibility.Hidden;
                img_display_result.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    GenerateMosaicWithProgress(originalImage, resolution);
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
                ValidationMessage();
            }
        }




        private void RetryButton_Click(object sender, RoutedEventArgs e)
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
            imagesItemsControl.ItemsSource = null;
            FolderNameTextBlock.Text = string.Empty;
            images = null;
            stkOpciones.Visibility = Visibility.Visible;
            resolution = 0;
            CleanRadioSelection();
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
                            //MessageBox.Show("La imagen se guardó correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                            Control controlWindow = new Control("La imagen se guardó correctamente.");
                            controlWindow.Show();
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show($"Ocurrió un error al guardar la imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Control controlWindow = new Control("Ocurrió un error inesperado.");
                            controlWindow.Show();
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Formato de archivo no compatible.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Control controlWindow = new Control("Formato de archivo no compatible.");
                        controlWindow.Show();
                    }
                }
            }
            else
            {
                //MessageBox.Show("No hay una imagen para guardar.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Control controlWindow = new Control("No hay una imagen para guardar.");
                controlWindow.Show();
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


        private void GenerateMosaicWithProgress(BitmapImage img, int resolution)
        {
            mosaicImage = null;
            Bitmap bitmapAux = BitmapImage2Bitmap(img);
            int totalSteps = (bitmapAux.Width / resolution) * (bitmapAux.Height / resolution);
            int currentStep = 0;
            int messageIndex = 0;

            double messageChangeValue = Math.Ceiling(totalSteps / (double)loadingMessages.Length);

            Dispatcher.Invoke(() =>
            {
                lblLoadingMessage.Content = loadingMessages[messageIndex];
            });

            for (int f = 0; f < bitmapAux.Width; f += resolution)
            {
                for (int c = 0; c < bitmapAux.Height; c += resolution)
                {
                    System.Drawing.Color p = bitmapAux.GetPixel(c, f);
                    int randomImg = new Random().Next(0, images.Length);
                    BitmapImage resizedImage = ResizeImage(images[randomImg], 80, 80);
                    AddToRow(ApplyColorFilter(resizedImage, p));

                    currentStep++;

                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar.Value = Math.Min((double)currentStep / totalSteps * 100, 100);
                        ProgressText.Text = $"{Math.Min((int)((double)currentStep / totalSteps * 100), 100)}% completado";

                        if (currentStep > messageChangeValue * (messageIndex + 1) && messageIndex < loadingMessages.Length - 1)
                        {
                            messageIndex++;
                            lblLoadingMessage.Content = loadingMessages[messageIndex];
                        }
                    });
                }
                Dispatcher.Invoke(() => AddNewRow(rowImage));
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
                Dispatcher.Invoke(() =>
                {
                    Control controlWindow = new Control("Error al agregar la imagen a la fila.");
                    controlWindow.Show();
                });
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
                Dispatcher.Invoke(() =>
                {
                    Control controlWindow = new Control("Error al agregar una nueva fila.");
                    controlWindow.Show();
                });
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
            imagesItemsControl.ItemsSource = null;
            FolderNameTextBlock.Text = string.Empty;
            images = null;
        }


        void ValidationMessage()
        {
            string message;
            if (img_display_original.Source == null && images == null && resolution == 0)
            {
                message = "Seleccione imagen, carpeta y resolución.";
            }
            else if (img_display_original.Source == null && images == null)
            {
                message = "Seleccione imagen y carpeta.";
            }
            else if (img_display_original.Source == null && resolution == 0)
            {
                message = "Seleccione imagen y resolución.";
            }
            else if (images == null && resolution == 0)
            {
                message = "Seleccione carpeta y resolución.";
            }
            else if (img_display_original.Source == null)
            {
                message = "Seleccione la imagen.";
            }
            else if (images == null)
            {
                message = "Seleccione carpeta fuente.";
            }
            else
            {
                message = "Seleccione la resolución.";
            }

            Control controlWindow = new Control(message);
            controlWindow.Show();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                resolution = int.Parse(radioButton.Tag.ToString());
            }
        }

        void CleanRadioSelection()
        {
            rb1.IsChecked = false;
            rb2.IsChecked = false;
            rb3.IsChecked = false;
            rb4.IsChecked = false;
        }
    }
}
