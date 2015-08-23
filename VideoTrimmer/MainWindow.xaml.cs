﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                    tbFileName.Text = String.Format("File: {0}", dialog.FileName);
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

        private async void buMakeGIF_Click(object sender, RoutedEventArgs e)
        {
            if (mp == null) return;
            toast.butter = Path.ChangeExtension(file, ".gif");
            double start = udStartTime.Value.Value;
            double stop = udStopTime.Value.Value;
            double fps = udFramerate.Value.Value;
            double outfps = udOutFramerate.Value.Value;
            double interval = 1.0 / fps;
            double scale = udScale.Value.Value;
            uint[] framePixels = new uint[mp.NaturalVideoWidth * mp.NaturalVideoHeight];
            int height = (int)(mp.NaturalVideoHeight * scale);
            int width = (int)(mp.NaturalVideoWidth * scale);
            int frames = (int)((stop - start) * udFramerate.Value.Value);

            var palette = paletteNames[cbPalette.SelectedItem as string];
            var gifEn = new GifEncoder(width, height, (float)outfps, palette);
            var outfile = Path.ChangeExtension(file, ".gif");
            if (!gifEn.SetFile(outfile))
            {
                MessageBox.Show("Error setting the output file");
                return;
            }
            gifEn.Start(frames);

            updateProgress(gifEn, frames, file);

            mp.Position = TimeSpan.FromSeconds(start);
            await Task.Delay(100);
            RenderBitmapAndCapturePixels(framePixels);
            
            for (int i=0; i<frames;i++)
            {
                mp.Position = TimeSpan.FromSeconds(start + interval * i);
                await Task.Delay(100);
                var img = RenderBitmapAndCapturePixels(framePixels);
                imFrame.Source = img;

                FormatConvertedBitmap fcb = new FormatConvertedBitmap(CreateResizedImage(img as ImageSource, width, height) as BitmapSource, PixelFormats.Indexed8, palette, 0);
                gifEn.QueueFrame(fcb);
            }
        }

        private async void updateProgress(GifEncoder gifEn, int frames, string file)
        {
            DateTime stime = DateTime.Now;
            tbProgress.Text = string.Format("Frames: {0}/{1}", 0, frames);
            Queue<double> eta_q = new Queue<double>(10);
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;

            do
            {
                TaskbarItemInfo.ProgressValue = (double)gifEn.CompletedFrames / frames;
                var elapsed = DateTime.Now - stime;
                eta_q.Enqueue((double)elapsed.Ticks / (gifEn.CompletedFrames == 0 ? 1 : gifEn.CompletedFrames));
                tbProgress.Text = string.Format("Frames: {0}/{1}", gifEn.CompletedFrames, frames);
                tbElapse.Text = string.Format("Elapsed: {0:mm\\:ss}", elapsed);
                tbETA.Text = string.Format("ETA: {0:mm\\:ss}", TimeSpan.FromTicks((long)(eta_q.Average() * (frames - gifEn.CompletedFrames))));
                await Task.Delay(100);
            }
            while (gifEn.CompletedFrames < frames);
            tbProgress.Text = string.Format("Frames: {0}/{1}", gifEn.CompletedFrames, frames);

            gifEn.Finish();

            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
            tbETA.Text = "ETA: Finished";
            toast.ShowToast(string.Format("Finished processing \"{0}\"", Path.GetFileName(file)));
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

        private void udOutFramerate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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
