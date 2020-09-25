using DrawTools.Utils;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DrawTools.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            color_picker.SelectedColorChanged += delegate { this.drawCanvas.Brush = color_picker.SelectedBrush; btn_color.IsChecked = false; };
            color_picker.Canceled += delegate { btn_color.IsChecked = false; };
        }

        private void OnDrawToolChecked(Object sender, RoutedEventArgs e)
        {
            if (e.Source is RadioButton btn && btn.Tag is String typeStr)
                drawCanvas.DrawingToolType = (DrawToolType)Enum.Parse(typeof(DrawToolType), typeStr);
        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            drawCanvas.Clear();
        }

        private void OnSaveClick(Object sender, RoutedEventArgs e)
        {
            if (this.drawCanvas.GetDrawGeometries().Count() == 0)
                return;

            var folder = Path.Combine(Environment.CurrentDirectory, "Draws");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory("Draws");

            var dlg = new SaveFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                OverwritePrompt = true,
                DefaultExt = "xml",
                InitialDirectory = folder,
                RestoreDirectory = true
            };

            if ((Boolean)dlg.ShowDialog())
                this.drawCanvas.Save(dlg.FileName);
        }

        private void OnOpenClick_1(Object sender, RoutedEventArgs e)
        {
            var folder = Path.Combine(Environment.CurrentDirectory, "Draws");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory("Draws");

            var dlg = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                DefaultExt = "xml",
                InitialDirectory = folder,
                RestoreDirectory = true
            };

            if ((Boolean)dlg.ShowDialog())
                this.drawCanvas.Load(dlg.FileName);
        }

        private void OnPrintClick(Object sender, RoutedEventArgs e)
        {
            var backgroundImage = this.drawViewer.BackgroundImage;

            this.drawCanvas.Print(backgroundImage.PixelWidth, backgroundImage.PixelHeight, DpiHelper.GetDpiFromVisual(this.drawCanvas), backgroundImage);
        }

        private void OnSaveImageClick(Object sender, RoutedEventArgs e)
        {
            var backgroundImage = this.drawViewer.BackgroundImage;

            var frame = this.drawCanvas.ToBitmapFrame(backgroundImage.PixelWidth, backgroundImage.PixelHeight, DpiHelper.GetDpiFromVisual(this.drawCanvas), backgroundImage);

            if (frame == null)
                return;

            var folder = Path.Combine(Environment.CurrentDirectory, "Images");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory("Images");

            var dlg = new SaveFileDialog
            {
                Filter = "Images files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp",
                OverwritePrompt = true,
                DefaultExt = "jpg",
                InitialDirectory = folder,
                RestoreDirectory = true
            };

            if ((Boolean)dlg.ShowDialog())
                ImageHelper.Save(dlg.FileName, frame);
        }
    }
}
