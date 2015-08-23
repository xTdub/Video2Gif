using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;

namespace VideoTrimmer
{
    /// <summary>
    /// Encoder for both single and multiple frame GIFs
    /// </summary>
    /// <remarks>Developed from http://giflib.sourceforge.net/whatsinagif/index.html </remarks>
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
        /// Encoder is currently processing frames
        /// </summary>
        public bool Processing;
        /// <summary>
        /// Number of frames processed
        /// </summary>
        public int CompletedFrames
        {
            get { return FrameCount; }
        }

        /// <summary>
        /// Determines if encoder is ready to start writing data to the stream
        /// </summary>
        private bool Ready
        {
            get { return (DataStream != null && Width != 0 && Height != 0 && ColorPalette != null); }
        }
        /// <summary>
        /// Stream to write GIF data into
        /// </summary>
        private Stream DataStream;
        /// <summary>
        /// Whether or not the encoder owns the stream
        /// </summary>
        private bool InternalStream;
        /// <summary>
        /// Number of frames currently encoded
        /// </summary>
        private int FrameCount;
        /// <summary>
        /// Background thread for freeing the UI during processing
        /// </summary>
        private Thread EncoderThread;
        /// <summary>
        /// Queue of frames ready to be processed
        /// </summary>
        public Queue<ImageData> ImageQueue;
        
