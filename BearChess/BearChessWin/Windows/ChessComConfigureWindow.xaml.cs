using System;
using System.Data.Entity;
using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication.ChessCom;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

namespace www.SoLaNoSoft.com.BearChessWin
{
    /// <summary>
    /// Interaktionslogik für ChessComConfigureWindow.xaml
    /// </summary>
    public partial class ChessComConfigureWindow : Window
    {
        private readonly Configuration _configuration;
        public ChessComConfigureWindow(Configuration configuration)
        {
            InitializeComponent();
            _configuration = configuration;
            textBlockUserName.Text = _configuration.GetConfigValue("chessComUserName", string.Empty);
        }

        private async void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
          
            _configuration.SetConfigValue("chessComUserName", textBlockUserName.Text);
            DialogResult = true;
        }

        private void ButtonCheck_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                buttonCancel.IsEnabled = false;
                buttonOk.IsEnabled = false;
                var profileResponse = ChessComReader.GetProfile(textBlockUserName.Text);
                textBlockProfile.Text = $"Username: {profileResponse.Username}\n" +
                                        $"Name: {profileResponse.Name}\n" +
                                        $"Title: {profileResponse.Title}\n" +
                                        $"Location: {profileResponse.Location}\n" +
                                        $"Status: {profileResponse.Status}";
            }
            catch (Exception ex)
            {
                BearChessMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                buttonCancel.IsEnabled = true;
                buttonOk.IsEnabled = true;
            }
        }
    }
}
