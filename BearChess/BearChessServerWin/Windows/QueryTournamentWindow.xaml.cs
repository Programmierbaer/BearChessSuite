using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
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
using www.SoLaNoSoft.com.BearChessWpfCustomControlLib;

namespace www.SoLaNoSoft.com.BearChessServerWin.Windows
{
    /// <summary>
    /// Interaktionslogik für QueryTournamentWindow.xaml
    /// </summary>
    public partial class QueryTournamentWindow : Window
    {

        public string TournamentName => textBoxName.Text;
        public int BoardsCount => numericUpDownUserControBoards.Value;

        public bool PublishTournament => checkBoxPublish.IsChecked.HasValue && checkBoxPublish.IsChecked.Value;

        private readonly ResourceManager _rm;

        public QueryTournamentWindow()
        {
            InitializeComponent();
            _rm = Properties.Resources.ResourceManager;
            _rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, false, true);
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxName.Text.Trim()))
            {
                DialogResult = true;
            }
            else
            {
                BearChessMessageBox.Show(_rm.GetString("TournamentNameMissing"), _rm.GetString("MissingParameter"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBoxName.Focus();
        }

      
    }
}
