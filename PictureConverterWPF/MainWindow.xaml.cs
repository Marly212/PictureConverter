using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace PictureConverterWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static int images = 0;
        public MainWindow()
        {
            InitializeComponent();
            cboxFormat.Items.Add("png");
            cboxFormat.Items.Add("jpg");
            cboxFormat.SelectedItem = cboxFormat.Items[0];
        }

        private void FolderSearch(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog dialog = new();

            bool? result = dialog.ShowDialog();

            if (result == false)
            {
                return;
            }

            txtFile.Text = "";
            txtFolder.Text = dialog.SelectedPath;
        }

        private void FileSearch(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*webp;*avif;*gif";
            dialog.Multiselect = true;
            dialog.ValidateNames = true;
            dialog.Title = "Select Picture to Convert";

            if (dialog.ShowDialog() == true)
            {
                txtFolder.Text = "";
                txtFile.Text = dialog.FileName;
            }
        }

        private async void Start(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            DisableAllButtons();

            if (string.IsNullOrEmpty(txtFolder.Text) && string.IsNullOrEmpty(txtFile.Text))
            {
                _ = MessageBox.Show("No Folder or File is Selected", "Information");
                progressBar.Visibility = Visibility.Hidden;
                EnableAllButtons();
                return;
            }

            if (!string.IsNullOrEmpty(txtFile.Text))
            {
                var index = cboxFormat.SelectedIndex;
                var format = "";

                switch (index)
                {
                    case 0:
                        format = "png";
                        break;

                    case 1:
                        format = "jpg";
                        break;

                    default:
                        break;
                }
                await PrepareData(txtFile.Text, false, false, format);
            }

            else if (!string.IsNullOrEmpty(txtFolder.Text))
            {
                var index = cboxFormat.SelectedIndex;
                var format = "";
                bool subfolder = false;

                switch (index)
                {
                    case 0:
                        format = "png";
                        break;

                    case 1:
                        format = "jpg";
                        break;

                    default:
                        break;
                }

                if (cboxSubfolder.IsChecked == true)
                {
                    subfolder = true;
                }
                //images = GetPictureCountToConvert(txtFolder.Text, true, subfolder);
                //lblProgress.Content = "0/" + images.ToString();

                await PrepareData(txtFolder.Text, true, subfolder, format);
            }
            progressBar.Visibility = Visibility.Hidden;
            EnableAllButtons();
            _ = MessageBox.Show("Finish Converting Images", "Message");
        }

        public static async Task PrepareData(string fullPath, bool folder, bool subfolder, string format) //format 0 = png, 1 = jpg
        {
            MainWindow main = new();
            var index = 0;
            if (folder)
            {
                foreach (string imageFileName in Directory.GetFiles(fullPath))
                {
                    index++;
                    main.lblProgress.Content = index + "/" + images.ToString();

                    await Task.Run(async () =>
                    {
                        await Convert.ConvertImage(imageFileName, format);
                    });
                }

                if (subfolder)
                {
                    foreach (string d in Directory.GetDirectories(fullPath))
                    {
                        foreach (string imageFileName in Directory.GetFiles(d))
                        {
                            index++;
                            main.lblProgress.Content = index + "/" + images.ToString();

                            await Task.Run(async () =>
                            {
                                await Convert.ConvertImage(imageFileName, format);
                            });
                        }
                    }
                }
            }
            else
            {
                if (File.Exists(fullPath))
                {
                    await Task.Run(async () =>
                    {
                        await Convert.ConvertImage(fullPath, format);
                    });
                }
            }
        }


        #region Helper Methodes
        private int GetPictureCountToConvert(string path, bool folder, bool subfolder)
        {
            if (folder)
            {
                int images = 0;

                if (subfolder)
                {
                    foreach (string d in Directory.GetDirectories(path))
                    {
                        foreach (string imageFileName in Directory.GetFiles(d))
                        {
                            images++;
                        }
                    }
                }
                else
                {
                    foreach (string image in Directory.GetFiles(path))
                    {
                        images++;
                    }
                }
                return images;
            }
            else
            {
                return 1;
            }
        }
        private void DisableAllButtons()
        {
            bBrowseFile.IsEnabled = false;
            bBrowseFolder.IsEnabled = false;
            bStart.IsEnabled = false;
        }
        private void EnableAllButtons()
        {
            bBrowseFile.IsEnabled = true;
            bBrowseFolder.IsEnabled = true;
            bStart.IsEnabled = true;
        }
        #endregion
    }
}
