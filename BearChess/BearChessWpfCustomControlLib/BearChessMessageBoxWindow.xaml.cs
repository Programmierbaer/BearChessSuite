using System.Windows;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    /// <summary>
    /// Interaktionslogik für BearChessMessageBoxWindow.xaml
    /// </summary>
    public partial class BearChessMessageBoxWindow : Window
    {

        private MessageBoxResult _result;
        private readonly string _messageBoxText;
        private readonly string _caption;
        private readonly MessageBoxButton _button;
        private readonly MessageBoxImage _icon;
        private readonly MessageBoxResult _defaultResult = MessageBoxResult.None;

        public BearChessMessageBoxWindow()
        {
            InitializeComponent();
        }

        public BearChessMessageBoxWindow(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) : this()
        {
            _messageBoxText = messageBoxText;
            _caption = caption;
            _button = button;
            _icon = icon;
        }

        public BearChessMessageBoxWindow(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult) : this(messageBoxText, caption, button, icon)
        {
            _defaultResult = defaultResult;

        }

        public MessageBoxResult GetResult()
        {
            return _result;
        }


        private void BearChessMessageBoxWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
           
            if (_defaultResult != MessageBoxResult.None)
            {
                _result = MessageBox.Show(_messageBoxText, _caption, _button, _icon, _defaultResult);
            }
            else
            {
                _result = MessageBox.Show(_messageBoxText, _caption, _button, _icon);
            }

            Close();
        }
    }
}
