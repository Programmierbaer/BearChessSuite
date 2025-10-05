using System;
using System.Resources;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für BCServerConfigureWindow.xaml
    /// </summary>
    public partial class BCServerConfigureWindow : Window
    {
        private readonly Configuration _configuration;
        private readonly ResourceManager _rm;

        public BCServerConfigureWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            _rm = SpeechTranslator.ResourceManager;
            textBlockUserNameBCS.Text = _configuration.GetConfigValue("BCUserName", _configuration.GetConfigValue("player",""));
            textBlockServerBCS.Text = _configuration.GetConfigValue("BCServerHostname", "localhost");
            textBlockPortBCS.Text = _configuration.GetConfigValue("BCServerPortnumber", "8888");
            checkBoxUseBCSforFTP.IsChecked = _configuration.GetBoolValue("BCSforFTP", false);
            groupBoxFTP.IsEnabled = !configuration.GetBoolValue("BCSforFTP", false);
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
            if (!string.IsNullOrWhiteSpace(textBlockPortBCS.Text) && !int.TryParse(textBlockPortBCS.Text, out _))
            {
                MessageBox.Show(_rm.GetString("PortMustBeANumber"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }
            if (!string.IsNullOrWhiteSpace(textBlockPort.Text) && !int.TryParse(textBlockPort.Text, out _))
            {
                MessageBox.Show(_rm.GetString("PortMustBeANumber"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            _configuration.SetConfigValue("BCUserName", textBlockUserNameBCS.Text);
            _configuration.SetConfigValue("BCServerHostname", textBlockServerBCS.Text);
            _configuration.SetConfigValue("BCServerPortnumber", textBlockPortBCS.Text);
            _configuration.SetBoolValue("BCSforFTP", checkBoxUseBCSforFTP.IsChecked.HasValue && checkBoxUseBCSforFTP.IsChecked.Value);
            _configuration.SetConfigValue("publishUserName", textBlockUserName.Text);
            _configuration.SetConfigValue("publishServer", textBlockServer.Text);
            _configuration.SetConfigValue("publishPort", textBlockPort.Text);
            _configuration.SetSecureConfigValue("publishPassword", textBlockPassword.Password);
            _configuration.SetConfigValue("publishPath", textBlockPath.Text);
            _configuration.SetConfigValue("publishFileName", textBlockFileName.Text);
            _configuration.SetBoolValue("publishSFTP", checkBoxSFTP.IsChecked.HasValue && checkBoxSFTP.IsChecked.Value);
            DialogResult = true;
        }

        private void CheckBoxUseBCSforFTP_OnChecked(object sender, RoutedEventArgs e)
        {
            groupBoxFTP.IsEnabled = false;
        }

        private void CheckBoxUseBCSforFTP_OnUnchecked(object sender, RoutedEventArgs e)
        {
            groupBoxFTP.IsEnabled = true;
        }
    }
}