        /// <summary>
        /// Creates an empty instance of the <c>GifEncoder</c>
        /// </summary>
        public GifEncoder()
        {
            FrameCount = Width = Height = FrameTime = 0;
            Animated = false;
            ColorPalette = BitmapPalettes.Halftone256Transparent;
        }
        /// <summary>
        /// Creates an instance of <c>GifEncoder</c> prepared for a still image
        /// </summary>
        public GifEncoder(int width, int height, BitmapPalette palette)
        {
            FrameCount = 0;
            Width = width;
            Height = height;
            ColorPalette = palette;
            Animated = false;
        }
        /// <summary>
        /// Creates an instance of <c>GifEncoder</c> prepared for an animated image
        /// </summary>
        public GifEncoder(int width, int height, float framerate, BitmapPalette palette)
        {
            FrameCount = 0;
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
        /// <summary>
        /// Set the destination to an external <c>Stream</c>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Set the destination to an internal <c>FileStream</c>
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if file was created and opened successfully, false upon error</returns>
        public bool SetFile(string path)
        {
            if (DataStream != null || string.IsNullOrWhiteSpace(path)) return false;
            try
            {
                DataStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
                InternalStream = true;
                return true;
            }
            catch (IOException ioex)
            {
                DataStream = null;
                return false;
            }
        }
        /// <summary>
        /// Determine number of bits required to store a number
        /// </summary>
        public byte NumBits(int x)
        {
            byte n = 0;
            do
            {
                x >>= 1;
                n++;
            } while (x != 0);
            return n;
        }
        /// <summary>
        /// Write the header bits to the file
        /// </summary>
        /// <param name="frames">Total number of frames to be processed</param>
        public void Start(int frames = 1)
        {
            if (!Ready) return;
            
            ImageQueue = new Queue<ImageData>(frames);
            EncoderThread = new Thread(new ThreadStart(ProcessQueue));

            byte[] headerBlock = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; //GIF89a

            byte[] lsd = new byte[7]; //Logical Screen Descriptor
            lsd.Replace(0, BitConverter.GetBytes((UInt16)Width));
            lsd.Replace(2, BitConverter.GetBytes((UInt16)Height));
            byte tblSize = NumBits(ColorPalette.Colors.Count - 1);
            lsd[4] = (byte)(0x80 | (tblSize - 1 << 4) | (tblSize - 1));
            lsd[5] = 0x00;
            lsd[6] = 0x00;

            DataStream.Write(headerBlock, 0, headerBlock.Length);
            DataStream.Write(lsd, 0, lsd.Length);

            genGCT();
            if (Animated) genAE();

            EncoderThread.Start();
        }
        /// <summary>
        /// Convert a <c>BitmapSource</c> to a thread safe struct and add it to the processing queue
        /// </summary>
        /// <param name="image">Image from which to extract basic information and convert to a pixel array</param>
        public void QueueFrame(BitmapSource image)
        {
            if (!Ready) return;
            if (!Animated && FrameCount == 1) return;
            if (image.PixelHeight != Height || image.PixelWidth != Width) return;
            if (image.Palette != ColorPalette) return;

            ImageData dat = new ImageData();
            dat.Width = image.PixelWidth;
            dat.Height = image.PixelHeight;
            dat.BitsPerPixel = image.Format.BitsPerPixel;
            int stride = (dat.Width * dat.BitsPerPixel + 7) / 8;
            dat.PixelData = new byte[dat.Height * stride];
            image.CopyPixels(dat.PixelData, stride, 0);
            ImageQueue.Enqueue(dat);
        }
        private void ProcessQueue()
        {
            while (true)
            {
                if (ImageQueue.Count > 0)
                {
                    Processing = true;

                    var image = ImageQueue.Dequeue();

                    //Graphics Control Extension
                    byte[] gct = new byte[8];
                    gct[0] = 0x21;
                    gct[1] = 0xF9;
                    gct[2] = 0x04;
                    gct[3] = 0x00; //Packed Field
                    gct.Replace(4, BitConverter.GetBytes((UInt16)FrameTime));
                    gct[6] = 0x00; //Transparency Color Index
                    gct[7] = 0x00;

                    //Image Descriptor
                    byte[] id = new byte[10];
                    id[0] = 0x2C;
                    id.Replace(1, BitConverter.GetBytes((UInt16)0)); //Left
                    id.Replace(3, BitConverter.GetBytes((UInt16)0)); //Top
                    id.Replace(5, BitConverter.GetBytes((UInt16)image.Width)); //Width
                    id.Replace(7, BitConverter.GetBytes((UInt16)image.Height)); //Height
                    id[9] = 0x00; //Packed Field

                    DataStream.Write(gct, 0, gct.Length);
                    DataStream.Write(id, 0, id.Length);

                    //Image Data
                    var lzw = new Gif.Components.LZWEncoder(image.Width, image.Height, image.PixelData, image.BitsPerPixel);
                    lzw.Encode(DataStream);

                    FrameCount++;

                    Processing = false;
                }
            }
        }
        /// <summary>
        /// Write an image frame to the file
        /// </summary>
        /// <param name="image"><c>BitmapSource</c> to encode</param>
        public void AddFrame(BitmapSource image)
        {
            if (!Ready) return;
            if (!Animated && FrameCount == 1) return;
            if (image.PixelHeight != Height || image.PixelWidth != Width) return;
            if (image.Palette != ColorPalette) return;

            int nStride = (image.PixelWidth * image.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[image.PixelHeight * nStride];
            image.CopyPixels(pixels, nStride, 0);

            //Graphics Control Extension
            byte[] gct = new byte[8];
            gct[0] = 0x21;
            gct[1] = 0xF9;
            gct[2] = 0x04;
            gct[3] = 0x00; //Packed Field
            gct.Replace(4, BitConverter.GetBytes((UInt16)FrameTime));
            gct[6] = 0x00; //Transparency Color Index
            gct[7] = 0x00;

            //Image Descriptor
            byte[] id = new byte[10];
            id[0] = 0x2C;
            id.Replace(1, BitConverter.GetBytes((UInt16)0)); //Left
            id.Replace(3, BitConverter.GetBytes((UInt16)0)); //Top
            id.Replace(5, BitConverter.GetBytes((UInt16)image.PixelWidth)); //Width
            id.Replace(7, BitConverter.GetBytes((UInt16)image.PixelHeight)); //Height
            id[9] = 0x00; //Packed Field

            DataStream.Write(gct, 0, gct.Length);
            DataStream.Write(id, 0, id.Length);

            //Image Data
            var lzw = new Gif.Components.LZWEncoder(image.PixelWidth, image.PixelHeight, pixels, image.Format.BitsPerPixel);
            lzw.Encode(DataStream);

            FrameCount++;
        }
        /// <summary>
        /// Write the footer bits to the file
        /// </summary>
        public void Finish()
        {
            if (!Ready) return;
            EncoderThread.Abort();

            DataStream.WriteByte(0x3B);
            DataStream.Flush();
        }

        /// <summary>
        /// Generate the Global Color Table
        /// </summary>
        private void genGCT()
        {
            var length = ColorPalette.Colors.Count;
            byte[] table = new byte[length*3];
            for (int i = 0; i < length; i++)
            {
                table[i * 3] = ColorPalette.Colors[i].R;
                table[i * 3 + 1] = ColorPalette.Colors[i].G;
                table[i * 3 + 2] = ColorPalette.Colors[i].B;
            }
            DataStream.Write(table, 0, table.Length);
        }

        /// <summary>
        /// Generate the Application Extension block
        /// </summary>
        private void genAE()
        {
            byte[] ext = new byte[19];
            ext[0] = 0x21;
            ext[1] = 0xFF;
            ext[2] = 0x0B;
            ext.Replace(3, Encoding.ASCII.GetBytes("NETSCAPE2.0"));
            ext[14] = 0x03;
            ext[15] = 0x01;
            ext.Replace(16, BitConverter.GetBytes((UInt16)Repeat));
            ext[18] = 0x00;

            DataStream.Write(ext, 0, ext.Length);
        }
    }
    /// <summary>
    /// Barebones representation of an image
    /// </summary>
    struct ImageData
    {
        public int Width, Height;
        public int BitsPerPixel;
        public byte[] PixelData;
    }
}
