using System;
using System.Resources;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für PublishConfigureWindow.xaml
    /// </summary>
    public partial class PublishConfigureWindow : Window
    {
        private readonly Configuration _configuration;
        private readonly ResourceManager _rm;
        public PublishConfigureWindow(Configuration configuration)
        {
            _configuration = configuration;
            InitializeComponent();
            _rm = SpeechTranslator.ResourceManager;
            textBlockUserName.Text = _configuration.GetConfigValue("publishUserName", string.Empty);
            textBlockPassword.Password = _configuration.GetSecureConfigValue("publishPassword", string.Empty);
            textBlockServer.Text = _configuration.GetConfigValue("publishServer", "ftp.server.com");
            textBlockPort.Text = _configuration.GetConfigValue("publishPort", "21");
            textBlockPath.Text = _configuration.GetConfigValue("publishPath", ".");
            textBlockFileName.Text = _configuration.GetConfigValue("publishFileName", "games.pgn");
            checkBoxSFTP.IsChecked = _configuration.GetBoolValue("publishSFTP", false);
        }
        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(textBlockPort.Text) && !int.TryParse(textBlockPort.Text, out _))
            {
                MessageBox.Show(_rm.GetString("PortMustBeANumber"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(textBlockServer.Text) || string.IsNullOrWhiteSpace(textBlockUserName.Text)
                                                                || string.IsNullOrWhiteSpace(textBlockPassword.Password)
                                                                || string.IsNullOrWhiteSpace(textBlockPort.Text))
            {
                var messageBoxResult =
                    MessageBox.Show(
                        $"{_rm.GetString("NotFilledAllFields")}{Environment.NewLine}{_rm.GetString("SaveEntriesAnyWay")}",
                        _rm.GetString("MissingParameter"), MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (messageBoxResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            _configuration.SetConfigValue("publishUserName", textBlockUserName.Text);
            _configuration.SetConfigValue("publishServer", textBlockServer.Text);
            _configuration.SetConfigValue("publishPort", textBlockPort.Text);
            _configuration.SetSecureConfigValue("publishPassword", textBlockPassword.Password);
            _configuration.SetConfigValue("publishPath", textBlockPath.Text);
            _configuration.SetConfigValue("publishFileName", textBlockFileName.Text);
            _configuration.SetBoolValue("publishSFTP", checkBoxSFTP.IsChecked.HasValue && checkBoxSFTP.IsChecked.Value);
            DialogResult = true;
        }

        private void buttonCheck_Click(object sender, RoutedEventArgs e)
        {
           var ftpClient = FtpClientFactory.GetFtpClient(textBlockUserName.Text, textBlockPassword.Password, textBlockServer.Text, textBlockPort.Text, checkBoxSFTP.IsChecked.HasValue && checkBoxSFTP.IsChecked.Value, null);
            if (string.IsNullOrWhiteSpace(ftpClient.ErrorMessage))
            {
                if (ftpClient.Connect())
                {
                    ftpClient.Disconnect();
                }
             
            }
            if(!string.IsNullOrWhiteSpace(ftpClient.ErrorMessage))
            {
                BearChessMessageBox.Show(ftpClient.ErrorMessage, _rm.GetString("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                BearChessMessageBox.Show(_rm.GetString("Connected"), _rm.GetString("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

}
