using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Implementations.pgn;
using www.SoLaNoSoft.com.BearChessTools;

namespace www.SoLaNoSoft.com.BearChessServerWin.Windows
{
    /// <summary>
    /// Interaktionslogik für ConfigureServerWindow.xaml
    /// </summary>
    public partial class ConfigureServerWindow : Window
    {
    
        public int PortNumberBCServer { get; private set; }
        public int PortNumberWebServer { get; private set; }
        public string ServerName => textBlockServer.Text;

        private readonly ResourceManager _rm;
        public ConfigureServerWindow()
        {
            InitializeComponent();
            PortNumberBCServer =  Configuration.Instance.GetIntValue("BCServerPortnumber", 8888);
            PortNumberWebServer =  Configuration.Instance.GetIntValue("WebServerPortnumber", 8080);
            textBlockPortBCServer.Text = PortNumberBCServer.ToString();
            textBlockPortWebServer.Text = PortNumberWebServer.ToString();
            textBlockServer.Text = Configuration.Instance.GetConfigValue("BCServerName", "localhost");
            _rm = SpeechTranslator.ResourceManager;
            var configValueLanguage = Configuration.Instance.GetConfigValue("Language", "default");
            if (configValueLanguage.Equals("default"))
            {
                radioButtonGlob.IsChecked = true;
            }
            if (configValueLanguage.Equals("en"))
            {
                radioButtonGB.IsChecked = true;
            }
            if (configValueLanguage.Equals("de"))
            {
                radioButtonDE.IsChecked = true;
            }
            textBlockPath.Text = $"http://{textBlockServer.Text}:{textBlockPortWebServer.Text}/chess/bearchess";
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            int portNumber = 0;
            if (!string.IsNullOrWhiteSpace(textBlockPortBCServer.Text) && !int.TryParse(textBlockPortBCServer.Text,out  portNumber ))
            {
                MessageBox.Show(_rm.GetString("PortMustBeANumber"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                                MessageBoxImage.Error);
                
               
                return;
            }
            PortNumberBCServer = portNumber;
            if (!string.IsNullOrWhiteSpace(textBlockPortWebServer.Text) && !int.TryParse(textBlockPortWebServer.Text, out  portNumber))
            {
                MessageBox.Show(_rm.GetString("PortMustBeANumber"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            PortNumberWebServer = portNumber;
            if (PortNumberWebServer==PortNumberBCServer)
            {
                MessageBox.Show(_rm.GetString("PortsAreEqual"), _rm.GetString("InvalidParameter"), MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            Configuration.Instance.SetIntValue("BCServerPortnumber", PortNumberBCServer);
            Configuration.Instance.SetIntValue("WebServerPortnumber", PortNumberWebServer);
            Configuration.Instance.SetConfigValue("BCServerName", ServerName);
            if (radioButtonGlob.IsChecked.HasValue && radioButtonGlob.IsChecked.Value)
            {
                Configuration.Instance.SetConfigValue("Language", "default");
            }

            if (radioButtonDE.IsChecked.HasValue && radioButtonDE.IsChecked.Value)
            {
                Configuration.Instance.SetConfigValue("Language", "de");
               
            }

            if (radioButtonGB.IsChecked.HasValue && radioButtonGB.IsChecked.Value)
            {
                Configuration.Instance.SetConfigValue("Language", "en");
            }

            DialogResult = true;
        }

        private void textBlockServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBlockServer!=null && textBlockPortWebServer!=null && textBlockPath!=null)
            textBlockPath.Text = $"http://{textBlockServer.Text}:{textBlockPortWebServer.Text}/chess/bearchess";
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            ClipboardHelper.SetText(textBlockPath.Text);
        }
    }
}
