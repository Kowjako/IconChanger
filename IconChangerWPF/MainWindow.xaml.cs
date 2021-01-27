using System;
using TAFactory.IconPack;
using Vestris.ResourceLib;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security;

namespace IconChangerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string endIconPath = null;
        private static string appPath = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void infoBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Autor: Wlodzimierz Kowjako\nData: 16.01.2021", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void endIcon_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            endIconPath = files[0];
            endIcon.Source = BitmapFromUri(new Uri(files[0]));
            endIcon.Margin = new Thickness(14.0);
            endIcon.Width = 50;
            endIcon.Height = 50;
            iconText.Text = "";
        }

        public static ImageSource BitmapFromUri(Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = source;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        private void refreshBtn_Click(object sender, RoutedEventArgs e)
        {
            /* Returns start appearance of endIcon and appIcon controls */
            endIcon.Source = new BitmapImage(new Uri("Resources/start.png", UriKind.Relative));
            iconText.Text = "ICON";
            appIcon.Source = new BitmapImage(new Uri("Resources/start.png", UriKind.Relative));
            appText.Text = "APP";
            endIcon.Margin = new Thickness(4.0);
            endIcon.Width = 70;
            endIcon.Height = 70;
            appIcon.Width = 70;
            appIcon.Height = 70;
            appIcon.Margin = new Thickness(4.0);
            File.Delete(@"D:\icon1.ico");
        }

        private void appIcon_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            appPath = files[0];
            List<Icon> icons = IconHelper.ExtractAllIcons(files[0]);
            Icon smallIcon;
            try
            {
                smallIcon = IconHelper.GetBestFitIcon(icons[0], new System.Drawing.Size(64, 64));
            }
            catch
            {
                /* If app hasn't icon */
                smallIcon = IconHelper.GetBestFitIcon(IconHelper.ExtractIcon(@"%SystemRoot%\system32\imageres.dll", 11), new System.Drawing.Size(64, 64));
            }
            string path = @"D:\icon1.ico";
            FileStream fs = File.Create(path);
            smallIcon.Save(fs);
            fs.Close();
            appIcon.Source = BitmapFromUri(new Uri(path));
            appIcon.Width = 50;
            appIcon.Height = 50;
            appIcon.Margin = new Thickness(14.0);
            appText.Text = ""; 
        }

        private void applyBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private byte[] getBytesFromImage(Uri source)
        {
            using(MemoryStream ms = new MemoryStream())
            {
                Image img = Image.FromFile(source.ToString());
                img.Save(ms,ImageFormat.Icon);
                return ms.ToArray();
            }
        }
    }
}
