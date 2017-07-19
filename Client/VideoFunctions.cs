using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Matrix;
using Windows.UI.Core;

namespace Client
{
    public sealed partial class MainPage
    {
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
                                    if (framesLeftToSkip > 0)
                                    {
                                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                        {
                                            textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Обнаружен штрих-код! Кадр " + framesLeftToSkip + " пропущен. \r\n";
                                            ScrolltoBottom(textBox);
                                        });

                                        framesLeftToSkip--;
                                    }
                                    else
                                    {
                                        latchActive = false;

                                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                        {
                                            textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Обнаружен штрих-код! Кадр " + framesLeftToSkip + " пропущен. \r\n";
                                            ScrolltoBottom(textBox);
                                        });
                                    }
                                }
                                else
                                {
                                    if (barcodeDetectedOld == true)
                                    {

                                    }
                                    else
                                    {
                                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                        {
                                            textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Обнаружен штрих-код! \r\n";
                                            ScrolltoBottom(textBox);
                                        });

                                        latchActive = true;
                                        framesLeftToSkip = 10;

                                        //Подготовка к отправке кадра на сервер
                                        Jid jid = new Jid(xmppClient.Username,
                                                          xmppClient.XmppDomain,
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
                                        await fm.Send(jid,
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

                Stream pixelStream = WB.PixelBuffer.AsStream();
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
