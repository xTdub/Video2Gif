using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoTrimmer
{
    static class Helpers
    {
        /// <summary>
        /// Creates a new ImageSource with the specified width/height
        /// </summary>
        /// <param name="source">Source image to resize</param>
        /// <param name="width">Width of resized image</param>
        /// <param name="height">Height of resized image</param>
        /// <returns>Resized image</returns>
        public static ImageSource CreateResizedImage(ImageSource source, int width, int height)
        {
            // Target Rect for the resize operation
            Rect rect = new Rect(0, 0, width, height);

            // Create a DrawingVisual/Context to render with
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawImage(source, rect);
            }

            // Use RenderTargetBitmap to resize the original image
            RenderTargetBitmap resizedImage = new RenderTargetBitmap(
                (int)rect.Width, (int)rect.Height,  // Resized dimensions
                96, 96,                             // Default DPI values
                PixelFormats.Default);              // Default pixel format
            resizedImage.Render(drawingVisual);

            // Return the resized image
            return resizedImage;
        }

        /// <summary>
        /// Covnert from a WPF <c>System.Windows.Media.BitmapImage</c> to a GDI <c>System.Drawing.Bitmap</c>
        /// </summary>
        /// <param name="bitmapsource">WPF <c>BitmapSource</c></param>
        /// <returns>GDI <c>Bitmap</c></returns>
        public static Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        /// <summary>
        /// Replace a chunk of this array with the contents of another
        /// </summary>
        /// <param name="arr">The current array</param>
        /// <param name="start">Index in the current array to start the replacement</param>
        /// <param name="src">The array to copy</param>
        /// <returns></returns>
        public static Array Replace(this Array arr, int start, Array src)
        {
            if (src == null) throw new ArgumentNullException("src");
            if (src.Length + start > arr.Length) throw new IndexOutOfRangeException();
            for(int i= 0; i < src.Length; i++)
            {
                arr.SetValue(src.GetValue(i), start + i);
                //arr[start + i] = src[i];
            }
            return arr;
        }
    }
}
