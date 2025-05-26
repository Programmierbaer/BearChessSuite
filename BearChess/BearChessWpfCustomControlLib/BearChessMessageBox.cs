using System.Windows;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChessWpfCustomControlLib
{
    public static class BearChessMessageBox
    {
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            if (Configuration.Instance.GetBoolValue("blindUser", false))
            {
                return Say(messageBoxText, caption, button, icon, MessageBoxResult.None);
            }

            var win = new BearChessMessageBoxWindow(messageBoxText, caption, button, icon);
            win.ShowDialog();
            return win.GetResult();

        }
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            if (Configuration.Instance.GetBoolValue("blindUser", false))
            {
                return Say(messageBoxText, caption, button, icon, defaultResult);
            }

            var win = new BearChessMessageBoxWindow(messageBoxText, caption, button, icon,defaultResult);
            win.ShowDialog();
            return win.GetResult();

        }

        private static MessageBoxResult Say(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            if (button == MessageBoxButton.OK)
            {
                var synthesizer = BearChessSpeech.Instance;
                synthesizer.SpeakAsync(caption);
                synthesizer.SpeakAsync(messageBoxText);
                return MessageBoxResult.OK;
            }
            var queryWindow = new QueryDialogWindow(messageBoxText)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            queryWindow.ShowDialog();
            if (queryWindow.QueryResult.Yes)
            {
                return MessageBoxResult.Yes;
            }

            if (queryWindow.QueryResult.Cancel)
            {
                return MessageBoxResult.Cancel;
            }

            return MessageBoxResult.No;
        }
    }
}
