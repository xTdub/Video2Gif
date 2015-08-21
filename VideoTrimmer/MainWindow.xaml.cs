using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Gif.Components;
using static VideoTrimmer.Helpers;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Toasts toast;
        public Dictionary<string, BitmapPalette> paletteNames;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            toast = new Toasts();
            toast.TryCreateShortcut();

            InitColorsDropdown();

            var en = new GifEncoder();
            var val1 = en.NumBits(256);
            var val2 = en.NumBits(255);
        }

        private void InitColorsDropdown()
        {
            paletteNames = new Dictionary<string, BitmapPalette>(11);
            paletteNames.Add("BlackAndWhite", BitmapPalettes.BlackAndWhite);
            paletteNames.Add("Gray4", BitmapPalettes.Gray4);
            paletteNames.Add("Gray16", BitmapPalettes.Gray16);
            paletteNames.Add("Gray256", BitmapPalettes.Gray256);
            paletteNames.Add("Halftone8", BitmapPalettes.Halftone8);
            paletteNames.Add("Halftone27", BitmapPalettes.Halftone27);
            paletteNames.Add("Halftone64", BitmapPalettes.Halftone64);
            paletteNames.Add("Halftone125", BitmapPalettes.Halftone125);
            paletteNames.Add("Halftone216", BitmapPalettes.Halftone216);
            paletteNames.Add("Halftone256", BitmapPalettes.Halftone256);
            paletteNames.Add("WebPalette", BitmapPalettes.WebPalette);
            cbPalette.ItemsSource = paletteNames.Keys;
            cbPalette.SelectedIndex = 9;
        }
        private MediaPlayer mp;
        private DispatcherTimer timer;
        private string file;
        private void buLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog()
            {
                AutoUpgradeEnabled = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Multiselect = false,
                CheckFileExists = true
            };
            var res = dialog.ShowDialog();
            if(res != System.Windows.Forms.DialogResult.Cancel)
            {
                file = dialog.FileName;
                try
                {
                    //avFile = new AudioVideoFile(dialog.FileName);
                    tbFileName.Text = String.Format("File: {0}", dialog.FileName);
                    //mdVid.MediaSource = new Uri(dialog.FileName);
                    mp = new MediaPlayer();
                    mp.ScrubbingEnabled = true;
                    mp.MediaOpened += Mp_MediaOpened;
                    mp.Open(new Uri(dialog.FileName));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    tbFileName.Text = "File: N/A";
                    tbDuration.Text = "Duration: N/A";
                    mp = null;
                }
            }
        }

        private void Mp_MediaOpened(object sender, EventArgs e)
        {
            tbDuration.Text = String.Format("Duration: {0}", udStartTime.Maximum = udStopTime.Maximum = mp.NaturalDuration.TimeSpan.TotalSeconds);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            //unqueue and get bitmap
        }

        private async void buGetFrame_Click(object sender, RoutedEventArgs e)
        {
            if(mp!= null)
            {
                mp.Position = TimeSpan.FromSeconds(udStartTime.Value.Value);
                await Task.Delay(50);
                uint[] framePixels = new uint[mp.NaturalVideoWidth * mp.NaturalVideoHeight];
                var img = RenderBitmapAndCapturePixels(framePixels);
                imFrame.Source = img;
            }
        }

        private ImageSource RenderBitmapAndCapturePixels(uint[] pixels)
        {
            // Render the current frame into a bitmap
            var drawingVisual = new DrawingVisual();
            var renderTargetBitmap = new RenderTargetBitmap(mp.NaturalVideoWidth, mp.NaturalVideoHeight, 96, 96, PixelFormats.Default);
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawVideo(mp, new Rect(0, 0, mp.NaturalVideoWidth, mp.NaturalVideoHeight));
            }
            renderTargetBitmap.Render(drawingVisual);

            // Copy the pixels to the specified location
            renderTargetBitmap.CopyPixels(pixels, mp.NaturalVideoWidth * 4, 0);
            
            // Return the bitmap
            return renderTargetBitmap;
        }
        private AnimatedGifEncoder enc;
        //private BitmapImage[] frames;
        private async void buMakeGIF_Click(object sender, RoutedEventArgs e)
        {
            toast.butter = Path.ChangeExtension(file, ".gif");
            double start = udStartTime.Value.Value;
            double stop = udStopTime.Value.Value;
            double fps = udFramerate.Value.Value;
            double interval = 1.0 / fps;
            double scale = udScale.Value.Value;
            uint[] framePixels = new uint[mp.NaturalVideoWidth * mp.NaturalVideoHeight];
            //frames = new BitmapImage[(int)((stop - start) * udFramerate.Value.Value) + 1];
            int height = (int)(mp.NaturalVideoHeight * scale);
            int width = (int)(mp.NaturalVideoWidth * scale);
            int frames = (int)((stop - start) * udFramerate.Value.Value) + 1;

            enc = new AnimatedGifEncoder();
            enc.Start(Path.ChangeExtension(file, ".gif"));
            enc.SetFrameRate((float)udFramerate.Value.Value);
            enc.SetSize(width, height);
            enc.SetRepeat(0);
            enc.SetQuality(30);
            //gifCol = new MagickImageCollection();
            //enc2 = new GifBitmapEncoder();
            //enc2.Palette = BitmapPalettes.Halftone256;
            var gifEn = new GifEncoder(width, height, (float)fps, paletteNames[cbPalette.SelectedItem as string]);
            var file2 = Path.ChangeExtension(file, ".gif");
            file2 = Path.Combine(Path.GetDirectoryName(file2), Path.GetFileNameWithoutExtension(file2) + "B.gif");
            gifEn.SetFile(file2);
            gifEn.Start();

            DateTime stime = DateTime.Now;
            tbProgress.Text = string.Format("Frames: {0}/{1}", 0, frames);
            Queue<double> eta_q = new Queue<double>(10);
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            mp.Position = TimeSpan.FromSeconds(start);
            await Task.Delay(50);
            RenderBitmapAndCapturePixels(framePixels);

            int i = 0;
            for (double t= start; t<= stop; t += interval)
            {
                mp.Position = TimeSpan.FromSeconds(t);
                await Task.Delay(100);
                var img = RenderBitmapAndCapturePixels(framePixels);
                imFrame.Source = img;
                //frames[i] = img as BitmapImage;
                //var im2 = new BitmapImage
                //FormatConvertedBitmap fcb = new FormatConvertedBitmap(img as BitmapSource, PixelFormats.Indexed8, paletteNames[cbPalette.SelectedItem as string], 0);
                FormatConvertedBitmap fcb = new FormatConvertedBitmap(CreateResizedImage(img as ImageSource, width, height) as BitmapSource, PixelFormats.Indexed8, paletteNames[cbPalette.SelectedItem as string], 0);
                gifEn.AddFrame(fcb);
                //en.AddFrame(new WriteableBitmap());
                //IWICBitmapFrameEncode outputFrame;
                //enc2.Frames.Add(BitmapFrame.Create(CreateResizedImage(fcb,width,height) as BitmapSource));
                //enc.AddFrame(new System.Drawing.Bitmap(BitmapFromSource(fcb as BitmapSource), width, height));
                //var bmp = new System.Drawing.Bitmap(BitmapFromSource(img as BitmapSource), width, height);
                //gifCol.Add(new MagickImage(new System.Drawing.Bitmap(BitmapFromSource(fcb as BitmapSource), width, height)));
                //gifCol[i].AnimationDelay = (int)(interval * 100);

                i++;
                TaskbarItemInfo.ProgressValue = (double)i / frames;
                var elapsed = DateTime.Now - stime;
                eta_q.Enqueue((double)elapsed.Ticks / i);
                tbProgress.Text = string.Format("Frames: {0}/{1}", i, frames);
                tbElapse.Text = string.Format("Elapsed: {0:mm\\:ss}", elapsed);
                tbETA.Text = string.Format("ETA: {0:mm\\:ss}", TimeSpan.FromTicks((long)(eta_q.Average()*(frames-i))));
            }
            gifEn.Finish();
            //FileStream fs = new FileStream(Path.ChangeExtension(file, ".gif"), FileMode.Create);
            //enc2.Save(fs);
            //fs.Close();
            //fs.Dispose();
            enc.Finish();
            //QuantizeSettings qset = new QuantizeSettings();
            //qset.Colors = 256;
            //gifCol.Quantize(qset);
            //gifCol.Optimize();
            //gifCol.Write(Path.ChangeExtension(file, ".gif"));

            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
            tbETA.Text = "ETA: Finished";
            toast.ShowToast(string.Format("Finished processing \"{0}\"", Path.GetFileName(file)));
            //System.Media.SystemSounds.Beep.Play();
        }

        private void udStartTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (mp == null) return;
            if (udStartTime.Value.Value + 1 > udStopTime.Value.Value) udStopTime.Value = udStartTime.Value + 1;
            UpdateEstimate();
        }

        private void udStopTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (mp == null) return;
            if (udStartTime.Value.Value + 1 > udStopTime.Value.Value) udStartTime.Value = udStopTime.Value - 1;
            UpdateEstimate();
        }

        private void udFramerate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (mp == null) return;
            UpdateEstimate();
        }

        private void udScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (mp == null) return;
            UpdateEstimate();
        }

        private void UpdateEstimate()
        {
            int frames = (int)((udStopTime.Value.Value - udStartTime.Value.Value) * udFramerate.Value.Value);
            long bytes = (long)(frames * (mp.NaturalVideoHeight * mp.NaturalVideoWidth * (udScale.Value.Value* udScale.Value.Value)));
            tbEstimate.Text = String.Format("Size Estimate: {0} frames, {1} KB", frames, bytes / 1024);
        }

    }
}
