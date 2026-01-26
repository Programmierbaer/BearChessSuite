using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für LichessPuzzleInfoWndow.xaml
    /// </summary>
    public partial class LichessPuzzleInfoWindow : Window
    {
        public LichessPuzzleInfoWindow()
        {
            InitializeComponent();
            bool english = Thread.CurrentThread.CurrentUICulture.Name.Contains("en");
            TextBlockEN1.Visibility = !english ? Visibility.Hidden : Visibility.Visible;
            TextBlockEN2.Visibility = !english ? Visibility.Hidden : Visibility.Visible;
            TextBlockEN4.Visibility = !english ? Visibility.Hidden : Visibility.Visible;
            TextBlockEN5.Visibility = !english ? Visibility.Hidden : Visibility.Visible;
            TextBlockGerman1.Visibility = english ? Visibility.Hidden : Visibility.Visible;
            TextBlockGerman2.Visibility = english ? Visibility.Hidden : Visibility.Visible;
            TextBlockGerman4.Visibility = english ? Visibility.Hidden : Visibility.Visible;
            TextBlockGerman5.Visibility = english ? Visibility.Hidden : Visibility.Visible;
        }

        private void HyperlinkUrl_OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
