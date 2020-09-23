using System.Windows;
using System.Windows.Media;

namespace DrawTools.Attach
{
    public static class IconElement
    {
        public static readonly DependencyProperty ImageProperty = DependencyProperty.RegisterAttached(
          "Image", typeof(ImageSource), typeof(IconElement));

        public static void SetImage(DependencyObject element, ImageSource value) => element.SetValue(ImageProperty, value);

        public static ImageSource GetImage(DependencyObject element) => (ImageSource)element.GetValue(ImageProperty);

        public static readonly DependencyProperty ImageSelectedProperty = DependencyProperty.RegisterAttached(
           "ImageSelected", typeof(ImageSource), typeof(IconElement));

        public static void SetImageSelected(DependencyObject element, ImageSource value) => element.SetValue(ImageSelectedProperty, value);

        public static ImageSource GetImageSelected(DependencyObject element) => (ImageSource)element.GetValue(ImageSelectedProperty);
    }
}
