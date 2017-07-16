using Matrix;
using Matrix.Xmpp.Client;
using Matrix.Xmpp.Sasl;
using Windows.UI.Core;
using System;

namespace Client
{
    public sealed partial class MainPage
    {
        private async void xmppClient_OnLogin(object sender, Matrix.EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Соединение с сервером XMPP установлено! \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void xmppClient_OnAuthError(object sender, SaslEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Соединение с сервером XMPP не удалось! \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void xmppClient_OnClose(object sender, Matrix.EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Соединение с сервером XMPP разорвано! \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void fm_OnDeny(object sender, FileTransferEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Отправка файла отклонена! \r\n";
            });
        }

        private async void fm_OnStart(object sender, FileTransferEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Отправка файла начата! \r\n";
            });
        }

        private async void fm_OnProgress(object sender, FileTransferEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Ход отправки файла: " + e.BytesTransmitted + "/" + e.FileSize + " байт \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void fm_OnAbort(object sender, FileTransferEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Отправка файла прервана! \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void fm_OnError(object sender, ExceptionEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Ошибка отправки файла! \r\n";
                ScrolltoBottom(textBox);
            });
        }

        private async void fm_OnEnd(object sender, FileTransferEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                textBox.Text += DateTime.Now.ToString("HH:mm:ss") + " Отправка файла завершена! \r\n";
                ScrolltoBottom(textBox);
            });
        }
    }
}
