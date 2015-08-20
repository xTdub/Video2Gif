using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace VideoTrimmer
{
    class GifEncoder
    {
        /// <summary>
        /// Height of the frame in pixels
        /// </summary>
        public int Height;
        /// <summary>
        /// Width of the frame in pixels
        /// </summary>
        public int Width;
        /// <summary>
        /// Time delay for animations in centiseconds (1/100s)
        /// </summary>
        public int FrameTime;
        /// <summary>
        /// Framerate for animations in frames per second
        /// </summary>
        /// <remarks>Directly modifies <c>FrameTime</c></remarks>
        public float FrameRate
        {
            get { return 100f/FrameTime; }
            set { FrameTime = (int)(100f / value); }
        }
        /// <summary>
        /// Whether or not the GIF contains multiple frames
        /// </summary>
        public bool Animated;
        /// <summary>
        /// Palette to be used when creating color table
        /// </summary>
        public BitmapPalette ColorPalette;
        /// <summary>
        /// Number of repeats, 0 for infinite
        /// </summary>
        public int Repeat;

        /// <summary>
        /// Stream to write GIF data into
        /// </summary>
        private Stream DataStream;
        /// <summary>
        /// Whether or not the encoder owns the stream
        /// </summary>
        private bool InternalStream;
        
        /// <summary>
        /// Creates an empty instance of the <c>GifEncoder</c>
        /// </summary>
        public GifEncoder()
        {
            Width = Height = FrameTime = 0;
            Animated = false;
            ColorPalette = BitmapPalettes.Halftone256Transparent;
        }
        /// <summary>
        /// Creates an instance of <c>GifEncoder</c> prepared for a still image
        /// </summary>
        public GifEncoder(int width, int height, BitmapPalette palette)
        {
            Width = width;
            Height = height;
            ColorPalette = palette;
            Animated = false;
        }
        /// <summary>
        /// Creates an isntance of <c>GifEncoder</c> prepared for an animated image
        /// </summary>
        public GifEncoder(int width, int height, float framerate, BitmapPalette palette)
        {
            Width = width;
            Height = height;
            ColorPalette = palette;
            FrameRate = framerate;
            Animated = true;
        }
        ~GifEncoder()
        {
            if (InternalStream)
            {
                DataStream.Close();
                DataStream.Dispose();
                DataStream = null;
            }
        }
        public bool SetStream(Stream stream)
        {
            if(stream == null || DataStream == null)
            {
                DataStream = stream;
                InternalStream = false;
                return true;
            }
            return false;
        }
        public bool SetFile(string path)
        {
            if (DataStream != null || string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                DataStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);
                InternalStream = true;
                return true;
            }
            catch (IOException)
            {
                DataStream = null;
                return false;
            }
        }
        /// <summary>
        /// Determine number of bits required to store a number
        /// </summary>
        public int NumBits(int x)
        {
            int n = 0;
            do
            {
                x >>= 1;
                n++;
            } while (x != 0);
            return n;
        }
        public void Start()
        {

        }
        public void AddFrame(BitmapSource image)
        {
            
        }
        public void Finish()
        {

        }
    }
}
