using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace DrawTools.Utils
{
    public static class ImageHelper
    {
        public static void Save(String filepath, params BitmapFrame[] frames)
        {
            BitmapEncoder encoder = null;


            switch (Path.GetExtension(filepath))
            {
                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                default:
                    encoder = new BmpBitmapEncoder();
                    break;
            }


            foreach (var frame in frames)
            {
                encoder.Frames.Add(frame);
            }

            using (var fs = new FileStream(filepath, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }
    }
}
