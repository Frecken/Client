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
using Matrix;
using Matrix.Xmpp.Client;

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

        bool latchActive = false;
        short lockedFrames = 0;
        bool barcodeDetectedOld = false;

        static string workMode = "INC";

        private FileTransferManager fm;
        private string sid = "";

        static XmppClient clientXMPP;
        string hostname;
        string username;
        string password;

        DispatcherTimer dispatcherTimer;

        MediaCapture mediaCapture;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Загрузка настроек XMPP из файла настроек 
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.GetFileAsync("XMPP.cfg");
            using (var inputStream = await file.OpenReadAsync())
            {
                using (var classicStream = inputStream.AsStreamForRead())
                {
                    using (var streamReader = new StreamReader(classicStream))
                    {
                        string[] xmppConfig = new string[3];
                        int i = 0;

                        while (streamReader.Peek() >= 0)
                        {
                            xmppConfig[i] = streamReader.ReadLine();
                            i++;
                        }

                        hostname = xmppConfig[0];
                        username = xmppConfig[1];
                        password = xmppConfig[2];
                    }
                }
            }

            //Подготовка к захвату кадров
            mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync();

            //Получение списка режимов, в которых способна работать камера
            List<IMediaEncodingProperties> videoResolutions = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview).ToList();
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview,
                                                                                   videoResolutions[3]);
            //Настройка вывода захваченных кадров
            previewElement.Source = mediaCapture;
            await mediaCapture.StartPreviewAsync();

            //Настройка обработчика кадров
            DispatcherTimerSetup();
        }

        public void DispatcherTimerSetup()
        {
            //Настройка частоты вызова обработчика кадров
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 33);
            dispatcherTimer.Start();
        }

        private async void DispatcherTimer_Tick(object sender, object e)
        {
            try
            {
                //Подготовка к получению кадра
                VideoEncodingProperties previewProperties = mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

                using (VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8,
                                                              (int)previewProperties.Width,
                                                              (int)previewProperties.Height))
                {
                    //Получение кадра
                    using (VideoFrame currentFrame = await mediaCapture.GetPreviewFrameAsync(videoFrame))
                    {
                        //Подготовка к работе с кадром
                        using (SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap)
                        {
                            WriteableBitmap writBitmap = new WriteableBitmap(previewFrame.PixelWidth,
                                                                             previewFrame.PixelHeight);
                            previewFrame.CopyToBuffer(writBitmap.PixelBuffer);

                            //Проверка наличия штрих-кода в кадре
                            var cv = new OCV3.OCV3_Class();
                            bool barcodeDetected = cv.DetectBarcode(writBitmap);

                            //Работа с защелкой
                            if (barcodeDetected == true)
                            {
                                if (latchActive == true)
                                {
                                    if (lockedFrames < 10)
                                    {
                                        lockedFrames++;
                                    }
                                    else
                                    {
                                        latchActive = false;
                                        lockedFrames = 0;
                                    }
                                }
                                else
                                {
                                    if (barcodeDetectedOld == true)
                                    {

                                    }
                                    else
                                    {
                                        latchActive = true;
                                        lockedFrames = 0;

                                        //Подготовка к отправке кадра на сервер
                                        Jid jid = new Jid(username,
                                                          hostname,
                                                          "server");
                                        StorageFile savedStorageFile = await WriteableBitmapToStorageFile(writBitmap,
                                                                                                          FileFormat.Jpeg);
                                        if (radioButton1.IsChecked == true)
                                        {
                                            workMode = "INC";
                                        }
                                        else if (radioButton2.IsChecked == true)
                                        {
                                            workMode = "DEC";
                                        }

                                        //Отправка кадра на сервер
                                        sid = await fm.Send(jid,
                                                            savedStorageFile,
                                                            workMode);
                                    }
                                }
                            }

                            barcodeDetectedOld = barcodeDetected;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //Остановка обработчика кадров
            dispatcherTimer.Stop();

            //Остановка захвата кадров
            await mediaCapture.StopPreviewAsync();
        }

        private async Task<StorageFile> WriteableBitmapToStorageFile(WriteableBitmap WB, FileFormat fileFormat)
        {
            string dateTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string FileName = "img-" + dateTime + ".";
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;

            switch (fileFormat)
            {
                case FileFormat.Jpeg:
                    FileName += "jpeg";
                    BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                    break;
                case FileFormat.Png:
                    FileName += "png";
                    BitmapEncoderGuid = BitmapEncoder.PngEncoderId;
                    break;
                case FileFormat.Bmp:
                    FileName += "bmp";
                    BitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
                    break;
                case FileFormat.Tiff:
                    FileName += "tiff";
                    BitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
                    break;
                case FileFormat.Gif:
                    FileName += "gif";
                    BitmapEncoderGuid = BitmapEncoder.GifEncoderId;
                    break;
            }

            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(FileName, CreationCollisionOption.GenerateUniqueName);

            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream);

                System.IO.Stream pixelStream = WB.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                     BitmapAlphaMode.Ignore,
                                     (uint)WB.PixelWidth,
                                     (uint)WB.PixelHeight,
                                     96.0,
                                     96.0,
                                     pixels);

                await encoder.FlushAsync();
            }
            return file;
        }

        private enum FileFormat
        {
            Jpeg,
            Png,
            Bmp,
            Tiff,
            Gif
        }

    }
}
