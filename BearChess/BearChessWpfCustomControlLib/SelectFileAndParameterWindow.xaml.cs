using Microsoft.Win32;
using System.Windows;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für SelectFileAndParameterWindow.xaml
    /// </summary>
    public partial class SelectFileAndParameterWindow : Window
    {

        public string Filename => textBoxFilename.Text;
        public string Parameter => textBoxParameter.Text;
        public string EngineName => textBoxName.Text;

        public SelectFileAndParameterWindow()
        {
            InitializeComponent();
        }

        private void ButtonEngine_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "UCI Engine|*.exe|UCI Engine|*.cmd|All Files|*.*" };
            var showDialog = openFileDialog.ShowDialog(this);
            if (showDialog.Value && !string.IsNullOrWhiteSpace(openFileDialog.FileName))
            {
                textBoxFilename.Text = openFileDialog.FileName;
            }
        }

        private void ButtonCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonOk_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
