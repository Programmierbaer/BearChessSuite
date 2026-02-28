using System;
using System.Windows;
using www.SoLaNoSoft.com.BearChess.BearChessCommunication.lichess;
using www.SoLaNoSoft.com.BearChessBase;
using www.SoLaNoSoft.com.BearChessBase.Interfaces;

namespace www.SoLaNoSoft.com.BearChessWin;

public partial class LichessConfigureWindow : Window
{
    private readonly ILogging _logger;

    private readonly LichessApiClient  _liChessClient;
    public LichessConfigureWindow(ILogging logger)
    {
        _logger = logger;
        InitializeComponent();
        _liChessClient = new LichessApiClient(logger);
        textBlockToken.Text = Configuration.Instance.GetSecureConfigValue("lichessToken", string.Empty);
    }

    private async void ButtonCheck_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await _liChessClient.SetToken(textBlockToken.Text);
            var userApi = new UserApi(_liChessClient,_logger);
            var myProfile = await userApi.GetMyProfile();
            textBlockProfile.Text = $"ID: {myProfile.Id}{Environment.NewLine}Username: {myProfile.Username}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Invalid", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
    {
        Configuration.Instance.SetSecureConfigValue("lichessToken", textBlockToken.Text);
        DialogResult = true;
    }
}