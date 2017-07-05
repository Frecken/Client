using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;

namespace Client
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        DispatcherTimer dispatcherTimer;

        MediaCapture mediaCapture;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            List<IMediaEncodingProperties> videoResolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).ToList();
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, videoResolutions[3]);

            previewElement.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();

            DispatcherTimerSetup();
        }

        public void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 33);
            dispatcherTimer.Start();
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            try
            {
                VideoEncodingProperties previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                using (VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8,
                                                              (int)previewProperties.Width,
                                                              (int)previewProperties.Height))
                {
                    using (VideoFrame currentFrame = await mediaCapture.GetPreviewFrameAsync(videoFrame))
                    {
                        using (SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap)
                        {
                            WriteableBitmap wb = new WriteableBitmap(previewFrame.PixelWidth, previewFrame.PixelHeight);
                            previewFrame.CopyToBuffer(wb.PixelBuffer);

                            var cv = new OCV3.OCV3_Class();
                            bool detection = cv.DetectBarcode(wb);                            
                        }
                    }
                }
            }
            catch(Exception)
            {

            }
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();

            await mediaCapture.StopPreviewAsync();
        }
    }
}
