using System.Windows;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für QuerySelectionWindow.xaml
    /// </summary>
    public partial class QuerySelectionWindow : Window
    {

        public object GetSelectedItem => comboBoxSelection.SelectedItem;

        public QuerySelectionWindow()
        {
            InitializeComponent();
        }

        public void SetTitle(string title)
        {
            Title = title;
        }

        public void SetComboBox(object[] comboBoxContent)
        {
            comboBoxSelection.Items.Clear();
            foreach (var s in comboBoxContent)
            {
                comboBoxSelection.Items.Add(s);
            }
            comboBoxSelection.SelectedIndex = 0;
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
