using System;
using System.Collections.Generic;
using System.Linq;
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
using www.SoLaNoSoft.com.BearChess.BearChessCommunication.ChessCom;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für ChessComDownloadWindow.xaml
    /// </summary>
    public partial class ChessComDownloadWindow : Window
    {
        public string Username => textBoxUserName.Text.Trim();
        public int Year => (int)numericYear.Value;
        public int MonthFrom => (int)numericMonthFrom.Value;
        public int MonthTo => (int)numericMonthTo.Value;

        public ChessComDownloadWindow()
        {
            InitializeComponent();
            numericYear.Value = DateTime.Now.Year;
            numericMonthFrom.Value = DateTime.Now.Month;
            numericMonthTo.Value = DateTime.Now.Month;
            textBoxUserName.Text = Configuration.Instance.GetConfigValue("chessComDownloadUserName", Configuration.Instance.GetConfigValue("chessComUserName", string.Empty));
        }

        private void ButtonCheck_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                buttonCancel.IsEnabled = false;
                buttonOk.IsEnabled = false;
                var profileResponse = ChessComReader.GetProfile(Username);
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

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            Configuration.Instance.SetConfigValue("chessComDownloadUserName", Username);
            DialogResult = true;
        }

        private void NumericMonthFrom_OnValueChanged(object sender, int e)
        {
            if (e > numericMonthTo.Value)
            {
                numericMonthTo.Value = e;
            }
        }

        private void NumericMonthTo_OnValueChanged(object sender, int e)
        {
            if (e < numericMonthFrom.Value)
            {
                numericMonthFrom.Value = e;
            }
        }
    }
}
