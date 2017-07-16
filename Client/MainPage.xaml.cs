using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Matrix.Xmpp.Client;
using Matrix.Xmpp.Sasl;

namespace Client
{
    public sealed partial class MainPage : Page
    {
        bool latchActive = false;
        short framesLeftToSkip = 0;
        bool barcodeDetectedOld = false;
        static string workMode = "INC";

        FileTransferManager fm = new FileTransferManager();
        XmppClient xmppClient = new XmppClient();

        DispatcherTimer dispatcherTimer;

        MediaCapture mediaCapture;

        public MainPage()
        {
            InitializeComponent();

            xmppClient.Resource = "client";
            xmppClient.Port = 5222;
            xmppClient.StartTls = true;
            xmppClient.OnLogin += new EventHandler<Matrix.EventArgs>(xmppClient_OnLogin);
            xmppClient.OnAuthError += new EventHandler<SaslEventArgs>(xmppClient_OnAuthError);
            xmppClient.OnClose += new EventHandler<Matrix.EventArgs>(xmppClient_OnClose);

            fm.XmppClient = xmppClient;
            fm.Blocking = true;
            fm.OnDeny += fm_OnDeny;
            fm.OnAbort += fm_OnAbort;
            fm.OnError += fm_OnError;
            fm.OnEnd += fm_OnEnd;
            fm.OnStart += fm_OnStart;
            fm.OnProgress += fm_OnProgress;
        }

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

                        xmppClient.SetXmppDomain(xmppConfig[0]);
                        xmppClient.SetUsername(xmppConfig[1]);
                        xmppClient.Password = xmppConfig[2];
                    }
                }
            }

            //Лицензия для библиотеки Matrix XMPP
            string lic = @"";
            Matrix.License.LicenseManager.SetLicense(lic);

            //Соединение с сервером XMPP
            xmppClient.Open();

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

        private async void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //Остановка обработчика кадров
            dispatcherTimer.Stop();

            //Остановка захвата кадров
            await mediaCapture.StopPreviewAsync();

            //Остановка работы XMPP
            xmppClient.Close();
        }
    }
}
